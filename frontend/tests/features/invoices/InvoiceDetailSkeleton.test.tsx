import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { InvoiceDetailSkeleton } from '@/features/invoices/components/InvoiceDetailSkeleton'

describe('InvoiceDetailSkeleton', () => {
  it('queda oculto a tecnologías de asistencia', () => {
    const { container } = render(<InvoiceDetailSkeleton />)
    expect(container.firstElementChild).toHaveAttribute('aria-hidden', 'true')
  })

  it('reproduce la estructura de seis grupos de campos del detalle', () => {
    const { container } = render(<InvoiceDetailSkeleton />)
    const grid = container.querySelector('.grid')
    expect(grid).not.toBeNull()
    expect(grid?.children).toHaveLength(6)
  })
})
