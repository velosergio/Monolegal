# Tasks: Vista de Configuración — Proveedor de Email, Plantillas, Prueba de Envío y Herramientas

**Input**: Design documents from `specs/017-configuracion-resend-tools/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUIDOS — la Constitución exige Test-First (NO NEGOCIABLE, >85% cobertura). Los tests de cada historia se escriben **antes** de su implementación y deben fallar primero (Red-Green-Refactor).

**Organization**: Tareas agrupadas por historia de usuario (US1–US4) para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario asociada (US1–US4)
- Rutas exactas incluidas en cada descripción

## Path Conventions

Aplicación web: backend por capas (`backend/Domain|Application|Infrastructure|Api`) y frontend por feature (`frontend/src/features/settings`). Tests backend en `backend/Tests/*`, tests frontend en `frontend/tests/*`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar configuración por entorno y dependencias compartidas.

- [X] T001 Extender `EmailOptions` (sección `Email`) con `Resend.ApiKey` (secreto, solo entorno) y `Resend.FromDomain` (no secreto) en `backend/Infrastructure/Email/EmailOptions.cs`
- [X] T002 [P] Documentar nuevas variables de entorno (`Email__Resend__ApiKey`, `Email__Resend__FromDomain`) en `.env` y `README.md` (sección de configuración de email)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modelo de dominio de email, abstracción multi-proveedor y servicio de envío respaldado por configuración. Comparten US1–US4.

**⚠️ CRITICAL**: Ninguna historia puede comenzar hasta completar esta fase.

- [X] T003 [P] Crear enum `EmailProvider` (`Smtp` | `Resend`) en `backend/Domain/Enums/EmailProvider.cs`
- [X] T004 [P] Crear catálogo cerrado `EmailTemplateVariables` (conjunto canónico de variables) en `backend/Domain/Email/EmailTemplateVariables.cs`
- [X] T005 Extender `SystemSettings` con `EmailSettings` (+ `SmtpSettings`, `ResendSettings`), `EmailTemplateSet`/`EmailTemplate` y métodos `UpdateEmailSettings`/`UpdateTemplate`/`ResetTemplate` en `backend/Domain/Entities/SystemSettings.cs`
- [X] T006 [P] Tests de dominio para `SystemSettings` email: update/reset, fallback a defaults, `UpdatedAt`, en `backend/Tests/Monolegal.Domain.Tests/SystemSettingsEmailTests.cs`
- [X] T007 [P] Definir abstracción `IEmailProvider` (`SendAsync`, `ValidateAsync`) + DTO `EmailMessage` en `backend/Application/Abstractions/IEmailProvider.cs`
- [X] T008 [P] Definir `IEmailProviderFactory` (resuelve proveedor activo) en `backend/Application/Abstractions/IEmailProviderFactory.cs`
- [X] T009 [P] Definir `IEmailCredentialStatus` (estado por proveedor, sin exponer valor) en `backend/Application/Abstractions/IEmailCredentialStatus.cs`
- [X] T010 [P] Tests unitarios de `TemplateRenderer`: sustitución `{{var}}`, variable no admitida, dato ausente→vacío, en `backend/Tests/Monolegal.Application.Tests/Email/TemplateRendererTests.cs`
- [X] T011 Implementar `EmailTemplateRenderer` (sustitución de variables del catálogo + validación) en `backend/Domain/Email/EmailTemplateRenderer.cs`
- [X] T012 Refactor `SmtpEmailService` → `SmtpEmailProvider : IEmailProvider` (envío + `ValidateAsync` que conecta/autentica sin enviar) en `backend/Infrastructure/Email/SmtpEmailProvider.cs`
- [X] T013 [P] Implementar `ResendEmailProvider : IEmailProvider` (envío y validación vía `HttpClient` contra la API de Resend) en `backend/Infrastructure/Email/ResendEmailProvider.cs`
- [X] T014 Implementar `EmailProviderFactory` (proveedor activo desde `SystemSettings.Email`, credenciales desde entorno) en `backend/Infrastructure/Email/EmailProviderFactory.cs`
- [X] T015 Refactor `EmailTemplateProvider` para leer plantillas persistidas con fallback a defaults vía `EmailTemplateRenderer` en `backend/Infrastructure/Email/EmailTemplateProvider.cs`
- [X] T016 Implementar `SettingsBackedEmailService : IEmailService` (lee settings por envío, compone y delega en la factory) en `backend/Infrastructure/Email/SettingsBackedEmailService.cs`
- [X] T017 Implementar `EmailCredentialStatusService : IEmailCredentialStatus` (lee entorno; estado por proveedor activo) en `backend/Infrastructure/Email/EmailCredentialStatusService.cs`
- [X] T018 Actualizar DI: registrar `HttpClient` Resend, ambos `IEmailProvider`, `IEmailProviderFactory`, `IEmailCredentialStatus`, y `SettingsBackedEmailService` como `IEmailService` en `backend/Infrastructure/Configuration/DependencyInjection.cs`
- [X] T019 Verificar tests existentes (`InvoiceTransitionNotifierTests`, worker) tras el refactor del envío: Domain 76/76, Application 108/108 verdes.

**Checkpoint**: Envío conmutable en runtime y modelo de configuración listos; las historias pueden comenzar.

---

## Phase 3: User Story 1 - Seleccionar/configurar/validar/persistir el proveedor (Priority: P1) 🎯 MVP

**Goal**: El admin elige proveedor activo (SMTP/Resend), edita el emisor, valida credenciales y persiste, con toasts.

**Independent Test**: En `/configuracion` cambiar proveedor, editar remitente, validar (toast éxito/error) y guardar; recargar y ver persistencia; el envío real usa el proveedor activo.

### Tests for User Story 1 ⚠️ (escribir primero, deben fallar)

- [X] T020 [P] [US1] Tests de `UpdateEmailSettingsValidator` (email válido, requeridos por proveedor) en `backend/Tests/Monolegal.Application.Tests/Validation/UpdateEmailSettingsValidatorTests.cs`
- [X] T021 [P] [US1] Test de integración HTTP GET/PUT/validate `/api/settings/email` (verifica que la respuesta no expone secretos) en `backend/Tests/Infrastructure/EmailSettingsEndpointsTests.cs`

### Implementation for User Story 1

- [X] T022 [P] [US1] `UpdateEmailSettingsValidator` (FluentValidation) en `backend/Application/Validation/UpdateEmailSettingsValidator.cs`
- [X] T023 [US1] Endpoint `GetEmailSettings` (GET `/api/settings/email`, incluye `credentialStatus`, sin secretos) en `backend/Api/Endpoints/Settings/GetEmailSettings.cs`
- [X] T024 [US1] Endpoint `UpdateEmailSettings` (PUT, validación, 204) en `backend/Api/Endpoints/Settings/UpdateEmailSettings.cs`
- [X] T025 [US1] Endpoint `ValidateEmailCredentials` (POST `/validate`, usa `ValidateAsync` sin enviar) en `backend/Api/Endpoints/Settings/ValidateEmailCredentials.cs`
- [X] T026 [US1] Registrar endpoints de US1 en `backend/Api/Program.cs`
- [X] T027 [P] [US1] Tipos de configuración de email en `frontend/src/features/settings/types.ts`
- [X] T028 [P] [US1] API client + hooks en `frontend/src/features/settings/api/` (`emailSettings.ts`, `useEmailSettings.ts`)
- [X] T029 [US1] Componente `EmailProviderSection` (selector proveedor, campos por proveedor, badge de estado de credencial, "Validar", "Guardar", validación cliente, toasts, anti doble envío) en `frontend/src/features/settings/components/EmailProviderSection.tsx`
- [X] T030 [US1] Integrar `EmailProviderSection` en `frontend/src/features/settings/components/SettingsPage.tsx`
- [X] T031 [P] [US1] Tests frontend (componente + hooks, stub de fetch) en `frontend/tests/features/settings/EmailProviderSection.test.tsx`

**Checkpoint**: US1 funcional e independientemente testeable (MVP).

---

## Phase 4: User Story 2 - Gestionar plantillas de email (Priority: P2)

**Goal**: Editar asunto/cuerpo de plantillas con variables del catálogo, vista previa, guardar y restablecer.

**Independent Test**: Editar una plantilla con variables, ver vista previa, guardar y recargar (persiste); rechazo de variable no admitida; restablecer a default.

### Tests for User Story 2 ⚠️ (escribir primero, deben fallar)

- [X] T032 [P] [US2] Tests de `UpdateEmailTemplateValidator` (no vacío, solo variables del catálogo) en `backend/Tests/Monolegal.Application.Tests/Validation/UpdateEmailTemplateValidatorTests.cs`
- [X] T033 [P] [US2] Test de integración HTTP templates (GET/PUT/reset/preview) en `backend/Tests/Infrastructure/EmailTemplatesEndpointsTests.cs`

### Implementation for User Story 2

- [X] T034 [P] [US2] `UpdateEmailTemplateValidator` (FluentValidation) en `backend/Application/Validation/UpdateEmailTemplateValidator.cs`
- [X] T035 [US2] Endpoint `GetEmailTemplates` (lista efectiva + `allowedVariables` + `isCustomized`) en `backend/Api/Endpoints/Settings/GetEmailTemplates.cs`
- [X] T036 [US2] Endpoint `UpdateEmailTemplate` (PUT `/templates/{type}`) en `backend/Api/Endpoints/Settings/UpdateEmailTemplate.cs`
- [X] T037 [US2] Endpoint `ResetEmailTemplate` (POST `/templates/{type}/reset`) en `backend/Api/Endpoints/Settings/ResetEmailTemplate.cs`
- [X] T038 [P] [US2] Endpoint `PreviewEmailTemplate` (POST `/templates/{type}/preview`, render con datos de ejemplo) en `backend/Api/Endpoints/Settings/PreviewEmailTemplate.cs`
- [X] T039 [US2] Registrar endpoints de US2 en `backend/Api/Program.cs`
- [X] T040 [P] [US2] API client + hooks de plantillas (`emailTemplates.ts`, `useEmailTemplates.ts`) en `frontend/src/features/settings/api/`
- [X] T041 [US2] Componente `EmailTemplatesSection` (editor asunto/cuerpo, chips de variables, vista previa, restablecer con confirmación, validación cliente) en `frontend/src/features/settings/components/EmailTemplatesSection.tsx`
- [X] T042 [US2] Integrar `EmailTemplatesSection` en `frontend/src/features/settings/components/SettingsPage.tsx`
- [X] T043 [P] [US2] Tests frontend de plantillas en `frontend/tests/features/settings/EmailTemplatesSection.test.tsx`

**Checkpoint**: US1 y US2 funcionales de forma independiente.

---

## Phase 5: User Story 3 - Enviar correo de prueba (Priority: P2)

**Goal**: Enviar un correo de prueba a una dirección indicada usando configuración y plantilla reales.

**Independent Test**: Introducir destino válido + plantilla → "Enviar prueba" → toast éxito/error; destino inválido se impide en cliente.

### Tests for User Story 3 ⚠️ (escribir primero, deben fallar)

- [X] T044 [P] [US3] Tests de `SendTestEmailValidator` + integración HTTP POST `/test` (resultado sent/failed) en `backend/Tests/Infrastructure/SendTestEmailEndpointTests.cs` y `.../Validation/SendTestEmailValidatorTests.cs`

### Implementation for User Story 3

- [X] T045 [P] [US3] `SendTestEmailValidator` (FluentValidation: `to` email, `templateType` válido) en `backend/Application/Validation/SendTestEmailValidator.cs`
- [X] T046 [US3] Endpoint `SendTestEmail` (POST `/test`, usa proveedor+plantilla reales, devuelve sent/failed con motivo) en `backend/Api/Endpoints/Settings/SendTestEmail.cs`
- [X] T047 [US3] Registrar endpoint en `backend/Api/Program.cs`
- [X] T048 [P] [US3] API client + hook (`emailTest.ts`/`useSendTestEmail.ts`) en `frontend/src/features/settings/api/`
- [X] T049 [US3] Componente `TestEmailSection` (destino, plantilla, enviar, validación cliente, loading, toasts) en `frontend/src/features/settings/components/TestEmailSection.tsx`
- [X] T050 [US3] Integrar `TestEmailSection` en `frontend/src/features/settings/components/SettingsPage.tsx`
- [X] T051 [P] [US3] Tests frontend de `TestEmailSection` en `frontend/tests/features/settings/TestEmailSection.test.tsx`

**Checkpoint**: US1–US3 funcionales de forma independiente.

---

## Phase 6: User Story 4 - Herramientas globales de administración (Priority: P3)

**Goal**: Reenvío masivo de fallidos y saneamiento de atascados (con confirmación), reportando conteos.

**Independent Test**: Con datos sembrados, reenviar fallidos (toast con conteos) y sanear (confirmación → conteo); caso sin candidatos informa "nada que procesar".

### Tests for User Story 4 ⚠️ (escribir primero, deben fallar)

- [X] T052 [P] [US4] Tests de `EmailAdminService` (reenvío `Failed`, saneo `None→Failed`, conteos, caso 0) en `backend/Tests/Monolegal.Application.Tests/EmailAdminServiceTests.cs`
- [X] T053 [P] [US4] Test de integración de herramientas (resend-failed, sanitize) en `backend/Tests/Infrastructure/EmailToolsEndpointsTests.cs`

### Implementation for User Story 4

- [X] T054 [P] [US4] Añadir consultas al repositorio: `GetByNotificationOutcomeAsync` (Failed/None), en `backend/Domain/Repositories/IInvoiceRepository.cs` y `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs`
- [X] T055 [P] [US4] Crear índice Mongo sobre `LastNotificationOutcome` en el constructor de índices (`backend/Infrastructure/Persistence/MongoIndexBuilder.cs`)
- [X] T056 [US4] Implementar `IEmailAdminService` + `EmailAdminService` (`ResendFailedAsync`, `SanitizeStuckAsync`, fail-soft, conteos, logs) en `backend/Application/Abstractions/IEmailAdminService.cs` y `backend/Infrastructure/Email/EmailAdminService.cs`
- [X] T057 [US4] Endpoint `ResendFailedNotifications` (POST `/tools/resend-failed`) en `backend/Api/Endpoints/Settings/ResendFailedNotifications.cs`
- [X] T058 [US4] Endpoint `SanitizeStuckNotifications` (POST `/tools/sanitize`) en `backend/Api/Endpoints/Settings/SanitizeStuckNotifications.cs`
- [X] T059 [US4] Registrar endpoints US4 en `backend/Api/Program.cs` y DI del admin service en `backend/Infrastructure/Configuration/DependencyInjection.cs`
- [X] T060 [P] [US4] API client + hooks (`emailTools.ts`, `useEmailTools.ts`) en `frontend/src/features/settings/api/`
- [X] T061 [US4] Componente `AdminToolsSection` (reenvío, saneamiento con confirmación destructiva `Dialog`, toasts con conteos, caso 0) en `frontend/src/features/settings/components/AdminToolsSection.tsx`
- [X] T062 [US4] Integrar `AdminToolsSection` en `frontend/src/features/settings/components/SettingsPage.tsx`
- [X] T063 [P] [US4] Tests frontend de `AdminToolsSection` (incluye flujo de confirmación) en `frontend/tests/features/settings/AdminToolsSection.test.tsx`

**Checkpoint**: Todas las historias funcionales de forma independiente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Calidad, seguridad, accesibilidad y validación end-to-end.

- [ ] T064 [P] E2E Playwright de la jornada completa de `/configuracion` (US1–US4) en `frontend/tests/e2e/settings.spec.ts` — DIFERIDO: el proyecto no tiene infraestructura Playwright (config ni dependencias) y requiere levantar API+Mongo. Pendiente de decisión de tooling E2E.
- [X] T065 [P] Test de seguridad: ninguna respuesta de API expone `password`/`apiKey` en `backend/Tests/Infrastructure/EmailSecretsNotExposedTests.cs`
- [X] T066 [P] Biome sin warnings (119 archivos) + `dotnet format --verify-no-changes` limpio + a11y (labels asociados, `Dialog` accesible de Radix, foco/teclado) y responsive de `/configuracion`
- [X] T067 [P] Actualizar documentación: `README.md` (sección Correos: multi-proveedor, secretos solo en entorno, plantillas, prueba y herramientas) y `.env`
- [X] T068 Gates: `dotnet format` limpio (proyectos backend) y suites verdes (backend Category=Application 144/144; frontend Vitest 125/125). Cobertura formal ≥85% pendiente de ejecutar en CI con Mongo disponible.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup. **BLOQUEA** todas las historias.
- **User Stories (Phase 3–6)**: dependen de Foundational. Pueden ir en paralelo (si hay equipo) o en orden P1 → P2 → P2 → P3.
- **Polish (Phase 7)**: depende de las historias deseadas completas.

### User Story Dependencies

- **US1 (P1)**: tras Foundational. Sin dependencias de otras historias. MVP.
- **US2 (P2)**: tras Foundational. Reusa `TemplateRenderer` (foundational). Independiente de US1.
- **US3 (P2)**: tras Foundational. Usa proveedor+plantillas reales; testeable de forma independiente.
- **US4 (P3)**: tras Foundational. Reusa el notifier/servicio de envío; independiente en su superficie de UI.

### Within Each User Story

- Tests primero (deben fallar) → validadores/modelos → servicios → endpoints → frontend.
- Integración de UI en `SettingsPage.tsx` es secuencial entre historias (mismo archivo): T030 → T042 → T050 → T062.
- Registro de endpoints en `Program.cs` es secuencial entre historias: T026 → T039 → T047 → T059.

### Parallel Opportunities

- Setup: T002 [P] junto a T001.
- Foundational: T003, T004, T006, T007, T008, T009, T010, T013 son [P] (archivos distintos); T011/T012/T014/T015/T016/T017/T018 dependen de las abstracciones.
- Dentro de cada historia: tests [P], tipos/api-client frontend [P], y tests frontend [P].
- Entre historias (con equipo): US1–US4 en paralelo salvo los archivos compartidos (`SettingsPage.tsx`, `Program.cs`).

---

## Parallel Example: User Story 1

```bash
# Tests primero (en paralelo):
Task: "T020 Tests de UpdateEmailSettingsValidator"
Task: "T021 Test de integración GET/PUT/validate /api/settings/email"

# Frontend en paralelo (archivos distintos):
Task: "T027 Tipos de configuración de email (types.ts)"
Task: "T028 API client + hooks de settings de email"
```

---

## Implementation Strategy

### MVP First (solo User Story 1)

1. Phase 1: Setup.
2. Phase 2: Foundational (CRÍTICO — bloquea todo).
3. Phase 3: US1 (proveedor + validar + persistir).
4. **PARAR y VALIDAR**: probar US1 de forma independiente.
5. Desplegar/demostrar si está listo.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → probar → demo (MVP).
3. US2 → probar → demo.
4. US3 → probar → demo.
5. US4 → probar → demo.

### Parallel Team Strategy

Tras Foundational: Dev A → US1, Dev B → US2, Dev C → US3, Dev D → US4; coordinar los archivos compartidos (`SettingsPage.tsx`, `Program.cs`) para evitar conflictos.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Cada historia es completable y testeable de forma independiente.
- Verificar que los tests fallan antes de implementar (Red-Green-Refactor; Constitución IV).
- Secretos solo por entorno; nunca en BD, respuestas ni logs (Constitución, FR-008, SC-007).
- Commit tras cada tarea o grupo lógico, referenciando la spec 017 (4.6).
