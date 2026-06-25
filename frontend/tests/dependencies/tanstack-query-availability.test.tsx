import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

// Smoke test de disponibilidad: TanStack Query (US4 / FR-005).
// Verifica que useQuery + QueryClientProvider funcionan y resuelven una query.

function DatoRemoto() {
  const { data, isSuccess } = useQuery({
    queryKey: ['smoke'],
    queryFn: () => Promise.resolve('cargado'),
  })
  return <span>{isSuccess ? data : 'cargando'}</span>
}

describe('Disponibilidad de TanStack Query', () => {
  it('resuelve una query dentro de QueryClientProvider', async () => {
    const client = new QueryClient()
    render(
      <QueryClientProvider client={client}>
        <DatoRemoto />
      </QueryClientProvider>
    )
    await waitFor(() => expect(screen.getByText('cargado')).toBeInTheDocument())
  })
})
