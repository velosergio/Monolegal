---
description: "Lista de tareas para la implementación de la feature Documentación de API y del Proyecto"
---

# Tasks: Documentación de API y del Proyecto

**Input**: Documentos de diseño en `/specs/025-documentacion-api/`

**Prerequisites**: plan.md (requerido), spec.md (historias de usuario), research.md, data-model.md, contracts/, quickstart.md

**Tests**: El único cambio de código de producción es el ítem de Swagger en el sidebar; se cubre con un test de Vitest **escrito primero** (Principio IV, contrato `sidebar-swagger-link.md`). Los artefactos generados se validan con el modo `--verify` del generador y con los escenarios del quickstart. No se añaden ni modifican otras pruebas (FR-012).

**Organization**: Tareas agrupadas por historia de usuario para permitir implementación y validación independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede correr en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1, US2, US3)
- Se incluyen rutas de archivo exactas (relativas a la raíz del repo)

## Path Conventions

- Monorepo web. Documentación en `docs/`; tooling de generación en `scripts/`; cambios de frontend en `frontend/src/components/layout/` y `frontend/`.
- Tests de frontend **co-ubicados** junto al componente (`Component.test.tsx`), según la convención del repo.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Crear la estructura de documentación y capturar la fuente OpenAPI.

- [X] T001 Crear la estructura de documentación: directorio `docs/postman/` y el índice `docs/README.md` (esqueleto en español que enlazará los seis entregables y el acceso a Swagger), conviviendo con `docs/adr/` existente.
- [X] T002 Capturar el snapshot del documento OpenAPI en `docs/openapi.json` desde `/openapi/v1.json` (backend en Development, p. ej. `curl http://localhost:5155/openapi/v1.json -o docs/openapi.json`); es la única fuente de los artefactos generados (research D3, contrato `api-docs-generator.md`).

**Checkpoint**: Existen `docs/`, `docs/postman/`, `docs/README.md` y `docs/openapi.json`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Construir el generador reproducible que alimenta la referencia de endpoints (US1) y la colección de Postman (US3).

**⚠️ CRITICAL**: La referencia de endpoints (US1) y la colección Postman (US3) dependen de esta fase.

- [X] T003 Implementar `scripts/gen-api-docs.mjs` (Node ESM, solo `node:fs`/`node:path`, sin dependencias): lee `docs/openapi.json` y emite `docs/api-reference.md` y `docs/postman/monolegal.postman_collection.json`; salida **determinista** (orden estable, sin timestamps), cobertura del 100% de operaciones, no interactivo, y modo `--verify` que falla (exit ≠0) si la salida difiere de lo versionado (contrato `api-docs-generator.md`, FR-010).
- [X] T004 Añadir al `package.json` de la raíz los scripts `"docs:api": "node scripts/gen-api-docs.mjs"` y `"docs:api:verify": "node scripts/gen-api-docs.mjs --verify"` (sin nuevas dependencias).

**Checkpoint**: `npm run docs:api` genera ambos artefactos desde el snapshot y `npm run docs:api:verify` termina en 0 cuando están sincronizados.

---

## Phase 3: User Story 1 - Entender y poner en marcha el proyecto con la documentación escrita (Priority: P1) 🎯 MVP

**Goal**: Documentación consolidada que explica arquitectura, modelo de datos, endpoints, configuración local y despliegue, suficiente para entender y arrancar el proyecto sin leer el código.

**Independent Test**: Una persona ajena sigue solo la documentación para describir arquitectura/ERD/endpoints y levantar el stack en local (quickstart §1, §2, §6).

### Implementation for User Story 1

- [X] T005 [P] [US1] Escribir `docs/architecture.md`: panorama de capas (Domain/Application/Infrastructure/Api) y componentes (backend, worker, frontend, MongoDB) con ≥1 diagrama Mermaid de relaciones (FR-001; contrato `documentation-deliverables.md` #1).
- [X] T006 [P] [US1] Escribir `docs/data-model.md`: `erDiagram` Mermaid con Client/Invoice/InvoiceItem/StatusChange (+ SystemSettings), atributos clave, relaciones, enums tomados de `backend/Domain/Enums` y nombres de colecciones (FR-002; data-model.md de la feature).
- [X] T007 [P] [US1] Escribir `docs/setup.md`: prerrequisitos (Docker, .NET 10, Node), pasos para levantar backend, worker, frontend y MongoDB en local, y variables de entorno (`.env.example`); coherente con el README (FR-004; contrato #4).
- [X] T008 [P] [US1] Escribir `docs/deployment.md`: pasos y requisitos de despliegue (Docker/VPS, entornos, variables) e **incluir la exposición de Swagger en producción** (deshabilitado por defecto, Spec 010 D3) (FR-005; contrato #5; research D4).
- [X] T009 [US1] Generar `docs/api-reference.md` con `npm run docs:api` y validar que lista el 100% de los endpoints (Clients, Invoices, Settings, Workers) con método/ruta/propósito/parámetros (FR-003, FR-010, SC-003; contrato #3). Depende de Phase 2.
- [X] T010 [P] [US1] Completar el índice `docs/README.md` y añadir/actualizar la sección "Documentación" del `README.md` raíz enlazando los seis entregables y el acceso a Swagger (FR-001..006).
- [X] T011 [P] [US1] Escribir el ADR `docs/adr/0002-documentacion-openapi-generada.md` justificando snapshot OpenAPI + generación reproducible (Principio VI; research D3/D5).
- [X] T012 [US1] Validar la historia con quickstart §1 (existen los entregables escritos), §2 (setup de extremo a extremo levanta el stack) y §6 (sin secretos, español, sin cambios de producción).

**Checkpoint**: La documentación escrita está completa y permite entender y arrancar el proyecto (MVP).

---

## Phase 4: User Story 2 - Acceder a Swagger UI desde el sidebar (Priority: P2)

**Goal**: Un ítem visible en el sidebar abre Swagger UI en una pestaña nueva, con URL configurable por entorno.

**Independent Test**: Con el frontend en ejecución, el sidebar muestra el acceso a la API y al activarlo abre Swagger UI; con `VITE_SWAGGER_URL` vacío el ítem se oculta (quickstart §4, §5; contrato `sidebar-swagger-link.md`).

### Tests for User Story 2 ⚠️

> **Escribir primero y verificar que FALLA antes de implementar (Principio IV).**

- [X] T013 [US2] Escribir `frontend/src/components/layout/Sidebar.test.tsx`: el sidebar renderiza el enlace "API (Swagger)" con `href` esperado, `target="_blank"` y `rel="noopener noreferrer"`; con `VITE_SWAGGER_URL` vacío el ítem no se renderiza; los ítems de ruta existentes no cambian (AC-1..AC-5).

### Implementation for User Story 2

- [X] T014 [US2] Extender `NavItem` en `frontend/src/components/layout/navigation.ts` para soportar ítems de enlace externo (`external: true` + `href`) y añadir el ítem "API (Swagger)" con `href = import.meta.env.VITE_SWAGGER_URL ?? '/swagger'`, omitiéndolo si la URL está vacía (FR-007, FR-009, AC-1/AC-3/AC-5).
- [X] T015 [US2] Modificar `frontend/src/components/layout/Sidebar.tsx` para renderizar ítems externos como `<a href target="_blank" rel="noopener noreferrer">` con el mismo estilo (icono+etiqueta; colapsado solo icono con `title`), sin afectar el resaltado de los `NavLink` (FR-008, AC-2, AC-4). Hace pasar T013.
- [X] T016 [P] [US2] Extender el proxy de dev en `frontend/vite.config.ts` para reenviar `/swagger` y `/openapi` al backend (igual que `/api`) y documentar `VITE_SWAGGER_URL` (default `/swagger`) en `frontend/.env.example` (research D4).
- [X] T017 [US2] Ejecutar `npm run test:run` (incluye T013), Biome y React Doctor sin warnings; validar quickstart §4 (un clic abre Swagger) y §5 (tests en verde).

**Checkpoint**: Swagger es descubrible desde el sidebar en un clic, multi-entorno y accesible.

---

## Phase 5: User Story 3 - Probar la API con una colección de Postman (Priority: P3)

**Goal**: Colección de Postman importable que cubre los endpoints principales, parametrizada por `{{baseUrl}}`/`{{token}}` y sin secretos.

**Independent Test**: Importar la colección + el entorno y ejecutar `GET {{baseUrl}}/api/invoices` contra una instancia local (quickstart §3; contrato `postman-collection.md`).

### Implementation for User Story 3

- [X] T018 [US3] Generar `docs/postman/monolegal.postman_collection.json` con `npm run docs:api` (Postman v2.1, carpetas por recurso, auth Bearer `{{token}}`, URLs con `{{baseUrl}}`, cuerpos de ejemplo desde el esquema, sin secretos) y validar cobertura coherente con `api-reference.md` (FR-006, FR-009; contrato `postman-collection.md`). Depende de Phase 2.
- [X] T019 [P] [US3] Crear `docs/postman/monolegal.postman_environment.json` con placeholders (`baseUrl=http://localhost:5155`, `token` vacío), sin credenciales reales (FR-009, SC-006).
- [X] T020 [US3] Validar la historia con quickstart §3: importación sin errores, `GET {{baseUrl}}/api/invoices` devuelve respuesta válida y el entorno no contiene secretos (SC-005, SC-006).

**Checkpoint**: La colección Postman permite probar la API rápidamente sin construir peticiones a mano.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Sincronización en CI y validación global.

- [X] T021 [P] Integrar `npm run docs:api:verify` como check de CI (paso en el workflow existente o nuevo) para detectar documentación generada desincronizada (FR-010, research D5).
- [X] T022 Ejecutar la validación completa del quickstart (§1–§6) y `npm run test:all`; confirmar SC-001…SC-007 (incluye SC-006 sin secretos y SC-007 sin cambios en endpoints/entidades/lógica).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — empieza de inmediato. T002 requiere el backend en Development para capturar el snapshot.
- **Foundational (Phase 2)**: depende de T002 (necesita `docs/openapi.json`). **BLOQUEA** T009 (US1) y T018 (US3).
- **US1 (Phase 3)**: T005–T008, T010, T011 dependen solo de Setup (docs escritas a mano, archivos distintos → paralelizables). T009 depende de Phase 2. T012 cierra la historia.
- **US2 (Phase 4)**: independiente de US1/US3 (toca solo el frontend). T013 (test) antes de T014/T015. T016 es `[P]` (archivos distintos).
- **US3 (Phase 5)**: depende de Phase 2 (T018). T019 es `[P]`. T020 cierra la historia.
- **Polish (Phase 6)**: depende de los artefactos generados (T009/T018) y de las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: arranca tras Setup; T009 además tras Foundational. Es el MVP. Sin dependencias de otras historias.
- **US2 (P2)**: totalmente independiente (frontend). Puede desarrollarse en paralelo con US1/US3.
- **US3 (P3)**: depende de Foundational (generador); independiente de US1/US2 a nivel de archivos.

### Parallel Opportunities

- **US1**: T005, T006, T007, T008, T010, T011 son `[P]` (documentos distintos) y pueden escribirse en paralelo; T009 tras Phase 2.
- **US2**: T016 `[P]` con T014/T015 (archivos distintos). T013 primero.
- **US3**: T019 `[P]` con T018.
- **Entre historias**: una vez hecha la Phase 2, US1, US2 y US3 pueden avanzar en paralelo (tocan conjuntos de archivos disjuntos: `docs/` vs `frontend/` vs `docs/postman/`).

---

## Parallel Example: User Story 1 (documentos escritos)

```bash
# Documentos en archivos distintos, en paralelo:
Task: "Escribir docs/architecture.md"     # T005
Task: "Escribir docs/data-model.md"        # T006
Task: "Escribir docs/setup.md"             # T007
Task: "Escribir docs/deployment.md"        # T008
Task: "Índice docs/README.md + README raíz" # T010
Task: "ADR 0002"                            # T011
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational: generador).
2. Completar Phase 3 (US1): documentación escrita + referencia de endpoints generada.
3. **PARAR y VALIDAR**: quickstart §1, §2, §6. La documentación ya entrega valor por sí sola.

### Incremental Delivery

1. Setup + Foundational → snapshot y generador listos.
2. US1 → documentación escrita + api-reference (MVP) → validar.
3. US2 → acceso a Swagger desde el sidebar → validar.
4. US3 → colección Postman → validar.
5. Polish → verificación en CI + validación global.

### Parallel Team Strategy

Tras la Phase 2, repartir: Dev A → US1 (`docs/`); Dev B → US2 (`frontend/`); Dev C → US3 (`docs/postman/`). Conjuntos de archivos disjuntos ⇒ integración independiente.

---

## Notes

- `[P]` = archivos distintos, sin dependencias. La documentación escrita de US1 es altamente paralelizable; las tareas de frontend de US2 comparten archivos y van en orden.
- No se añaden ni modifican endpoints, entidades ni lógica de negocio; el único código de producción tocado es el ítem de navegación del sidebar (FR-012, SC-007).
- Sin secretos en `docs/` ni en la colección/entorno Postman (SC-006).
- Toda la documentación en español (FR-011); se permite contenido OpenAPI en su forma original.
- Refrescar `docs/openapi.json` y regenerar (`npm run docs:api`) cuando cambien endpoints; `docs:api:verify` detecta divergencias (FR-010).
