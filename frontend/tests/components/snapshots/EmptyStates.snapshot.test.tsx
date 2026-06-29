import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DashboardEmptyState } from '@/features/dashboard/components/DashboardEmptyState'
import { InvoicesEmptyState } from '@/features/invoices/components/InvoicesEmptyState'
import { ShipmentsEmptyState } from '@/features/shipments/components/ShipmentsEmptyState'

describe('Estados vacíos (snapshot)', () => {
  it('InvoicesEmptyState mantiene su marcado', () => {
    const { asFragment } = render(<InvoicesEmptyState />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('DashboardEmptyState mantiene su marcado', () => {
    const { asFragment } = render(<DashboardEmptyState />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('ShipmentsEmptyState mantiene su marcado con filtros activos', () => {
    const { asFragment } = render(<ShipmentsEmptyState filtered={true} />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('ShipmentsEmptyState mantiene su marcado sin envíos', () => {
    const { asFragment } = render(<ShipmentsEmptyState filtered={false} />)
    expect(asFragment()).toMatchSnapshot()
  })
})
