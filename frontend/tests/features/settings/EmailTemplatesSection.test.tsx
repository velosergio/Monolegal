import { fireEvent, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { EmailTemplatesSection } from '@/features/settings/components/EmailTemplatesSection'
import type { EmailTemplatesResponse } from '@/features/settings/types'
import { renderWithQuery } from '../../test-utils'

const templates: EmailTemplatesResponse = {
  allowedVariables: ['factura.id', 'factura.monto', 'cliente.nombre'],
  templates: [
    {
      type: 'reminder',
      subject: 'Recordatorio {{factura.id}}',
      body: 'Hola {{cliente.nombre}}',
      isCustomized: true,
    },
    { type: 'paymentconfirmation', subject: 'Pago', body: 'Gracias', isCustomized: false },
    { type: 'deactivationnotice', subject: 'Aviso', body: 'Desactivada', isCustomized: false },
  ],
}

interface FetchCall {
  url: string
  method: string
}

function stubFetch() {
  const calls: FetchCall[] = []
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    const method = init?.method ?? 'GET'
    calls.push({ url, method })

    if (url.endsWith('/api/settings/email/templates') && method === 'GET') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve(templates),
      } as Response)
    }
    if (url.endsWith('/preview') && method === 'POST') {
      return Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ subject: 'Recordatorio F-1', body: 'Hola Ana' }),
      } as Response)
    }
    // PUT update / POST reset
    return Promise.resolve({ ok: true, status: 204, json: () => Promise.resolve({}) } as Response)
  })
  vi.stubGlobal('fetch', fetchMock)
  return { calls }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('EmailTemplatesSection', () => {
  it('muestra la plantilla cargada y su estado', async () => {
    stubFetch()
    renderWithQuery(<EmailTemplatesSection />)

    expect(await screen.findByLabelText('Asunto')).toHaveValue('Recordatorio {{factura.id}}')
    expect(screen.getByText('Personalizada')).toBeInTheDocument()
  })

  it('rechaza guardar con asunto vacío', async () => {
    const { calls } = stubFetch()
    renderWithQuery(<EmailTemplatesSection />)

    const subject = await screen.findByLabelText('Asunto')
    fireEvent.change(subject, { target: { value: '' } })
    fireEvent.click(screen.getByRole('button', { name: /^guardar$/i }))

    expect(await screen.findByText('El asunto es obligatorio.')).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'PUT')).toBe(false)
  })

  it('rechaza guardar con una variable no admitida', async () => {
    const { calls } = stubFetch()
    renderWithQuery(<EmailTemplatesSection />)

    const body = await screen.findByLabelText('Cuerpo')
    fireEvent.change(body, { target: { value: 'Hola {{cliente.desconocido}}' } })
    fireEvent.click(screen.getByRole('button', { name: /^guardar$/i }))

    expect(await screen.findByText(/variable no admitida/i)).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'PUT')).toBe(false)
  })

  it('guarda una plantilla válida y muestra toast de éxito', async () => {
    const { calls } = stubFetch()
    renderWithQuery(<EmailTemplatesSection />)

    await screen.findByLabelText('Asunto')
    fireEvent.click(screen.getByRole('button', { name: /^guardar$/i }))

    expect(await screen.findByText('Plantilla guardada.')).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'PUT' && c.url.endsWith('/templates/reminder'))).toBe(
      true
    )
  })
})
