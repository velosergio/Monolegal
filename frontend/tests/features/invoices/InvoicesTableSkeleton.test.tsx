import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { InvoicesTableSkeleton } from '@/features/invoices/components/InvoicesTableSkeleton'

// El esqueleto es decorativo (aria-hidden), por lo que se consulta el DOM
// directamente en lugar de la árbol de accesibilidad.

describe('InvoicesTableSkeleton', () => {
  it('mantiene las mismas columnas que la tabla real', () => {
    const { container } = render(<InvoicesTableSkeleton rows={3} />)
    const headers = Array.from(container.querySelectorAll('thead th')).map((th) =>
      th.textContent?.trim()
    )
    expect(headers).toEqual(['ID', 'Cliente', 'Monto', 'Estado', 'Última Acción', 'Acciones'])
  })

  it('renderiza el número de filas solicitado', () => {
    const { container } = render(<InvoicesTableSkeleton rows={3} />)
    expect(container.querySelectorAll('tbody tr')).toHaveLength(3)
  })

  it('usa 10 filas por defecto', () => {
    const { container } = render(<InvoicesTableSkeleton />)
    expect(container.querySelectorAll('tbody tr')).toHaveLength(10)
  })

  it('marca la tabla como decorativa para lectores de pantalla', () => {
    const { container } = render(<InvoicesTableSkeleton rows={2} />)
    expect(container.querySelector('table')).toHaveAttribute('aria-hidden', 'true')
  })
})
