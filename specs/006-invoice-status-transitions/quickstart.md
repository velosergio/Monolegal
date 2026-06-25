# Quickstart: Invoice Status Transitions

> **Spec**: 006-invoice-status-transitions | **Branch**: `006-invoice-status-transitions`

---

## ¿Qué hace esta feature?

Implementa un motor de ciclo de vida para facturas. Las facturas avanzan automáticamente a través de estados de recordatorio cuando vencen los días de gracia configurados, y pueden marcarse como pagadas manualmente en cualquier momento.

El objetivo es que el sistema notifique al cliente de forma progresiva (primer y segundo recordatorio) y eventualmente desactive la factura si no se paga, todo sin intervención manual del administrador.

---

## Levantamiento del entorno

```bash
docker-compose up -d
```

El backend queda disponible en `http://localhost` (puerto 80 por defecto).

---

## Estados de factura

| Valor numérico | Nombre            | Descripción                                      |
|:--------------:|-------------------|--------------------------------------------------|
| 0              | `Draft`           | Borrador, no entra al flujo de transiciones      |
| 1              | `Pending`         | Pendiente de pago — estado inicial activo        |
| 2              | `Pagado`          | Pagada manualmente o por integración             |
| 3              | `Overdue`         | Vencida (estado legacy, no usa el motor nuevo)   |
| 4              | `Cancelled`       | Cancelada (estado legacy)                        |
| 10             | `PrimerRecordatorio`  | Primer recordatorio enviado                  |
| 11             | `SegundoRecordatorio` | Segundo recordatorio enviado                 |
| 12             | `Desactivado`     | Desactivada por falta de pago                    |

---

## Flujo de transiciones

```
         [días configurados: PendingToFirstReminderDays]
Pending ──────────────────────────────────────────────► PrimerRecordatorio
                                                                │
                         [FirstToSecondReminderDays]            │
                                                                ▼
                                                    SegundoRecordatorio
                                                                │
                             [SecondToDeactivatedDays]          │
                                                                ▼
                                                          Desactivado
                                                          (estado final)

En cualquier estado activo (Pending / PrimerRecordatorio / SegundoRecordatorio):
──────────────────────────────────────────────────────────────────────────────►
                                POST /api/invoices/{id}/pay
                                                                          Pagado
                                                                     (estado final)
```

Las transiciones automáticas las ejecuta el **worker** una vez por hora.
Los estados `Pagado` y `Desactivado` son terminales: no reciben más transiciones.

---

## Endpoints disponibles

### GET /api/settings/invoice-transitions — Ver configuración

Devuelve los días de gracia configurados para cada tramo del flujo.

```bash
curl -X GET http://localhost/api/settings/invoice-transitions
```

**Respuesta 200:**
```json
{
  "pendingToFirstReminderDays": 3,
  "firstToSecondReminderDays": 3,
  "secondToDeactivatedDays": 3
}
```

Los valores por defecto son **3 días** para cada tramo.

---

### PUT /api/settings/invoice-transitions — Actualizar días de transición

Reemplaza la configuración completa. Los tres campos son obligatorios.

```bash
curl -X PUT http://localhost/api/settings/invoice-transitions \
  -H "Content-Type: application/json" \
  -d '{
    "pendingToFirstReminderDays": 7,
    "firstToSecondReminderDays": 5,
    "secondToDeactivatedDays": 10
  }'
```

**Respuesta exitosa:** `204 No Content` (sin body).

---

### POST /api/workers/trigger-transitions — Disparar ciclo manualmente

Ejecuta el mismo ciclo que corre el worker automático, sin esperar la siguiente hora. Útil para pruebas y para forzar el procesamiento inmediato.

```bash
curl -X POST http://localhost/api/workers/trigger-transitions
```

**Respuesta 200:**
```json
{
  "evaluated": 12,
  "transitioned": 3
}
```

- `evaluated` — número total de facturas candidatas revisadas (en estados `Pending`, `PrimerRecordatorio` o `SegundoRecordatorio`)
- `transitioned` — facturas que efectivamente cambiaron de estado en este ciclo

---

### POST /api/invoices/{id}/pay — Marcar factura como pagada

Cambia el estado de una factura a `Pagado` desde cualquier estado activo. Devuelve el nuevo estado y la fecha de la transición.

```bash
curl -X POST http://localhost/api/invoices/6849f2a3c1e0b700123abc45/pay
```

**Respuesta 200:**
```json
{
  "id": "6849f2a3c1e0b700123abc45",
  "status": 2,
  "lastStatusTransitionAt": "2025-06-24T15:30:00Z"
}
```

**Respuesta 404** — si la factura no existe.

**Respuesta 409** — si la factura ya estaba en estado `Pagado`:
```json
{
  "error": "No se puede marcar como pagada una factura que ya se encuentra en estado 'Pagado'."
}
```

---

## Escenarios de uso

### Escenario 1: Factura pasa de Pending → PrimerRecordatorio automáticamente

Este es el flujo principal del motor. El worker evalúa facturas cada hora.

**Pasos:**

1. Verificar la configuración actual:
   ```bash
   curl -X GET http://localhost/api/settings/invoice-transitions
   # → { "pendingToFirstReminderDays": 3, ... }
   ```

2. Localizar una factura en estado `Pending` (status = 1). En MongoDB, simular que han pasado los días de gracia actualizando `lastStatusTransitionAt` a una fecha anterior:
   ```js
   // MongoDB Shell
   db.invoices.updateOne(
     { _id: ObjectId("6849f2a3c1e0b700123abc45") },
     { $set: { lastStatusTransitionAt: new Date(Date.now() - 4 * 24 * 60 * 60 * 1000) } }
   )
   // 4 días atrás → supera el umbral de 3 días
   ```

3. Disparar el ciclo manualmente:
   ```bash
   curl -X POST http://localhost/api/workers/trigger-transitions
   # → { "evaluated": 1, "transitioned": 1 }
   ```

4. Verificar el nuevo estado de la factura (debe ser `PrimerRecordatorio` = 10).

---

### Escenario 2: Administrador cambia la configuración a 7 días

Ajustar el tiempo de gracia para que las facturas aguanten más antes de escalar.

**Pasos:**

1. Actualizar la configuración vía API (o desde la UI en `/settings`):
   ```bash
   curl -X PUT http://localhost/api/settings/invoice-transitions \
     -H "Content-Type: application/json" \
     -d '{
       "pendingToFirstReminderDays": 7,
       "firstToSecondReminderDays": 7,
       "secondToDeactivatedDays": 14
     }'
   # → 204 No Content
   ```

2. Confirmar que los nuevos valores persistieron:
   ```bash
   curl -X GET http://localhost/api/settings/invoice-transitions
   # → { "pendingToFirstReminderDays": 7, "firstToSecondReminderDays": 7, "secondToDeactivatedDays": 14 }
   ```

3. A partir de ahora el worker usará estos valores en el próximo ciclo. Las facturas que tenían menos de 7 días en estado `Pending` **no** transitarán aún.

---

### Escenario 3: Usuario paga una factura manualmente

El pago manual puede hacerse desde cualquier estado activo, sin importar cuántos días lleva.

**Pasos:**

1. Obtener el `id` de la factura a pagar (por ejemplo desde la lista de facturas).

2. Ejecutar el endpoint de pago:
   ```bash
   curl -X POST http://localhost/api/invoices/6849f2a3c1e0b700123abc45/pay
   ```

3. Verificar la respuesta — el campo `status` debe ser `2` (`Pagado`):
   ```json
   {
     "id": "6849f2a3c1e0b700123abc45",
     "status": 2,
     "lastStatusTransitionAt": "2025-06-24T15:30:00Z"
   }
   ```

4. Una segunda llamada al mismo endpoint devuelve `409` porque `Pagado` es un estado terminal.

---

### Escenario 4: Factura recorre todo el flujo hasta Desactivado

Útil para validar el flujo completo en un entorno de desarrollo.

**Pasos:**

1. Bajar la configuración a 1 día para que las transiciones sean rápidas:
   ```bash
   curl -X PUT http://localhost/api/settings/invoice-transitions \
     -H "Content-Type: application/json" \
     -d '{ "pendingToFirstReminderDays": 1, "firstToSecondReminderDays": 1, "secondToDeactivatedDays": 1 }'
   ```

2. Establecer `lastStatusTransitionAt` de una factura `Pending` a hace 2 días en MongoDB y disparar el worker → pasa a `PrimerRecordatorio` (10).

3. Establecer `lastStatusTransitionAt` a hace 2 días otra vez y disparar de nuevo → pasa a `SegundoRecordatorio` (11).

4. Ídem → pasa a `Desactivado` (12). La factura ya no participa en ciclos futuros.

---

## Worker automático

El `InvoiceTransitionsWorker` es un `BackgroundService` de ASP.NET Core que corre dentro del contenedor `backend`.

| Propiedad          | Valor                                      |
|--------------------|--------------------------------------------|
| Intervalo          | **1 hora** (configurable en código)        |
| Primera ejecución  | Inmediata al arrancar el proceso           |
| Estados evaluados  | `Pending`, `PrimerRecordatorio`, `SegundoRecordatorio` |
| Estados ignorados  | `Pagado`, `Desactivado`, `Draft`, `Overdue`, `Cancelled` |

**Cómo verificar en logs** — el worker emite logs estructurados con Serilog en tres momentos:

```
# Inicio de ciclo
[INF] InvoiceTransitionsWorker — inicio de ciclo. Timestamp=2025-06-24T15:00:00Z

# Por cada transición aplicada
[INF] Transición aplicada. InvoiceId=6849f2a3... De=Pending A=PrimerRecordatorio

# Fin de ciclo con resumen
[INF] InvoiceTransitionsWorker — fin de ciclo. Evaluadas=12 Transicionadas=3 DuracionMs=47
```

Para ver los logs en tiempo real:
```bash
docker-compose logs -f backend
```

Para filtrar solo las líneas del worker:
```bash
docker-compose logs -f backend | grep InvoiceTransitionsWorker
```
