import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ClientFormModal } from '@/features/clients/components/ClientFormModal'
import { renderWithQuery } from '../../test-utils'

describe('ClientFormModal', () => {
  it('muestra errores de validación cuando se envía vacío', async () => {
    const user = userEvent.setup()
    renderWithQuery(<ClientFormModal open onClose={vi.fn()} />)

    await user.click(screen.getByRole('button', { name: 'Crear cliente' }))

    expect(await screen.findByText('El nombre es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('El email es obligatorio.')).toBeInTheDocument()
  })

  it('rechaza un email con formato inválido', async () => {
    const user = userEvent.setup()
    renderWithQuery(<ClientFormModal open onClose={vi.fn()} />)

    await user.type(screen.getByLabelText('Nombre'), 'Acme')
    await user.type(screen.getByLabelText('Email'), 'no-es-email')
    await user.click(screen.getByRole('button', { name: 'Crear cliente' }))

    await waitFor(() =>
      expect(screen.getByText('El email debe tener un formato válido.')).toBeInTheDocument()
    )
  })
})
