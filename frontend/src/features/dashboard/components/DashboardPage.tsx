import { FileText, Tag, Users } from 'lucide-react'
import { domAnimation, LazyMotion, m, useReducedMotion } from 'motion/react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { FILTERABLE_STATUSES, INVOICE_STATUS_LABELS } from '@/features/invoices/types'
import { fadeInUp, motionTransition, staggerContainer } from '@/lib/motion'
import { useInvoiceStats } from '../api/useInvoiceStats'
import { statusChartClass } from '../statusChartColors'
import type { ChartDatum, InvoiceStats } from '../types'
import { topClients } from '../utils'
import { ClientDistributionChart } from './ClientDistributionChart'
import { DashboardEmptyState } from './DashboardEmptyState'
import { DashboardSkeleton } from './DashboardSkeleton'
import { LastRefreshIndicator } from './LastRefreshIndicator'
import { StatCard } from './StatCard'
import { StatusDistributionChart } from './StatusDistributionChart'

/**
 * Página del dashboard de estadísticas (spec 015, US4). Orquesta tarjetas, gráficos
 * animados y el indicador de último refresco a partir de `useInvoiceStats`, con
 * estados de carga, vacío y error.
 */
export function DashboardPage() {
  const query = useInvoiceStats()

  if (query.isLoading) {
    return <DashboardSkeleton />
  }

  if (query.isError) {
    return (
      <div role="alert" className="flex flex-col items-center gap-3 py-16 text-center">
        <p className="font-medium text-foreground">No se pudieron cargar las estadísticas.</p>
        <p className="text-sm text-muted-foreground">Revisa tu conexión e inténtalo de nuevo.</p>
        <Button type="button" variant="outline" onClick={() => query.refetch()}>
          Reintentar
        </Button>
      </div>
    )
  }

  if (!query.data) {
    return null
  }

  const stats = query.data

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>
        <LastRefreshIndicator
          updatedAt={query.dataUpdatedAt}
          onRefresh={() => query.refetch()}
          isRefreshing={query.isFetching}
        />
      </header>

      {stats.totalInvoices === 0 ? <DashboardEmptyState /> : <DashboardContent stats={stats} />}
    </div>
  )
}

function DashboardContent({ stats }: { stats: InvoiceStats }) {
  const reduced = useReducedMotion()
  const statusData: ChartDatum[] = FILTERABLE_STATUSES.map((status) => ({
    label: INVOICE_STATUS_LABELS[status],
    value: stats.byStatus[status] ?? 0,
    color: statusChartClass(status),
  }))
  const clientData = topClients(stats.byClient)
  const distinctClients = Object.keys(stats.byClient).length
  const distinctStatuses = Object.keys(stats.byStatus).length

  return (
    <LazyMotion features={domAnimation}>
      <m.div
        className="flex flex-col gap-6"
        variants={staggerContainer}
        initial="hidden"
        animate="visible"
      >
        <m.div
          className="grid grid-cols-1 gap-4 sm:grid-cols-3"
          variants={fadeInUp}
          transition={motionTransition(reduced)}
        >
          <StatCard label="Total de facturas" value={stats.totalInvoices} icon={FileText} />
          <StatCard label="Estados con facturas" value={distinctStatuses} icon={Tag} />
          <StatCard label="Clientes" value={distinctClients} icon={Users} />
        </m.div>

        <m.div
          className="grid grid-cols-1 gap-4 lg:grid-cols-2"
          variants={fadeInUp}
          transition={motionTransition(reduced)}
        >
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Facturas por estado</CardTitle>
            </CardHeader>
            <CardContent>
              <StatusDistributionChart data={statusData} total={stats.totalInvoices} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Facturas por cliente</CardTitle>
            </CardHeader>
            <CardContent>
              <ClientDistributionChart data={clientData} />
            </CardContent>
          </Card>
        </m.div>
      </m.div>
    </LazyMotion>
  )
}
