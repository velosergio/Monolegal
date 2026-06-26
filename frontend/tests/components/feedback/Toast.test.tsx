import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ToastProvider } from '@/components/feedback/ToastProvider'
import { ToastViewport } from '@/components/feedback/ToastViewport'
import { useToast } from '@/components/feedback/useToast'

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

function Harness({ children }: { children?: ReactNode }) {
  return (
    <ToastProvider>
      {children}
      <ToastViewport />
    </ToastProvider>
  )
}

function SuccessTrigger() {
  const toast = useToast()
  return (
    <button type="button" onClick={() => toast.success('Estado actualizado')}>
      lanzar éxito
    </button>
  )
}

function ErrorTrigger() {
  const toast = useToast()
  return (
    <button type="button" onClick={() => toast.error('Algo falló')}>
      lanzar error
    </button>
  )
}

describe('Sistema de toast', () => {
  it('muestra un toast de éxito en una región aria-live polite', async () => {
    const user = userEvent.setup()
    render(
      <Harness>
        <SuccessTrigger />
      </Harness>
    )

    await user.click(screen.getByRole('button', { name: /lanzar éxito/i }))

    const toast = await screen.findByText('Estado actualizado')
    expect(toast).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Estado actualizado')
  })

  it('el toast de error usa role alert (assertive) y persiste', async () => {
    vi.useFakeTimers()
    render(
      <Harness>
        <ErrorTrigger />
      </Harness>
    )

    // Disparo directo para no depender de timers de userEvent.
    await act(async () => {
      screen.getByRole('button', { name: /lanzar error/i }).click()
    })

    expect(screen.getByRole('alert')).toHaveTextContent('Algo falló')

    // Tras el tiempo de auto-cierre del éxito, el error sigue presente.
    await act(async () => {
      vi.advanceTimersByTime(8000)
    })
    expect(screen.getByText('Algo falló')).toBeInTheDocument()
  })

  it('el toast de éxito se cierra solo tras el tiempo de espera', async () => {
    vi.useFakeTimers()
    render(
      <Harness>
        <SuccessTrigger />
      </Harness>
    )

    await act(async () => {
      screen.getByRole('button', { name: /lanzar éxito/i }).click()
    })
    expect(screen.getByText('Estado actualizado')).toBeInTheDocument()

    await act(async () => {
      vi.advanceTimersByTime(4500)
    })
    expect(screen.queryByText('Estado actualizado')).not.toBeInTheDocument()
  })

  it('se puede cerrar manualmente con el botón de cierre', async () => {
    const user = userEvent.setup()
    render(
      <Harness>
        <ErrorTrigger />
      </Harness>
    )

    await user.click(screen.getByRole('button', { name: /lanzar error/i }))
    expect(screen.getByText('Algo falló')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /cerrar notificación/i }))
    expect(screen.queryByText('Algo falló')).not.toBeInTheDocument()
  })

  it('useToast lanza si se usa fuera de ToastProvider', () => {
    function Orphan() {
      useToast()
      return null
    }
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    expect(() => render(<Orphan />)).toThrow(/ToastProvider/)
    spy.mockRestore()
  })
})
