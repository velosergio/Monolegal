import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { InvoicesPagination } from '@/features/invoices/components/InvoicesPagination'

describe('InvoicesPagination', () => {
  it('muestra el rango y el total de páginas', () => {
    render(<InvoicesPagination page={1} pageSize={10} total={25} onPageChange={vi.fn()} />)
    expect(screen.getByText('Mostrando 1–10 de 25')).toBeInTheDocument()
    expect(screen.getByText('Página 1 de 3')).toBeInTheDocument()
  })

  it('deshabilita "Anterior" en la primera página', () => {
    render(<InvoicesPagination page={1} pageSize={10} total={25} onPageChange={vi.fn()} />)
    expect(screen.getByRole('button', { name: /Anterior/ })).toBeDisabled()
    expect(screen.getByRole('button', { name: /Siguiente/ })).toBeEnabled()
  })

  it('deshabilita "Siguiente" en la última página', () => {
    render(<InvoicesPagination page={3} pageSize={10} total={25} onPageChange={vi.fn()} />)
    expect(screen.getByRole('button', { name: /Siguiente/ })).toBeDisabled()
  })

  it('notifica el cambio de página', async () => {
    const onPageChange = vi.fn()
    const user = userEvent.setup()
    render(<InvoicesPagination page={1} pageSize={10} total={25} onPageChange={onPageChange} />)

    await user.click(screen.getByRole('button', { name: /Siguiente/ }))
    expect(onPageChange).toHaveBeenCalledWith(2)
  })
})
