import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { InvoiceItemsEditor } from '@/features/invoices/components/InvoiceItemsEditor'
import type { InvoiceItemForm } from '@/features/invoices/types'

describe('InvoiceItemsEditor', () => {
  const items: InvoiceItemForm[] = [
    { rowId: 'row-1', description: 'Asesoría', quantity: 2, unitPrice: 150 },
    { rowId: 'row-2', description: 'Trámite', quantity: 1, unitPrice: 50 },
  ]

  it('muestra el total derivado de la suma de subtotales', () => {
    render(<InvoiceItemsEditor items={items} onChange={vi.fn()} />)
    // 2*150 + 1*50 = 350. Se busca el valor (350) en el bloque de Total.
    expect(screen.getByText('Total')).toBeInTheDocument()
    expect(screen.getAllByText(/350/).length).toBeGreaterThan(0)
  })

  it('renderiza una fila por línea de detalle', () => {
    render(<InvoiceItemsEditor items={items} onChange={vi.fn()} />)
    expect(screen.getByLabelText('Descripción de la línea 1')).toHaveValue('Asesoría')
    expect(screen.getByLabelText('Descripción de la línea 2')).toHaveValue('Trámite')
  })
})
