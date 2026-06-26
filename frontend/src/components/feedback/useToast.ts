import { use } from 'react'
import {
  type ToastApi,
  ToastApiContext,
  type ToastMessage,
  ToastStateContext,
} from './ToastProvider'

/**
 * Acceso a la API de notificaciones (`success`/`error`/`dismiss`).
 * Lanza si se usa fuera de `<ToastProvider>`.
 */
export function useToast(): ToastApi {
  const api = use(ToastApiContext)
  if (!api) {
    throw new Error('useToast debe usarse dentro de <ToastProvider>')
  }
  return api
}

/** Lista actual de toasts (para el `ToastViewport`). */
export function useToastState(): ToastMessage[] {
  return use(ToastStateContext)
}
