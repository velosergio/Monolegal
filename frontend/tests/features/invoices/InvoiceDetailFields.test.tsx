import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { InvoiceDetailFields } from '@/features/invoices/components/InvoiceDetailFields'
import type { InvoiceDetail } from '@/features/invoices/types'

const detail: InvoiceDetail = {
  id: 'abcdef1234567890',
  clientId: 'Acme S.A.',
  amount: 1_500_000,
  dueDate: '2026-02-01T00:00:00.000Z',
  items: [{ description: 'Concepto', quantity: 1, unitPrice: 1_500_000, subtotal: 1_500_000 }],
  status: 'primerrecordatorio',
  createdAt: '2026-01-01T08:00:00.000Z',
  updatedAt: '2026-06-01T10:30:00.000Z',
  remindersCount: 2,
  lastReminderSentAt: '2026-06-01T10:30:00.000Z',
  lastStatusTransitionAt: '2026-06-01T10:30:00.000Z',
  statusHistory: [],
  allowedTransitions: ['segundorecordatorio', 'pagado'],
}

describe('InvoiceDetailFields', () => {
  it('muestra todos los campos con formato legible', () => {
    render(<InvoiceDetailFields invoice={detail} />)

    expect(screen.getByText('Acme S.A.')).toBeInTheDocument()
    expect(screen.getByText((t) => t.includes('1.500.000'))).toBeInTheDocument()
    expect(screen.getByText('1er Recordatorio')).toBeInTheDocument()
    expect(screen.getByText('abcdef1234567890')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()

    for (const label of [
      'Identificador',
      'Cliente',
      'Monto',
      'Estado',
      'Creada',
      'Última actualización',
      'Recordatorios enviados',
      'Último recordatorio',
      'Última transición de estado',
    ]) {
      expect(screen.getByText(label)).toBeInTheDocument()
    }
  })

  it('muestra un guion cuando no hay último recordatorio', () => {
    render(<InvoiceDetailFields invoice={{ ...detail, lastReminderSentAt: null }} />)
    expect(screen.getByText('—')).toBeInTheDocument()
  })
})
