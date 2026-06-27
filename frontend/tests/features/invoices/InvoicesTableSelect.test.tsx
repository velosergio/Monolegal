import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { InvoicesTable } from '@/features/invoices/components/InvoicesTable'
import type { Invoice } from '@/features/invoices/types'
import { renderWithQuery } from '../../test-utils'

const invoices: Invoice[] = [
  {
    id: 'abcdef1234567890',
    clientId: 'client-1',
    clientName: 'Acme S.A.',
    amount: 1_500_000,
    status: 'pending',
    createdAt: '2026-01-01T08:00:00.000Z',
    lastStatusTransitionAt: '2026-06-01T10:30:00.000Z',
  },
]

describe('InvoicesTable — selección', () => {
  it('expone un control accesible que abre el detalle de la factura', async () => {
    const onSelect = vi.fn()
    renderWithQuery(<InvoicesTable invoices={invoices} onSelectInvoice={onSelect} />)

    const trigger = screen.getByRole('button', { name: 'Ver detalle de la factura de Acme S.A.' })
    await userEvent.click(trigger)

    expect(onSelect).toHaveBeenCalledWith('abcdef1234567890')
  })

  it('es operable por teclado', async () => {
    const onSelect = vi.fn()
    renderWithQuery(<InvoicesTable invoices={invoices} onSelectInvoice={onSelect} />)

    await userEvent.tab()
    await userEvent.keyboard('{Enter}')

    expect(onSelect).toHaveBeenCalledWith('abcdef1234567890')
  })
})
