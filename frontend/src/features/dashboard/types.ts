/**
 * Estadísticas agregadas de la cartera de facturas (spec 015), tal como las devuelve
 * `GET /api/invoices/stats`. Las claves de `byStatus`/`byClient` solo aparecen con ≥1 factura.
 */
export interface InvoiceStats {
  totalInvoices: number
  byStatus: Record<string, number>
  byClient: Record<string, number>
}

/** Punto de datos genérico para los gráficos del dashboard. */
export interface ChartDatum {
  label: string
  value: number
  color?: string
}
