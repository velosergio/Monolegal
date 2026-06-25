---
description: "Lista de tareas para la implementación de Dependencias Frontend"
---

# Tasks: Dependencias Frontend

**Input**: Documentos de diseño en `/specs/003-frontend-dependencies/`

**Prerequisites**: [plan.md](plan.md) (requerido), [spec.md](spec.md) (historias de usuario), [research.md](research.md), [data-model.md](data-model.md), [contracts/dependency-matrix.md](contracts/dependency-matrix.md), [quickstart.md](quickstart.md)

**Tests**: Esta feature es de configuración de dependencias. Las "pruebas" son **smoke tests de disponibilidad** (Vitest + Testing Library) que verifican que cada paquete resuelve y es utilizable (derivadas de los Escenarios de Aceptación y de [quickstart.md](quickstart.md)). Se incluyen porque son el mecanismo de verificación de la propia feature y porque la constitución exige Test-First (Principio IV).

**Organización**: Tareas agrupadas por historia de usuario para implementación y verificación independiente. Orden de fases por prioridad: P1 (US1, US2, US3, US4, US6, US7) y luego P2 (US5 — Motion).

## Formato: `[ID] [P?] [Story] Descripción`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US7)
- Rutas de archivo exactas incluidas en cada descripción

## Convenciones de Rutas

Proyecto frontend SPA en `frontend/` (ver [plan.md](plan.md)): `frontend/src`, `frontend/tests`, configs en `frontend/` (`package.json`, `vite.config.ts`, `vitest.config.ts`, `tsconfig.json`, `biome.json`, `components.json`). Smoke tests en `frontend/tests/dependencies/`.

---

## Phase 1: Setup (Infraestructura Compartida)

**Propósito**: Establecer línea base reproducible antes de modificar dependencias.

- [X] T001 Verificar que Node.js y npm están disponibles ejecutando `node --version` y `npm --version` en `frontend/`
- [X] T002 Instalar dependencias base existentes con `npm install` en `frontend/` y confirmar ausencia de conflictos de versión
- [X] T003 [P] Build baseline con `npm run build` en `frontend/` para registrar el estado inicial pre-cambios
- [X] T004 [P] Ejecución baseline de pruebas con `npm run test:run` en `frontend/` para confirmar runner operativo

**Checkpoint**: El proyecto frontend instala, compila y ejecuta pruebas en su estado actual (pre-cambios).

---

## Phase 2: Foundational (Prerequisitos Bloqueantes)

**Propósito**: Añadir todas las dependencias nuevas y habilitar los matchers de DOM, prerequisito transversal de las smoke tests de las historias.

**⚠️ CRÍTICO**: Las smoke tests de US3, US4 y US5 requieren los paquetes instalados aquí; ninguna puede ejecutarse hasta completar esta fase.

- [X] T005 Añadir a `dependencies` de `frontend/package.json`: `@tanstack/react-query` (^5), `motion`, `tailwindcss` (^4), `@tailwindcss/vite` (^4), `clsx`, `tailwind-merge`, `class-variance-authority`, `lucide-react`; y a `devDependencies`: `@testing-library/jest-dom` (decisiones 1, 3, 4, 5 de research.md)
- [X] T006 Ejecutar `npm install` en `frontend/` y confirmar que todas las dependencias nuevas resuelven sin conflictos de versión (FR-009, RV-010)
- [X] T007 Crear `frontend/tests/setup.ts` con `import '@testing-library/jest-dom'` y registrarlo en `test.setupFiles` de `frontend/vitest.config.ts` (decisión 5 de research.md)
- [X] T008 [P] Crear carpeta `frontend/tests/dependencies/` para alojar las smoke tests de disponibilidad

**Checkpoint**: Dependencias instaladas y matchers de DOM disponibles — las historias pueden implementarse/verificarse en paralelo.

---

## Phase 3: User Story 1 - Base de UI con React y TypeScript (Priority: P1) 🎯 MVP

**Goal**: React 19+ y TypeScript en modo estricto están disponibles y un componente tipado compila sin errores.

**Independent Test**: Renderizar un componente tipado trivial sin errores de tipos; `tsconfig.json` con `strict: true` y `noImplicitAny: true`.

### Implementación US1

- [X] T009 [US1] Verificar que `react` y `react-dom` resuelven a versión mayor ≥ 19 con `npm ls react react-dom` en `frontend/` (FR-001, RV-001)
- [X] T010 [US1] Verificar que `frontend/tsconfig.json` tiene `compilerOptions.strict === true` y `noImplicitAny === true` (FR-002, RV-002, SC-005)
- [X] T011 [P] [US1] Smoke test: renderizar un componente tipado trivial con Testing Library en `frontend/tests/dependencies/react-availability.test.tsx`, aseverando que aparece en el DOM (matcher jest-dom)

**Checkpoint**: US1 funcional — base de UI tipada disponible y verificada.

---

## Phase 4: User Story 2 - Herramienta de Build y Dev Server (Priority: P1)

**Goal**: Vite (sin webpack) está operativo para servidor de desarrollo y build de producción.

**Independent Test**: `npm run build` genera artefactos y `npm run dev` arranca sin errores de dependencias.

### Implementación US2

- [X] T012 [US2] Verificar que `vite` (^8) y `@vitejs/plugin-react` están en `frontend/package.json` y que `frontend/vite.config.ts` registra el plugin de React (FR-003, RV-003)
- [X] T013 [US2] Verificar arranque del dev server con `npm run dev` en `frontend/` (sirve en `http://localhost:5173`; detener con Ctrl+C) y build con `npm run build` (genera `dist/` sin errores)

**Checkpoint**: US2 funcional — Vite operativo para dev y build.

---

## Phase 5: User Story 3 - Sistema de Componentes y Estilos (Priority: P1)

**Goal**: shadcn/ui está inicializado sobre Tailwind v4 con dark mode built-in y un componente se renderiza respetando el tema.

**Independent Test**: Añadir y renderizar un componente shadcn; `cn()` disponible; clase `.dark` alterna el tema sin variantes `dark:`.

### Implementación US3

- [X] T014 [US3] Configurar `frontend/vite.config.ts`: añadir el plugin `tailwindcss()` de `@tailwindcss/vite` y el alias `@ → ./src` en `resolve.alias` (decisión 1 de research.md)
- [X] T015 [US3] Añadir el alias de rutas `"@/*": ["./src/*"]` (`baseUrl`/`paths`) a `frontend/tsconfig.json` (RV-004)
- [X] T016 [US3] Configurar Tailwind v4 en `frontend/src/index.css` con el patrón de 4 pasos: `@import "tailwindcss"`, variables `:root`/`.dark` con `hsl()` a nivel raíz, bloque `@theme inline` mapeando todas las variables, y `@layer base` con `var(--x)` sin doble-wrap (decisión 1 de research.md)
- [X] T017 [US3] Crear `frontend/components.json` (con `"tailwind.config": ""`, `cssVariables: true`, `css: "src/index.css"`) y `frontend/src/lib/utils.ts` exportando `cn()` (clsx + tailwind-merge); confirmar que **no** existe `frontend/tailwind.config.ts` (RV-004, INV-2)
- [X] T018 [US3] Añadir un componente shadcn/ui base (p. ej. `button`) a `frontend/src/components/ui/` vía la CLI de shadcn
- [X] T019 [US3] Crear `frontend/src/components/theme-provider.tsx` (tema light/dark/system con persistencia en `localStorage`, clave `ml-ui-theme`, aplicando la clase `.dark` en `<html>`) y envolver la app en `frontend/src/main.tsx` (decisión 2 de research.md, FR-004, RV-005)
- [X] T020 [P] [US3] Smoke test: renderizar el componente shadcn y usar `cn()` en `frontend/tests/dependencies/shadcn-availability.test.tsx`, aseverando que el componente aparece en el DOM

**Checkpoint**: US3 funcional — shadcn/ui + Tailwind v4 + dark mode built-in disponibles y verificados.

---

## Phase 6: User Story 4 - Gestión de Estado de Servidor (Priority: P1)

**Goal**: TanStack Query está disponible y su proveedor envuelve la app.

**Independent Test**: Configurar `QueryClientProvider` sin errores de tipos; una `useQuery` compila y ejecuta.

### Implementación US4

- [X] T021 [US4] Verificar que `@tanstack/react-query` (^5) está en `frontend/package.json` y crear un `QueryClient` + envolver la app con `QueryClientProvider` en `frontend/src/main.tsx` (FR-005, RV-006)
- [X] T022 [P] [US4] Smoke test: renderizar un componente que use `useQuery` (con `queryFn` que resuelve un valor) dentro de un `QueryClientProvider` en `frontend/tests/dependencies/tanstack-query-availability.test.tsx`, aseverando el estado resuelto

**Checkpoint**: US4 funcional — estado de servidor disponible y proveedor configurado.

---

## Phase 7: User Story 6 - Framework de Pruebas Frontend (Priority: P1)

**Goal**: Vitest + Testing Library están operativos con matchers de DOM legibles.

**Independent Test**: `vitest run` descubre y ejecuta una prueba que renderiza un componente y usa un matcher de jest-dom.

### Implementación US6

- [X] T023 [US6] Verificar que `vitest` (^4), `@testing-library/react`, `@testing-library/user-event` y `jsdom` están en `frontend/package.json`, y que `frontend/vitest.config.ts` referencia `tests/setup.ts` en `setupFiles` (T007) con `environment: 'jsdom'` (FR-007, RV-008)
- [X] T024 [P] [US6] Smoke test del framework: prueba que renderiza un componente, simula interacción con `user-event` y asevera con un matcher de jest-dom (`toBeInTheDocument`) en `frontend/tests/dependencies/testing-library-availability.test.tsx`

**Checkpoint**: US6 funcional — framework de pruebas + matchers legibles operativo.

---

## Phase 8: User Story 7 - Herramienta de Linting y Formateo (Priority: P1)

**Goal**: Biome está instalado y configurado, y `biome check` se ejecuta sin fallo de herramienta.

**Independent Test**: `npm run lint` (`biome check .`) analiza el código y reporta resultados sin fallo de ejecución.

### Implementación US7

- [X] T025 [US7] Verificar que `@biomejs/biome` (^2) está en `frontend/package.json` y que existe `frontend/biome.json` con linter y formatter habilitados (FR-008, RV-009)
- [X] T026 [US7] Ejecutar `npm run lint` (`biome check .`) en `frontend/` y confirmar que la herramienta analiza el código sin fallo de ejecución (SC-006)

**Checkpoint**: US7 funcional — Biome operativo como gate de calidad.

---

## Phase 9: User Story 5 - Librería de Animaciones (Priority: P2)

**Goal**: Motion está disponible y un componente animado compila y renderiza.

**Independent Test**: Renderizar un `motion.*` sin errores de tipos.

### Implementación US5

- [X] T027 [US5] Verificar que `motion` está en `frontend/package.json` con `npm ls motion` en `frontend/` (FR-006, RV-007)
- [X] T028 [P] [US5] Smoke test: renderizar un `motion.div` (import desde `motion/react`) con una animación trivial en `frontend/tests/dependencies/motion-availability.test.tsx`, aseverando que aparece en el DOM

**Checkpoint**: US5 funcional — animaciones disponibles.

---

## Phase 10: Polish & Verificación Transversal

**Propósito**: Validar la fase completa contra los criterios de éxito de la spec y el contrato.

- [X] T029 Ejecutar `npm run test:run` en `frontend/` y confirmar que las 5 smoke tests (T011, T020, T022, T024, T028) más la prueba existente pasan en verde (SC-004)
- [X] T030 Verificar las cláusulas C1–C4 de [contracts/dependency-matrix.md](contracts/dependency-matrix.md): dependencias declaradas, configs requeridas (incl. ausencia de `tailwind.config.ts`), e invariantes (dark mode built-in, sin webpack, cero `any`) (SC-001, SC-005)
- [X] T031 [P] Ejecutar los Pasos 1–7 de [quickstart.md](quickstart.md) (install, verificar presentes/añadidas, configs shadcn, build, dev, test, lint) y confirmar resultados esperados (SC-002, SC-003, SC-006)
- [X] T032 [P] Verificar que las 8 dependencias objetivo están referenciadas/configuradas según la matriz de [data-model.md](data-model.md) (SC-001, FR-010)

---

## Dependencies & Execution Order

### Dependencias de Fase

- **Setup (Phase 1)**: Sin dependencias — inicia de inmediato
- **Foundational (Phase 2)**: Depende de Setup — **BLOQUEA** las smoke tests que usan paquetes nuevos (US3, US4, US5) y el setup de jest-dom (usado por todas)
- **User Stories (Phase 3–9)**: Dependen de Foundational
  - US1, US2, US6, US7 son mayormente **verificación** (paquetes ya presentes) y pueden ejecutarse en paralelo entre sí
  - US3, US4, US5 **añaden/configuran** y dependen de los paquetes de Foundational
  - Orden recomendado por prioridad: US1 → US2 → US3 → US4 → US6 → US7 → US5
- **Polish (Phase 10)**: Depende de que todas las historias deseadas estén completas

### Dependencias entre Historias

- **US3** modifica `vite.config.ts`, `tsconfig.json`, `src/index.css`, `src/main.tsx` (theme provider) — es la historia más amplia.
- **US4** también modifica `src/main.tsx` (QueryClientProvider) → **coordinar con US3** para evitar conflicto en el mismo archivo (no marcar T019 y T021 como `[P]` entre sí).
- El resto de historias tocan archivos/configs distintos y son independientes.
- **Dependencia transversal**: todas las smoke tests usan `tests/setup.ts` (jest-dom, T007, en Foundational).

### Dentro de Cada Historia

- Verificación/configuración antes de su smoke test.

### Oportunidades de Paralelización

- Setup: T003 y T004 en paralelo.
- Tras Foundational, las smoke tests T011, T020, T022, T024, T028 son `[P]` (archivos distintos en `tests/dependencies/`).
- US1, US2, US6, US7 (verificación) pueden repartirse entre desarrolladores.
- En Polish, T031 y T032 son `[P]`.
- **Excepción**: T019 (US3) y T021 (US4) tocan `src/main.tsx` — ejecutar secuencialmente.

---

## Parallel Example: Smoke tests tras Foundational

```bash
# Una vez completada la Phase 2 (paquetes instalados + jest-dom), lanzar las smoke tests en paralelo:
Task: "Smoke test React en frontend/tests/dependencies/react-availability.test.tsx"
Task: "Smoke test shadcn/ui en frontend/tests/dependencies/shadcn-availability.test.tsx"
Task: "Smoke test TanStack Query en frontend/tests/dependencies/tanstack-query-availability.test.tsx"
Task: "Smoke test Testing Library en frontend/tests/dependencies/testing-library-availability.test.tsx"
Task: "Smoke test Motion en frontend/tests/dependencies/motion-availability.test.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRÍTICO — instala paquetes + habilita verificación)
3. Completar Phase 3: US1 (base de UI tipada disponible)
4. **DETENER y VALIDAR**: ejecutar la smoke test de US1 de forma independiente
5. Continuar con las siguientes historias

### Incremental Delivery

1. Setup + Foundational → base lista (paquetes instalados, jest-dom disponible)
2. US1 → smoke test verde → base de UI lista (MVP)
3. US2 → Vite verificado (dev + build)
4. US3 → shadcn/ui + Tailwind v4 + dark mode configurados
5. US4 → TanStack Query disponible (proveedor)
6. US6 → framework de pruebas confirmado
7. US7 → Biome confirmado
8. US5 → Motion disponible (P2)
9. Polish → verificación transversal contra SC-001..SC-007

### Parallel Team Strategy

Tras Foundational, repartir: Dev A→US3 (la más amplia), Dev B→US4, Dev C→US1/US2/US6/US7 (verificación), Dev D→US5. Coordinar US3 y US4 por el archivo compartido `src/main.tsx`.

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes.
- Historias de **verificación** (paquetes ya presentes de la Fase 0.1): US1, US2, US6, US7. Historias que **añaden/configuran**: US3 (shadcn/ui + Tailwind v4 + dark mode), US4 (TanStack Query provider), US5 (Motion), más el setup de jest-dom en Foundational.
- Las smoke tests no requieren servicios externos; solo comprueban que los paquetes resuelven y son utilizables en el entorno jsdom.
- Tailwind v4 es CSS-first: **no** debe crearse `tailwind.config.ts` (decisión 1 de research.md; gotcha del stack probado).
- Documentación de requisitos y comentarios de specs en español (constitución, Principio III).
- Hacer commit tras cada tarea o grupo lógico, referenciando la spec (ej. `feat(spec-0.3): inicializar shadcn/ui con Tailwind v4`).
