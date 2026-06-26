import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteAllData, flushDatabase } from './maintenance'

/** Mutación: eliminar todos los registros de negocio; invalida facturas y estadísticas. */
export function useDeleteAllData() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => deleteAllData(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      queryClient.invalidateQueries({ queryKey: ['invoice'] })
      queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
    },
  })
}

/** Mutación: vaciar la base de datos completa y re-sembrar; invalida facturas y estadísticas. */
export function useFlushDatabase() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => flushDatabase(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      queryClient.invalidateQueries({ queryKey: ['invoice'] })
      queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
    },
  })
}
