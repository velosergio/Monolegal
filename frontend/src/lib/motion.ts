import type { Transition, Variants } from 'motion/react'

/**
 * Duraciones y variantes de animación centralizadas (Motion).
 *
 * Las animaciones deben respetar `prefers-reduced-motion`: los componentes
 * consultan `useReducedMotion()` de Motion y, cuando es `true`, usan
 * `REDUCED_TRANSITION` (duración ~0) o desactivan los desplazamientos.
 */

export const DURATION = {
  fast: 0.15,
  base: 0.25,
  slow: 0.35,
} as const

const EASE_OUT: Transition['ease'] = [0.16, 1, 0.3, 1]

/** Transición instantánea para usuarios con movimiento reducido. */
export const REDUCED_TRANSITION: Transition = { duration: 0 }

/** Entrada sutil del contenido (skeleton → datos): fade + leve translate. */
export const fadeInUp: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: { opacity: 1, y: 0 },
}

/** Entrada de una barra de gráfico: crece desde 0 en el eje correspondiente. */
export const growBar: Variants = {
  hidden: { scaleX: 0 },
  visible: { scaleX: 1 },
}

/**
 * Devuelve la transición de un gráfico con un retardo escalonado por índice,
 * desactivando el retardo y la duración cuando el movimiento está reducido.
 */
export function chartTransition(reduced: boolean | null, index = 0): Transition {
  if (reduced) return REDUCED_TRANSITION
  return { duration: DURATION.slow, ease: EASE_OUT, delay: index * 0.05 }
}

/**
 * Devuelve la transición adecuada según la preferencia de movimiento reducido.
 */
export function motionTransition(reduced: boolean | null): Transition {
  return reduced ? REDUCED_TRANSITION : { duration: DURATION.base, ease: EASE_OUT }
}
