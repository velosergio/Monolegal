import { screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ClientsPage } from '@/features/clients/components/ClientsPage'
import type { PagedClients } from '@/features/clients/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

const paged: PagedClients = {
  data: [
    {
      id: 'c1',
      name: 'Acme',
      email: 'acme@correo.com',
      phone: null,
      address: null,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z',
    },
  ],
  total: 1,
  pageSize: 10,
}

afterEach(() => vi.restoreAllMocks())

describe('ClientsPage', () => {
  it('muestra el listado de clientes', async () => {
    mockFetchJson(paged)
    renderWithQuery(<ClientsPage />)

    expect(await screen.findByText('Acme')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Nuevo cliente/ })).toBeInTheDocument()
  })

  it('muestra el estado vacío cuando no hay clientes', async () => {
    mockFetchJson({ data: [], total: 0, pageSize: 10 })
    renderWithQuery(<ClientsPage />)

    await waitFor(() => expect(screen.getByText('No se encontraron clientes.')).toBeInTheDocument())
  })
})
