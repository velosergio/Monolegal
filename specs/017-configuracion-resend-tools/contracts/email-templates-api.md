# Contrato API — Plantillas de email y prueba de envío

Feature: 017-configuracion-resend-tools · Tag OpenAPI: `Settings`

---

## GET `/api/settings/email/templates`

Lista las plantillas (efectivas: personalizadas o por defecto) y el catálogo de variables admitidas.

**200 OK**

```json
{
  "allowedVariables": [
    "factura.id", "factura.monto", "factura.vencimiento", "factura.estado",
    "factura.fechaEmision", "cliente.nombre", "cliente.email", "cliente.empresa", "enlacePago"
  ],
  "templates": [
    {
      "type": "reminder",
      "subject": "Recordatorio de pago — Factura {{factura.id}}",
      "body": "Estimado {{cliente.nombre}}, su factura {{factura.id}} por {{factura.monto}}...",
      "isCustomized": false
    },
    { "type": "paymentConfirmation", "subject": "...", "body": "...", "isCustomized": true },
    { "type": "deactivationNotice", "subject": "...", "body": "...", "isCustomized": false }
  ]
}
```

- `type`: `"reminder"` | `"paymentConfirmation"` | `"deactivationNotice"`.
- `isCustomized`: `true` si hay personalización persistida; `false` si se muestra el default.

---

## PUT `/api/settings/email/templates/{type}`

Actualiza una plantilla.

**Request body**

```json
{ "subject": "Recordatorio — {{factura.id}}", "body": "Hola {{cliente.nombre}}, ..." }
```

**Validación (`UpdateEmailTemplateValidator`)**
- `subject`: requerido, no vacío, solo variables del catálogo.
- `body`: requerido, no vacío, solo variables del catálogo.
- `{type}` de ruta ∈ {reminder, paymentConfirmation, deactivationNotice}.

**Respuestas**
- **204 No Content**: guardado.
- **400**: `{ "error": "Variable no admitida: {{factura.xyz}}." }` o subject/body vacío.
- **404**: `{type}` desconocido.

---

## POST `/api/settings/email/templates/{type}/reset`

Restablece una plantilla a su contenido por defecto (elimina la personalización).

**Respuestas**
- **204 No Content**.
- **404**: `{type}` desconocido.

---

## POST `/api/settings/email/templates/{type}/preview`

(Opcional, para vista previa server-side.) Renderiza una plantilla con datos de ejemplo. Si la vista previa se hace en cliente, este endpoint puede omitirse; se documenta para el caso de render consistente con backend.

**Request body**

```json
{ "subject": "...", "body": "..." }
```

**200 OK**

```json
{ "subject": "Recordatorio — INV-2026-001", "body": "Hola Ana Pérez, ..." }
```

- Sustituye con los valores de ejemplo deterministas (ver `ui-contracts.md`).
- **400** ante variables no admitidas (misma validación que PUT).

---

## POST `/api/settings/email/test`

Envía un correo de prueba a una dirección indicada, usando la configuración y la plantilla reales del proveedor activo (US3).

**Request body**

```json
{ "to": "prueba@dominio.com", "templateType": "reminder" }
```

**Validación (`SendTestEmailValidator`)**
- `to`: requerido, formato email.
- `templateType`: ∈ {reminder, paymentConfirmation, deactivationNotice}.

**Respuestas**
- **200 OK**: `{ "to": "prueba@dominio.com", "result": "sent" }`.
- **400**: validación de entrada.
- **200 con error de envío**: `{ "to": "...", "result": "failed", "message": "Credencial inválida (401)." }` — el envío fallido no es un 5xx; se reporta como resultado con motivo legible para el toast.

**Observabilidad**: log `{ provider, templateType, result, durationMs }`, sin secreto ni cuerpo completo.

---

## Mapeo a requisitos

| Endpoint | Requisitos |
|----------|-----------|
| GET /templates | FR-009, FR-010 |
| PUT /templates/{type} | FR-011, FR-013, FR-015, SC-004 |
| POST /templates/{type}/reset | FR-014 |
| POST /templates/{type}/preview | FR-012 |
| POST /test | FR-016, FR-017, FR-018, FR-019, SC-005 |
