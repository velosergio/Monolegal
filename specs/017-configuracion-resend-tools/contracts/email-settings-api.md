# Contrato API — Configuración del proveedor de email

Feature: 017-configuracion-resend-tools · Tag OpenAPI: `Settings`

Base: endpoints Minimal API, JSON, enums en minúscula (política existente `LowerCaseNamingPolicy`). Acceso Admin-only (capa de autenticación previa). **Ningún endpoint devuelve ni acepta valores secretos** (contraseña SMTP, API key Resend).

---

## GET `/api/settings/email`

Devuelve la configuración no secreta del email y el estado de la credencial del proveedor activo.

**200 OK**

```json
{
  "activeProvider": "smtp",
  "fromAddress": "no-reply@monolegal.local",
  "fromName": "Monolegal",
  "smtp": {
    "host": "smtp.example.com",
    "port": 587,
    "username": "apikey",
    "useStartTls": true
  },
  "resend": {
    "fromDomain": "mg.monolegal.co"
  },
  "credentialStatus": "configured"
}
```

- `activeProvider`: `"smtp"` | `"resend"`.
- `credentialStatus`: `"notConfigured"` | `"configured"` | `"validated"` (del proveedor activo; ver data-model §3).
- Nunca incluye `password` ni `apiKey`.

---

## PUT `/api/settings/email`

Actualiza la configuración no secreta. Reemplaza el bloque `Email` de `SystemSettings` preservando el resto del agregado.

**Request body**

```json
{
  "activeProvider": "resend",
  "fromAddress": "facturas@monolegal.co",
  "fromName": "Monolegal Facturación",
  "smtp": { "host": "smtp.example.com", "port": 587, "username": "apikey", "useStartTls": true },
  "resend": { "fromDomain": "mg.monolegal.co" }
}
```

**Validación (FluentValidation — `UpdateEmailSettingsValidator`)**
- `fromAddress`: requerido, formato email.
- `fromName`: requerido, no vacío.
- Si `activeProvider == "smtp"`: `smtp.host` requerido; `smtp.port` ∈ [1,65535].
- Si `activeProvider == "resend"`: `resend.fromDomain` requerido.
- Rechaza cualquier propiedad secreta (no forma parte del contrato).

**Respuestas**
- **204 No Content**: guardado correcto (patrón de `UpdateInvoiceTransitions`).
- **400 Bad Request**: `ValidationProblem` / `{ "error": "..." }` con motivo legible.

> Efecto runtime: el worker in-process leerá `activeProvider` y los parámetros no secretos en su siguiente ciclo/envío (FR-002b). Los secretos siguen viniendo del entorno.

---

## POST `/api/settings/email/validate`

Valida la configuración del **proveedor activo** usando la credencial del entorno, **sin enviar correo** (D4).

**Request body**: vacío (usa el proveedor activo persistido). Opcionalmente `{ "provider": "resend" }` para validar un proveedor específico antes de activarlo.

**200 OK**

```json
{ "provider": "resend", "status": "validated", "message": null }
```

**200 OK (fallo de credencial)**

```json
{ "provider": "resend", "status": "invalid", "message": "API key rechazada (401)." }
```

- `status`: `"validated"` | `"invalid"` | `"notConfigured"`.
- Errores de red/proveedor caído ⇒ `status: "invalid"` con `message` legible.
- **400**: solo si el `provider` solicitado es desconocido.

**Observabilidad**: log estructurado Serilog `{ provider, result, durationMs }` sin incluir el secreto.

---

## Mapeo a requisitos

| Endpoint | Requisitos |
|----------|-----------|
| GET /email | FR-001, FR-002, FR-008 (estado), SC-007 |
| PUT /email | FR-003, FR-005, FR-006, FR-002a, FR-002b, SC-003 |
| POST /email/validate | FR-004, SC-002 |
