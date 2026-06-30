# Implementation Plan: Comentarios de Código y Documentación de Arquitectura

**Branch**: `026-comentarios-arquitectura` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/026-comentarios-arquitectura/spec.md`

## Summary

Consolidar y completar la documentación de arquitectura y calidad de código del proyecto: (1) verificar que el README y `docs/architecture.md` explican la Clean Architecture (capas, dirección de dependencias, diagramas); (2) añadir comentarios a nivel de clase que declaren el/los principio(s) SOLID en las clases clave del backend y worker (hueco principal: **0 comentarios SOLID hoy**); (3) producir un mapa de Inyección de Dependencias que liste cada abstracción → implementación concreta → ciclo de vida, sincronizado con `DependencyInjection.cs`/`Program.cs`; (4) formalizar el repositorio de ADRs (`docs/adr/`) con índice y plantilla, registrando retroactivamente las decisiones no obvias vigentes. Feature exclusivamente de documentación y comentarios; sin refactorizaciones funcionales.

## Technical Context

**Language/Version**: C# / .NET 10 (backend + worker); TypeScript strict (frontend); Markdown + Mermaid (documentación)

**Primary Dependencies**: Documentación existente en `docs/` (architecture.md, data-model.md, adr/), README.md, registro DI en `backend/Infrastructure/Configuration/DependencyInjection.cs` y `backend/Api/Program.cs`; worker en `worker/`

**Storage**: N/A (feature de documentación; sin cambios de persistencia)

**Testing**: Verificación manual/checklist en code review; validación de sincronía mapa DI ↔ registro real; sin nuevas suites automatizadas requeridas. Build del backend debe seguir verde tras añadir comentarios (`dotnet build`).

**Target Platform**: Repositorio versionado (documentación) + código fuente backend/worker

**Project Type**: Aplicación web multi-proyecto (backend ASP.NET Core 10 + worker + frontend React 19) — documentación transversal

**Performance Goals**: N/A (documentación). Criterio temporal de usuario: comprensión de arquitectura en < 15 min (SC-001)

**Constraints**: Toda la documentación en español (Constitución §III); configuración DI centralizada (§VI); documentos vivos sincronizados con el código; no introducir secretos ni detalles sensibles en docs

**Scale/Scope**: 4 capas backend (Domain/Application/Infrastructure/Api) + worker + frontend; ~clases clave en Application/Services, Application/Notifications, Domain/Services, Infrastructure/Repositories, Infrastructure/Email, Infrastructure/Workers; ~2 ADRs existentes + nuevos retroactivos; 1 mapa DI

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Esta feature **implementa directamente** el Principio VI (Código Observable y Mantenible) y refuerza I y II. Evaluación de puertas:

| Principio | Aplicación en esta feature | Estado |
|-----------|----------------------------|--------|
| I. Arquitectura Limpia | La documentación describe las capas y la dirección de dependencias reales; no altera la estructura | ✅ Cumple (la documenta) |
| II. SOLID | Añade comentarios que declaran el principio SOLID por clase clave; no modifica diseño | ✅ Cumple (lo evidencia) |
| III. SDD + Documentación en español | Spec en GIVEN/WHEN/THEN; todos los artefactos en español | ✅ Cumple |
| IV. Test-First | Feature de documentación: sin código funcional nuevo. El build del backend debe permanecer verde tras los comentarios | ✅ N/A funcional; build verificado |
| V. Frontend calidad producción | Sin cambios de código frontend (solo posible referencia en docs) | ✅ N/A |
| VI. Código Observable y Mantenible | Núcleo de la feature: diagramas en README, comentarios SOLID/DI, ADRs | ✅ Cumple (objetivo directo) |

**Resultado**: PASS. Sin violaciones. No requiere sección de Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/026-comentarios-arquitectura/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0: decisiones de enfoque
├── data-model.md        # Fase 1: entidades documentales (comentario SOLID, mapa DI, ADR)
├── quickstart.md        # Fase 1: guía de validación
├── contracts/           # Fase 1: formatos/plantillas (convención SOLID, esquema mapa DI, plantilla ADR)
│   ├── convencion-comentarios-solid.md
│   ├── esquema-mapa-di.md
│   └── plantilla-adr.md
└── tasks.md             # Fase 2 (/speckit-tasks — NO lo crea /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/                 # Entidades, Enums, Interfaces, Repositories, Services, Email
│   └── Services/           # ← comentarios SOLID (InvoiceTransitionService, etc.)
├── Application/
│   ├── Services/           # ← comentarios SOLID (casos de uso)
│   ├── Notifications/      # ← comentarios SOLID (InvoiceTransitionNotifier, resolvers)
│   ├── Validation/         # ← comentarios SOLID (validadores)
│   └── Abstractions/       # interfaces de aplicación
├── Infrastructure/
│   ├── Configuration/      # DependencyInjection.cs (fuente del mapa DI)
│   ├── Repositories/       # ← comentarios SOLID (Mongo*Repository)
│   ├── Email/              # ← comentarios SOLID (proveedores, factory)
│   └── Workers/            # ← comentarios SOLID (InvoiceTransitionsWorker)
└── Api/
    └── Program.cs          # composición/registro DI adicional

worker/                     # Worker independiente (mismas convenciones SOLID/DI)

docs/
├── README.md               # índice de documentación
├── architecture.md         # capas + dirección dependencias + Mermaid (ampliar si falta DI/SOLID)
├── dependency-injection.md # ← NUEVO: mapa DI (abstracción → impl → ciclo de vida)
└── adr/
    ├── README.md           # ← NUEVO: índice de ADRs
    ├── 0000-plantilla.md   # ← NUEVO: plantilla ADR
    ├── 0001-...            # existentes
    ├── 0002-...            # existentes
    └── 00NN-...            # ← nuevos ADRs retroactivos de decisiones no obvias

README.md                   # sección "Arquitectura" (verificar/cross-link DI, SOLID, ADR)
```

**Structure Decision**: Aplicación web multi-proyecto ya existente. La feature no crea proyectos nuevos; consolida documentación en `docs/` y el `README.md`, añade un documento de mapa DI (`docs/dependency-injection.md`), formaliza `docs/adr/` (índice + plantilla + ADRs retroactivos) y agrega comentarios SOLID a nivel de clase en las clases clave de `backend/` y `worker/`.

## Complexity Tracking

> No aplica — Constitution Check PASS sin violaciones.
