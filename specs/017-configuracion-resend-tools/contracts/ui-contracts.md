# Contrato UI — Vista de Configuración (`/configuracion`)

Feature: 017-configuracion-resend-tools · Frontend `features/settings`

La página compone secciones verticales. Se **mantiene** la sección "Apariencia" (tema) existente y se añaden cuatro secciones nuevas. Toda la UI en español, dark mode, accesible por teclado, con toasts (`useToast`) y respeto a "reducir movimiento".

---

## Layout general

```
SettingsPage
├── Apariencia (existente)
├── Proveedor de email          → EmailProviderSection
├── Plantillas de email         → EmailTemplatesSection
├── Prueba de envío             → TestEmailSection
└── Herramientas de administración → AdminToolsSection
```

Estados transversales: cada sección que carga datos muestra skeleton/loading (`useQuery` `isPending`), estado de error legible y toasts en mutaciones (patrón `useTransitionInvoice` + `useToast`).

---

## 1. EmailProviderSection

**Datos**: `useEmailSettings()` → GET `/api/settings/email`.

**Controles**
- Selector de **proveedor activo** (`Select` shadcn): SMTP / Resend. Al cambiar, muestra los campos propios sin perder los ya introducidos (FR-002).
- Campos comunes: `fromAddress` (`Input` type email), `fromName` (`Input`).
- Campos SMTP: `host`, `port`, `username`, `useStartTls` (toggle).
- Campos Resend: `fromDomain`.
- **Indicador de estado de credencial** (badge): "No configurada" | "Configurada" | "Validada" — del proveedor activo. Nunca muestra el secreto.
- Botón **"Validar credenciales"** → `useValidateEmailCredentials()` (POST validate). Toast éxito/error con `message`.
- Botón **"Guardar"** → `useUpdateEmailSettings()` (PUT). Toast éxito/error; invalida `['email-settings']`.

**Validación cliente** (refleja backend): `fromAddress` formato email; `fromName` no vacío; si SMTP activo ⇒ `host` requerido y `port` 1–65535; si Resend activo ⇒ `fromDomain` requerido. Mensajes inline + impide submit. Controles deshabilitados mientras `isPending` (anti doble envío).

**A11y**: labels asociadas, `aria-describedby` para errores (patrón existente), foco visible.

---

## 2. EmailTemplatesSection

**Datos**: `useEmailTemplates()` → GET `/api/settings/email/templates`.

**Controles**
- Selector de plantilla (reminder / paymentConfirmation / deactivationNotice).
- `Input` para `subject` y `textarea` para `body`.
- Lista visible de **variables admitidas** (chips) insertables; muestra `isCustomized`.
- **Vista previa** con datos de ejemplo (render en cliente; opcionalmente POST `/preview`).
- Botón **"Guardar"** → `useUpdateEmailTemplate()` (PUT `/templates/{type}`). Toast; invalida `['email-templates']`.
- Botón **"Restablecer por defecto"** → confirmación (`Dialog`) → POST `/reset`.

**Validación cliente**: subject/body no vacíos; detectar `{{var}}` fuera del catálogo y señalarlo antes de guardar (FR-011).

**Valores de ejemplo para vista previa (deterministas)**

| Variable | Valor de ejemplo |
|----------|------------------|
| `factura.id` | `INV-2026-001` |
| `factura.monto` | `$ 1.250.000` |
| `factura.estado` | `Primer recordatorio` |
| `factura.fechaEmision` | `2026-06-01` |
| `factura.vencimiento` | `2026-06-15` |
| `cliente.nombre` | `Ana Pérez` |
| `cliente.email` | `ana.perez@cliente.com` |
| `cliente.empresa` | `Comercializadora XYZ` |
| `enlacePago` | `https://pagos.monolegal.co/INV-2026-001` |

---

## 3. TestEmailSection

**Controles**
- `Input` destino (`to`, type email) + selector de plantilla.
- Botón **"Enviar prueba"** → `useSendTestEmail()` (POST `/test`).
- Toast de éxito (confirma destino) o error (con `message`); botón en estado de carga, anti doble envío (FR-019).

**Validación cliente**: `to` formato email antes de enviar (FR-017).

---

## 4. AdminToolsSection

**Controles**
- **Reenvío manual (fallidos)**: botón → `useResendFailed()` (POST `/tools/resend-failed`). Toast con `{ attempted, resent, failed }`. Caso 0 ⇒ mensaje "No había envíos fallidos que reenviar".
- **Limpieza/saneamiento**: botón → **confirmación obligatoria** (`Dialog` destructivo) → `useSanitizeStuck()` (POST `/tools/sanitize`). Toast con `{ sanitized }`. Caso 0 ⇒ "No había envíos atascados".
- Descripción clara del efecto de cada herramienta (FR-020/FR-021/FR-022).

**A11y**: el diálogo de confirmación es operable por teclado, foco atrapado, `Esc` cancela.

---

## Claves de TanStack Query

| Clave | Endpoint | Invalida tras |
|-------|----------|---------------|
| `['email-settings']` | GET /email | PUT /email, POST /validate (status) |
| `['email-templates']` | GET /templates | PUT/reset templates |
| (sin caché) | POST /test, /tools/* | acciones puntuales (mutaciones) |

---

## Calidad (Constitución V)

- TypeScript strict, Biome 100%, **React Doctor 100/100 honesto** (sin supresiones).
- Dark mode y responsive (móvil/escritorio) sin desbordamiento.
- Animaciones con `Motion` + `useReducedMotion`.
- Toasts accesibles (ya provistos por `ToastProvider`/`ToastViewport`).

## Mapeo a requisitos

| Sección | Requisitos |
|---------|-----------|
| EmailProviderSection | FR-001..FR-008, SC-001, SC-002, SC-003, SC-007 |
| EmailTemplatesSection | FR-009..FR-015, SC-004 |
| TestEmailSection | FR-016..FR-019, SC-005 |
| AdminToolsSection | FR-020..FR-023, SC-006 |
| Transversal (a11y/UX/calidad) | FR-024..FR-027, SC-008, SC-009, SC-010 |
