import { QueryClient } from '@tanstack/react-query'

/**
 * Cliente compartido de TanStack Query (estado de servidor).
 * Configuración funcional concreta (reintentos, staleTime por recurso) en fases posteriores.
 */
export const queryClient = new QueryClient()
