import { Plus } from 'lucide-react'
import { domAnimation, LazyMotion, m, useReducedMotion } from 'motion/react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { fadeInUp, motionTransition } from '@/lib/motion'
import { cn } from '@/lib/utils'
import { useInvoiceDetail } from '../api/useInvoiceDetail'
import { useInvoices } from '../api/useInvoices'
import { PAGE_SIZE, useInvoicesViewState } from '../hooks/useInvoicesViewState'
import { useSelectedInvoice } from '../hooks/useSelectedInvoice'
import { ClientSearch } from './ClientSearch'
import { DeleteInvoiceDialog } from './DeleteInvoiceDialog'
import { InvoiceDetailModal } from './InvoiceDetailModal'
import { InvoiceFormModal } from './InvoiceFormModal'
import { InvoicesEmptyState } from './InvoicesEmptyState'
import { InvoicesPagination } from './InvoicesPagination'
import { InvoicesTable } from './InvoicesTable'
import { InvoicesTableSkeleton } from './InvoicesTableSkeleton'
import { StatusFilter } from './StatusFilter'

/**
 * Página de Facturas: compone filtro de estado, búsqueda por cliente, tabla con
 * sus estados (carga/vacío/error/datos) y paginación. El estado de servidor lo
 * gestiona `useInvoices` (con `keepPreviousData` para evitar parpadeos).
 */
export function InvoicesPage() {
  useDocumentTitle('Facturas')
  const { status, searchInput, search, page, setStatus, setSearchInput, setPage } =
    useInvoicesViewState()
  const { selectedId, open: openInvoice, close: closeInvoice } = useSelectedInvoice()
  const reduceMotion = useReducedMotion()

  // Estado del CRUD (spec 018): alta, edición (por id → detalle) y borrado.
  const [isCreating, setIsCreating] = useState(false)
  const [editId, setEditId] = useState<string | null>(null)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const editDetail = useInvoiceDetail(editId)

  const query = useInvoices({ status, search, page, pageSize: PAGE_SIZE })
  const { data, isLoading, isError, isPlaceholderData } = query

  return (
    <section aria-labelledby="invoices-title" className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 id="invoices-title" className="font-heading text-2xl font-black tracking-tight">
          Facturas
        </h1>
        <p className="text-sm text-muted-foreground">
          Consulta y gestiona el estado de cobro de las facturas.
        </p>
      </header>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <StatusFilter value={status} onChange={setStatus} />
        <ClientSearch value={searchInput} onChange={setSearchInput} />
        <Button type="button" className="sm:ml-auto" onClick={() => setIsCreating(true)}>
          <Plus className="h-4 w-4" aria-hidden="true" />
          Nueva factura
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
              <p className="font-medium text-foreground">No se pudieron cargar las facturas.</p>
              <p className="text-sm text-muted-foreground">
                Revisa tu conexión e inténtalo de nuevo.
              </p>
              <Button type="button" variant="outline" onClick={() => query.refetch()}>
                Reintentar
              </Button>
            </div>
          ) : isLoading ? (
            <InvoicesTableSkeleton rows={PAGE_SIZE} />
          ) : !data || data.data.length === 0 ? (
            <InvoicesEmptyState />
          ) : (
            <div
              className={cn('transition-opacity', isPlaceholderData && 'opacity-60')}
              aria-busy={isPlaceholderData}
            >
              <InvoicesTable
                invoices={data.data}
                onSelectInvoice={openInvoice}
                onEditInvoice={setEditId}
                onDeleteInvoice={setDeleteId}
              />
            </div>
          )}
        </m.div>
      </LazyMotion>

      <InvoiceDetailModal invoiceId={selectedId} onClose={closeInvoice} />

      {/* Alta de factura */}
      <InvoiceFormModal open={isCreating} onClose={() => setIsCreating(false)} />

      {/* Edición: se abre cuando el detalle de la factura seleccionada ya cargó */}
      <InvoiceFormModal
        open={editId != null && editDetail.data != null}
        invoice={editDetail.data ?? null}
        onClose={() => setEditId(null)}
      />

      {/* Borrado con confirmación */}
      <DeleteInvoiceDialog invoiceId={deleteId} onClose={() => setDeleteId(null)} />

      {!isError && data && data.total > 0 ? (
        <InvoicesPagination
          page={page}
          pageSize={data.pageSize || PAGE_SIZE}
          total={data.total}
          onPageChange={setPage}
        />
      ) : null}
    </section>
  )
}
