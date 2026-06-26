import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { DeleteClientDialog } from '@/features/clients/components/DeleteClientDialog'
import type { Client } from '@/features/clients/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

const client: Client = {
  id: 'c1',
  name: 'Acme',
  email: 'acme@correo.com',
  phone: null,
  address: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
}

afterEach(() => vi.restoreAllMocks())

describe('DeleteClientDialog', () => {
  it('confirma el borrado y cierra al recibir 204', async () => {
    const user = userEvent.setup()
    mockFetchJson({}, { ok: true, status: 204 })
    const onClose = vi.fn()
    renderWithQuery(<DeleteClientDialog client={client} onClose={onClose} />)

    await user.click(screen.getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(onClose).toHaveBeenCalled())
  })

  it('muestra el mensaje de conflicto cuando el cliente tiene facturas (409)', async () => {
    const user = userEvent.setup()
    mockFetchJson(
      { error: 'No se puede eliminar: el cliente tiene facturas asociadas.' },
      { ok: false, status: 409 }
    )
    const onClose = vi.fn()
    renderWithQuery(<DeleteClientDialog client={client} onClose={onClose} />)

    await user.click(screen.getByRole('button', { name: 'Eliminar' }))

    expect(
      await screen.findByText('No se puede eliminar: el cliente tiene facturas asociadas.')
    ).toBeInTheDocument()
    expect(onClose).not.toHaveBeenCalled()
  })
})
