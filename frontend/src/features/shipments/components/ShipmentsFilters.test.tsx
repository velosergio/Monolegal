import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ShipmentsFilters } from './ShipmentsFilters'

describe('ShipmentsFilters', () => {
  it('renderiza el filtro de estado y el campo de búsqueda', () => {
    render(
      <ShipmentsFilters
        sendStatus="all"
        searchInput=""
        onSendStatusChange={vi.fn()}
        onSearchChange={vi.fn()}
      />
    )
    expect(screen.getByLabelText('Filtrar por estado de envío')).toBeInTheDocument()
    expect(screen.getByLabelText('Buscar por cliente o correo')).toBeInTheDocument()
  })

  it('invoca onSearchChange al escribir en la búsqueda', async () => {
    const onSearchChange = vi.fn()
    render(
      <ShipmentsFilters
        sendStatus="all"
        searchInput=""
        onSendStatusChange={vi.fn()}
        onSearchChange={onSearchChange}
      />
    )

    await userEvent.type(screen.getByLabelText('Buscar por cliente o correo'), 'acme')

    expect(onSearchChange).toHaveBeenCalled()
    expect(onSearchChange).toHaveBeenLastCalledWith('e')
  })
})
