# Tasks: invoice-status-transitions

**Input**: Design documents from `/specs/006-invoice-status-transitions/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/transitions-api.md

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Inicializar estructura de feature de configuraciones en frontend `frontend/src/features/settings/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Crear el modelo de dominio `SystemSettings` en `backend/src/Monolegal.Domain/Entities/SystemSettings.cs`
- [x] T003 Crear el repositorio para la configuración `ISystemSettingsRepository` en `backend/src/Monolegal.Domain/Repositories/ISystemSettingsRepository.cs`
- [x] T004 Implementar repositorio MongoDB en `backend/src/Monolegal.Infrastructure/Repositories/MongoSystemSettingsRepository.cs`
- [x] T005 [P] Implementar endpoint GET configuración `backend/src/Monolegal.Api/Endpoints/Settings/GetInvoiceTransitions.cs`
- [x] T006 [P] Implementar endpoint PUT configuración `backend/src/Monolegal.Api/Endpoints/Settings/UpdateInvoiceTransitions.cs`
- [x] T007 Implementar UI (Tab) de configuración en `frontend/src/features/settings/components/InvoiceTransitionsTab.tsx`
- [x] T008 [P] Integrar API en frontend `frontend/src/features/settings/api/getInvoiceTransitions.ts` y `updateInvoiceTransitions.ts`

**Checkpoint**: Foundation ready - La configuración es operable, user story implementation can now begin.

---

## Phase 3: User Story 1 - Transición Automática de Recordatorios (Priority: P1) 🎯 MVP

**Goal**: Permitir que el sistema cambie el estado de las facturas automáticamente con el paso de los días.

**Independent Test**: Simular el paso del tiempo y forzar al worker, verificar cambio de estado.

### Tests for User Story 1

- [x] T009 [P] [US1] Unit test para reglas de transición de dominio en `backend/tests/Monolegal.Domain.Tests/InvoiceStatusTransitionsTests.cs`
- [x] T010 [P] [US1] Integration test para endpoint de trigger en `backend/tests/Monolegal.Application.Tests/InvoiceWorkerTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Actualizar entidad `Invoice` para almacenar `LastStatusTransitionAt` en `backend/src/Monolegal.Domain/Entities/Invoice.cs`
- [x] T012 [US1] Implementar servicio de dominio para evaluación de tiempo en `backend/src/Monolegal.Domain/Services/InvoiceTransitionService.cs`
- [x] T013 [US1] Implementar el background worker en `backend/src/Monolegal.Infrastructure/Workers/InvoiceTransitionsWorker.cs`
- [x] T014 [US1] Agregar endpoint E2E de trigger manual para el worker en `backend/src/Monolegal.Api/Endpoints/Workers/TriggerTransitions.cs`

**Checkpoint**: El proceso de fondo transiciona automáticamente facturas vencidas basándose en la configuración persistida.

---

## Phase 4: User Story 2 - Transición a Pagado (Priority: P1)

**Goal**: Permitir marcar una factura como pagada desde cualquier estado activo.

**Independent Test**: Cambiar el estado de una factura a pagada a través de la API y verificar persistencia.

### Tests for User Story 2

- [x] T015 [P] [US2] Unit test de pago en `backend/tests/Monolegal.Domain.Tests/InvoicePaymentTests.cs`
- [x] T016 [P] [US2] Integration test de pago en API en `backend/tests/Monolegal.Application.Tests/Endpoints/PayInvoiceTests.cs`

### Implementation for User Story 2

- [x] T017 [US2] Implementar endpoint POST de pago en `backend/src/Monolegal.Api/Endpoints/Invoices/PayInvoice.cs`
- [x] T018 [P] [US2] Actualizar hook de frontend para el botón de pagar `frontend/src/features/invoices/api/payInvoice.ts`
- [x] T019 [P] [US2] Actualizar el componente de lista de facturas para mostrar el botón de pago `frontend/src/features/invoices/components/InvoiceList.tsx`

**Checkpoint**: User Story 1 AND 2 should both work independently.

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T020 Actualizar Quickstart (quickstart.md) con resultados reales y posibles escenarios
- [x] T021 Verificar y ajustar índices en MongoDB en `backend/src/Monolegal.Infrastructure/Persistence/MongoIndexBuilder.cs`
- [x] T022 [P] Validar logs de Serilog para todas las transiciones (worker y manuales)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2)
- **User Story 2 (P1)**: Can start after Foundational (Phase 2)

### Parallel Opportunities

- Todos los endpoints de la Phase 2 se pueden hacer en paralelo a la UI de React.
- US1 (worker) y US2 (endpoint manual de pago) se pueden trabajar de forma independiente una vez lista la entidad Invoice y la configuración de tiempos.

## Implementation Strategy

### Incremental Delivery

1. Completar la base (Configuración en MongoDB y API/UI).
2. Completar US1 (Worker backend de evaluación de tiempo).
3. Completar US2 (Endpoint de pago y botón frontend).
