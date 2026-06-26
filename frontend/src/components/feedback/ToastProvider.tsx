import { createContext, type ReactNode, useCallback, useMemo, useRef, useState } from 'react'

/** Variante de una notificación: éxito (transitoria) o error (persistente). */
export type ToastVariant = 'success' | 'error'

/** Una notificación tipo *toast* en la cola. */
export interface ToastMessage {
  id: string
  variant: ToastVariant
  message: string
  createdAt: number
}

/** API estable para emitir/descartar notificaciones. */
export interface ToastApi {
  success: (message: string) => void
  error: (message: string) => void
  dismiss: (id: string) => void
}

/** Tiempo de auto-cierre de los toasts de éxito (ms). Los de error no auto-cierran. */
const SUCCESS_TIMEOUT_MS = 4000

/**
 * Contextos separados para que los consumidores de la API (`useToast`) no se
 * re-rendericen cuando cambia la cola de toasts (solo el `ToastViewport` la observa).
 */
export const ToastApiContext = createContext<ToastApi | null>(null)
export const ToastStateContext = createContext<ToastMessage[]>([])

function createId(): string {
  const cryptoObj = globalThis.crypto as Crypto | undefined
  if (cryptoObj?.randomUUID) return cryptoObj.randomUUID()
  return `toast-${Date.now()}-${Math.random().toString(36).slice(2)}`
}

/**
 * Proveedor del sistema de *toast* in-house (sin dependencias de runtime).
 * Mantiene la cola y expone una API estable; programa el auto-cierre de los éxitos.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([])
  const timers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map())

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((toast) => toast.id !== id))
    const timer = timers.current.get(id)
    if (timer) {
      clearTimeout(timer)
      timers.current.delete(id)
    }
  }, [])

  const push = useCallback(
    (variant: ToastVariant, message: string) => {
      const id = createId()
      setToasts((prev) => [...prev, { id, variant, message, createdAt: Date.now() }])
      if (variant === 'success') {
        const timer = setTimeout(() => dismiss(id), SUCCESS_TIMEOUT_MS)
        timers.current.set(id, timer)
      }
    },
    [dismiss]
  )

  const api = useMemo<ToastApi>(
    () => ({
      success: (message: string) => push('success', message),
      error: (message: string) => push('error', message),
      dismiss,
    }),
    [push, dismiss]
  )

  return (
    <ToastApiContext.Provider value={api}>
      <ToastStateContext.Provider value={toasts}>{children}</ToastStateContext.Provider>
    </ToastApiContext.Provider>
  )
}
