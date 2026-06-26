import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ClientSearch } from '@/features/invoices/components/ClientSearch'

describe('ClientSearch', () => {
  it('muestra el valor controlado', () => {
    render(<ClientSearch value="acme" onChange={vi.fn()} />)
    expect(screen.getByRole('searchbox', { name: 'Buscar por cliente' })).toHaveValue('acme')
  })

  it('notifica cada cambio del input al contenedor', () => {
    const onChange = vi.fn()
    render(<ClientSearch value="" onChange={onChange} />)

    fireEvent.change(screen.getByRole('searchbox', { name: 'Buscar por cliente' }), {
      target: { value: 'acme' },
    })

    expect(onChange).toHaveBeenCalledWith('acme')
  })
})
