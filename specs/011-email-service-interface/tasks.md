---
description: "Lista de tareas para Email Service Interface (Spec 011)"
---

# Tasks: Email Service Interface

**Input**: Documentos de diseño desde `specs/011-email-service-interface/`

**Prerequisites**: plan.md (requerido), spec.md (historias de usuario), research.md, data-model.md, contracts/IEmailService.md, quickstart.md

**Tests**: INCLUIDOS. La Constitución IV (Test-First NO NEGOCIABLE) y el criterio CE-002 (sustituibilidad por un fake) exigen pruebas de contrato. Se escriben ANTES de declarar la interfaz.

**Organization**: Tareas agrupadas por historia de usuario. Ambas historias son P1 y comparten el mismo archivo de interfaz (`IEmailService.cs`), por lo que la implementación de US2 extiende el archivo creado por US1 (no son paralelizables entre sí en el mismo archivo).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede correr en paralelo (distinto archivo, sin dependencias)
- **[Story]**: Historia de usuario asociada (US1, US2)
- Rutas de archivo exactas incluidas en cada descripción

## Path Conventions

- Backend por capas: `backend/Application/`, `backend/Domain/`, `backend/Tests/`
- Contrato objetivo: `backend/Application/Abstractions/IEmailService.cs`
- Pruebas: `backend/Tests/Monolegal.Application.Tests/Email/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verificar la estructura existente y las dependencias necesarias antes de definir el contrato.

- [X] T001 Verificar que existe la carpeta `backend/Application/Abstractions/` (precedente `IDevDataSeeder.cs`) y que `backend/Application/Application.csproj` referencia el proyecto `Domain` (acceso a `Monolegal.Domain.Entities.Invoice`).
- [X] T002 Verificar que el proyecto de pruebas `backend/Tests/Tests.csproj` referencia `Application` y tiene xUnit + Shouldly disponibles; crear la carpeta `backend/Tests/Monolegal.Application.Tests/Email/` para las pruebas de contrato.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Prerrequisitos de dominio compartidos por ambas historias.

**⚠️ CRITICAL**: Ninguna historia de usuario puede completarse hasta que esto esté listo.

- [X] T003 Confirmar que la entidad `Monolegal.Domain.Entities.Invoice` está disponible y es instanciable en pruebas (`new Invoice(clientId, amount)`) desde `backend/Tests/`, según `data-model.md`.

**Checkpoint**: Fundamento listo — las historias de usuario pueden comenzar.

---

## Phase 3: User Story 1 - Contrato para Envío de Recordatorios (Priority: P1) 🎯 MVP

**Goal**: Disponer del contrato `IEmailService` con una operación asíncrona de envío de recordatorio que reciba el correo del cliente y la factura asociada.

**Independent Test**: Crear un `FakeEmailService : IEmailService`, usarlo como `IEmailService`, invocar `await SendReminderAsync("cliente@correo.com", invoice)` y verificar que la llamada completa y el fake registró el correo y la factura exactos.

### Tests for User Story 1 ⚠️

> **NOTA: Escribir estas pruebas PRIMERO y asegurar que FALLAN (no compila / método inexistente) antes de implementar.**

- [X] T004 [P] [US1] Crear `FakeEmailService` (doble de prueba que implementa `IEmailService` y registra invocaciones) en `backend/Tests/Monolegal.Application.Tests/Email/FakeEmailService.cs`.
- [X] T005 [US1] Escribir prueba de contrato de recordatorio en `backend/Tests/Monolegal.Application.Tests/Email/EmailServiceContractTests.cs`: asignar el fake a una variable `IEmailService` (sustituibilidad), invocar `await SendReminderAsync(...)` con una `Invoice` de prueba y verificar (Shouldly) que la tarea completa y que el fake capturó `clientEmail` e `invoice` (RF-002, RF-004, CE-002).

### Implementation for User Story 1

- [X] T006 [US1] Crear la interfaz `IEmailService` en `backend/Application/Abstractions/IEmailService.cs` (namespace `Backend.Application.Abstractions`) con la operación `Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)` y XML-doc en español, según `contracts/IEmailService.md` (RF-001, RF-002, RF-004, RF-006).

**Checkpoint**: El contrato de recordatorio existe, compila y la prueba de contrato de US1 pasa de forma independiente.

---

## Phase 4: User Story 2 - Contrato para Confirmación de Pago (Priority: P1)

**Goal**: Extender `IEmailService` con una operación asíncrona de envío de confirmación de pago que reciba el correo del cliente y la factura pagada.

**Independent Test**: Con el mismo `FakeEmailService`, invocar `await SendPaymentConfirmationAsync("cliente@correo.com", invoice)` y verificar que completa y registra el correo y la factura esperados.

### Tests for User Story 2 ⚠️

> **NOTA: Escribir esta prueba PRIMERO y asegurar que FALLA antes de implementar el método.**

- [X] T007 [US2] Extender `FakeEmailService` en `backend/Tests/Monolegal.Application.Tests/Email/FakeEmailService.cs` para registrar también las invocaciones de confirmación de pago (depende de T004).
- [X] T008 [US2] Añadir prueba de contrato de confirmación de pago en `backend/Tests/Monolegal.Application.Tests/Email/EmailServiceContractTests.cs`: invocar `await SendPaymentConfirmationAsync(...)` con una `Invoice` y verificar que completa y captura `clientEmail` e `invoice` (RF-003, RF-004, CE-002, CE-003).

### Implementation for User Story 2

- [X] T009 [US2] Añadir la operación `Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)` con XML-doc en español a la interfaz existente `backend/Application/Abstractions/IEmailService.cs` (RF-003, RF-004). Depende de T006 (mismo archivo, no paralelizable con US1).

**Checkpoint**: Ambas operaciones del contrato existen; las pruebas de contrato de US1 y US2 pasan.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verificación final del contrato y de la calidad documental.

- [X] T010 Revisar que `IEmailService` no referencia ningún proveedor de correo concreto (solo BCL + `Invoice`) y que el XML-doc en español está completo, conforme a `contracts/IEmailService.md` (C-002, Constitución III).
- [X] T011 Ejecutar la validación del `quickstart.md`: `cd backend && dotnet build && dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~EmailServiceContract"` y confirmar build verde y pruebas de contrato en verde.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar de inmediato.
- **Foundational (Phase 2)**: Depende de Setup — BLOQUEA las historias de usuario.
- **User Stories (Phase 3+)**: Dependen de Foundational.
- **Polish (Phase 5)**: Depende de US1 y US2 completas.

### User Story Dependencies

- **User Story 1 (P1)**: Empieza tras Foundational. Crea el archivo de interfaz (MVP).
- **User Story 2 (P1)**: Empieza tras Foundational, pero su implementación (T009) **depende de T006** porque extiende el mismo archivo `IEmailService.cs`. Las pruebas (T007/T008) pueden prepararse en paralelo a US1.

### Within Each User Story

- Pruebas escritas y en FALLO antes de la implementación.
- El contrato (interfaz) antes de cualquier consumidor.
- Historia completa antes de pasar a la siguiente.

### Parallel Opportunities

- T004 [P] (crear el fake) puede prepararse en paralelo a otras tareas de configuración.
- T009 NO es paralelizable con T006 (mismo archivo `IEmailService.cs`).
- Por ser un único archivo de contrato compartido, el paralelismo entre US1 y US2 es limitado; se recomienda orden secuencial P1 → P1.

---

## Parallel Example: User Story 1

```bash
# Preparar el doble de prueba mientras se define el esquema de la prueba:
Task: "Crear FakeEmailService en backend/Tests/Monolegal.Application.Tests/Email/FakeEmailService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 1: Setup.
2. Completar Phase 2: Foundational (CRÍTICO — bloquea las historias).
3. Completar Phase 3: User Story 1 (contrato de recordatorio).
4. **PARAR y VALIDAR**: prueba de contrato de US1 en verde de forma independiente.
5. El contrato de recordatorio es el MVP entregable.

### Incremental Delivery

1. Setup + Foundational → fundamento listo.
2. US1 (recordatorio) → probar → entregable MVP.
3. US2 (confirmación de pago) → probar → contrato completo.
4. Polish → verificación final y quickstart.

---

## Notes

- Esta spec entrega SOLO la abstracción; la implementación concreta (proveedor, plantillas), el registro en DI y la persistencia del resultado se difieren a la Spec 3.3 (roadmap).
- [P] = distinto archivo, sin dependencias. T009 comparte archivo con T006 → secuencial.
- [Story] mapea cada tarea a su historia para trazabilidad.
- Verificar que las pruebas fallan antes de implementar (Red-Green-Refactor).
- Commit tras cada tarea o grupo lógico, referenciando la spec (ej: "feat(spec-3.1): definir IEmailService").
