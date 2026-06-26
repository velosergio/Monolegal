const currencyFormatter = new Intl.NumberFormat('es-CO', {
  style: 'currency',
  currency: 'COP',
  maximumFractionDigits: 0,
})

const dateFormatter = new Intl.DateTimeFormat('es-CO', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

/** Formatea un monto como moneda local (COP, sin decimales). */
export function formatAmount(amount: number): string {
  return currencyFormatter.format(amount)
}

/** Formatea una fecha ISO-8601 a una representación local legible. */
export function formatDate(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return dateFormatter.format(date)
}

/** Acorta un identificador largo para la tabla (los 8 primeros caracteres). */
export function shortId(id: string): string {
  return id.length > 8 ? `${id.slice(0, 8)}…` : id
}
