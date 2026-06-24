import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from '../src/App'

describe('App', () => {
  it('monta sin errores', () => {
    render(<App />)
    // Verifica que el componente raíz monta correctamente
    expect(document.body).toBeTruthy()
  })

  it('renderiza el encabezado de la aplicación', () => {
    render(<App />)
    expect(screen.getByRole('heading', { level: 1 })).toBeTruthy()
  })

  it('renderiza el contenido principal', () => {
    render(<App />)
    expect(screen.getByRole('main')).toBeTruthy()
  })
})
