---
description: "Task list template for feature implementation"
---

# Tasks: 005-invoice-entity

**Input**: Design documents from `/specs/005-invoice-entity/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: The Constitution explicitly demands Test-First (TDD) for Domain logic, so tests are included and mandatory before implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 [P] Verify/Create directories `backend/src/Monolegal.Domain/Entities`, `Enums`, `Repositories`
- [x] T002 [P] Verify/Create directories `backend/tests/Monolegal.Domain.Tests/Entities`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Ensure `Shouldly` and `xunit` are referenced in `backend/tests/Monolegal.Domain.Tests/Monolegal.Domain.Tests.csproj`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Creación de Entidad Invoice (Priority: P1) 🎯 MVP

**Goal**: Crear la entidad `Invoice` base con todas sus reglas de negocio en la capa de Dominio, libre de dependencias de persistencia (Clean Architecture).

**Independent Test**: Ejecutar pruebas unitarias para confirmar la lógica de negocio (constructor, invariantes de monto, cambio de estado y control de fecha `UpdatedAt`).

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T004 [P] [US1] Create unit tests for `Invoice` constructor and validation in `backend/tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs`
- [x] T005 [P] [US1] Create unit tests for state mutators (e.g. `RecordReminderSent`) and audit dates in `backend/tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs`

### Implementation for User Story 1

- [x] T006 [P] [US1] Create `InvoiceStatus` enum with `Draft, Pending, Paid, Overdue, Cancelled` in `backend/src/Monolegal.Domain/Enums/InvoiceStatus.cs`
- [x] T007 [P] [US1] Create `IInvoiceRepository` interface in `backend/src/Monolegal.Domain/Repositories/IInvoiceRepository.cs`
- [x] T008 [US1] Implement `Invoice` entity with basic properties and constructor in `backend/src/Monolegal.Domain/Entities/Invoice.cs`
- [x] T009 [US1] Implement domain validation logic (`Amount > 0`, ClientId check) in constructor in `backend/src/Monolegal.Domain/Entities/Invoice.cs`
- [x] T010 [US1] Implement audit and state mutation methods in `backend/src/Monolegal.Domain/Entities/Invoice.cs`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently. Test coverage should be 100% for this entity.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T011 Run `dotnet format` on `backend/src/Monolegal.Domain/` and `backend/tests/Monolegal.Domain.Tests/` to ensure format compliance
- [x] T012 Run `quickstart.md` validation locally to confirm success criteria CE-001 y CE-002

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
- **Polish (Final Phase)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories. 

### Within Each User Story

- Tests (if included) MUST be written and FAIL before implementation
- Enums/Interfaces (T006, T007) can be implemented in parallel.
- Entity Core implementation (T008) must proceed after Enum is created.
- Business Logic (T009, T010) adds to the core Entity implementation sequentially.

### Parallel Opportunities

- T001 and T002 can be run in parallel.
- T004 and T005 tests can be written in parallel.
- T006 and T007 can be written in parallel.

---

## Parallel Example: User Story 1

```bash
# Launch all Setup tasks together:
Task: "Verify/Create directories backend/src/Monolegal.Domain/Entities, Enums, Repositories"
Task: "Verify/Create directories backend/tests/Monolegal.Domain.Tests/Entities"

# Launch all independent components for User Story 1 together:
Task: "Create InvoiceStatus enum in backend/src/Monolegal.Domain/Enums/InvoiceStatus.cs"
Task: "Create IInvoiceRepository interface in backend/src/Monolegal.Domain/Repositories/IInvoiceRepository.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Test-First Approach)
4. **STOP and VALIDATE**: Verify `InvoiceTests` pass with 100% coverage
5. Move to Polish Phase and commit.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Verify tests fail before implementing domain logic (Red-Green-Refactor).
- Commit after each task or logical group.
