import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { type RenderOptions, render } from '@testing-library/react'
import type { ReactElement, ReactNode } from 'react'
import { vi } from 'vitest'

/**
 * Crea un QueryClient aislado por test (sin reintentos ni caché compartida) para
 * que los estados de error/éxito sean deterministas.
 */
function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  })
}

function Wrapper({ children, client }: { children: ReactNode; client: QueryClient }) {
  return <QueryClientProvider client={client}>{children}</QueryClientProvider>
}

/**
 * Renderiza un componente envuelto en un QueryClientProvider de prueba.
 */
export function renderWithQuery(
  ui: ReactElement,
  options?: { client?: QueryClient } & Omit<RenderOptions, 'wrapper'>
) {
  const client = options?.client ?? createTestQueryClient()
  return {
    client,
    ...render(ui, {
      wrapper: ({ children }) => <Wrapper client={client}>{children}</Wrapper>,
      ...options,
    }),
  }
}

/**
 * Sustituye `fetch` por una respuesta JSON controlada (ok 200 por defecto).
 */
export function mockFetchJson(body: unknown, init?: { ok?: boolean; status?: number }) {
  const ok = init?.ok ?? true
  const status = init?.status ?? (ok ? 200 : 500)
  const fetchMock = vi.fn(() =>
    Promise.resolve({
      ok,
      status,
      json: () => Promise.resolve(body),
    } as Response)
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}
