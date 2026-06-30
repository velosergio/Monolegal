# Monolegal — Contexto del agente

Proyecto con arquitectura limpia: backend ASP.NET Core 10 (Minimal APIs, MongoDB, FluentValidation, Serilog), worker de transiciones, y frontend React 19 + Vite + TanStack Query + shadcn/ui. Toda la documentación de especificaciones va en español (ver `.specify/memory/constitution.md`).

<!-- SPECKIT START -->
## Feature activo

- **025 — Documentación de API y del Proyecto**: `specs/025-documentacion-api/plan.md` (spec, research, data-model, contracts, quickstart en la misma carpeta). Documentación consolidada en `docs/` (architecture, data-model/ERD, api-reference, setup, deployment) con diagramas Mermaid; `api-reference.md` y la colección Postman se **generan** desde `docs/openapi.json` con `scripts/gen-api-docs.mjs` (Node, sin deps); acceso a Swagger UI desde el sidebar (ítem de enlace externo, `VITE_SWAGGER_URL`, default `/swagger`).
<!-- SPECKIT END -->
