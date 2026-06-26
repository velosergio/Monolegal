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
    } as Response)
  )
)

// jsdom/Node no siempre expone localStorage; lo polirellenamos en memoria para
// que el estado persistido (tema, sidebar colapsado) no rompa los tests.
if (!globalThis.localStorage) {
  const store = new Map<string, string>()
  vi.stubGlobal('localStorage', {
    getItem: (key: string) => (store.has(key) ? (store.get(key) as string) : null),
    setItem: (key: string, value: string) => {
      store.set(key, String(value))
    },
    removeItem: (key: string) => {
      store.delete(key)
    },
    clear: () => {
      store.clear()
    },
    key: (index: number) => Array.from(store.keys())[index] ?? null,
    get length() {
      return store.size
    },
  })
}

// jsdom no implementa matchMedia (lo usa Motion para prefers-reduced-motion).
// Por defecto reportamos "sin preferencia de movimiento reducido".
if (!window.matchMedia) {
  vi.stubGlobal(
    'matchMedia',
    vi.fn((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }))
  )
}

// Polyfills que Radix UI (Select/Dialog) requiere en jsdom.
if (!window.ResizeObserver) {
  vi.stubGlobal(
    'ResizeObserver',
    class {
      observe() {}
      unobserve() {}
      disconnect() {}
    }
  )
}

Element.prototype.scrollIntoView = vi.fn()
Element.prototype.hasPointerCapture = vi.fn(() => false)
Element.prototype.setPointerCapture = vi.fn()
Element.prototype.releasePointerCapture = vi.fn()
