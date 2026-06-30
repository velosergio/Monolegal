# Monolegal — Contexto del agente

Proyecto con arquitectura limpia: backend ASP.NET Core 10 (Minimal APIs, MongoDB, FluentValidation, Serilog), worker de transiciones, y frontend React 19 + Vite + TanStack Query + shadcn/ui. Toda la documentación de especificaciones va en español (ver `.specify/memory/constitution.md`).

<!-- SPECKIT START -->
## Feature activo

- **026 — Comentarios de Código y Documentación de Arquitectura**: `specs/026-comentarios-arquitectura/plan.md` (spec, research, data-model, contracts, quickstart en la misma carpeta). Feature de documentación: (1) Clean Architecture en README/`docs/architecture.md` (capas + dirección de dependencias + Mermaid); (2) comentarios SOLID a nivel de clase en clases clave de `backend/` y `worker/` (XML-doc con `SOLID: <PRINCIPIO> — <justificación>`, hueco principal: 0 hoy); (3) mapa de Inyección de Dependencias en `docs/dependency-injection.md` (abstracción → impl → ciclo de vida) sincronizado con `backend/Infrastructure/Configuration/DependencyInjection.cs` + `Program.cs`; (4) repositorio de ADRs en `docs/adr/` (índice `README.md` + `0000-plantilla.md` + ADRs retroactivos de decisiones no obvias). Solo documentación/comentarios; sin refactor funcional.
<!-- SPECKIT END -->
