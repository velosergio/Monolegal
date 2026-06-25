# Interface Contracts: Invoice Status Transitions

## REST API Endpoints

### Configuración de Tiempos

**GET /api/settings/invoice-transitions**
Retorna la configuración actual de días.
```json
{
  "pendingToFirstReminderDays": 3,
  "firstToSecondReminderDays": 5,
  "secondToDeactivatedDays": 7
}
```

**PUT /api/settings/invoice-transitions**
Actualiza la configuración de días.
```json
// Payload
{
  "pendingToFirstReminderDays": 4,
  "firstToSecondReminderDays": 4,
  "secondToDeactivatedDays": 4
}
// Response: 204 No Content
```

### Transición de Estado Manual / Pago

**POST /api/invoices/{id}/pay**
Marca una factura como pagada de manera explícita (simulación manual o webhook de pasarela).
```json
// Response: 200 OK
{
  "id": "...",
  "status": "pagado"
}
```
