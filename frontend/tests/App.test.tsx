import { screen, waitFor } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it } from 'vitest'
import App from '../src/App'
import { mockFetchJson, renderWithQuery } from './test-utils'

const stats = {
  totalInvoices: 3,
  byStatus: { pending: 2, pagado: 1 },
  byClient: { 'cliente-001': 3 },
}

// Precarga el módulo diferido del Dashboard (ahora la pantalla de inicio) para
// que React.lazy resuelva de inmediato.
beforeAll(async () => {
  await import('@/features/dashboard/components/DashboardPage')
})

afterEach(() => {
  window.history.pushState({}, '', '/')
})

describe('App · ruteo', () => {
  it('la ruta raíz "/" muestra el Dashboard como pantalla de inicio', async () => {
    window.history.pushState({}, '', '/')
    mockFetchJson(stats)
    renderWithQuery(<App />)

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Monolegal' })).toBeInTheDocument()
  })

  it('una ruta desconocida redirige a "/" (dashboard)', async () => {
    window.history.pushState({}, '', '/ruta-inexistente')
    mockFetchJson(stats)
    renderWithQuery(<App />)

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()
    await waitFor(() => {
      expect(window.location.pathname).toBe('/')
    })
  })

  it('la antigua ruta /dashboard (eliminada) redirige a "/"', async () => {
    window.history.pushState({}, '', '/dashboard')
    mockFetchJson(stats)
    renderWithQuery(<App />)

    expect(await screen.findByRole('heading', { name: 'Dashboard' })).toBeInTheDocument()
    await waitFor(() => {
      expect(window.location.pathname).toBe('/')
    })
  })
})
