# Monolegal — Contexto del agente

Proyecto con arquitectura limpia: backend ASP.NET Core 10 (Minimal APIs, MongoDB, FluentValidation, Serilog), worker de transiciones, y frontend React 19 + Vite + TanStack Query + shadcn/ui. Toda la documentación de especificaciones va en español (ver `.specify/memory/constitution.md`).

<!-- SPECKIT START -->
## Feature activo

- **024 — Test Runner Unificado**: `specs/024-test-runner-unificado/plan.md` (spec, research, data-model, contracts, quickstart en la misma carpeta). Comando único `npm run test:all` → `node scripts/test-all.mjs` que orquesta las 4 suites (backend, worker, frontend, E2E) con reporte consolidado y exit code agregado.
<!-- SPECKIT END -->
