# Phase 0 — Research & Decisiones de Diseño

Feature: 017-configuracion-resend-tools · Fecha: 2026-06-26

Este documento resuelve las incógnitas técnicas antes del diseño detallado. Cada decisión incluye racional y alternativas descartadas. Las clarificaciones de negocio ya están fijadas en `spec.md` (sesión 2026-06-26).

---

## D1 — Abstracción multi-proveedor (SMTP + Resend) conmutable en runtime

**Decisión**: Introducir una abstracción de bajo nivel `IEmailProvider` (Infrastructure-facing, en `Application/Abstractions`) con dos operaciones: `SendAsync(EmailMessage, ct)` y `ValidateAsync(ct)`. Implementaciones concretas en Infrastructure: `SmtpEmailProvider` (refactor de `SmtpEmailService`, MailKit) y `ResendEmailProvider` (`HttpClient` contra la API REST de Resend). Un `IEmailProviderFactory` resuelve el proveedor **activo** leyendo `SystemSettings.Email.ActiveProvider` en cada operación. El contrato de alto nivel `IEmailService` (usado por `InvoiceTransitionNotifier`) se reimplementa como `SettingsBackedEmailService`, que en cada envío: carga settings, compone el mensaje con `TemplateRenderer`, y delega en el proveedor activo vía la factory.

**Racional**:
- Cumple Arquitectura Limpia y OCP: añadir un tercer proveedor no toca consumidores ni el notifier.
- El worker de transiciones corre **in-process con la API**, así que leer settings por envío habilita el cambio de proveedor en runtime (FR-002b) sin reinicio ni recompilación.
- `IEmailService` mantiene su firma actual (`SendReminderAsync`/`SendPaymentConfirmationAsync`/`SendDeactivationNoticeAsync`), evitando cambios en `InvoiceTransitionNotifier` y sus tests.

**Alternativas descartadas**:
- *Mantener `SmtpEmailService` y elegir proveedor en arranque por DI* (como hoy): no permite cambio en runtime; viola la clarificación Q4.
- *SDK oficial de Resend*: dependencia extra innecesaria; la API REST de Resend (`POST https://api.resend.com/emails`) se cubre con `HttpClient` + `IHttpClientFactory`.
- *Patrón Strategy inyectando ambos y un selector booleano*: la factory que lee settings por llamada es más simple y explícita para el cambio en runtime.

---

## D2 — Persistencia de configuración (no secreta) y manejo de credenciales (secretas)

**Decisión**: Extender la entidad `SystemSettings` (colección singleton `singleton-settings`, ya existente) con un objeto `Email` que contiene **solo datos no secretos**: `ActiveProvider` (enum), `FromAddress`, `FromName`, y parámetros no sensibles por proveedor (SMTP: `Host`, `Port`, `Username`, `UseStartTls`; Resend: `FromDomain`). Estos se persisten/leen vía API con el mismo patrón que `InvoiceTransitions`.

Las **credenciales secretas** (contraseña SMTP, API key de Resend) se leen **solo de variables de entorno / secrets** (`Email__Password`, `Email__Resend__ApiKey`) mediante `EmailOptions` y **nunca** se almacenan en BD, se devuelven por API ni se loguean. La vista recibe únicamente un **estado** de credencial por proveedor: `NotConfigured` (no presente en entorno), `Configured` (presente, sin validar en esta sesión) o `Validated` (última validación exitosa, en memoria/efímera).

**Racional**: Resuelve la tensión roadmap vs Constitución (clarificación Q2=C). Cumple "secrets solo por entorno; sin credenciales en BD".

**Alternativas descartadas**:
- *Cifrar la API key en Mongo*: contradice la política de secretos y añade gestión de claves de cifrado.
- *Campo de entrada de secreto en la UI*: innecesario bajo el modelo por-entorno; se elimina el riesgo de fuga.

**Nota de seguridad**: `Username` SMTP se considera no secreto (identificador), coherente con el `EmailOptions` actual. Solo `Password` y `ApiKey` son secretos.

---

## D3 — Plantillas editables con variables canónicas y vista previa

**Decisión**: Añadir `SystemSettings.EmailTemplates`: un mapa por `NotificationType` (`Reminder`, `PaymentConfirmation`, `DeactivationNotice`) con `Subject` y `Body`. `EmailTemplateProvider` se refactoriza para **leer la plantilla persistida** y, si no existe/está vacía, usar el **contenido por defecto** actual (los textos hardcodeados pasan a ser los defaults). La sustitución la realiza un `TemplateRenderer` con el **conjunto canónico** de variables (clarificación):

`factura.id`, `factura.monto`, `factura.vencimiento`, `factura.estado`, `factura.fechaEmision`, `cliente.nombre`, `cliente.email`, `cliente.empresa`, `enlacePago`.

Sintaxis de marcador: `{{ variable }}` (dobles llaves, con espacios opcionales). La validación rechaza cualquier marcador `{{...}}` cuyo nombre no esté en el catálogo y exige `Subject`/`Body` no vacíos. La vista previa renderiza con **datos de ejemplo** fijos por variable.

**Racional**: `{{ }}` es legible, inequívoco y fácil de validar con una expresión regular; no colisiona con el texto habitual de correos. El fallback a defaults garantiza que el sistema nunca queda sin contenido.

**Datos disponibles**: `factura.id`, `factura.monto`, `factura.estado`, `factura.fechaEmision`(=`CreatedAt`) provienen de `Invoice`; `factura.vencimiento` y `cliente.*`/`enlacePago` pueden no existir en el dominio actual — se rellenan vacíos cuando no haya dato y se documentan como opcionales (ver data-model). No se inventa un campo de vencimiento si el dominio no lo expone: la variable se admite pero rinde vacío hasta que exista el dato.

**Alternativas descartadas**:
- *Motor de plantillas (Handlebars/Scriban)*: sobredimensionado para sustitución simple; añade dependencia. La sustitución por catálogo cerrado es suficiente y más segura (sin ejecución de lógica).
- *Sintaxis `%VAR%` o `${var}`*: `${}` colisiona con expectativas de interpolación; `%%` es menos legible.

---

## D4 — Validación de credenciales por proveedor

**Decisión**: `IEmailProvider.ValidateAsync` comprueba la configuración del proveedor activo usando la credencial del entorno:
- **SMTP**: conectar y autenticar contra el host configurado (MailKit `ConnectAsync` + `AuthenticateAsync`), sin enviar correo; cerrar la conexión. Éxito ⇒ válido; excepción ⇒ inválido con motivo.
- **Resend**: llamada ligera autenticada a la API de Resend (p. ej. `GET /domains` o equivalente) con la API key; `200/2xx` ⇒ válido; `401/403` ⇒ credencial inválida; otros ⇒ error con motivo.

El endpoint `POST /api/settings/email/validate` devuelve `{ provider, status, message? }`. El resultado actualiza el estado efímero a `Validated` cuando es exitoso.

**Racional**: Validar sin enviar evita correos accidentales y da feedback inmediato (FR-004). Motivos legibles alimentan el toast de error.

**Alternativas descartadas**: *Enviar un correo real como validación*: efecto secundario indeseado; eso lo cubre la "prueba de envío" explícita (US3).

---

## D5 — Modelo de "envíos": reenvío y saneamiento sobre el estado embebido en `Invoice`

**Contexto**: El envío es **síncrono** dentro del worker; el resultado se registra en la factura (`LastNotificationOutcome` ∈ {None, Sent, Skipped, Failed}, más `LastNotificationType/At/Error`). **No existe** una colección de envíos ni un estado "pendiente/en curso". Por tanto "cola" y "atascado" se interpretan sobre estos datos.

**Decisión**:
- **Reenvío manual (global)** — actúa sobre las facturas con `LastNotificationOutcome == Failed`: por cada una, reintenta la notificación correspondiente a su estado actual (reusando `InvoiceTransitionNotifier`/`SettingsBackedEmailService`), persiste el nuevo resultado y devuelve conteos `{ intentados, reenviados, fallidos }`.
- **Saneamiento de atascados (global)** — "atascado" = factura en un estado **notificable** (`PrimerRecordatorio`, `SegundoRecordatorio`, `Pagado`, `Desactivado`) cuyo `LastNotificationOutcome == None` (debía existir una notificación y no se registró, p. ej. por caída a mitad de proceso). El saneamiento los **marca como `Failed`** (con motivo "saneado: notificación no registrada"), **conservando** el registro, de modo que pasan a ser candidatos de reenvío. Devuelve `{ saneados }`. No borra ni reintenta automáticamente (clarificación Q2=A).

**Racional**: Usa exclusivamente datos existentes (sin nueva colección ni cambios de esquema), es falsable y compone con el reenvío (Q3=A): saneamiento `None→Failed`, luego reenvío `Failed→Sent`. Honra el modelo síncrono real en vez de inventar una cola.

**Alternativas descartadas**:
- *Introducir un Outbox/cola persistente con estados Pending/InProgress*: cambio arquitectónico mayor fuera del alcance de 4.6 y del modelo síncrono actual; lo registramos como posible evolución futura, no para esta feature.
- *Definir "atascado" por umbral temporal sobre `Sent`*: los `Sent` no están atascados; mezclaría conceptos.

**Soporte de repositorio**: añadir consultas en `MongoInvoiceRepository`: por `LastNotificationOutcome` (para reenvío) y por (estado notificable ∧ outcome None) (para saneamiento), con índice sobre `LastNotificationOutcome`. Operaciones en lotes acotados e idempotentes; observabilidad Serilog (conteos, duración).

---

## D6 — Endpoints y validación (FluentValidation)

**Decisión**: Endpoints Minimal API bajo `/api/settings/email/*` (tag `Settings`), siguiendo el patrón de `GetInvoiceTransitions`/`UpdateInvoiceTransitions`. Inputs validados con FluentValidation (patrón ya usado en `TransitionInvoiceRequestValidator`): `UpdateEmailSettingsValidator` (FromAddress formato email, FromName no vacío, campos requeridos según proveedor), `UpdateEmailTemplateValidator` (subject/body no vacíos, solo variables del catálogo), `SendTestEmailValidator` (destino con formato email, tipo de plantilla válido). Errores devueltos como `ValidationProblem`/`{ error }`, que el cliente ya sabe leer (`readErrorMessage`). Ver contratos en `contracts/`.

**Racional**: Coherencia con endpoints y validadores existentes; el frontend ya extrae mensajes legibles de `{ error }`/`errors`.

---

## D7 — Frontend: estructura, estado de servidor y feedback

**Decisión**: Ampliar `features/settings` reutilizando patrones existentes: funciones `fetch` por endpoint + hooks `useQuery`/`useMutation` (como `getInvoices`/`useTransitionInvoice`), `useToast` para éxito/error (ya disponible globalmente vía `ToastProvider`), componentes shadcn (`Select`, `Input`, `Button`, `Dialog` para confirmación destructiva), `Motion` con `useReducedMotion`. Validación en cliente reflejando reglas backend, con mensaje inline + toast. Invalidación dirigida de las claves de query (`['email-settings']`, `['email-templates']`) tras mutaciones.

**Racional**: Cero dependencias nuevas; consistencia visual/UX y de calidad (React Doctor 100, dark mode, a11y) con el resto del panel.

**Alternativas descartadas**: *react-hook-form / librería de formularios nueva*: el resto del panel usa estado controlado simple + validación a mano; mantener coherencia.

---

## D8 — Configuración por entorno (variables nuevas)

**Decisión**: `EmailOptions` (sección `Email`) se amplía con credenciales/parámetros leídos del entorno:
- SMTP (existentes): `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__UseStartTls`, `Email__From`, `Email__FromName`.
- Resend (nuevos): `Email__Resend__ApiKey` (secreto), `Email__Resend__FromDomain` (no secreto, también editable por UI).

Los valores **no secretos** persistidos en BD **tienen prioridad** sobre los de entorno cuando existen (la BD es la fuente editable); los **secretos** vienen **siempre** del entorno. La factory combina ambos: config no secreta efectiva = BD (si existe) ∨ entorno (fallback); credencial = entorno.

**Racional**: Permite arranque sin BD poblada (defaults por entorno) y edición posterior por UI sin tocar secretos. Documentado en quickstart y `.env`.

---

## Resumen de decisiones

| ID | Tema | Decisión |
|----|------|----------|
| D1 | Multi-proveedor runtime | `IEmailProvider` + `IEmailProviderFactory` + `SettingsBackedEmailService` lee settings por envío |
| D2 | Persistencia/secrets | No secretos en `SystemSettings.Email` vía API; secretos solo por entorno; UI muestra estado |
| D3 | Plantillas/variables | `EmailTemplates` por tipo, sintaxis `{{var}}`, catálogo canónico cerrado, fallback a defaults |
| D4 | Validación credenciales | `ValidateAsync` por proveedor sin enviar correo |
| D5 | Reenvío/saneamiento | Sobre `Invoice.LastNotificationOutcome`; Failed→reenvío; None notificable→saneo a Failed |
| D6 | Endpoints/validación | Minimal API `/api/settings/email/*` + FluentValidation |
| D7 | Frontend | Patrón fetch+hooks, `useToast`, shadcn, Motion, sin dependencias nuevas |
| D8 | Entorno | `Email__Resend__ApiKey` (secreto) + fallback BD-sobre-entorno para no secretos |

Sin marcadores `NEEDS CLARIFICATION` pendientes.
