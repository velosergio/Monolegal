import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { ToastProvider } from '@/components/feedback/ToastProvider'
import { ToastViewport } from '@/components/feedback/ToastViewport'
import { useToast } from '@/components/feedback/useToast'
import { AppShell } from '@/components/layout/AppShell'
import { ChangeStatusControl } from '@/features/invoices/components/ChangeStatusControl'
import { mockFetchJson, renderWithQuery } from '../test-utils'

function renderShell(children: React.ReactNode, route = '/facturas') {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <AppShell>{children}</AppShell>
    </MemoryRouter>
  )
}

describe('Navegación por teclado', () => {
  it('el botón de menú es enfocable y tiene nombre accesible', () => {
    renderShell(<button type="button">Acción</button>)

    const menu = screen.getByRole('button', { name: 'Abrir menú' })
    menu.focus()
    expect(menu).toHaveFocus()
  })

  it('abre el menú lateral activando el botón con teclado', async () => {
    const user = userEvent.setup()
    renderShell(<button type="button">Acción</button>)

    screen.getByRole('button', { name: 'Abrir menú' }).focus()
    await user.keyboard('{Enter}')

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
  })

  it('los ítems de navegación son operables y exponen su estado', () => {
    renderShell(<button type="button">Acción</button>, '/facturas')

    const facturas = screen.getByRole('link', { name: 'Facturas' })
    expect(facturas).toHaveAttribute('aria-current', 'page')
  })

  it('el formulario de transición es operable solo con teclado', async () => {
    mockFetchJson({
      id: 'abcdef1234567890',
      clientId: 'Acme',
      amount: 1000,
      status: 'pagado',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
      remindersCount: 0,
      statusHistory: [],
      allowedTransitions: ['pagado'],
    })
    const user = userEvent.setup()
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="primerrecordatorio"
        allowedTransitions={['pagado']}
      />
    )

    const trigger = screen.getByRole('combobox', { name: /nuevo estado/i })
    trigger.focus()
    expect(trigger).toHaveFocus()
    await user.keyboard('{Enter}')
    await user.click(await screen.findByRole('option', { name: 'Pagado' }))

    const submit = screen.getByRole('button', { name: /cambiar estado/i })
    submit.focus()
    expect(submit).toHaveFocus()
  })

  it('un toast puede cerrarse con el teclado', async () => {
    function ErrorTrigger() {
      const toast = useToast()
      return (
        <button type="button" onClick={() => toast.error('Fallo de prueba')}>
          disparar
        </button>
      )
    }
    const user = userEvent.setup()
    render(
      <ToastProvider>
        <ErrorTrigger />
        <ToastViewport />
      </ToastProvider>
    )

    await user.click(screen.getByRole('button', { name: 'disparar' }))
    const close = await screen.findByRole('button', { name: /cerrar notificación/i })
    close.focus()
    expect(close).toHaveFocus()
    await user.keyboard('{Enter}')
    expect(screen.queryByText('Fallo de prueba')).not.toBeInTheDocument()
  })
})
