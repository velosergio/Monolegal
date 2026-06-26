import { Inbox } from 'lucide-react'

/** Estado vacío del dashboard cuando aún no hay facturas registradas. */
export function DashboardEmptyState() {
  return (
    <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border py-16 text-center">
      <Inbox aria-hidden="true" className="size-10 text-muted-foreground" />
      <p className="text-base font-medium text-foreground">No hay facturas todavía</p>
      <p className="max-w-sm text-sm text-muted-foreground">
        Cuando se registren facturas, aquí verás las estadísticas de la cartera.
      </p>
    </div>
  )
}
