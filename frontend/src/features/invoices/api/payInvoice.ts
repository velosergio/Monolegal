import { useMutation, useQueryClient } from '@tanstack/react-query'

/**
 * Represents the response returned by POST /api/invoices/{id}/pay.
 *
 * - `status` mirrors the domain InvoiceStatus enum (integer).
 * - `lastStatusTransitionAt` is an ISO-8601 UTC timestamp.
 */
export interface PayInvoiceResponse {
  id: string
  status: number
  lastStatusTransitionAt: string
}

/**
 * Calls POST /api/invoices/{id}/pay and returns the updated invoice state.
 *
 * Throws on any 4xx/5xx response so the caller (or a TanStack mutation) can
 * handle the error appropriately.
 */
const payInvoice = async (id: string): Promise<PayInvoiceResponse> => {
  const response = await fetch(`/api/invoices/${id}/pay`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
  })

  if (!response.ok) {
    // Surface the error message from the API body when available.
    let message = `Error al pagar la factura (${response.status})`
    try {
      const body = await response.json()
      if (body?.error) message = body.error
    } catch {
      // ignore JSON parse errors — keep the default message
    }
    throw new Error(message)
  }

  return response.json() as Promise<PayInvoiceResponse>
}

/**
 * TanStack Query mutation hook for marking an invoice as paid.
 *
 * Usage:
 *   const { mutate, isPending, isError } = usePayInvoice()
 *   mutate(invoiceId)
 *
 * On success the hook invalidates the 'invoices' query so any list that
 * depends on it re-fetches automatically.
 */
export const usePayInvoice = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => payInvoice(id),
    onSuccess: () => {
      // Invalidate the invoices list so it reflects the new status.
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
    },
  })
}
