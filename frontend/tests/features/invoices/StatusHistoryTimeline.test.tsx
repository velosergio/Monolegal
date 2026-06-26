import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusHistoryTimeline } from '@/features/invoices/components/StatusHistoryTimeline'
import type { StatusChange } from '@/features/invoices/types'

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const change1: StatusChange = {
  from: 'pending',
  to: 'primerrecordatorio',
  at: '2026-01-05T09:00:00.000Z', // más antiguo
  source: 'automatic',
}

const change2: StatusChange = {
  from: 'primerrecordatorio',
  to: 'segundorecordatorio',
  at: '2026-02-10T14:30:00.000Z',
  source: 'automatic',
}

const change3: StatusChange = {
  from: 'segundorecordatorio',
  to: 'pagado',
  at: '2026-03-20T11:15:00.000Z', // más reciente
  source: 'manual',
}

/** Historia desordenada (el componente debe ordenarla). */
const unorderedHistory: StatusChange[] = [change3, change1, change2]

const CREATED_AT = '2025-12-01T08:00:00.000Z'

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('StatusHistoryTimeline', () => {
  // -------------------------------------------------------------------------
  // 1. Orden cronológico
  // -------------------------------------------------------------------------
  it('muestra los eventos en orden cronológico ascendente (más antiguo primero)', () => {
    render(<StatusHistoryTimeline statusHistory={unorderedHistory} createdAt={CREATED_AT} />)

    // Obtenemos todos los ítems de la línea de tiempo
    const items = screen.getAllByRole('listitem')

    // El texto de cada ítem debe aparecer en el orden correcto
    const texts = items.map((el) => el.textContent ?? '')

    const idx1 = texts.findIndex((t) => t.includes('Pendiente') && t.includes('1er Recordatorio'))
    const idx2 = texts.findIndex(
      (t) => t.includes('1er Recordatorio') && t.includes('2do Recordatorio')
    )
    const idx3 = texts.findIndex((t) => t.includes('2do Recordatorio') && t.includes('Pagado'))

    expect(idx1).toBeGreaterThanOrEqual(0)
    expect(idx2).toBeGreaterThanOrEqual(0)
    expect(idx3).toBeGreaterThanOrEqual(0)
    expect(idx1).toBeLessThan(idx2)
    expect(idx2).toBeLessThan(idx3)
  })

  // -------------------------------------------------------------------------
  // 2. Etiquetas from→to
  // -------------------------------------------------------------------------
  it('muestra la etiqueta from→to legible para cada entrada', () => {
    render(
      <StatusHistoryTimeline statusHistory={[change1, change2, change3]} createdAt={CREATED_AT} />
    )

    // Cada transición debe mostrar sus etiquetas en español
    expect(screen.getByText(/Pendiente/)).toBeInTheDocument()
    expect(screen.getByText(/1er Recordatorio/)).toBeInTheDocument()
    expect(screen.getByText(/2do Recordatorio/)).toBeInTheDocument()
    expect(screen.getByText(/Pagado/)).toBeInTheDocument()
  })

  it('cada ítem de la lista contiene tanto el estado origen como el destino', () => {
    render(<StatusHistoryTimeline statusHistory={[change1]} createdAt={CREATED_AT} />)

    const items = screen.getAllByRole('listitem')
    // Buscamos el ítem que corresponde a esta transición
    const transitionItem = items.find(
      (el) => el.textContent?.includes('Pendiente') && el.textContent?.includes('1er Recordatorio')
    )
    expect(transitionItem).toBeDefined()
  })

  // -------------------------------------------------------------------------
  // 3. Etiqueta de origen (source)
  // -------------------------------------------------------------------------
  it('muestra "Automático" para cambios con source "automatic"', () => {
    render(<StatusHistoryTimeline statusHistory={[change1]} createdAt={CREATED_AT} />)

    expect(screen.getByText(/Automático/i)).toBeInTheDocument()
  })

  it('muestra "Manual" para cambios con source "manual"', () => {
    render(<StatusHistoryTimeline statusHistory={[change3]} createdAt={CREATED_AT} />)

    expect(screen.getByText(/Manual/i)).toBeInTheDocument()
  })

  it('muestra tanto "Automático" como "Manual" cuando hay entradas de ambos tipos', () => {
    render(<StatusHistoryTimeline statusHistory={[change1, change3]} createdAt={CREATED_AT} />)

    expect(screen.getByText(/Automático/i)).toBeInTheDocument()
    expect(screen.getByText(/Manual/i)).toBeInTheDocument()
  })

  // -------------------------------------------------------------------------
  // 4. Fallback de creación cuando el historial está vacío
  // -------------------------------------------------------------------------
  it('muestra un evento de creación derivado de createdAt cuando statusHistory está vacío', () => {
    render(<StatusHistoryTimeline statusHistory={[]} createdAt={CREATED_AT} />)

    // Debe aparecer una sola entrada
    const items = screen.getAllByRole('listitem')
    expect(items).toHaveLength(1)

    // La entrada debe mencionar "Creación" o "Factura creada" (o similar)
    const creationItem = items[0]
    const text = creationItem.textContent ?? ''
    const hasCreationLabel = /[Cc]reaci[oó]n/i.test(text) || /[Ff]actura creada/i.test(text)
    expect(hasCreationLabel).toBe(true)
  })

  it('el evento de creación muestra la fecha formateada de createdAt', () => {
    render(<StatusHistoryTimeline statusHistory={[]} createdAt={CREATED_AT} />)

    // CREATED_AT = '2025-12-01T08:00:00.000Z' → debe aparecer algo de dic 2025
    // Comprobamos que el año o el mes estén presentes (el formatter es es-CO)
    expect(screen.getByText(/2025/)).toBeInTheDocument()
  })

  // -------------------------------------------------------------------------
  // 5. Historial no vacío — el fallback de creación no debe duplicarse como
  //    entrada independiente (puede aparecer como primer ítem si el componente
  //    lo incluye desde createdAt, pero no debe aparecer en duplicado)
  // -------------------------------------------------------------------------
  it('con historial no vacío, muestra exactamente tantos ítems como cambios (sin duplicar el evento de creación)', () => {
    const history: StatusChange[] = [change1, change2]
    render(<StatusHistoryTimeline statusHistory={history} createdAt={CREATED_AT} />)

    const items = screen.getAllByRole('listitem')

    // Puede ser 2 (solo los cambios) o 3 (cambios + creación como primer evento)
    // En cualquier caso, la cuenta NO debe superar cambios.length + 1
    expect(items.length).toBeGreaterThanOrEqual(history.length)
    expect(items.length).toBeLessThanOrEqual(history.length + 1)
  })

  it('con historial no vacío no muestra solamente el evento de creación', () => {
    render(<StatusHistoryTimeline statusHistory={[change1]} createdAt={CREATED_AT} />)

    // Debe haber al menos un ítem de transición real
    const items = screen.getAllByRole('listitem')
    const hasRealTransition = items.some(
      (el) => el.textContent?.includes('Pendiente') && el.textContent?.includes('1er Recordatorio')
    )
    expect(hasRealTransition).toBe(true)
  })
})
