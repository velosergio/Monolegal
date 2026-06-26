import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { InvoicesTable } from '@/features/invoices/components/InvoicesTable'
import type { Invoice } from '@/features/invoices/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

const invoices: Invoice[] = [
  {
    id: 'abcdef1234567890',
    clientId: 'Acme S.A.',
    amount: 1_500_000,
    status: 'pending',
    createdAt: '2026-01-01T08:00:00.000Z',
    lastStatusTransitionAt: '2026-06-01T10:30:00.000Z',
  },
  {
    id: 'feedc0de',
    clientId: 'Globex',
    amount: 250_000,
    status: 'pagado',
    createdAt: '2026-02-01T08:00:00.000Z',
    lastStatusTransitionAt: '2026-06-10T12:00:00.000Z',
  },
  {
    id: 'unknown99',
    clientId: 'Initech',
    amount: 99_000,
    status: 'algo-raro',
    createdAt: '2026-03-01T08:00:00.000Z',
    lastStatusTransitionAt: '2026-06-12T09:00:00.000Z',
  },
]

describe('InvoicesTable', () => {
  it('renderiza las columnas esperadas', () => {
    renderWithQuery(<InvoicesTable invoices={invoices} />)

    for (const header of ['ID', 'Cliente', 'Monto', 'Estado', 'Última Acción', 'Acciones']) {
      expect(screen.getByRole('columnheader', { name: header })).toBeInTheDocument()
    }
  })

  it('formatea el monto y muestra el cliente', () => {
    renderWithQuery(<InvoicesTable invoices={invoices} />)
    expect(screen.getByText((t) => t.includes('1.500.000'))).toBeInTheDocument()
    expect(screen.getByText('Acme S.A.')).toBeInTheDocument()
  })

  it('muestra la etiqueta de estado conocida y el valor crudo para desconocidos', () => {
    renderWithQuery(<InvoicesTable invoices={invoices} />)
    expect(screen.getByText('Pendiente')).toBeInTheDocument()
    expect(screen.getByText('Pagado')).toBeInTheDocument()
    expect(screen.getByText('algo-raro')).toBeInTheDocument()
  })

  it('ofrece "Pagar" solo para facturas en estado no terminal', () => {
    renderWithQuery(<InvoicesTable invoices={invoices} />)
    // pending + desconocido => 2 botones Pagar; pagado (terminal) no.
    expect(screen.getAllByRole('button', { name: 'Pagar' })).toHaveLength(2)
  })

  it('acorta el ID y conserva el completo en el title', () => {
    renderWithQuery(<InvoicesTable invoices={invoices} />)
    const cell = screen.getByTitle('abcdef1234567890')
    expect(cell).toHaveTextContent('abcdef12')
  })

  it('al pulsar "Pagar" invoca el endpoint de pago de la factura', async () => {
    const fetchMock = mockFetchJson({
      id: 'abcdef1234567890',
      status: 'pagado',
      lastStatusTransitionAt: '2026-06-20T10:00:00.000Z',
    })
    const user = userEvent.setup()
    renderWithQuery(<InvoicesTable invoices={invoices} />)

    await user.click(screen.getAllByRole('button', { name: 'Pagar' })[0])

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/invoices/abcdef1234567890/pay',
        expect.objectContaining({ method: 'POST' })
      )
    })
  })
})
