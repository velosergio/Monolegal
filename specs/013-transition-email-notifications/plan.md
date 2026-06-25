# Plan de Implementación: Envío de Correos y Registro en Transiciones

**Branch**: `013-transition-email-notifications` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/013-transition-email-notifications/spec.md`

## Summary

Cuando una factura cambia de estado —por el worker automático (spec 012) o por una transición manual vía API (`POST /api/invoices/{id}/pay`, `POST /api/invoices/transition/{id}`)— el sistema debe **enviar al cliente un correo cuya plantilla corresponda al nuevo estado**, actualizar los metadatos de recordatorio en envíos exitosos, **registrar el resultado del envío sobre la propia factura** y emitir un **log estructurado JSON** (timestamp, factura, estado anterior/nuevo, resultado). Un fallo de envío no revierte la transición ni aborta el lote; la factura se reintenta de forma natural en el siguiente ciclo.

Enfoque técnico: la maquinaria de transición ya existe (`InvoiceTransitionService`, `InvoiceTransitionsWorker`, endpoints manuales), pero **faltan tres piezas** que esta feature aporta:

1. **Orquestación de notificación reutilizable** (`IInvoiceTransitionNotifier` en Application): única pieza que, dada una factura recién transicionada, selecciona la plantilla por el nuevo estado, resuelve el correo del destinatario, invoca `IEmailService`, registra el resultado en la entidad y actualiza los metadatos de recordatorio. La invocan **tanto el worker como los endpoints manuales** (DRY, clarificación de alcance = cualquier transición).
2. **Implementación concreta de `IEmailService`** (Infrastructure): el contrato existe (spec 011) pero **no tiene implementación ni registro DI**. Se añade un emisor SMTP (MailKit) configurable por entorno + un emisor "no-op/log" para Desarrollo, y plantillas por estado (recordatorio, confirmación de pago, aviso de desactivación). Requiere **extender el contrato** con el envío de aviso de desactivación.
3. **Persistencia del resultado + observabilidad** (Domain + Serilog): se añaden campos de "último resultado de notificación" a `Invoice` (persistidos por el `ReplaceOneAsync` existente) y un **sink Serilog persistente en JSON** (hoy sólo hay consola).

Brecha adicional resuelta en research: **no existe fuente del correo del cliente** (la entidad `Invoice` sólo tiene `ClientId`; no hay entidad `Client`). Se introduce la abstracción `IClientEmailResolver`; si no se resuelve un correo, el envío se omite y se registra como `Skipped` (RF-010). La gestión completa de clientes queda como feature futura.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`).

**Primary Dependencies**:
- Existentes: `Microsoft.Extensions.Hosting`/`Options`, Serilog, MongoDB.Driver, `IEmailService` (contrato, spec 011).
- **Nuevas**: `MailKit` + `MimeKit` (emisor SMTP en Infrastructure) y `Serilog.Sinks.File` + `Serilog.Formatting.Compact` (logs JSON persistidos). Sin dependencias en Domain/Application salvo el contrato ya existente.

**Storage**: MongoDB. El resultado del envío y los metadatos de recordatorio se persisten **sobre el documento de la factura** mediante el `UpdateAsync`/`ReplaceOneAsync` existente (sin colección nueva). El POCO `Invoice` se auto-mapea; los campos nuevos se persisten sin cambios de mapeo.

**Testing**: xUnit + Shouldly. Pruebas unitarias del orquestador con `FakeEmailService` (ya existe) y un `FakeClientEmailResolver`; pruebas del worker y de los endpoints `pay`/`transition` verificando que se invoca el envío según el nuevo estado, que se actualizan/omiten metadatos según éxito/fallo y que el lote continúa ante fallo. Pruebas de contrato del `IEmailService` extendido.

**Target Platform**: Servicio Linux en contenedor Docker (API + worker en proceso de larga vida). SMTP saliente configurado por variables de entorno; en Desarrollo se usa el emisor "no-op/log" para no requerir un servidor SMTP real.

**Project Type**: Web service por capas (Domain/Application/Infrastructure/Api). Esta feature toca: `Domain` (campos + método en `Invoice`, enums de notificación), `Application` (contrato `IInvoiceTransitionNotifier`, `IClientEmailResolver`, extensión de `IEmailService`), `Infrastructure` (emisor SMTP, plantillas, resolver, orquestador concreto, DI, sink Serilog) y `Api` (inyectar el orquestador en los endpoints `pay`/`transition`).

**Performance Goals**: Proceso de fondo / endpoint manual; sin presupuesto p95 estricto para el worker. El envío de correo por factura no debe bloquear indefinidamente el lote: se respeta `CancellationToken` y los fallos/timeout se aíslan por factura. En endpoints manuales, el envío ocurre dentro de la petición; un fallo de correo no debe convertir en error la transición ya aplicada (se responde con la transición exitosa y el resultado de envío registrado).

**Constraints**: Arquitectura limpia (proveedor SMTP confinado a Infrastructure; el orquestador depende de abstracciones). Sin secretos hardcodeados (credenciales SMTP por entorno). Logging estructurado JSON (Serilog) persistido. Documentación en español (Constitución III). No revertir la transición ante fallo de envío; no actualizar contadores si el envío falla.

**Scale/Scope**: Decenas–miles de facturas por ciclo del worker. Cambios acotados pero transversales a las 4 capas. Plantillas iniciales: 3 (recordatorio, confirmación de pago, aviso de desactivación).

### Unknowns resueltos (ver research.md)

| Tema | Estado |
|------|--------|
| Proveedor/librería de correo concreto | Resuelto → MailKit + emisor no-op para Dev (D1) |
| Origen del correo del cliente (no hay entidad Client) | Resuelto → abstracción `IClientEmailResolver`; sin correo ⇒ `Skipped` (D2) |
| Estrategia de plantillas por estado | Resuelto → plantillas por estado, render simple por placeholders (D3) |
| Dónde vive la orquestación notificar-en-transición | Resuelto → servicio Application reutilizado por worker + endpoints (D4) |
| Extensión de `IEmailService` para desactivación | Resuelto → nuevo método de aviso de desactivación (D5) |
| Sink de logs persistente JSON | Resuelto → `Serilog.Sinks.File` + `CompactJsonFormatter` (D6) |
| Acoplamiento envío/transición y reintento | Resuelto → sin rollback, reintento natural del worker (D7) |

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | El proveedor SMTP y las plantillas viven en `Infrastructure`; los contratos (`IEmailService`, `IInvoiceTransitionNotifier`, `IClientEmailResolver`) en `Application`; `Domain` sólo gana datos de resultado de notificación. Un cambio de proveedor de correo no se propaga fuera de Infrastructure. | ✅ PASS |
| II. SOLID | SRP: el orquestador sólo coordina notificar-en-transición; el emisor sólo envía; el resolver sólo resuelve correo. DIP: worker y endpoints dependen de `IInvoiceTransitionNotifier`; el orquestador depende de `IEmailService`/`IClientEmailResolver`. OCP: nuevas plantillas/estados se añaden por mapeo sin modificar a los consumidores. ISP: el resolver y el emisor son contratos pequeños y específicos. | ✅ PASS |
| III. SDD (specs en español) | Spec 013 escrita, clarificada y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Se escriben primero las pruebas del orquestador (selección de plantilla por estado, actualización/omisión de metadatos según éxito/fallo, aislamiento de error) y la extensión del contrato, antes de implementar. | ✅ PASS |
| V. Frontend Producción | No aplica: feature de backend/worker. | ➖ N/A |
| VI. Observable y Mantenible | Serilog estructurado JSON por factura procesada (timestamp, invoiceId, estado anterior/nuevo, resultado de envío), persistido en archivo/nube. DI por constructor. | ✅ PASS |
| Stack tecnológico | Serilog + MongoDB Driver ya en uso; se añaden MailKit/MimeKit (SMTP) y `Serilog.Sinks.File` — dependencias estándar del ecosistema .NET, sin EF ni full MVC. | ✅ PASS |
| Seguridad | Credenciales SMTP sólo por variables de entorno (Docker secrets en producción); sin secretos en código. El correo del destinatario no se loguea en texto plano más allá de lo necesario para diagnóstico. | ✅ PASS |
| Performance & Escalabilidad | Worker sin estado en memoria (resultado en Mongo) → replicable. Envío aislado por factura; fallo no aborta el lote. | ✅ PASS |

**Resultado del gate**: PASS. Ningún principio NO NEGOCIABLE se incumple. Las dependencias nuevas (MailKit, Serilog.Sinks.File) son estándar y quedan confinadas a Infrastructure.

> **Nota de idempotencia multi-réplica**: con múltiples réplicas del worker, una factura podría notificarse dos veces si dos ciclos la evalúan en paralelo entre la transición y la persistencia. El efecto se mitiga porque la elegibilidad de transición se basa en `LastStatusTransitionAt` (que cambia al transicionar) y la actualización es por documento; un bloqueo distribuido / entrega exactamente-una-vez queda fuera de alcance (documentado en research D7).

## Project Structure

### Documentation (this feature)

```text
specs/013-transition-email-notifications/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas (proveedor, plantillas, resolver, orquestación, logging)
├── data-model.md        # Phase 1 — campos de notificación en Invoice y enums
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/
│   └── transition-notifications.md   # Contrato del orquestador + IEmailService extendido + log estructurado
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Entities/Invoice.cs                         # (EDITADO) campos LastNotification* + RecordNotificationResult()
│   └── Enums/
│       ├── NotificationType.cs                     # (NUEVO) Reminder | PaymentConfirmation | DeactivationNotice
│       └── NotificationOutcome.cs                  # (NUEVO) None | Sent | Skipped | Failed
├── Application/
│   ├── Abstractions/
│   │   ├── IEmailService.cs                         # (EDITADO) + SendDeactivationNoticeAsync(...)
│   │   ├── IClientEmailResolver.cs                  # (NUEVO) resuelve el correo destino por ClientId
│   │   └── IInvoiceTransitionNotifier.cs            # (NUEVO) orquesta notificar-en-transición
│   └── Notifications/
│       └── InvoiceTransitionNotifier.cs            # (NUEVO) impl. del orquestador (selección plantilla, envío, registro)
├── Infrastructure/
│   ├── Email/
│   │   ├── SmtpEmailService.cs                      # (NUEVO) emisor SMTP (MailKit) + plantillas por estado
│   │   ├── NoOpEmailService.cs                      # (NUEVO) emisor de Desarrollo (sólo loguea)
│   │   ├── EmailOptions.cs                          # (NUEVO) host/puerto/credenciales/from por entorno
│   │   └── Templates/                               # (NUEVO) plantillas por estado (recordatorio, pago, desactivación)
│   ├── Clients/
│   │   └── ConfiguredClientEmailResolver.cs         # (NUEVO) resolver inicial (config/derivado); null ⇒ Skipped
│   ├── Workers/InvoiceTransitionsWorker.cs          # (EDITADO) invoca el notifier tras transicionar, antes de persistir
│   └── Configuration/DependencyInjection.cs         # (EDITADO) registra IEmailService, resolver y notifier; sink Serilog
├── Api/
│   ├── Program.cs                                   # (EDITADO) configurar sink Serilog JSON persistente
│   └── Endpoints/Invoices/
│       ├── PayInvoice.cs                            # (EDITADO) invoca el notifier (confirmación de pago) antes de persistir
│       └── TransitionInvoice.cs                     # (EDITADO) invoca el notifier según el nuevo estado
└── Tests/
    ├── Monolegal.Application.Tests/Notifications/
    │   └── InvoiceTransitionNotifierTests.cs        # (NUEVO) plantilla por estado, metadatos éxito/fallo, skip sin correo
    ├── Monolegal.Application.Tests/Email/
    │   └── EmailServiceContractTests.cs             # (EDITADO) cubre SendDeactivationNoticeAsync
    └── Monolegal.Application.Tests/Endpoints|Workers/
        └── *                                        # (EDITADO/NUEVO) pay/transition/worker invocan el notifier y aíslan fallos
```

**Structure Decision**: Web service por capas ya establecido. La lógica de "notificar al transicionar" se centraliza en un servicio de **Application** (`InvoiceTransitionNotifier`) que depende sólo de abstracciones (`IEmailService`, `IClientEmailResolver`), de modo que worker (Infrastructure) y endpoints (Api) la reutilizan sin duplicar reglas. El proveedor de correo concreto (MailKit/SMTP), las plantillas y el resolver de correo viven en **Infrastructure**, preservando la dirección de dependencias de la Arquitectura Limpia. `Domain` sólo incorpora el dato del último resultado de notificación.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. La feature es transversal a las 4 capas por necesidad (datos en Domain, contratos en Application, proveedor en Infrastructure, wiring en Api), pero cada pieza mantiene responsabilidad única y se conecta por abstracciones. Las únicas dependencias nuevas (MailKit/MimeKit, Serilog.Sinks.File) son estándar del ecosistema y quedan aisladas en Infrastructure, alineadas con el principio de reemplazabilidad de la capa de infraestructura.
