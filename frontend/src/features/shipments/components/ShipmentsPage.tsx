import { useQueryClient } from '@tanstack/react-query'
import { RefreshCw } from 'lucide-react'
import { domAnimation, LazyMotion, m, useReducedMotion } from 'motion/react'
import { useState } from 'react'
import { useToast } from '@/components/feedback/useToast'
import { Button } from '@/components/ui/button'
import { useResendFailed } from '@/features/settings/api/useEmailTools'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { fadeInUp, motionTransition } from '@/lib/motion'
import { cn } from '@/lib/utils'
import { useShipments } from '../api/useShipments'
import { PAGE_SIZE, useShipmentsViewState } from '../hooks/useShipmentsViewState'
import type { Shipment } from '../types'
import { CancelNotificationDialog } from './CancelNotificationDialog'
import { ShipmentsEmptyState } from './ShipmentsEmptyState'
import { ShipmentsFilters } from './ShipmentsFilters'
import { ShipmentsPagination } from './ShipmentsPagination'
import { ShipmentsTable } from './ShipmentsTable'
import { ShipmentsTableSkeleton } from './ShipmentsTableSkeleton'

/**
 * Página de Envíos (spec 019): compone filtro/búsqueda, la acción global "Reintentar fallidos",
 * la tabla con sus estados (carga/vacío/error/datos), la cancelación por factura y la paginación.
 */
export function ShipmentsPage() {
  useDocumentTitle('Envíos')
  const {
    sendStatus,
    searchInput,
    search,
    page,
    hasActiveFilters,
    setSendStatus,
    setSearchInput,
    setPage,
  } = useShipmentsViewState()
  const reduceMotion = useReducedMotion()
  const toast = useToast()
  const queryClient = useQueryClient()
  const resendFailed = useResendFailed()

  const [cancelTarget, setCancelTarget] = useState<Shipment | null>(null)

  const query = useShipments({ sendStatus, search, page, pageSize: PAGE_SIZE })
  const { data, isLoading, isError, isPlaceholderData } = query

  async function handleResendFailed() {
    try {
      const result = await resendFailed.mutateAsync()
      queryClient.invalidateQueries({ queryKey: ['shipments'] })
      if (result.attempted === 0) {
        toast.success('No hay notificaciones fallidas que reintentar.')
      } else {
        toast.success(
          `Reintentadas ${result.attempted}: ${result.resent} enviadas, ${result.failed} fallidas.`
        )
      }
    } catch (error) {
      toast.error(
        error instanceof Error ? error.message : 'No se pudieron reintentar las fallidas.'
      )
    }
  }

  return (
    <section aria-labelledby="shipments-title" className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 id="shipments-title" className="font-heading text-2xl font-black tracking-tight">
          Envíos
        </h1>
        <p className="text-sm text-muted-foreground">
          Supervisa el estado de las notificaciones por correo y gestiona los reenvíos.
        </p>
      </header>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <ShipmentsFilters
          sendStatus={sendStatus}
          searchInput={searchInput}
          onSendStatusChange={setSendStatus}
          onSearchChange={setSearchInput}
        />
        <Button
          type="button"
          variant="outline"
          className="sm:ml-auto"
          disabled={resendFailed.isPending}
          onClick={handleResendFailed}
        >
          <RefreshCw className="h-4 w-4" aria-hidden="true" />
          {resendFailed.isPending ? 'Reintentando…' : 'Reintentar fallidos'}
        </Button>
      </div>

      <LazyMotion features={domAnimation}>
        <m.div
          initial="hidden"
          animate="visible"
          variants={fadeInUp}
          transition={motionTransition(reduceMotion)}
          className="rounded-lg border"
        >
          {isError ? (
            <div role="alert" className="flex flex-col items-center gap-3 py-16 text-center">
              <p className="font-medium text-foreground">No se pudieron cargar los envíos.</p>
              <p className="text-sm text-muted-foreground">
                Revisa tu conexión e inténtalo de nuevo.
              </p>
              <Button type="button" variant="outline" onClick={() => query.refetch()}>
                Reintentar
              </Button>
            </div>
          ) : isLoading ? (
            <ShipmentsTableSkeleton rows={PAGE_SIZE} />
          ) : !data || data.data.length === 0 ? (
            <ShipmentsEmptyState filtered={hasActiveFilters} />
          ) : (
            <div
              className={cn('transition-opacity', isPlaceholderData && 'opacity-60')}
              aria-busy={isPlaceholderData}
            >
              <ShipmentsTable shipments={data.data} onCancelShipment={setCancelTarget} />
            </div>
          )}
        </m.div>
      </LazyMotion>

      <CancelNotificationDialog shipment={cancelTarget} onClose={() => setCancelTarget(null)} />

      {!isError && data && data.total > 0 ? (
        <ShipmentsPagination
          page={page}
          pageSize={data.pageSize || PAGE_SIZE}
          total={data.total}
          onPageChange={setPage}
        />
      ) : null}
    </section>
  )
}
