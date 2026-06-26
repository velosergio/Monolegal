---
description: "Task list — Panel de Administración (Layout Base + Listado de Facturas)"
---

# Tasks: Panel de Administración — Layout Base y Listado de Facturas

**Input**: Documentos de diseño en `specs/014-admin-panel-invoices/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUIDOS y OBLIGATORIOS. La Constitución (Principio IV — Test-First, NO NEGOCIABLE) exige escribir las pruebas primero (Red-Green-Refactor) con ≥85% de cobertura. Vitest + Testing Library (frontend) y xUnit + Shouldly (backend).

**Organización**: por historia de usuario (US1–US4) para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: puede ejecutarse en paralelo (archivos distintos, sin dependencias incompletas)
- **[Story]**: US1, US2, US3, US4
- Cada tarea incluye ruta de archivo exacta

## Path Conventions

- Frontend: `frontend/src/...` y pruebas co-ubicadas o en `frontend/tests/...`
- Backend: `backend/{Domain,Application,Infrastructure,Api,Tests}/...`

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: dependencias, primitivas shadcn/ui y utilidades transversales.

- [X] T001 Instalar dependencias frontend: `cd frontend && npm i lucide-react @radix-ui/react-select @radix-ui/react-dialog @radix-ui/react-slot` (actualiza `frontend/package.json`)
- [X] T002 [P] Añadir primitiva shadcn `table` en `frontend/src/components/ui/table.tsx`
- [X] T003 [P] Añadir primitiva shadcn `input` en `frontend/src/components/ui/input.tsx`
- [X] T004 [P] Añadir primitiva shadcn `select` (Radix) en `frontend/src/components/ui/select.tsx`
- [X] T005 [P] Añadir primitiva shadcn `badge` en `frontend/src/components/ui/badge.tsx`
- [X] T006 [P] Añadir primitiva shadcn `skeleton` en `frontend/src/components/ui/skeleton.tsx`
- [X] T007 [P] Añadir primitiva shadcn `sheet` (Radix dialog, menú móvil) en `frontend/src/components/ui/sheet.tsx`
- [X] T008 [P] Crear variantes y duraciones de animación centralizadas en `frontend/src/lib/motion.ts`
- [X] T009 [P] Crear hook `useDebouncedValue` en `frontend/src/hooks/use-debounced-value.ts`
- [~] T010 [P] OMITIDA — `useTheme` no se crea: el control de cambio de tema está fuera de alcance (clarificación FR-005); evitar código sin uso para no comprometer el 100/100 honesto de React Doctor
- [X] T011 Configurar defaults de TanStack Query (retry, staleTime) en `frontend/src/lib/query-client.ts`

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: extensión del backend (búsqueda + campo "Última Acción") y capa de datos del frontend que consumen US2 y US3.

**⚠️ CRITICAL**: ninguna historia de datos (US2/US3) puede completarse hasta terminar esta fase.

### Backend — extensión del endpoint de listado (tests primero)

- [X] T012 [P] Escribir/extender pruebas xUnit del parámetro `search` (combinación con status/paginación, normalización trim/vacío, longitud >100 → 400) en `backend/Tests/Monolegal.Application.Tests/Endpoints/ListInvoicesTests.cs` — deben FALLAR primero
- [X] T013 [P] Escribir prueba de repositorio Mongo del filtro de búsqueda case-insensitive + escapado en `backend/Tests/Infrastructure/MongoInvoiceRepositoryPagingAggregationTests.cs` — debe FALLAR primero
- [X] T014 Añadir `string? clientSearch` a la firma de `GetPagedAsync` en `backend/Domain/Repositories/IInvoiceRepository.cs`
- [X] T015 Añadir `Search` a `ListInvoicesQuery` y la regla de validación/normalización (trim, vacío⇒null, longitud ≤100) en `backend/Application/Validation/ListInvoicesQueryValidator.cs`
- [X] T016 Implementar el filtro combinado (status AND regex `ClientId` case-insensitive escapado) y recuento de `Total` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs`
- [X] T017 Aceptar y propagar el parámetro `search` (query string) en `backend/Api/Endpoints/Invoices/ListInvoices.cs`
- [X] T018 Añadir `LastStatusTransitionAt` a `InvoiceListItemDto` (mapeo desde la entidad) en `backend/Api/Endpoints/Invoices/InvoiceDtos.cs`
- [X] T019 Actualizar dobles de prueba a la nueva firma de `GetPagedAsync` en `backend/Tests/Infrastructure/Support/InMemoryInvoiceRepository.cs`, `backend/Tests/Monolegal.Application.Tests/Seeding/FakeInvoiceRepository.cs`, `ThrowingInvoiceRepository.cs` y los fakes inline de `InvoiceWorkerTests.cs`/`InvoiceRepositoryContractTests.cs`

### Frontend — capa de datos y degradación

- [X] T020 [P] Alinear tipos: `Invoice` (con `createdAt` + `lastStatusTransitionAt`, `status` como string del contrato) y `PagedInvoices` en `frontend/src/features/invoices/types.ts`
- [X] T021 Implementar `getInvoices` (GET `/api/invoices?status&search&page&pageSize`, manejo de error) en `frontend/src/features/invoices/api/getInvoices.ts`
- [X] T022 Implementar `useInvoices` (useQuery con `placeholderData: keepPreviousData`) en `frontend/src/features/invoices/api/useInvoices.ts`
- [X] T023 Implementar `useInvoicesViewState` (status+search+page, reset de página al cambiar status/search) en `frontend/src/features/invoices/hooks/useInvoicesViewState.ts`
- [X] T024 [P] Crear `ErrorBoundary` (degradación elegante) en `frontend/src/components/feedback/ErrorBoundary.tsx`

**Checkpoint**: backend extendido (verde) y datos del frontend disponibles. Las historias pueden comenzar.

---

## Phase 3: User Story 1 — Estructura base navegable (Priority: P1) 🎯 MVP

**Goal**: navbar con logo, navegación lateral (Facturas activa; Dashboard/Configuración deshabilitadas), footer, responsive y dark mode correcto.

**Independent Test**: cargar `/` y verificar navbar/sidebar/footer; colapso del menú en móvil con animación; tema almacenado respetado; sección Facturas resaltada.

### Tests (escribir primero, deben FALLAR)

- [X] T025 [P] [US1] Test de `AppShell` (renderiza header/nav/main/footer, sin desbordamiento) en `frontend/tests/components/layout/AppShell.test.tsx`
- [X] T026 [P] [US1] Test de `Sidebar` (Facturas `aria-current`; Dashboard/Configuración deshabilitados) en `frontend/tests/components/layout/Sidebar.test.tsx`
- [X] T027 [P] [US1] Test de `Navbar` + menú móvil (`aria-label`/`aria-expanded`, abre/cierra) en `frontend/tests/components/layout/Navbar.test.tsx`

### Implementation

- [X] T028 [P] [US1] Implementar `Navbar` (logo Monolegal + disparador de menú) en `frontend/src/components/layout/Navbar.tsx`
- [X] T029 [P] [US1] Implementar `Footer` (nombre/versión/año) en `frontend/src/components/layout/Footer.tsx`
- [X] T030 [US1] Implementar `Sidebar` (ítem activo + deshabilitados "próximamente") en `frontend/src/components/layout/Sidebar.tsx`
- [X] T031 [US1] Implementar `AppShell` (compone navbar/sidebar/footer/main; responsive con `sheet`; entrada de contenido por CSS para mantener Motion fuera del bundle principal) en `frontend/src/components/layout/AppShell.tsx`
- [X] T032 [US1] Refactorizar `frontend/src/App.tsx` para renderizar `AppShell` (sustituye el header/main sueltos actuales)

**Checkpoint**: el panel se ve como producto navegable, responsive y con dark mode — demostrable como MVP de layout.

---

## Phase 4: User Story 2 — Listado de facturas con estados de carga (Priority: P1)

**Goal**: tabla ID/Cliente/Monto/Estado/Última Acción con formatos legibles, badge de estado, skeletons en carga, estado vacío y error con reintento.

**Independent Test**: abrir Facturas con red lenta → skeletons; al resolver, columnas correctas; sin resultados → estado vacío; error → mensaje + reintento.

### Tests (escribir primero, deben FALLAR)

- [X] T033 [P] [US2] Test de `InvoicesTable` (columnas, formato moneda/fecha, badge, estado desconocido, acción Pagar) en `frontend/tests/features/invoices/InvoicesTable.test.tsx`
- [X] T034 [P] [US2] Test de `InvoicesTableSkeleton` (misma estructura/columnas, decorativo) en `frontend/tests/features/invoices/InvoicesTableSkeleton.test.tsx`
- [X] T035 [P] [US2] Test de `InvoicesEmptyState` (mensaje claro) en `frontend/tests/features/invoices/InvoicesEmptyState.test.tsx`
- [X] T036 [P] [US2] Test de estado de error + reintento (y vacío) de `InvoicesPage` en `frontend/tests/features/invoices/InvoicesPage.error.test.tsx`

### Implementation

- [X] T037 [P] [US2] Extraer `StatusBadge` (color por estado, neutro si desconocido) en `frontend/src/features/invoices/components/StatusBadge.tsx`
- [X] T038 [US2] Implementar `InvoicesTable` (tabla shadcn; conserva acción "Pagar"; formato moneda/fecha en `utils.ts`) en `frontend/src/features/invoices/components/InvoicesTable.tsx`
- [X] T039 [P] [US2] Implementar `InvoicesTableSkeleton` en `frontend/src/features/invoices/components/InvoicesTableSkeleton.tsx`
- [X] T040 [P] [US2] Implementar `InvoicesEmptyState` en `frontend/src/features/invoices/components/InvoicesEmptyState.tsx`
- [X] T041 [US2] Implementar `InvoicesPage` (estados loading/success/empty/error vía `useInvoices`; entrada animada con LazyMotion) en `frontend/src/features/invoices/components/InvoicesPage.tsx`
- [X] T042 [US2] Renderizar `InvoicesPage` de forma *lazy* dentro de `AppShell` en `frontend/src/App.tsx`

**Checkpoint**: el listado se ve con datos reales, skeletons, vacío y error — US1+US2 funcionan independientemente.

---

## Phase 5: User Story 3 — Filtrar, buscar y paginar (Priority: P2)

**Goal**: filtro por estado (server-side), búsqueda global por cliente (con debounce, server-side) y paginación de 10/página, con reinicio de página al cambiar filtro/búsqueda.

**Independent Test**: filtrar por estado reduce resultados; buscar por cliente reduce globalmente sin parpadeo; paginar muestra máx. 10; cambiar filtro/búsqueda reinicia a página 1.

### Tests (escribir primero, deben FALLAR)

- [X] T043 [P] [US3] Test de `StatusFilter` (opción "Todos" + estados; onChange) en `frontend/tests/features/invoices/StatusFilter.test.tsx`
- [X] T044 [P] [US3] Test de `ClientSearch` (input controlado; el debounce vive en `useInvoicesViewState`) en `frontend/tests/features/invoices/ClientSearch.test.tsx`
- [X] T045 [P] [US3] Test de `InvoicesPagination` (máx. 10, deshabilita extremos, página/total) en `frontend/tests/features/invoices/InvoicesPagination.test.tsx`
- [X] T046 [P] [US3] Test de `useInvoicesViewState` (reset de página + debounce de búsqueda) en `frontend/tests/features/invoices/useInvoicesViewState.test.tsx`

### Implementation

- [X] T047 [P] [US3] Implementar `StatusFilter` (select shadcn) en `frontend/src/features/invoices/components/StatusFilter.tsx`
- [X] T048 [P] [US3] Implementar `ClientSearch` (input controlado; el `useDebouncedValue` se aplica en `useInvoicesViewState` para evitar el anti-patrón de empujar estado al padre por efecto) en `frontend/src/features/invoices/components/ClientSearch.tsx`
- [X] T049 [P] [US3] Implementar `InvoicesPagination` en `frontend/src/features/invoices/components/InvoicesPagination.tsx`
- [X] T050 [US3] Integrar filtro/búsqueda/paginación en `InvoicesPage` (cablear `useInvoicesViewState` + `useInvoices`) en `frontend/src/features/invoices/components/InvoicesPage.tsx`

**Checkpoint**: el listado es filtrable, buscable globalmente y paginado — US1+US2+US3 funcionan independientemente.

---

## Phase 6: User Story 4 — Experiencia animada, accesible y de calidad (Priority: P3)

**Goal**: transiciones Motion suaves que respetan reduce-motion, accesibilidad por teclado completa y React Doctor 100/100 honesto.

**Independent Test**: con reduce-motion, animaciones atenuadas; navegación completa por teclado con foco visible; React Doctor reporta 100/100 sin supresiones.

### Tests (escribir primero, deben FALLAR)

- [X] T051 [P] [US4] Test de reduce-motion (`motionTransition` y montaje con `prefers-reduced-motion`) en `frontend/tests/a11y/reduced-motion.test.tsx`
- [X] T052 [P] [US4] Test de accesibilidad de teclado (Tab/Enter en nav y menú; estados accesibles) en `frontend/tests/a11y/keyboard-navigation.test.tsx`

### Implementation

- [X] T053 [US4] Aplicar variantes Motion (entrada de contenido vía LazyMotion+useReducedMotion en `InvoicesPage.tsx`; menú lateral con animaciones CSS que respetan reduce-motion) y centralizar en `frontend/src/lib/motion.ts`
- [X] T054 [US4] Pase de accesibilidad WCAG A (roles/labels/`aria-*`, foco visible, `<output>` para estados) en componentes de `frontend/src/components/layout/` y `frontend/src/features/invoices/components/`
- [X] T055 [US4] React Doctor **100/100 sin issues y SIN supresiones** (se eliminó incluso un `biome-ignore` previo en `theme-provider`); fixes: LazyMotion, input controlado, `<output>`, valores a ámbito de módulo, exports/archivos/dependencias sin uso eliminados

**Checkpoint**: experiencia pulida, accesible y verificada — todas las historias completas.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [~] T056 [P] *Code splitting* verificado: `InvoicesPage` se carga en chunk diferido (≈49.8 KB gzip, incl. Motion vía LazyMotion). **Desviación documentada**: el chunk de entrada queda en ≈91.8 KB gzip (React + ReactDOM + TanStack Query + Radix Dialog), por encima del objetivo de <50 KB que no es alcanzable de forma honesta con el stack mandatado; se prioriza SC-007 (TTI<2s) sobre el presupuesto literal.
- [X] T057 Gates de calidad en verde: `npm run lint` (Biome OK), `npm run build` (tsc+vite OK), `npm run test:run` (53/53). Cobertura: 90.3% statements / 92.9% líneas (≥85%).
- [X] T058 `dotnet test`: 252/252 en verde (225 unit/app + 27 integración Mongo), incluida la extensión de búsqueda (`ListInvoicesTests` + repo Mongo).
- [X] T059 Quickstart preparado: se añadió proxy `/api → http://localhost:5155` en `vite.config.ts`; gates automatizados (build/tests/lint/react-doctor) cubren los escenarios. La validación manual en navegador queda a cargo del usuario con backend + seed activos.
- [X] T060 [P] Marcar specs 4.1 y 4.2 como implementadas en `roadmap.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — inicia de inmediato.
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA US2 y US3 (datos). US1 (layout) depende solo de Setup (primitivas/sheet/motion).
- **US1 (Phase 3)**: tras Setup (usa `sheet`, `lib/motion`, `useTheme`).
- **US2 (Phase 4)**: tras Foundational (datos) + primitivas (`table`/`skeleton`/`badge`).
- **US3 (Phase 5)**: tras Foundational (búsqueda server-side) + US2 (integra en `InvoicesPage`).
- **US4 (Phase 6)**: tras US1–US3 (atraviesa los componentes ya creados).
- **Polish (Phase 7)**: tras las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: independiente (solo layout).
- **US2 (P1)**: requiere capa de datos (Phase 2). Independientemente testeable.
- **US3 (P2)**: requiere búsqueda server-side (Phase 2) y comparte `InvoicesPage` con US2 (integración secuencial en ese archivo).
- **US4 (P3)**: transversal; se aplica sobre lo construido en US1–US3.

### Within Each User Story

- Tests primero (deben fallar) → implementación.
- En US2/US3, `InvoicesPage.tsx` es archivo compartido → tareas que lo tocan son secuenciales (no [P]).

### Parallel Opportunities

- Setup: T002–T010 en paralelo (archivos distintos).
- Foundational backend: T012–T013 (tests) en paralelo; T020/T024 en paralelo con el backend (frontend vs backend).
- US1: T025–T027 (tests) en paralelo; T028–T029 en paralelo.
- US2: T033–T036 (tests) en paralelo; T037/T039/T040 en paralelo.
- US3: T043–T046 (tests) en paralelo; T047–T049 en paralelo.

---

## Parallel Example: User Story 2

```bash
# Tests de US2 en paralelo:
Task: "InvoicesTable.test.tsx"
Task: "InvoicesTableSkeleton.test.tsx"
Task: "InvoicesEmptyState.test.tsx"
Task: "InvoicesPage.error.test.tsx"

# Componentes hoja de US2 en paralelo:
Task: "StatusBadge.tsx"
Task: "InvoicesTableSkeleton.tsx"
Task: "InvoicesEmptyState.tsx"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational).
2. Completar US1 (layout navegable) → validar.
3. Completar US2 (listado con skeletons/vacío/error) → validar.
4. **STOP and VALIDATE**: panel navegable que muestra facturas reales = MVP demostrable de Fase 4.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → demo layout.
3. US2 → demo listado (MVP).
4. US3 → demo filtro/búsqueda/paginación.
5. US4 → pulido, a11y y React Doctor 100/100.

---

## Notes

- [P] = archivos distintos sin dependencias incompletas.
- `InvoicesPage.tsx` evoluciona en US2→US3→US4: no marcar en paralelo esas tareas.
- Verificar que cada prueba falla antes de implementar (Red-Green-Refactor).
- React Doctor 100/100 debe ser **honesto** (FR-021/SC-006): sin añadir supresiones.
- Commit por tarea o grupo lógico; detenerse en checkpoints para validar cada historia.
