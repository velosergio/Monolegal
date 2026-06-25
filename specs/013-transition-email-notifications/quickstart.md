# Quickstart — Validación de Envío de Correos y Registro en Transiciones (013)

Guía para validar end-to-end que las transiciones disparan el correo correcto, actualizan/omiten metadatos según el resultado y producen logs estructurados. Las firmas y postcondiciones están en [contracts/transition-notifications.md](./contracts/transition-notifications.md); el modelo en [data-model.md](./data-model.md).

## Prerrequisitos

- .NET 10 SDK.
- MongoDB accesible (`MONGODB_URI`), igual que en specs 004/012.
- Entorno `Development` para usar el emisor `NoOpEmailService` (no requiere SMTP real).

## Configuración

Variables de entorno / `appsettings` (las credenciales SMTP **sólo** por entorno, nunca hardcodeadas):

| Variable | Descripción | Por defecto |
|----------|-------------|-------------|
| `Email__Host` | Host del servidor SMTP. **Si está vacío se usa `NoOpEmailService`** (no envía, sólo loguea); si tiene valor se usa `SmtpEmailService` (MailKit). | _(vacío → NoOp)_ |
| `Email__Port` | Puerto SMTP. | `587` |
| `Email__Username` | Usuario SMTP (opcional). | _(vacío)_ |
| `Email__Password` | Contraseña SMTP (opcional; sólo por entorno/secretos). | _(vacío)_ |
| `Email__From` | Dirección remitente. | `no-reply@monolegal.local` |
| `Email__FromName` | Nombre visible del remitente. | `Monolegal` |
| `Email__UseStartTls` | Usar STARTTLS al conectar. | `true` |
| `ClientEmails__<clientId>` | Correo del cliente por `ClientId` (resolver inicial mientras no exista gestión de clientes). Sin valor ⇒ notificación `Skipped`. | _(vacío)_ |
| `Logging__File__Path` | Ruta del archivo de logs JSON (rolling diario, `CompactJsonFormatter`). | `logs/monolegal-.json` |

**Dev/CI**: dejar `Email__Host` vacío → `NoOpEmailService` permite ejercitar todos los flujos sin SMTP real.
**Producción**: definir `Email__Host` (+ credenciales) para activar el envío SMTP real.

## 1. Compilar y ejecutar pruebas

```powershell
dotnet build backend/Monolegal.sln
dotnet test backend/Monolegal.sln
```

**Esperado**: build sin errores; todas las suites verdes, incluyendo:
- `InvoiceTransitionNotifierTests` — selección de plantilla por estado; `Sent` incrementa `RemindersCount` sólo en recordatorio; `Failed` no toca contadores ni revierte estado; `Skipped` sin correo/plantilla.
- `EmailServiceContractTests` — cubre `SendDeactivationNoticeAsync`.
- Pruebas de worker y de endpoints `pay`/`transition` — invocan el notifier y aíslan fallos de correo.

## 2. Validar la transición automática (worker)

1. Sembrar/forzar una factura en `PrimerRecordatorio` con `LastStatusTransitionAt` suficientemente antigua para cumplir el umbral configurado.
2. Disparar un ciclo del worker (esperar el intervalo o usar `POST /api/workers/trigger-transitions`).
3. **Esperado**:
   - La factura avanza a `SegundoRecordatorio` (regla de dominio existente).
   - Con `NoOpEmailService`/`FakeEmailService` se observa una invocación de **recordatorio**.
   - En la factura persistida: `LastNotificationOutcome = Sent`, `LastNotificationType = Reminder`, `RemindersCount` incrementado y `LastReminderSentAt` actualizado.
   - Log JSON con `InvoiceId`, `PreviousStatus=PrimerRecordatorio`, `NewStatus=SegundoRecordatorio`, `NotificationType=Reminder`, `NotificationOutcome=Sent`.

## 3. Validar el aviso de desactivación

1. Factura en `SegundoRecordatorio` que cumpla el umbral a `Desactivado`.
2. Disparar el ciclo del worker.
3. **Esperado**: transición a `Desactivado`; invocación de **aviso de desactivación**; `LastNotificationType = DeactivationNotice`, `LastNotificationOutcome = Sent`; `RemindersCount` **sin cambios**; log con `NotificationType=DeactivationNotice`.

## 4. Validar la confirmación de pago (transición manual)

```powershell
curl -X POST http://localhost:5000/api/invoices/{id}/pay
```

**Esperado**: respuesta 200 con `status = pagado`; invocación de **confirmación de pago**; `LastNotificationType = PaymentConfirmation`, `LastNotificationOutcome = Sent`; `RemindersCount` sin cambios; log con `NewStatus=Pagado`, `NotificationOutcome=Sent`.

## 5. Validar el manejo de fallo de envío

1. Configurar el emisor para fallar (en pruebas, un `IEmailService` que lance; o credenciales SMTP inválidas en un entorno de prueba).
2. Disparar una transición (worker o manual).
3. **Esperado**:
   - La transición de estado **persiste** (no se revierte).
   - `LastNotificationOutcome = Failed` y `LastNotificationError` con el motivo; `RemindersCount`/`LastReminderSentAt` **sin cambios**.
   - El worker **continúa** con el resto del lote (un fallo no aborta el ciclo); el endpoint manual responde con la transición aplicada.
   - Log JSON a nivel error con `NotificationOutcome=Failed` y `Error`.
   - En el siguiente ciclo, si la factura sigue siendo elegible, se reintenta de forma natural.

## 6. Validar omisión sin correo / sin plantilla

1. Factura cuyo `ClientId` no resuelve correo (resolver devuelve `null`).
2. Disparar la transición.
3. **Esperado**: `LastNotificationOutcome = Skipped`; no se invoca al proveedor; log indicando "sin correo de destinatario"; transición persistida; lote continúa.

## 7. Validar logs persistidos (JSON)

1. Tras ejecutar los pasos anteriores, revisar el archivo de logs configurado (`Logging__File__Path`).
2. **Esperado**: eventos en formato JSON compacto, uno por factura procesada, con las propiedades del esquema del contrato (sección 5).

## Criterios de aceptación cubiertos

| Escenario | Criterio de éxito |
|-----------|-------------------|
| Pasos 2–4 | CE-001, CE-002, CE-005 |
| Paso 5 | CE-003, CE-006 |
| Pasos 2–6 | CE-004 (resultado consultable en la factura) |
| Paso 7 | CE-005 (logs estructurados JSON persistidos) |
