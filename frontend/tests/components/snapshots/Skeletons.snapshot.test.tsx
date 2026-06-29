import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DashboardSkeleton } from '@/features/dashboard/components/DashboardSkeleton'
import { InvoiceDetailSkeleton } from '@/features/invoices/components/InvoiceDetailSkeleton'
import { InvoicesTableSkeleton } from '@/features/invoices/components/InvoicesTableSkeleton'
import { ShipmentsTableSkeleton } from '@/features/shipments/components/ShipmentsTableSkeleton'

describe('Esqueletos de carga (snapshot)', () => {
  it('InvoicesTableSkeleton mantiene su marcado con un número fijo de filas', () => {
    const { asFragment } = render(<InvoicesTableSkeleton rows={2} />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('InvoiceDetailSkeleton mantiene su marcado', () => {
    const { asFragment } = render(<InvoiceDetailSkeleton />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('DashboardSkeleton mantiene su marcado', () => {
    const { asFragment } = render(<DashboardSkeleton />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('ShipmentsTableSkeleton mantiene su marcado con un número fijo de filas', () => {
    const { asFragment } = render(<ShipmentsTableSkeleton rows={2} />)
    expect(asFragment()).toMatchSnapshot()
  })
})
