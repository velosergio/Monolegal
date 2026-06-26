import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { InvoicesEmptyState } from '@/features/invoices/components/InvoicesEmptyState'

describe('InvoicesEmptyState', () => {
  it('muestra un mensaje claro de ausencia de resultados', () => {
    render(<InvoicesEmptyState />)
    expect(screen.getByText('No se encontraron facturas')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent(/Ajusta los filtros/)
  })
})
