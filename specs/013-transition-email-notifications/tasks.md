---
description: "Lista de tareas para Envío de Correos y Registro en Transiciones (013)"
---

# Tasks: Envío de Correos y Registro en Transiciones

**Input**: Documentos de diseño en `specs/013-transition-email-notifications/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/transition-notifications.md

**Tests**: INCLUIDOS y obligatorios. La Constitución (IV. Test-First, NO NEGOCIABLE) exige Red-Green-Refactor; cada historia escribe sus pruebas antes de implementar.

**Organización**: Tareas agrupadas por historia de usuario. US1 crea el orquestador compartido; US2 y US3 lo extienden (dependencia secuencial documentada).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: US1, US2, US3 (mapea a las historias de spec.md)

## Path Conventions

Web service por capas: `backend/Domain`, `backend/Application`, `backend/Infrastructure`, `backend/Api`, pruebas en `backend/Tests/Tests.csproj` (assembly `Tests`, namespace `Monolegal.Application.Tests`) y `backend/Tests/Monolegal.Domain.Tests`.

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: Añadir dependencias nuevas y verificar build base.

- [X] T001 Añadir paquetes `MailKit` y `MimeKit` al proyecto `backend/Infrastructure/Infrastructure.csproj` (emisor SMTP, research D1)
- [X] T002 [P] Añadir paquetes `Serilog.Sinks.File` y `Serilog.Formatting.Compact` al proyecto `backend/Api/Api.csproj` (sink JSON persistente, research D6)
- [X] T003 Verificar compilación de la solución con `dotnet build backend/backend.csproj` tras agregar dependencias

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: Modelo de dominio y contratos que TODAS las historias necesitan.

**⚠️ CRITICAL**: Ninguna historia puede empezar hasta completar esta fase.

- [X] T004 [P] Crear enum `NotificationType` (Reminder | PaymentConfirmation | DeactivationNotice) en `backend/Domain/Enums/NotificationType.cs`
- [X] T005 [P] Crear enum `NotificationOutcome` (None | Sent | Skipped | Failed) en `backend/Domain/Enums/NotificationOutcome.cs`
- [X] T006 [P] Escribir pruebas de dominio (FALLAN primero) para los campos `LastNotification*` y `RecordNotificationResult` en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs` (invariantes de data-model.md)
- [X] T007 Añadir campos `LastNotificationType`, `LastNotificationOutcome`, `LastNotificationAt`, `LastNotificationError` y el método `RecordNotificationResult(...)` en `backend/Domain/Entities/Invoice.cs` (depende de T004, T005, T006)
- [X] T008 Extender el contrato `IEmailService` con `SendDeactivationNoticeAsync(string, Invoice, CancellationToken)` en `backend/Application/Abstractions/IEmailService.cs` (research D5)
- [X] T009 [P] Crear el contrato `IClientEmailResolver` (`Task<string?> ResolveEmailAsync(string clientId, CancellationToken)`) en `backend/Application/Abstractions/IClientEmailResolver.cs`
- [X] T010 [P] Crear el contrato `IInvoiceTransitionNotifier` (`Task NotifyTransitionAsync(Invoice, InvoiceStatus previousStatus, CancellationToken)`) en `backend/Application/Abstractions/IInvoiceTransitionNotifier.cs`

**Checkpoint**: Modelo y contratos listos — las historias pueden comenzar.

---

## Phase 3: User Story 1 - Notificación al Cliente Según el Nuevo Estado (Priority: P1) 🎯 MVP

**Goal**: Al transicionar (worker o manual), se envía al cliente el correo cuya plantilla corresponde al nuevo estado; sin plantilla o sin correo se omite.

**Independent Test**: Disparar una transición con un `IEmailService` falso y verificar que se invoca el método correcto (recordatorio / confirmación / desactivación) según el nuevo estado, y que no se envía cuando no hay plantilla o correo.

### Tests for User Story 1 ⚠️ (escribir primero, deben FALLAR)

- [X] T011 [P] [US1] Crear doble de prueba `FakeClientEmailResolver` en `backend/Tests/Monolegal.Application.Tests/Notifications/FakeClientEmailResolver.cs`
- [X] T012 [P] [US1] Actualizar `FakeEmailService` para registrar llamadas a `SendDeactivationNoticeAsync` en `backend/Tests/Monolegal.Application.Tests/Email/FakeEmailService.cs`
- [X] T013 [P] [US1] Ampliar `EmailServiceContractTests` para cubrir `SendDeactivationNoticeAsync` en `backend/Tests/Monolegal.Application.Tests/Email/EmailServiceContractTests.cs`
- [X] T014 [US1] Crear `InvoiceTransitionNotifierTests` (selección de plantilla por estado; invoca método correcto; omite sin plantilla; omite sin correo) en `backend/Tests/Monolegal.Application.Tests/Notifications/InvoiceTransitionNotifierTests.cs` (depende de T011, T012)

### Implementation for User Story 1

- [X] T015 [US1] Implementar la ruta de envío de `InvoiceTransitionNotifier` (seleccionar tipo por `invoice.Status`, resolver correo, invocar `IEmailService`, omitir sin plantilla/sin correo) en `backend/Application/Notifications/InvoiceTransitionNotifier.cs` (depende de T008, T009, T010)
- [X] T016 [P] [US1] Crear `EmailOptions` (Host/Port/Username/Password/From/UseStartTls) en `backend/Infrastructure/Email/EmailOptions.cs`
- [X] T017 [P] [US1] Crear el proveedor de plantillas por estado (recordatorio, confirmación de pago, aviso de desactivación) en `backend/Infrastructure/Email/EmailTemplateProvider.cs`
- [X] T018 [US1] Implementar `SmtpEmailService` (MailKit) usando `EmailOptions` y el proveedor de plantillas en `backend/Infrastructure/Email/SmtpEmailService.cs` (depende de T008, T016, T017)
- [X] T019 [P] [US1] Implementar `NoOpEmailService` (Development/CI: sólo loguea, completa con éxito) en `backend/Infrastructure/Email/NoOpEmailService.cs` (depende de T008)
- [X] T020 [P] [US1] Implementar `ConfiguredClientEmailResolver` (resuelve correo por config/convención; `null` si no hay) en `backend/Infrastructure/Clients/ConfiguredClientEmailResolver.cs` (depende de T009)
- [X] T021 [US1] Registrar en DI `IEmailService` (SMTP en prod / NoOp en Development), `IClientEmailResolver` e `IInvoiceTransitionNotifier`, y bind de `EmailOptions`, en `backend/Infrastructure/Configuration/DependencyInjection.cs` (depende de T015, T018, T019, T020)
- [X] T022 [US1] Invocar `NotifyTransitionAsync(invoice, previousStatus, ct)` tras una transición exitosa y antes de `UpdateAsync` en `backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs` (inyectar `IInvoiceTransitionNotifier`)
- [X] T023 [P] [US1] Invocar el notifier tras `ApplyPayment` y antes de `UpdateAsync` en `backend/Api/Endpoints/Invoices/PayInvoice.cs`
- [X] T024 [P] [US1] Invocar el notifier tras `ApplyManualTransition` y antes de `UpdateAsync` en `backend/Api/Endpoints/Invoices/TransitionInvoice.cs`

**Checkpoint**: Las transiciones disparan el correo correcto por estado (verificable con dobles de prueba y con `NoOpEmailService` en local).

---

## Phase 4: User Story 2 - Seguimiento de Recordatorios en la Factura (Priority: P1)

**Goal**: Tras un envío exitoso se actualizan `LastReminderSentAt`/`RemindersCount` (sólo recordatorios) y el resultado de cada intento queda registrado en la factura; un fallo no toca contadores ni revierte la transición.

**Independent Test**: Ejecutar una transición con envío exitoso y verificar metadatos actualizados + `LastNotificationOutcome = Sent`; con envío fallido verificar `Failed`, contadores intactos y estado no revertido.

### Tests for User Story 2 ⚠️ (escribir primero, deben FALLAR)

- [X] T025 [US2] Ampliar `InvoiceTransitionNotifierTests` con casos de registro de resultado (éxito ⇒ `Sent` + recordatorio incrementa contadores; fallo ⇒ `Failed` sin tocar contadores ni revertir; omisión ⇒ `Skipped`) en `backend/Tests/Monolegal.Application.Tests/Notifications/InvoiceTransitionNotifierTests.cs` (depende de T014)
- [X] T026 [P] [US2] Ampliar pruebas de ciclo del worker: un envío fallido mantiene la transición, no cambia contadores y el lote continúa, en `backend/Tests/Monolegal.Application.Tests/InvoiceTransitionsWorkerCycleTests.cs`
- [X] T027 [P] [US2] Ampliar pruebas de endpoints: `pay`/`transition` registran el resultado y responden éxito aunque el correo falle, en `backend/Tests/Monolegal.Application.Tests/Endpoints/PayInvoiceTests.cs` y `backend/Tests/Monolegal.Application.Tests/Endpoints/TransitionInvoiceTests.cs`

### Implementation for User Story 2

- [X] T028 [US2] Extender `InvoiceTransitionNotifier` para registrar el resultado vía `RecordNotificationResult` (Sent/Skipped/Failed), llamar `RecordReminderSent()` sólo en recordatorio exitoso, capturar excepciones sin relanzar (salvo cancelación) y no revertir el estado, en `backend/Application/Notifications/InvoiceTransitionNotifier.cs` (depende de T015, T007)
- [X] T029 [US2] Verificar/ajustar que el worker ejecuta una única `UpdateAsync` tras `NotifyTransitionAsync` (persistencia de estado + resultado en una escritura) en `backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs` (depende de T028, T022)
- [X] T030 [P] [US2] Verificar/ajustar orden notify→`UpdateAsync` en `backend/Api/Endpoints/Invoices/PayInvoice.cs` (depende de T028, T023)
- [X] T031 [P] [US2] Verificar/ajustar orden notify→`UpdateAsync` en `backend/Api/Endpoints/Invoices/TransitionInvoice.cs` (depende de T028, T024)

**Checkpoint**: Metadatos y resultado de envío persisten correctamente en la factura; fallos no corrompen estado ni contadores.

---

## Phase 5: User Story 3 - Observabilidad Estructurada del Envío (Priority: P2)

**Goal**: Cada factura procesada produce un log estructurado JSON (timestamp, invoiceId, estado anterior/nuevo, tipo y resultado de envío), persistido en archivo/nube.

**Independent Test**: Procesar una transición y verificar que se emite un evento estructurado con las propiedades requeridas (y `Error` en fallo), y que el sink persiste JSON.

### Tests for User Story 3 ⚠️ (escribir primero, deben FALLAR)

- [X] T032 [US3] Añadir prueba que verifica el log estructurado del notifier (propiedades `InvoiceId`, `PreviousStatus`, `NewStatus`, `NotificationType`, `NotificationOutcome`, `Error`) capturando `ILogger` en `backend/Tests/Monolegal.Application.Tests/Notifications/InvoiceTransitionNotifierTests.cs` (depende de T025)

### Implementation for User Story 3

- [X] T033 [US3] Emitir el log estructurado por factura procesada (nivel Information en éxito/omisión; Error/Warning en fallo con motivo) en `backend/Application/Notifications/InvoiceTransitionNotifier.cs` (depende de T028)
- [X] T034 [US3] Configurar el sink Serilog persistente con `CompactJsonFormatter` + `Serilog.Sinks.File` (rolling diario, ruta por `Logging__File__Path`) conservando consola, en `backend/Api/Program.cs` (depende de T002)
- [X] T035 [P] [US3] Documentar las variables de configuración (`Email__*`, `Logging__File__Path`) y el comportamiento Dev/Prod en `specs/013-transition-email-notifications/quickstart.md` (sección de configuración)

**Checkpoint**: Logs estructurados JSON persistidos con el esquema del contrato.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T036 [P] Ejecutar la validación de `specs/013-transition-email-notifications/quickstart.md` (escenarios 1–7)
- [X] T037 [P] Verificar cobertura ≥85% del código nuevo (notifier, emisores, resolver, campos de dominio) con el reporte de pruebas
- [X] T038 Ejecutar `dotnet format` y `dotnet build backend/backend.csproj` para asegurar build limpio y formato consistente

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup; BLOQUEA todas las historias.
- **US1 (Phase 3)**: depende de Foundational. Crea el orquestador compartido.
- **US2 (Phase 4)**: depende de Foundational y de US1 (extiende `InvoiceTransitionNotifier` y el wiring de los llamadores).
- **US3 (Phase 5)**: depende de US2 (añade logging sobre el notifier ya completo) y de T002 (sink).
- **Polish (Phase 6)**: depende de las historias deseadas completas.

### User Story Dependencies

- **US1 (P1)**: independiente tras Foundational. MVP.
- **US2 (P1)**: secuencial tras US1 (mismo archivo `InvoiceTransitionNotifier.cs` y llamadores).
- **US3 (P2)**: secuencial tras US2 (logging sobre el notifier).

> Nota: aunque US1 y US2 comparten archivos (orquestador y llamadores), son **testeables de forma independiente** — US1 valida el envío por estado; US2 valida el registro/persistencia y el comportamiento ante fallo.

### Within Each User Story

- Tests primero (deben fallar) → implementación.
- Dominio antes que servicios; servicios (Application) antes que wiring (Infrastructure/Api).

### Parallel Opportunities

- T001 y T002 (paquetes en csproj distintos) pueden ir en paralelo.
- T004, T005, T006 (enums + pruebas de dominio) en paralelo.
- T009 y T010 (contratos en archivos distintos) en paralelo.
- US1: T011/T012/T013 en paralelo; T016/T017/T019/T020 en paralelo; T023/T024 en paralelo.
- US2: T026/T027 en paralelo; T030/T031 en paralelo.

---

## Parallel Example: User Story 1

```bash
# Pruebas de US1 (escribir primero, deben fallar):
Task: "FakeClientEmailResolver en backend/Tests/Monolegal.Application.Tests/Notifications/FakeClientEmailResolver.cs"
Task: "FakeEmailService cubre desactivación en backend/Tests/Monolegal.Application.Tests/Email/FakeEmailService.cs"
Task: "EmailServiceContractTests cubre desactivación en backend/Tests/Monolegal.Application.Tests/Email/EmailServiceContractTests.cs"

# Componentes de Infrastructure en paralelo (archivos distintos):
Task: "EmailOptions en backend/Infrastructure/Email/EmailOptions.cs"
Task: "EmailTemplateProvider en backend/Infrastructure/Email/EmailTemplateProvider.cs"
Task: "NoOpEmailService en backend/Infrastructure/Email/NoOpEmailService.cs"
Task: "ConfiguredClientEmailResolver en backend/Infrastructure/Clients/ConfiguredClientEmailResolver.cs"
```

---

## Implementation Strategy

### MVP

- **MVP mínimo**: Setup + Foundational + **US1** (las transiciones envían el correo correcto). 
- **MVP recomendado para producción**: incluir también **US2** (ambas son P1): sin el registro de metadatos, el worker podría reenviar o no dejar traza del resultado. US3 (observabilidad) puede seguir como incremento P2.

### Entrega incremental

1. Setup + Foundational → base lista.
2. US1 → validar envío por estado (con `NoOpEmailService`).
3. US2 → validar registro/persistencia y manejo de fallo.
4. US3 → validar logs JSON persistidos.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- El emisor SMTP real (MailKit) no se prueba con servidor; en pruebas se usan dobles (`FakeEmailService`) y en local `NoOpEmailService`.
- Verificar que las pruebas fallan antes de implementar (Red-Green-Refactor, Constitución IV).
- Sin secretos hardcodeados: credenciales SMTP sólo por variables de entorno.
