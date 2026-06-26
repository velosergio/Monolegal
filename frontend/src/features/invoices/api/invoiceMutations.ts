import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { InvoiceDetail, InvoiceFormValues } from '../types'
import { invoiceDetailKey } from './useInvoiceDetail'

async function readErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string; errors?: Record<string, string[]> }
    if (typeof body?.error === 'string' && body.error.length > 0) return body.error
    if (body?.errors) {
      const first = Object.values(body.errors).flat()[0]
      if (typeof first === 'string' && first.length > 0) return first
    }
  } catch {
    // Cuerpo no-JSON.
  }
  return fallback
}

/** Convierte los valores del formulario al cuerpo de la API (sin amount: lo deriva el backend). */
function toPayload(values: InvoiceFormValues) {
  return {
    clientId: values.clientId,
    dueDate: new Date(values.dueDate).toISOString(),
    items: values.items.map((i) => ({
      description: i.description.trim(),
      quantity: i.quantity,
      unitPrice: i.unitPrice,
    })),
  }
}

async function createInvoice(values: InvoiceFormValues): Promise<InvoiceDetail> {
  const response = await fetch('/api/invoices', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(toPayload(values)),
  })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo crear la factura (${response.status}).`)
    )
  }
  return (await response.json()) as InvoiceDetail
}

async function updateInvoice(id: string, values: InvoiceFormValues): Promise<InvoiceDetail> {
  const response = await fetch(`/api/invoices/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(toPayload(values)),
  })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo actualizar la factura (${response.status}).`)
    )
  }
  return (await response.json()) as InvoiceDetail
}

async function deleteInvoice(id: string): Promise<void> {
  const response = await fetch(`/api/invoices/${encodeURIComponent(id)}`, { method: 'DELETE' })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo eliminar la factura (${response.status}).`)
    )
  }
}

/**
 * Invalida de forma dirigida las claves afectadas por una mutación de factura, manteniendo
 * coherentes el listado y el dashboard sin recargar (RF-008, patrón de useTransitionInvoice).
 */
function useInvalidateInvoices() {
  const queryClient = useQueryClient()
  return () => {
    queryClient.invalidateQueries({ queryKey: ['invoices'] })
    queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
  }
}

export function useCreateInvoice() {
  const invalidate = useInvalidateInvoices()
  return useMutation({
    mutationFn: (values: InvoiceFormValues) => createInvoice(values),
    onSuccess: invalidate,
  })
}

export function useUpdateInvoice() {
  const invalidate = useInvalidateInvoices()
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, values }: { id: string; values: InvoiceFormValues }) =>
      updateInvoice(id, values),
    onSuccess: (_data, { id }) => {
      invalidate()
      queryClient.invalidateQueries({ queryKey: invoiceDetailKey(id) })
    },
  })
}

export function useDeleteInvoice() {
  const invalidate = useInvalidateInvoices()
  return useMutation({
    mutationFn: (id: string) => deleteInvoice(id),
    onSuccess: invalidate,
  })
}
