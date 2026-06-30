# Implementation Plan: Test Runner Unificado

**Branch**: `024-test-runner-unificado` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/024-test-runner-unificado/spec.md`

## Summary

Establecer un **punto de entrada único** (un solo comando) que ejecute las cuatro suites de pruebas del proyecto —backend (`dotnet test` en `backend/`), worker (`dotnet test` en `worker/`), frontend (`vitest run`) y E2E (`playwright test`)— de forma multiplataforma (Windows/Linux), produzca un **reporte consolidado PASS/FAIL por suite** y devuelva un **código de salida agregado** apto para CI (cero solo si todas pasan).

**Enfoque técnico**: un orquestador escrito en **Node.js puro** (sin dependencias nuevas) en `scripts/test-all.mjs`, expuesto mediante un `package.json` en la raíz con el script `test:all`. Node ya es un prerrequisito del frontend y está disponible en local y en CI, lo que garantiza el comportamiento idéntico entre plataformas sin scripts `.sh`/`.ps1` paralelos. El orquestador lanza cada suite como proceso hijo, ejecuta **todas** antes de terminar (no fail-fast, para reportar el estado real de las cuatro), imprime un resumen final y termina con código distinto de cero si alguna falla. No se añade, elimina ni modifica ninguna prueba ni código de producción.

## Technical Context

**Language/Version**: Node.js 22+ (orquestador, vía `.node-version`); .NET SDK 10.0.301 (`global.json`) para las suites de backend/worker

**Primary Dependencies**: Ninguna nueva. Reutiliza herramientas ya presentes: `dotnet` CLI, `npm`/`vitest`, `@playwright/test`. El orquestador usa solo módulos integrados de Node (`node:child_process`, `node:process`)

**Storage**: N/A (orquestación de procesos; sin persistencia propia)

**Testing**: Suites existentes — xUnit + Shouldly (backend `backend/Tests`), xUnit (worker `worker/Tests`), Vitest + Testing Library (frontend), Playwright (E2E `frontend/e2e`)

**Target Platform**: Windows (desarrollo local del equipo) y Linux (CI GitHub Actions, `ubuntu-latest`)

**Project Type**: Monorepo web (backend .NET + worker .NET + frontend React) con tooling de orquestación de pruebas en la raíz

**Performance Goals**: N/A funcional. La duración total es la suma de las suites; el orquestador añade sobrecarga despreciable (lanzar procesos y agregar resultados)

**Constraints**: No interactivo (apto para CI); multiplataforma sin scripts por plataforma; código de salida agregado fiable; ninguna suite omitida silenciosamente; suite no ejecutable = FAIL (nunca PASS)

**Scale/Scope**: 4 suites; 1 orquestador (~1 archivo) + 1 `package.json` raíz + 1 workflow CI opcional + documentación. Sin cambios en pruebas ni en producción

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Arquitectura Limpia**: ✅ El orquestador es una utilidad de tooling externa a las capas Domain/Application/Infrastructure/Api; no introduce dependencias entre capas ni toca código de producción.
- **II. SOLID**: ✅ Responsabilidad única (orquestar y agregar resultados); cada suite se define de forma declarativa y extensible (añadir una suite = añadir una entrada a la lista, sin modificar la lógica de ejecución → Open/Closed).
- **III. Documentación en español (SDD)**: ✅ Spec, plan y todos los artefactos en español. La spec deriva del roadmap Spec 5.5 en formato GIVEN/WHEN/THEN.
- **IV. Test-First / CI Gate**: ✅ Esta feature **materializa** el CI Gate ("sin merge sin pasar todas las suites"). No se permiten skips: una suite no ejecutable cuenta como FAIL. No añade pruebas nuevas porque su objeto **es** ejecutar las existentes; su propia corrección se valida vía el quickstart (casos PASS/FAIL/exit code).
- **V. Frontend de Calidad**: ✅ No modifica el frontend de producción; solo invoca `vitest run` y `playwright test` ya configurados.
- **VI. Observable y Mantenible**: ✅ Salida estructurada y legible (resumen por suite + veredicto global); el mecanismo (Node script + `package.json` raíz) queda documentado en quickstart y contrato.

**Resultado**: PASS. Sin violaciones; sección Complexity Tracking no aplica.

## Project Structure

### Documentation (this feature)

```text
specs/024-test-runner-unificado/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0 (/speckit-plan)
├── data-model.md        # Fase 1 (/speckit-plan)
├── quickstart.md        # Fase 1 (/speckit-plan)
├── contracts/
│   └── test-runner.md   # Contrato del orquestador (CLI: invocación, salida, exit codes)
└── tasks.md             # Fase 2 (/speckit-tasks — NO lo crea /speckit-plan)
```

### Source Code (repository root)

```text
package.json             # NUEVO — punto de entrada raíz; script "test:all" → node scripts/test-all.mjs
scripts/
└── test-all.mjs         # NUEVO — orquestador Node: define las 4 suites, ejecuta, agrega, reporta

.github/workflows/
└── test-all.yml         # NUEVO (opcional) — workflow CI que levanta Mongo+backend y corre `npm run test:all`

# Suites existentes que el orquestador invoca (SIN cambios):
backend/   → dotnet test            (backend/Tests: xUnit + integración)
worker/    → dotnet test            (worker/Tests: xUnit)
frontend/  → npm run test:run       (vitest run)
frontend/  → npm run test:e2e       (playwright test, e2e/)
```

**Structure Decision**: Monorepo web existente. Se añade tooling de orquestación en la **raíz** (`package.json` + `scripts/test-all.mjs`) por ser el único nivel que abarca backend, worker y frontend. Se elige **Node.js** como runtime del orquestador porque ya es prerrequisito (frontend) y ofrece la misma semántica en Windows y Linux sin duplicar scripts por plataforma — superando a un par `.ps1`/`.sh` (paridad frágil) o a `npm-run-all`/`concurrently` (dependencia extra y reporte/exit-code menos controlable). El `package.json` raíz se mantiene mínimo (`private: true`, sin dependencias) para no interferir con el `frontend/package.json`.

## Complexity Tracking

> No aplica. La Constitution Check pasó sin violaciones.
