import type { LucideIcon } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

interface StatCardProps {
  label: string
  value: number | string
  icon?: LucideIcon
}

/** Tarjeta de métrica destacada del dashboard (total, nº de estados, nº de clientes). */
export function StatCard({ label, value, icon: Icon }: StatCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between gap-2 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
        {Icon ? <Icon aria-hidden="true" className="size-4 text-muted-foreground" /> : null}
      </CardHeader>
      <CardContent>
        <p className="text-3xl font-bold tabular-nums text-foreground">{value}</p>
      </CardContent>
    </Card>
  )
}
