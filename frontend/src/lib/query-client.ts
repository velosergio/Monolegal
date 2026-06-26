import { QueryClient } from '@tanstack/react-query'

/**
 * Cliente compartido de TanStack Query (estado de servidor).
 *
 * Defaults:
 * - `staleTime` de 30s: evita refetches inmediatos al re-montar listados.
 * - `retry` 1: un reintento ante fallos transitorios sin penalizar la UX.
 * - `refetchOnWindowFocus` desactivado: el panel no necesita refrescar al
 *   recuperar el foco de la ventana.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})
