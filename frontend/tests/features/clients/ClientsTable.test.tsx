import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ClientsTable } from '@/features/clients/components/ClientsTable'
import type { Client } from '@/features/clients/types'

const clients: Client[] = [
  {
    id: 'c1',
    name: 'Acme',
    email: 'acme@correo.com',
    phone: '300',
    address: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'c2',
    name: 'Beta',
    email: 'beta@correo.com',
    phone: null,
    address: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
]

describe('ClientsTable', () => {
  it('renderiza una fila por cliente con su email y teléfono', () => {
    render(<ClientsTable clients={clients} onEdit={vi.fn()} onDelete={vi.fn()} />)
    expect(screen.getByText('Acme')).toBeInTheDocument()
    expect(screen.getByText('acme@correo.com')).toBeInTheDocument()
    expect(screen.getByText('300')).toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument() // teléfono ausente de Beta
  })

  it('invoca onEdit y onDelete con el cliente correcto', async () => {
    const user = userEvent.setup()
    const onEdit = vi.fn()
    const onDelete = vi.fn()
    render(<ClientsTable clients={clients} onEdit={onEdit} onDelete={onDelete} />)

    await user.click(screen.getByLabelText('Editar a Acme'))
    await user.click(screen.getByLabelText('Eliminar a Beta'))

    expect(onEdit).toHaveBeenCalledWith(clients[0])
    expect(onDelete).toHaveBeenCalledWith(clients[1])
  })
})
