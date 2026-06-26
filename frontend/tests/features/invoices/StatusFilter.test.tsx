import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { StatusFilter } from '@/features/invoices/components/StatusFilter'

describe('StatusFilter', () => {
  it('expone un combobox con nombre accesible y la selección actual', () => {
    render(<StatusFilter value="all" onChange={vi.fn()} />)
    const trigger = screen.getByRole('combobox', { name: 'Filtrar por estado' })
    expect(trigger).toHaveTextContent('Todos los estados')
  })

  it('lista los estados filtrables y notifica el cambio', async () => {
    const onChange = vi.fn()
    const user = userEvent.setup()
    render(<StatusFilter value="all" onChange={onChange} />)

    await user.click(screen.getByRole('combobox', { name: 'Filtrar por estado' }))

    expect(await screen.findByRole('option', { name: 'Pagado' })).toBeInTheDocument()
    await user.click(screen.getByRole('option', { name: 'Pendiente' }))

    expect(onChange).toHaveBeenCalledWith('pending')
  })
})
