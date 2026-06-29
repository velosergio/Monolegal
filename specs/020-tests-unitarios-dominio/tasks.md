---
description: "Task list — Tests Unitarios del Dominio (spec 020)"
---

# Tasks: Tests Unitarios del Dominio

**Input**: Design documents from `specs/020-tests-unitarios-dominio/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/test-inventory.md ✅

**Tests**: Esta feature **es** sobre pruebas unitarias; por tanto, todas las tareas de implementación son tareas de test (xUnit + Shouldly). No aplica la distinción opcional habitual.

**Organization**: Tareas agrupadas por historia de usuario. Toda ruta es relativa a la raíz del repo. Proyecto de pruebas: `backend/Tests/Monolegal.Domain.Tests/` (SUT: `backend/Domain/`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede correr en paralelo (archivo distinto, sin dependencias incompletas)
- **[Story]**: US1 / US2 / US3 (de spec.md)
- Cada tarea referencia los casos del inventario `contracts/test-inventory.md` (C1–C10)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar el proyecto de pruebas y establecer la línea base reproducible.

- [X] T001 Eliminar el andamiaje vacío `backend/Tests/Monolegal.Domain.Tests/UnitTest1.cs` (FR-012)
- [X] T002 [P] Crear la carpeta de pruebas de la capa Email: `backend/Tests/Monolegal.Domain.Tests/Email/` (contenedor de T010–T011)
- [X] T003 Capturar la línea base de cobertura ejecutando el comando de `quickstart.md` y registrar el % actual (referencia: 62.06% líneas) para medir el delta

**Checkpoint**: Suite ejecutable, sin andamiaje, línea base conocida.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirmar que la suite vigente está verde y aislada antes de ampliarla.

**⚠️ CRITICAL**: Ninguna historia debe partir de una suite roja.

- [X] T004 Ejecutar `dotnet test backend/Tests/Monolegal.Domain.Tests/Monolegal.Domain.Tests.csproj` y confirmar 105+ pruebas en verde, `Omitido: 0`, duración < 10 s (SC-004, SC-005)

**Checkpoint**: Fundamento verde; las historias pueden comenzar.

---

## Phase 3: User Story 1 — Transiciones de estado válidas e inválidas (Priority: P1) 🎯 MVP

**Goal**: Garantizar que la matriz de transiciones (manual y por tiempo) tiene casos permitidos y prohibidos para **cada** estado de origen, con rechazo explícito y estado inalterado.

**Independent Test**: Revisar que `InvoiceManualTransitionTests` cubre por `[Theory]` ≥1 transición permitida y ≥1 prohibida por cada estado de origen, y que las transiciones por tiempo prueban plazo cumplido/no cumplido (SC-002).

- [X] T005 [US1] Auditar `backend/Tests/Monolegal.Domain.Tests/InvoiceStatusTransitionsTests.cs` y `InvoiceManualTransitionTests.cs` contra la matriz de `data-model.md`; anotar como comentario qué pares origen→destino ya están cubiertos (base para T006–T007)
- [X] T006 [US1] Añadir en `backend/Tests/Monolegal.Domain.Tests/InvoiceManualTransitionTests.cs` un `[Theory]` con `[InlineData]` que cubra **todos** los pares permitidos de la matriz (C1.5): `Pending→{PrimerRecordatorio,Pagado}`, `PrimerRecordatorio→{SegundoRecordatorio,Pagado}`, `SegundoRecordatorio→{Desactivado,Pagado}`, `Desactivado→{Pagado}` — afirmar estado resultante y append a `StatusHistory` con Shouldly
- [X] T007 [US1] Añadir en `backend/Tests/Monolegal.Domain.Tests/InvoiceManualTransitionTests.cs` un `[Theory]` que cubra ≥1 transición **prohibida** por cada estado de origen, incluido `Pagado→*` (C2.1, C2.2): afirmar `Should.Throw<InvalidOperationException>()` y que `Status` no cambió
- [X] T008 [P] [US1] Añadir en `backend/Tests/Monolegal.Domain.Tests/InvoiceStatusTransitionsTests.cs` los casos de argumentos nulos de `TryApplyTransition` (C3.4): `invoice` nulo y `config` nulo → `Should.Throw<ArgumentNullException>()`
- [X] T009 [P] [US1] Verificar/añadir en `backend/Tests/Monolegal.Domain.Tests/InvoiceStatusTransitionsTests.cs` el caso `Pagado`/`Desactivado` → `TryApplyTransition` devuelve `false` (C3.3) si no estuviera ya cubierto

**Checkpoint**: 100% de estados de origen con caso permitido y prohibido; transiciones por tiempo completas (SC-002). US1 verificable de forma independiente.

---

## Phase 4: User Story 2 — Creación y validación de facturas (Priority: P1)

**Goal**: Garantizar que cada invariante de creación de factura (cliente, monto, items, monto derivado, estado terminal) tiene un caso de aceptación y un caso de rechazo explícito.

**Independent Test**: Construir facturas con datos válidos e inválidos y comprobar creación correcta vs. rechazo claro, sin dependencias externas (SC-003).

- [X] T010 [US2] Auditar `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs` y confirmar que existen los casos C4.1, C4.2, C5.1, C5.2, C5.3, C6.1, C6.2; anotar huecos
- [X] T011 [US2] Completar en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs` cualquier invariante de rechazo faltante: `clientId` vacío/blanco (C5.1), `amount ≤ 0` (C5.2), `items` vacía/nula en `Create` (C5.3) → `Should.Throw<ArgumentException>()`
- [X] T012 [US2] Asegurar en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs` el caso de creación válida (C4.1): estado inicial `Pending`, `Amount == Σ Items.Subtotal`, `StatusHistory` vacío, `NotificationRetryCount == 0` (Shouldly)
- [X] T013 [P] [US2] Asegurar en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs` el caso de estado terminal (C6.1): `UpdateDetails(...)` sobre factura `Pagado`/`Desactivado` → `Should.Throw<InvalidOperationException>()`
- [X] T014 [P] [US2] Verificar en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceItemsTests.cs` los invariantes de `InvoiceItem` (C7.1, C7.2): descripción vacía / cantidad ≤0 / precio ≤0 lanzan, y `Subtotal == Quantity × UnitPrice`

**Checkpoint**: 100% de invariantes de creación con caso de rechazo (SC-003). US2 verificable de forma independiente.

---

## Phase 5: User Story 3 — Umbral de cobertura ≥ 85% verificable (Priority: P2)

**Goal**: Cerrar los huecos de cobertura concentrados en `Domain/Email` y bordes de configuración, y convertir el umbral del 85% en un gate automático reproducible.

**Independent Test**: Ejecutar la suite con recolección de cobertura y comprobar que `line-rate` del proyecto de dominio ≥ 0.85 (SC-001).

- [X] T015 [P] [US3] Crear `backend/Tests/Monolegal.Domain.Tests/Email/EmailTemplateVariablesTests.cs` cubriendo C8.1–C8.4: `All` contiene exactamente las 9 variables esperadas, `IsAllowed` true/false, consistencia `All`↔`AllowedSet` (Shouldly)
- [X] T016 [P] [US3] Crear `backend/Tests/Monolegal.Domain.Tests/Email/EmailTemplateRendererTests.cs` cubriendo C9.1–C9.8: sustitución de admitido con valor, admitido sin valor → `""`, no admitido intacto, plantilla null/vacía → `""`, espacios `{{  factura.id  }}`, `ExtractVariables` sin duplicados, `FindInvalidVariables` (solo no admitidos / vacío si válida)
- [X] T017 [P] [US3] Ampliar `backend/Tests/Monolegal.Domain.Tests/SystemSettingsEmailTests.cs` con los bordes C10.1–C10.4: `UpdateEmailSettings(null)` lanza `ArgumentNullException`, `ResetTemplate` sobre tipo inexistente no cambia `UpdatedAt`, `ResetTemplate` sobre existente elimina y refresca `UpdatedAt`, lectura/asignación de `SmtpSettings`/`ResendSettings`
- [X] T018 [US3] Ejecutar el comando de cobertura de `quickstart.md` y el script de verificación de `line-rate`; confirmar ≥ 0.85 (SC-001). Si algún archivo de dominio sigue arrastrando el promedio, añadir los casos faltantes o documentar el código muerto como hallazgo en `research.md`
- [X] T019 [US3] Implementar el gate automático de cobertura (research D5) en `backend/Tests/Monolegal.Domain.Tests/Monolegal.Domain.Tests.csproj` (propiedades MSBuild de coverlet: `Threshold=85`, `ThresholdType=line`, `ThresholdStat=total`) **o** un paso de CI que parsee `coverage.cobertura.xml` y falle si `line-rate < 0.85` (FR-009, FR-010)

**Checkpoint**: Cobertura del dominio ≥ 85% medida y forzada por gate. US3 verificable de forma independiente.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cierre, validación end-to-end y documentación.

- [X] T020 Ejecutar la guía completa `specs/020-tests-unitarios-dominio/quickstart.md` y confirmar todos los escenarios (US1/US2/US3, higiene SC-004/SC-005)
- [X] T021 [P] Confirmar ausencia de skips/`[Ignore]` en todo `backend/Tests/Monolegal.Domain.Tests/` (FR-012) y que las aserciones usan Shouldly de forma consistente (FR-007)
- [X] T022 Actualizar `specs/020-tests-unitarios-dominio/research.md` con el % de cobertura final alcanzado (cerrar el delta frente a la línea base 62.06%)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — inicia de inmediato
- **Foundational (Phase 2)**: tras Setup — confirma suite verde antes de tocarla
- **US1 (Phase 3)** y **US2 (Phase 4)**: tras Foundational; **independientes entre sí** (tocan archivos distintos) — pueden ir en paralelo
- **US3 (Phase 5)**: tras Foundational; T015–T017 son independientes de US1/US2 y pueden empezar en paralelo, pero **T018 (verificación del umbral) debe ejecutarse al final**, una vez sumadas todas las pruebas, porque mide la cobertura acumulada
- **Polish (Phase 6)**: tras completar US1+US2+US3

### User Story Dependencies

- **US1 (P1)**: independiente — solo `InvoiceStatusTransitionsTests.cs` / `InvoiceManualTransitionTests.cs`
- **US2 (P1)**: independiente — solo `Entities/InvoiceTests.cs` / `Entities/InvoiceItemsTests.cs`
- **US3 (P2)**: el grueso (T015–T017) es independiente; T018/T019 (medición y gate) consolidan el resultado de las tres historias

### Parallel Opportunities

- T002 ∥ (resto de Setup)
- US1 (Phase 3) ∥ US2 (Phase 4) — archivos disjuntos
- Dentro de US3: **T015 ∥ T016 ∥ T017** (tres archivos distintos)
- T008 ∥ T009 (mismo objetivo, distinto método); T013 ∥ T014 (archivos distintos)

---

## Parallel Example: User Story 3

```bash
# Lanzar en paralelo las tres tareas de cobertura de la capa Email/config:
Task: "Crear Email/EmailTemplateVariablesTests.cs (C8.1–C8.4)"   # T015
Task: "Crear Email/EmailTemplateRendererTests.cs (C9.1–C9.8)"     # T016
Task: "Ampliar SystemSettingsEmailTests.cs (C10.1–C10.4)"         # T017
# Después, secuencial:
Task: "Verificar line-rate >= 0.85"                               # T018
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1.
4. **STOP & VALIDATE**: matriz de transiciones completa (permitidas + prohibidas por origen). Es el comportamiento de mayor riesgo del dominio.

### Incremental Delivery

1. Setup + Foundational → suite verde.
2. US1 → transiciones blindadas → validar (MVP).
3. US2 → creación/validación blindada → validar.
4. US3 → cerrar cobertura Email/config + gate → verificar ≥ 85%.
5. Polish → quickstart end-to-end + documentar % final.

### Nota de realidad del proyecto

El núcleo (US1/US2) ya está ≥ 95% en la línea base; gran parte de US1/US2 es **auditoría + consolidación en `[Theory]`** para garantizar SC-002/SC-003, no escritura desde cero. El esfuerzo neto de cobertura está en **US3** (capa `Domain/Email` al 0%). El umbral global no se alcanza sin completar US3.

---

## Notes

- [P] = archivos distintos, sin dependencias incompletas
- Aserciones con Shouldly (FR-007); pruebas deterministas con tiempo inyectado (FR-008)
- Sin dependencias externas: BD/red/FS fuera del SUT (FR-011)
- Sin skips/`[Ignore]` (FR-012)
- Commit por tarea o grupo lógico; detenerse en cada checkpoint para validar la historia
