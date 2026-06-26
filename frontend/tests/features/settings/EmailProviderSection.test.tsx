import { fireEvent, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { EmailProviderSection } from '@/features/settings/components/EmailProviderSection'
import type { EmailSettings } from '@/features/settings/types'
import { renderWithQuery } from '../../test-utils'

const settings: EmailSettings = {
  activeProvider: 'smtp',
  fromAddress: 'no-reply@monolegal.co',
  fromName: 'Monolegal',
  smtp: { host: 'smtp.example.com', port: 587, username: 'user', useStartTls: true },
  resend: { fromDomain: null },
  credentialStatus: 'configured',
}

interface FetchCall {
  url: string
  method: string
  body: unknown
}

function stubFetch(options?: { validateStatus?: string }) {
  const calls: FetchCall[] = []
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    const body = init?.body ? JSON.parse(init.body as string) : undefined
    calls.push({ url, method, body })

    if (url.endsWith('/api/settings/email') && method === 'GET') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve(settings),
      } as Response)
    }
    if (url.endsWith('/validate') && method === 'POST') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: () =>
          Promise.resolve({
            provider: 'smtp',
            status: options?.validateStatus ?? 'validated',
            message: null,
          }),
      } as Response)
    }
    if (url.endsWith('/api/settings/email') && method === 'PUT') {
      return Promise.resolve({ ok: true, status: 204, json: () => Promise.resolve({}) } as Response)
    }
    return Promise.resolve({ ok: false, status: 404, json: () => Promise.resolve({}) } as Response)
  })
  vi.stubGlobal('fetch', fetchMock)
  return { fetchMock, calls }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('EmailProviderSection', () => {
  it('muestra la configuración cargada y el estado de la credencial', async () => {
    stubFetch()
    renderWithQuery(<EmailProviderSection />)

    expect(await screen.findByLabelText('Correo remitente')).toHaveValue('no-reply@monolegal.co')
    expect(screen.getByText('Configurada')).toBeInTheDocument()
  })

  it('valida la credencial y muestra un toast de éxito', async () => {
    stubFetch({ validateStatus: 'validated' })
    renderWithQuery(<EmailProviderSection />)

    fireEvent.click(await screen.findByRole('button', { name: /validar credencial/i }))

    expect(await screen.findByText('Credencial validada correctamente.')).toBeInTheDocument()
  })

  it('rechaza guardar con un correo remitente inválido (validación cliente)', async () => {
    const { calls } = stubFetch()
    renderWithQuery(<EmailProviderSection />)

    const input = await screen.findByLabelText('Correo remitente')
    fireEvent.change(input, { target: { value: 'no-es-correo' } })
    fireEvent.click(screen.getByRole('button', { name: /guardar/i }))

    expect(await screen.findByText('Introduce un correo remitente válido.')).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'PUT')).toBe(false)
  })
})
