import { X } from 'lucide-react'
import { domAnimation, LazyMotion, m, useReducedMotion } from 'motion/react'
import { use } from 'react'
import { motionTransition, toastInOut } from '@/lib/motion'
import { cn } from '@/lib/utils'
import { ToastApiContext, type ToastMessage } from './ToastProvider'
import { useToastState } from './useToast'

/** Clases visuales por variante (coherentes con el tema y dark mode). */
const VARIANT_CLASSES: Record<ToastMessage['variant'], string> = {
  success:
    'border-lime-300 bg-lime-50 text-lime-900 dark:border-lime-900 dark:bg-lime-950 dark:text-lime-100',
  error:
    'border-destructive/40 bg-destructive/10 text-destructive dark:bg-destructive/20 dark:text-destructive-foreground',
}

/**
 * Región visual de notificaciones (una sola instancia, montada en `AppShell`).
 * Los toasts de éxito usan `role="status"` (polite) y los de error `role="alert"`
 * (assertive). La entrada/salida se anima con Motion respetando `prefers-reduced-motion`.
 */
export function ToastViewport() {
  const toasts = useToastState()
  // Lectura tolerante: el viewport puede montarse en layouts sin provider (tests);
  // sin provider no hay toasts, por lo que `dismiss` nunca se invoca.
  const api = use(ToastApiContext)
  const dismiss = api?.dismiss ?? (() => {})
  const reduced = useReducedMotion()

  return (
    <div
      className="pointer-events-none fixed inset-x-0 bottom-0 z-50 flex flex-col items-center gap-2 p-4 sm:inset-x-auto sm:right-0 sm:items-end"
      data-testid="toast-viewport"
    >
      <LazyMotion features={domAnimation}>
        {toasts.map((toast) => (
          <m.div
            key={toast.id}
            variants={toastInOut}
            initial="hidden"
            animate="visible"
            transition={motionTransition(reduced)}
            role={toast.variant === 'error' ? 'alert' : 'status'}
            className={cn(
              'pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-md border px-4 py-3 text-sm shadow-lg',
              VARIANT_CLASSES[toast.variant]
            )}
          >
            <span className="flex-1 leading-snug">{toast.message}</span>
            <button
              type="button"
              onClick={() => dismiss(toast.id)}
              aria-label="Cerrar notificación"
              className="-mr-1 -mt-0.5 inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-[2px] opacity-70 transition-opacity hover:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-current"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          </m.div>
        ))}
      </LazyMotion>
    </div>
  )
}
