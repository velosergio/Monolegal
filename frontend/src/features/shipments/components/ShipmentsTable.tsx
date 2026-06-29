import { Ban, RotateCw } from 'lucide-react'
import { useToast } from '@/components/feedback/useToast'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { useResendInvoice } from '../api/useShipmentMutations'
import type { Shipment } from '../types'
import { formatDate, shortId } from '../utils'
import { ShipmentStatusBadge } from './ShipmentStatusBadge'

interface ShipmentsTableProps {
  shipments: Shipment[]
  /** Abre la confirmación de cancelación de un envío pendiente. */
  onCancelShipment: (shipment: Shipment) => void
}

/**
 * Tabla de envíos (spec 019): ID, Cliente, Email, Estado de envío, Último intento, Reintentos y
 * acciones por fila (Reenviar, Cancelar). Mientras una fila se reenvía muestra el estado transitorio
 * "reintentando"; "Cancelar" sólo se habilita para envíos pendientes.
 */
export function ShipmentsTable({ shipments, onCancelShipment }: ShipmentsTableProps) {
  const toast = useToast()
  const resend = useResendInvoice()

  async function handleResend(shipment: Shipment) {
    try {
      const result = await resend.mutateAsync(shipment.id)
      if (result.sendStatus === 'sent') {
        toast.success(`Notificación reenviada a ${shipment.clientName}.`)
      } else if (result.sendStatus === 'failed') {
        toast.error(result.lastError ?? 'El reenvío falló. Inténtalo de nuevo.')
      } else {
        toast.success(`Envío de ${shipment.clientName} omitido (sin correo o plantilla).`)
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'No se pudo reenviar la notificación.')
    }
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-[120px]">ID</TableHead>
          <TableHead>Cliente</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Estado de envío</TableHead>
          <TableHead>Último intento</TableHead>
          <TableHead className="text-right">Reintentos</TableHead>
          <TableHead className="text-right">Acciones</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {shipments.map((shipment) => {
          const isResending = resend.isPending && resend.variables === shipment.id
          const canCancel = shipment.sendStatus === 'pending'

          return (
            <TableRow key={shipment.id}>
              <TableCell className="font-mono text-xs" title={shipment.id}>
                {shortId(shipment.id)}
              </TableCell>
              <TableCell className="font-medium" title={shipment.clientId}>
                {shipment.clientName}
              </TableCell>
              <TableCell className="text-muted-foreground">
                {shipment.clientEmail ?? <span className="italic">Sin correo</span>}
              </TableCell>
              <TableCell>
                <ShipmentStatusBadge status={isResending ? 'retrying' : shipment.sendStatus} />
              </TableCell>
              <TableCell className="text-muted-foreground" title={shipment.lastError ?? undefined}>
                {formatDate(shipment.lastAttemptAt)}
              </TableCell>
              <TableCell className="text-right tabular-nums">{shipment.retryCount}</TableCell>
              <TableCell className="text-right">
                <div className="flex justify-end gap-2">
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isResending || !shipment.clientEmail}
                    onClick={() => handleResend(shipment)}
                    aria-label={`Reenviar la notificación de ${shipment.clientName}`}
                  >
                    <RotateCw className="h-4 w-4" aria-hidden="true" />
                    {isResending ? 'Reenviando…' : 'Reenviar'}
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={!canCancel}
                    onClick={() => onCancelShipment(shipment)}
                    aria-label={`Cancelar el envío de ${shipment.clientName}`}
                  >
                    <Ban className="h-4 w-4" aria-hidden="true" />
                    Cancelar
                  </Button>
                </div>
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
