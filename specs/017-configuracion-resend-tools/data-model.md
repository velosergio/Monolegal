# Phase 1 — Data Model

Feature: 017-configuracion-resend-tools · Fecha: 2026-06-26

Modelo de datos derivado de `spec.md` (Key Entities) y `research.md`. Se extiende el agregado existente `SystemSettings` y se reutiliza el estado de notificación ya embebido en `Invoice`. No se introducen colecciones nuevas.

---

## 1. SystemSettings (extensión del agregado existente)

Documento singleton en colección `SystemSettings` (`Id = "singleton-settings"`). Se añaden dos objetos: `Email` y `EmailTemplates`. Se conserva `InvoiceTransitions` y `UpdatedAt`.

```
SystemSettings
├── Id: string                         (existente, singleton)
├── InvoiceTransitions: InvoiceTransitionsConfig  (existente)
├── Email: EmailSettings               (NUEVO)
├── EmailTemplates: EmailTemplateSet   (NUEVO)
└── UpdatedAt: DateTime                 (existente; se actualiza en cada cambio)
```

Métodos de dominio nuevos (mutan + tocan `UpdatedAt`):
- `UpdateEmailSettings(EmailSettings settings)`
- `UpdateTemplate(NotificationType type, string subject, string body)`
- `ResetTemplate(NotificationType type)` — elimina la personalización (vuelve a default por fallback)

### 1.1 EmailSettings (solo datos NO secretos)

| Campo | Tipo | Reglas / Notas |
|-------|------|----------------|
| `ActiveProvider` | `EmailProvider` (enum) | `Smtp` \| `Resend`. Determina el proveedor de envío en runtime. Default `Smtp`. |
| `FromAddress` | string | Dirección remitente. Formato email válido. Requerido para guardar. |
| `FromName` | string | Nombre visible del remitente. No vacío. |
| `Smtp` | `SmtpSettings` | Parámetros no secretos de SMTP (ver 1.1.1). |
| `Resend` | `ResendSettings` | Parámetros no secretos de Resend (ver 1.1.2). |

> **Nunca** contiene `Password` ni `ApiKey`. Esos secretos viven solo en entorno (`EmailOptions`).

#### 1.1.1 SmtpSettings (no secreto)

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Host` | string? | Host SMTP. Requerido si `ActiveProvider == Smtp`. |
| `Port` | int | 1–65535. Default 587. |
| `Username` | string? | Identificador (no secreto). Opcional. |
| `UseStartTls` | bool | Default `true`. |

#### 1.1.2 ResendSettings (no secreto)

| Campo | Tipo | Reglas |
|-------|------|--------|
| `FromDomain` | string? | Dominio remitente verificado en Resend. Requerido si `ActiveProvider == Resend`. |

### 1.2 EmailTemplateSet / EmailTemplate

`EmailTemplateSet` = mapa por `NotificationType` → `EmailTemplate`. Tipos: `Reminder`, `PaymentConfirmation`, `DeactivationNotice`.

| Campo | Tipo | Reglas |
|-------|------|--------|
| `Subject` | string | No vacío. Solo variables del catálogo (D3). |
| `Body` | string | No vacío. Solo variables del catálogo (D3). |

- Si un tipo **no está presente** o tiene `Subject`/`Body` vacíos ⇒ se usa el **contenido por defecto** (los textos hoy hardcodeados en `EmailTemplateProvider`).
- `ResetTemplate(type)` elimina la entrada del mapa (vuelve a default).

---

## 2. Catálogo de variables de plantilla (Domain)

Constante de dominio `EmailTemplateVariables` con el conjunto canónico **cerrado** (clarificación):

| Variable | Origen | Disponibilidad |
|----------|--------|----------------|
| `factura.id` | `Invoice.Id` | Siempre |
| `factura.monto` | `Invoice.Amount` (formato `es-CO`) | Siempre |
| `factura.estado` | `Invoice.Status` (etiqueta en español) | Siempre |
| `factura.fechaEmision` | `Invoice.CreatedAt` | Siempre |
| `factura.vencimiento` | (no existe campo de vencimiento en el dominio actual) | Vacío hasta que exista el dato |
| `cliente.nombre` | Resolución de cliente | Vacío si no disponible |
| `cliente.email` | `IClientEmailResolver` | Vacío si no disponible |
| `cliente.empresa` | Resolución de cliente | Vacío si no disponible |
| `enlacePago` | (no existe aún) | Vacío hasta que exista el dato |

- **Reglas de validación**: cualquier marcador `{{ nombre }}` cuyo `nombre` no esté en el catálogo ⇒ inválido (rechazo de guardado). Marcadores válidos con dato ausente ⇒ se sustituyen por cadena vacía al renderizar (no fallan).
- **Vista previa**: usa valores de ejemplo deterministas por variable (documentados en `contracts/ui-contracts.md`).

---

## 3. Estado de credencial (efímero, NO persistido)

`EmailCredentialStatus` describe la credencial del proveedor **activo**; se calcula en cada lectura y no se guarda en BD.

| Valor | Significado |
|-------|-------------|
| `NotConfigured` | El secreto del proveedor activo no está presente en el entorno. |
| `Configured` | Secreto presente; sin validación exitosa registrada en esta sesión. |
| `Validated` | Última validación contra el proveedor fue exitosa (estado en memoria, efímero). |

No se expone nunca el valor del secreto, solo este estado.

---

## 4. Notificación / "Envío" (reutiliza estado embebido en Invoice)

No hay entidad nueva. Se reutilizan los campos existentes de `Invoice` (spec 013):

| Campo (existente) | Tipo | Uso en esta feature |
|-------------------|------|---------------------|
| `LastNotificationType` | `NotificationType?` | Tipo del último intento. |
| `LastNotificationOutcome` | `NotificationOutcome` | `None` \| `Sent` \| `Skipped` \| `Failed`. Base de las herramientas. |
| `LastNotificationAt` | `DateTime?` | Momento del último intento. |
| `LastNotificationError` | string? | Motivo cuando `Failed`. |

### 4.1 Conjuntos objetivo de las herramientas globales

- **Reenvío manual** → facturas con `LastNotificationOutcome == Failed`.
- **Saneamiento** → facturas con `Status ∈ {PrimerRecordatorio, SegundoRecordatorio, Pagado, Desactivado}` **y** `LastNotificationOutcome == None`. Efecto: `RecordNotificationResult(type, Failed, now, "saneado: notificación no registrada")` (marca Failed conservando registro).

### 4.2 Transiciones de outcome inducidas por las herramientas

```
Saneamiento:   None (estado notificable) ── marcar ──▶ Failed
Reenvío éxito: Failed ── reintento ──▶ Sent
Reenvío fallo: Failed ── reintento ──▶ Failed (con nuevo motivo/fecha)
```

---

## 5. Índices MongoDB

- `Invoices`: índice sobre `LastNotificationOutcome` para soportar consultas de reenvío/saneamiento sin barridos completos (coherente con principio de performance: índices en campos consultados).
- `SystemSettings`: sin cambios (acceso por `_id` singleton).

---

## 6. Reglas de validación (resumen, fuente de FluentValidation)

| Entrada | Reglas |
|---------|--------|
| Actualizar EmailSettings | `FromAddress` email válido; `FromName` no vacío; si `Smtp` activo ⇒ `Host` requerido, `Port` 1–65535; si `Resend` activo ⇒ `FromDomain` requerido. Nunca acepta campos secretos. |
| Actualizar plantilla | `Subject` y `Body` no vacíos; todos los `{{var}}` ∈ catálogo. |
| Enviar prueba | `to` con formato email; `templateType` ∈ {Reminder, PaymentConfirmation, DeactivationNotice}. |

---

## 7. Mapeo a requisitos de la spec

| Entidad/Regla | Requisitos |
|---------------|-----------|
| `EmailSettings.ActiveProvider` + factory runtime | FR-002, FR-002a, FR-002b, FR-007 |
| Secretos solo entorno + estado credencial | FR-002, FR-008, SC-007 |
| Persistencia no secreta vía API | FR-005, FR-006, SC-003 |
| `EmailTemplateSet` + catálogo + render | FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-015, SC-004 |
| Validación credenciales | FR-004, SC-002 |
| Reenvío/saneamiento sobre Invoice | FR-020, FR-021, FR-022, FR-023, SC-006 |
