import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { Client, ClientFormValues } from '../types'

/** Error de conflicto al eliminar un cliente con facturas asociadas (HTTP 409, RF-018). */
export class ClientHasInvoicesError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'ClientHasInvoicesError'
  }
}

/** Normaliza los valores del formulario al cuerpo de la API (campos vacíos → null). */
function toPayload(values: ClientFormValues) {
  return {
    name: values.name.trim(),
    email: values.email.trim(),
    phone: values.phone.trim() || null,
    address: values.address.trim() || null,
  }
}

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

async function createClient(values: ClientFormValues): Promise<Client> {
  const response = await fetch('/api/clients', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(toPayload(values)),
  })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo crear el cliente (${response.status}).`)
    )
  }
  return (await response.json()) as Client
}

async function updateClient(id: string, values: ClientFormValues): Promise<Client> {
  const response = await fetch(`/api/clients/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(toPayload(values)),
  })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo actualizar el cliente (${response.status}).`)
    )
  }
  return (await response.json()) as Client
}

async function deleteClient(id: string): Promise<void> {
  const response = await fetch(`/api/clients/${encodeURIComponent(id)}`, { method: 'DELETE' })
  if (response.status === 409) {
    throw new ClientHasInvoicesError(
      await readErrorMessage(response, 'No se puede eliminar: el cliente tiene facturas asociadas.')
    )
  }
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo eliminar el cliente (${response.status}).`)
    )
  }
}

/** Invalida el listado de clientes tras una mutación exitosa (RF-021). */
function useInvalidateClients() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: ['clients'] })
}

export function useCreateClient() {
  const invalidate = useInvalidateClients()
  return useMutation({
    mutationFn: (values: ClientFormValues) => createClient(values),
    onSuccess: invalidate,
  })
}

export function useUpdateClient() {
  const invalidate = useInvalidateClients()
  return useMutation({
    mutationFn: ({ id, values }: { id: string; values: ClientFormValues }) =>
      updateClient(id, values),
    onSuccess: invalidate,
  })
}

export function useDeleteClient() {
  const invalidate = useInvalidateClients()
  return useMutation({
    mutationFn: (id: string) => deleteClient(id),
    onSuccess: invalidate,
  })
}
