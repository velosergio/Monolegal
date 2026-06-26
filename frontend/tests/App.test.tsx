import { screen } from '@testing-library/react'
import { beforeAll, describe, expect, it } from 'vitest'
import App from '../src/App'
import { renderWithQuery } from './test-utils'

// Precarga el módulo diferido de InvoicesPage para que React.lazy resuelva de
// inmediato y los asserts no dependan del tiempo de carga en frío del chunk.
beforeAll(async () => {
  await import('@/features/invoices/components/InvoicesPage')
})

describe('App', () => {
  it('monta sin errores y muestra la marca', async () => {
    renderWithQuery(<App />)
    expect(screen.getByRole('img', { name: 'Monolegal' })).toBeInTheDocument()
    await screen.findByRole('heading', { name: 'Facturas' })
  })

  it('renderiza el área principal', async () => {
    renderWithQuery(<App />)
    expect(screen.getByRole('main')).toBeInTheDocument()
    await screen.findByRole('heading', { name: 'Facturas' })
  })
})
