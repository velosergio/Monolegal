import { fireEvent, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { TestEmailSection } from '@/features/settings/components/TestEmailSection'
import { renderWithQuery } from '../../test-utils'

function stubFetch(result: 'sent' | 'failed', message: string | null = null) {
  const calls: { url: string; method: string; body: unknown }[] = []
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({
      url: String(input),
      method: init?.method ?? 'GET',
      body: init?.body ? JSON.parse(init.body as string) : undefined,
    })
    return Promise.resolve({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ to: 'prueba@dominio.com', result, message }),
    } as Response)
  })
  vi.stubGlobal('fetch', fetchMock)
  return { calls }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('TestEmailSection', () => {
  it('impide enviar con un correo de destino inválido (sin llamada de red)', async () => {
    const { calls } = stubFetch('sent')
    renderWithQuery(<TestEmailSection />)

    fireEvent.change(screen.getByLabelText('Correo de destino'), { target: { value: 'invalido' } })
    fireEvent.click(screen.getByRole('button', { name: /enviar prueba/i }))

    expect(await screen.findByText('Introduce un correo de destino válido.')).toBeInTheDocument()
    expect(calls.length).toBe(0)
  })

  it('muestra toast de éxito cuando el envío resulta "sent"', async () => {
    stubFetch('sent')
    renderWithQuery(<TestEmailSection />)

    fireEvent.change(screen.getByLabelText('Correo de destino'), {
      target: { value: 'prueba@dominio.com' },
    })
    fireEvent.click(screen.getByRole('button', { name: /enviar prueba/i }))

    expect(
      await screen.findByText('Correo de prueba enviado a prueba@dominio.com.')
    ).toBeInTheDocument()
  })

  it('muestra toast de error con el motivo cuando el envío resulta "failed"', async () => {
    stubFetch('failed', 'Credencial inválida (401).')
    renderWithQuery(<TestEmailSection />)

    fireEvent.change(screen.getByLabelText('Correo de destino'), {
      target: { value: 'prueba@dominio.com' },
    })
    fireEvent.click(screen.getByRole('button', { name: /enviar prueba/i }))

    expect(await screen.findByText('Credencial inválida (401).')).toBeInTheDocument()
  })
})
