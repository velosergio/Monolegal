import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { InvoiceItemForm } from '../types'
import { formatAmount } from '../utils'

interface InvoiceItemsEditorProps {
  items: InvoiceItemForm[]
  onChange: (items: InvoiceItemForm[]) => void
  /** Mensaje de error de validación a nivel de la lista (p. ej. sin líneas). */
  error?: string
  disabled?: boolean
}

/** Subtotal de una línea: cantidad × precio unitario. */
function lineSubtotal(item: InvoiceItemForm): number {
  return (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0)
}

/**
 * Editor de líneas de detalle (spec 018, RF-011). Permite añadir/quitar líneas y muestra el
 * subtotal por línea y el total general, ambos derivados y de solo lectura.
 */
export function InvoiceItemsEditor({ items, onChange, error, disabled }: InvoiceItemsEditorProps) {
  const total = items.reduce((sum, item) => sum + lineSubtotal(item), 0)

  const updateItem = (index: number, patch: Partial<InvoiceItemForm>) => {
    onChange(items.map((item, i) => (i === index ? { ...item, ...patch } : item)))
  }

  const addItem = () => onChange([...items, { description: '', quantity: 1, unitPrice: 0 }])
  const removeItem = (index: number) => onChange(items.filter((_, i) => i !== index))

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">Líneas de detalle</span>
        <Button type="button" size="sm" variant="outline" onClick={addItem} disabled={disabled}>
          <Plus className="h-4 w-4" aria-hidden="true" />
          Añadir línea
        </Button>
      </div>

      <div className="flex flex-col gap-2">
        {items.map((item, index) => (
          // biome-ignore lint/suspicious/noArrayIndexKey: las líneas no tienen id estable en el formulario
          <div key={index} className="flex items-end gap-2">
            <label className="flex flex-1 flex-col gap-1 text-xs">
              <span className="text-muted-foreground">Descripción</span>
              <Input
                value={item.description}
                onChange={(e) => updateItem(index, { description: e.target.value })}
                disabled={disabled}
                aria-label={`Descripción de la línea ${index + 1}`}
              />
            </label>
            <label className="flex w-20 flex-col gap-1 text-xs">
              <span className="text-muted-foreground">Cant.</span>
              <Input
                type="number"
                min="0"
                step="1"
                value={item.quantity}
                onChange={(e) => updateItem(index, { quantity: Number(e.target.value) })}
                disabled={disabled}
                aria-label={`Cantidad de la línea ${index + 1}`}
              />
            </label>
            <label className="flex w-28 flex-col gap-1 text-xs">
              <span className="text-muted-foreground">Precio unit.</span>
              <Input
                type="number"
                min="0"
                step="0.01"
                value={item.unitPrice}
                onChange={(e) => updateItem(index, { unitPrice: Number(e.target.value) })}
                disabled={disabled}
                aria-label={`Precio unitario de la línea ${index + 1}`}
              />
            </label>
            <div className="flex w-24 flex-col gap-1 text-right text-xs">
              <span className="text-muted-foreground">Subtotal</span>
              <span className="h-10 px-1 py-2 tabular-nums">
                {formatAmount(lineSubtotal(item))}
              </span>
            </div>
            <Button
              type="button"
              size="icon"
              variant="ghost"
              onClick={() => removeItem(index)}
              disabled={disabled || items.length <= 1}
              aria-label={`Eliminar la línea ${index + 1}`}
            >
              <Trash2 className="h-4 w-4" aria-hidden="true" />
            </Button>
          </div>
        ))}
      </div>

      {error ? (
        <span role="alert" className="text-xs text-destructive">
          {error}
        </span>
      ) : null}

      <div className="flex items-center justify-between border-t pt-2">
        <span className="text-sm font-medium">Total</span>
        <span className="font-heading text-lg font-bold tabular-nums">{formatAmount(total)}</span>
      </div>
    </div>
  )
}
