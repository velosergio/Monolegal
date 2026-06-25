import type React from 'react'
import { useEffect, useState } from 'react'
import { getInvoiceTransitions, type InvoiceTransitionsConfig } from '../api/getInvoiceTransitions'
import { updateInvoiceTransitions } from '../api/updateInvoiceTransitions'

export const InvoiceTransitionsTab: React.FC = () => {
  const [config, setConfig] = useState<InvoiceTransitionsConfig>({
    pendingToFirstReminderDays: 3,
    firstToSecondReminderDays: 3,
    secondToDeactivatedDays: 3,
  })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [message, setMessage] = useState('')

  useEffect(() => {
    getInvoiceTransitions()
      .then((data) => {
        setConfig(data)
        setLoading(false)
      })
      .catch((err) => {
        console.error(err)
        setLoading(false)
      })
  }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setConfig((prev) => ({
      ...prev,
      [name]: parseInt(value, 10) || 0,
    }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setMessage('')
    try {
      await updateInvoiceTransitions(config)
      setMessage('Configuración guardada exitosamente.')
    } catch (_err) {
      setMessage('Error al guardar la configuración.')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div>Cargando...</div>

  return (
    <div className="p-4 bg-white dark:bg-gray-800 rounded shadow">
      <h2 className="text-xl font-semibold mb-4">Tiempos de Transición de Facturas</h2>
      {message && <div className="mb-4 text-green-600">{message}</div>}
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label htmlFor="pendingToFirstReminderDays" className="block text-sm font-medium">
            Días de Pendiente a 1er Recordatorio
          </label>
          <input
            id="pendingToFirstReminderDays"
            type="number"
            name="pendingToFirstReminderDays"
            value={config.pendingToFirstReminderDays}
            onChange={handleChange}
            className="mt-1 block w-full border rounded p-2"
            min="1"
          />
        </div>
        <div>
          <label htmlFor="firstToSecondReminderDays" className="block text-sm font-medium">
            Días de 1er a 2do Recordatorio
          </label>
          <input
            id="firstToSecondReminderDays"
            type="number"
            name="firstToSecondReminderDays"
            value={config.firstToSecondReminderDays}
            onChange={handleChange}
            className="mt-1 block w-full border rounded p-2"
            min="1"
          />
        </div>
        <div>
          <label htmlFor="secondToDeactivatedDays" className="block text-sm font-medium">
            Días de 2do Recordatorio a Desactivado
          </label>
          <input
            id="secondToDeactivatedDays"
            type="number"
            name="secondToDeactivatedDays"
            value={config.secondToDeactivatedDays}
            onChange={handleChange}
            className="mt-1 block w-full border rounded p-2"
            min="1"
          />
        </div>
        <button
          type="submit"
          disabled={saving}
          className="bg-blue-600 text-white px-4 py-2 rounded disabled:opacity-50"
        >
          {saving ? 'Guardando...' : 'Guardar'}
        </button>
      </form>
    </div>
  )
}
