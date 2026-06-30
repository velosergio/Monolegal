# Implementation Plan: Tests E2E con Playwright

**Branch**: `023-tests-e2e-playwright` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/023-tests-e2e-playwright/spec.md`

## Summary

Cerrar la **Spec 5.4 (E2E Tests - Playwright)** del roadmap estableciendo la primera suite de pruebas end-to-end del proyecto. Hoy no existe nada de Playwright ni infraestructura E2E; solo hay pruebas unitarias (xUnit), de integración (xUnit + WebApplicationFactory) y de componentes (Vitest). Esta feature ejercita la aplicación fullstack real (frontend servido + backend ASP.NET + MongoDB) a través de un navegador, cubriendo la jornada crítica del usuario: **abrir lista de facturas → filtrar por estado → transición manual → ver dashboard actualizado**.

El enfoque: Playwright Test (`@playwright/test`) en una nueva carpeta `frontend/e2e/`, con `webServer` orquestando el frontend (Vite `preview` sobre el build, o `dev`) y un backend en entorno Development (que siembra datos deterministas de forma idempotente y expone el endpoint de reset `POST /api/settings/maintenance/flush-database`). Las pruebas se anclan a roles/etiquetas accesibles y textos visibles ya existentes (`aria-label="Filtrar por estado"`, `aria-label="Nuevo estado"`, botón "Cambiar Estado", badges de estado). El estado de datos se restablece a un punto conocido antes de la corrida mediante el endpoint de flush + sembrado. No se modifica código de producción; el alcance es exclusivamente añadir pruebas, configuración y utilidades de prueba, más un script npm dedicado apto para CI.

## Technical Context

**Language/Version**: TypeScript 6 (strict) / Node 22+. Pruebas E2E en TypeScript ejecutadas por el runner de Playwright.

**Primary Dependencies**: `@playwright/test` (runner + navegadores). Reutiliza el frontend existente (React 19 + Vite 8) y el backend ASP.NET Core 10. Sin nuevas dependencias de producción.

**Storage**: MongoDB (real, vía backend en Development). Estado restablecido por `POST /api/settings/maintenance/flush-database` (vacía + reconstruye índices + re-siembra datos deterministas).

**Testing**: Playwright Test. Ejecución vía `npm run test:e2e` (`playwright test`) desde `frontend/`. `playwright.config.ts` con `webServer` para levantar frontend (y, opcionalmente, asumir backend ya levantado en `:5155`). Convive con Vitest sin solaparse (Vitest ignora `e2e/`, Playwright solo corre `e2e/`).

**Target Platform**: Navegadores de escritorio gestionados por Playwright (Chromium como mínimo; Firefox/WebKit opcionales). Multiplataforma Windows/Linux para desarrollo y CI.

**Project Type**: Web application (frontend React + backend ASP.NET + worker). Esta feature toca exclusivamente la capa de pruebas del frontend (carpeta `e2e/`) y consume el backend como caja negra.

**Performance Goals**: Suite E2E acotada a los 4 flujos críticos; objetivo de ejecución en pocos minutos en CI. Sin objetivos de carga (no es prueba de rendimiento).

**Constraints**: Pruebas **deterministas y sin flakiness** (Principio IV); estado de datos reproducible por reset+seed antes de la corrida; selectores estables por rol/etiqueta accesible y texto visible (no por estructura interna ni clases CSS); sin pruebas omitidas (`.skip`/`.only`); sin tocar código de producción; documentación y nombres de prueba en español; el frontend habla con el backend vía el proxy `/api` ya configurado.

**Scale/Scope**: 3 archivos de spec E2E (facturas+filtro, transición manual, dashboard) con ~6–9 casos en total; 1 `playwright.config.ts`; utilidades de fixture (reset de datos, helpers de navegación/localizadores). Sin nuevos proyectos backend.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación |
|-----------|------------|
| **I. Arquitectura Limpia** | ✅ Las pruebas E2E viven en la capa de tests del frontend (`frontend/e2e/`) y tratan al sistema como caja negra a través de su interfaz pública (UI + API HTTP). No introducen lógica de negocio ni acoplan a internals de ninguna capa. |
| **II. SOLID** | ✅ No aplica a código de producción (no se modifica). Las utilidades de prueba se diseñan con responsabilidad única (fixture de reset, helpers de localización) y se inyectan vía fixtures de Playwright. |
| **III. SDD** | ✅ Feature nace de spec GIVEN/WHEN/THEN (spec.md 023) derivada de la Spec 5.4 del roadmap; criterios trazables a casos E2E (ver `contracts/e2e-flow-matrix.md`). Documentación en español. |
| **IV. Test-First** | ✅ La feature **es** la capa E2E exigida explícitamente por el Principio IV ("E2E Tests: jornadas críticas del usuario — listar → filtrar → transicionar → confirmar"; "CI Gate: sin merge sin pasar todas las suites"). Sin skips; resultado con código de salida apto para CI; flakiness tratada como defecto. |
| **V. Frontend Calidad** | ✅ Refuerza la calidad: valida accesibilidad de facto al localizar por roles/etiquetas, y la jornada crítica de extremo a extremo. Biome debe seguir 100% compliant en los nuevos archivos `.ts`. |
| **VI. Observable/Mantenible** | ✅ La suite documenta de forma ejecutable la jornada crítica y sirve de regresión; trazas/artefactos de Playwright (trace, screenshot, video on failure) facilitan el diagnóstico sin acoplamiento oculto. |

**Resultado**: PASS. Sin violaciones que requieran justificación en Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/023-tests-e2e-playwright/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0: decisiones técnicas (runner, orquestación de servidores, datos, selectores)
├── data-model.md        # Fase 1: estados/transiciones, datos sembrados y entidades de prueba
├── quickstart.md        # Fase 1: cómo levantar el entorno y ejecutar/validar la suite E2E
├── contracts/
│   └── e2e-flow-matrix.md  # Fase 1: matriz flujo crítico → casos → localizadores/aserciones
├── checklists/
│   └── requirements.md  # Checklist de calidad de la spec (de /speckit-specify)
└── tasks.md             # Fase 2 (/speckit-tasks — NO creado aquí)
```

### Source Code (repository root)

```text
frontend/
├── package.json                 # + devDependency @playwright/test; + script "test:e2e"
├── playwright.config.ts         # NUEVO: testDir=e2e, webServer (frontend), baseURL, proyectos de navegador
├── vite.config.ts               # Sin cambios (proxy /api → backend ya configurado)
├── src/                         # Sin cambios (código de producción intacto)
└── e2e/                         # NUEVO: suite end-to-end
    ├── fixtures/
    │   ├── reset-data.ts        # Fixture: resetea BD vía POST /api/settings/maintenance/flush-database
    │   └── test.ts              # Fixture base de Playwright (extiende test con reset + helpers)
    ├── pages/                   # (Opcional) page objects / helpers de localización por rol/etiqueta
    │   ├── invoices.page.ts
    │   └── dashboard.page.ts
    ├── invoices-list-filter.spec.ts   # US1: abrir lista + filtrar por estado
    ├── manual-transition.spec.ts      # US2: transición manual (no terminal) + estado terminal
    └── dashboard-updated.spec.ts      # US3: dashboard refleja la transición

backend/                         # Sin cambios. Consumido como caja negra en Development:
                                 #   - DevDataSeeder (idempotente) siembra 3 clientes / 8 facturas
                                 #   - POST /api/settings/maintenance/flush-database (reset + re-seed)
```

**Structure Decision**: Web application; el trabajo se ubica exclusivamente en la capa de pruebas del frontend, en una carpeta nueva `frontend/e2e/` separada de las pruebas de componentes (`frontend/tests/`, Vitest). Playwright se configura para correr solo `e2e/` y Vitest se mantiene sobre `frontend/tests/` (sin solape de runners). El backend se trata como caja negra en entorno Development, aprovechando el sembrado idempotente y el endpoint de flush ya existentes para crear un estado de datos conocido. No se crean proyectos nuevos ni se modifica código de producción ni la configuración de Vite/Vitest.

## Complexity Tracking

> No aplica. La Constitution Check pasa sin violaciones; no se introducen patrones adicionales, proyectos nuevos ni cambios en código de producción. La única dependencia nueva (`@playwright/test`) es de desarrollo y está mandada por el Principio IV (E2E Tests) y el stack de la constitución (Playwright para E2E).
