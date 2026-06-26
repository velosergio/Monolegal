import { fireEvent, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AdminToolsSection } from '@/features/settings/components/AdminToolsSection'
import { renderWithQuery } from '../../test-utils'

interface StubResponses {
  resendFailed?: { attempted: number; resent: number; failed: number }
  sanitize?: { sanitized: number }
}

function stubFetch(responses: StubResponses) {
  const calls: string[] = []
  const fetchMock = vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    calls.push(url)
    const body = url.endsWith('/resend-failed')
      ? (responses.resendFailed ?? { attempted: 0, resent: 0, failed: 0 })
      : (responses.sanitize ?? { sanitized: 0 })
    return Promise.resolve({
      ok: true,
      status: 200,
      json: () => Promise.resolve(body),
    } as Response)
  })
  vi.stubGlobal('fetch', fetchMock)
  return { calls }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('AdminToolsSection', () => {
  it('reenvía fallidas y muestra toast con conteos', async () => {
    stubFetch({ resendFailed: { attempted: 3, resent: 2, failed: 1 } })
    renderWithQuery(<AdminToolsSection />)

    fireEvent.click(screen.getByRole('button', { name: /reenviar fallidas/i }))

    expect(
      await screen.findByText('Reenvío completado: 2 reenviadas, 1 fallidas (de 3).')
    ).toBeInTheDocument()
  })

  it('informa cuando no hay fallidas que reenviar', async () => {
    stubFetch({ resendFailed: { attempted: 0, resent: 0, failed: 0 } })
    renderWithQuery(<AdminToolsSection />)

    fireEvent.click(screen.getByRole('button', { name: /reenviar fallidas/i }))

    expect(
      await screen.findByText('No hay notificaciones fallidas que reenviar.')
    ).toBeInTheDocument()
  })

  it('no sanea hasta confirmar en el diálogo', async () => {
    const { calls } = stubFetch({ sanitize: { sanitized: 4 } })
    renderWithQuery(<AdminToolsSection />)

    // Abre el diálogo; aún no debe haber llamada de red.
    fireEvent.click(screen.getByRole('button', { name: /sanear atascadas/i }))
    expect(calls.some((u) => u.endsWith('/sanitize'))).toBe(false)

    // Confirma en el diálogo.
    const confirm = await screen.findByRole('button', { name: /^sanear$/i })
    fireEvent.click(confirm)

    expect(
      await screen.findByText('Saneamiento completado: 4 notificaciones marcadas.')
    ).toBeInTheDocument()
    expect(calls.some((u) => u.endsWith('/sanitize'))).toBe(true)
  })
})
