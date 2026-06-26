# Contrato — `POST /api/invoices/transition/{id}` (respuesta extendida)

Transición manual de estado, **reutilizada** por el modal para ejecutar el cambio. La firma de la petición no cambia; la respuesta hereda los campos nuevos de `InvoiceDetailDto` (`statusHistory`, `allowedTransitions`).

Habilita: FR-015, FR-016, FR-017.

## Petición

```
POST /api/invoices/transition/{id}
Content-Type: application/json

{ "newStatus": "segundorecordatorio" }
```

- `id` (path, requerido).
- `newStatus` (body, requerido): estado destino en minúscula; debe pertenecer a los `allowedTransitions` de la factura.

## Respuestas

| Código | Caso |
|--------|------|
| `200 OK` | Transición aplicada. Devuelve `InvoiceDetailDto` **con** `statusHistory` (incluye el evento recién creado, `source: "manual"`) y `allowedTransitions` recalculados para el nuevo estado. |
| `400 Bad Request` | `newStatus` ausente/ inválido, o transición no permitida por la matriz de dominio (sin cambios). |
| `404 Not Found` | `id` inexistente o con formato inválido. |

## Reglas

- La transición a `pagado` delega en la regla de pago existente; `source` queda `manual`.
- El envío de correo de notificación (spec 013) ocurre tras la transición; un fallo de envío no revierte el cambio ni la respuesta.
- El historial registrado lleva `source: "manual"` (vs. `automatic` del worker).

## Uso por el frontend (research D5)

- `useTransitionInvoice` (`useMutation`) → en `onSuccess` invalida `['invoice', id]`, `['invoices']`, `['invoice-stats']`; opcionalmente fija el `InvoiceDetailDto` devuelto en el cache de `['invoice', id]` para actualización inmediata del modal.
- Estado ocupado durante la mutación (evita doble envío). Un `400` muestra mensaje legible y refresca el detalle.

## Pruebas de contrato (xUnit)

- Transición válida → 200, `status` actualizado, `statusHistory` contiene el nuevo evento con `source: "manual"`, `allowedTransitions` recalculados.
- Transición no permitida → 400, sin cambios en estado ni historial.
- `id` inexistente → 404.
