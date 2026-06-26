import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { DeleteInvoiceDialog } from '@/features/invoices/components/DeleteInvoiceDialog'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

afterEach(() => vi.restoreAllMocks())

describe('DeleteInvoiceDialog', () => {
  it('elimina y cierra al confirmar (204)', async () => {
    const user = userEvent.setup()
    mockFetchJson({}, { ok: true, status: 204 })
    const onClose = vi.fn()
    renderWithQuery(<DeleteInvoiceDialog invoiceId="abcdef1234567890" onClose={onClose} />)

    await user.click(screen.getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(onClose).toHaveBeenCalled())
  })

  it('no renderiza acción cuando invoiceId es null', () => {
    renderWithQuery(<DeleteInvoiceDialog invoiceId={null} onClose={vi.fn()} />)
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument()
  })
})
