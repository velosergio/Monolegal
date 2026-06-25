// Setup global de pruebas: registra los matchers de jest-dom
// (toBeInTheDocument, toHaveTextContent, etc.) para Testing Library.
import '@testing-library/jest-dom'
import { vi } from 'vitest'

// En jsdom no existe una URL base, por lo que fetch con rutas relativas
// lanza ERR_INVALID_URL. Mockeamos fetch globalmente para que los
// componentes que llaman a la API no rompan tests que no los ejercitan.
vi.stubGlobal(
  'fetch',
  vi.fn(() =>
    Promise.resolve({
      ok: true,
      json: () => Promise.resolve({}),
    } as Response),
  ),
)
