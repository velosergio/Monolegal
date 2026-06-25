import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from '../src/App'

describe('App', () => {
  it('monta sin errores', async () => {
    render(<App />)
    // Verifica que el componente raíz monta correctamente
    expect(document.body).toBeTruthy()
    // Espera a que el efecto asíncrono de InvoiceTransitionsTab termine
    // para evitar actualizaciones de estado fuera de act(...).
    await screen.findByText('Tiempos de Transición de Facturas')
  })

  it('renderiza el encabezado de la aplicación', async () => {
    render(<App />)
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy()
    await screen.findByText('Tiempos de Transición de Facturas')
  })

  it('renderiza el contenido principal', async () => {
    render(<App />)
    expect(screen.getByRole('main')).toBeTruthy()
    await screen.findByText('Tiempos de Transición de Facturas')
  })
})
