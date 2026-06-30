# Implementation Plan: Documentación de API y del Proyecto

**Branch**: `025-documentacion-api` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/025-documentacion-api/spec.md`

## Summary

Entregar la **documentación consolidada del proyecto finalizado** y hacerla **descubrible**. Se materializan los seis artefactos del roadmap (Spec 6.1): panorama de arquitectura, diagrama de entidades (ERD), referencia de endpoints, instrucciones de configuración (setup), guía de despliegue y colección de Postman; además se añade un acceso visible a **Swagger UI desde el sidebar** del frontend.

**Enfoque técnico**: documentación versionada en `docs/` con diagramas **Mermaid** (texto, renderizan en GitHub); la **referencia de endpoints** y la **colección de Postman** se **generan** de forma reproducible a partir de un snapshot del documento OpenAPI (`docs/openapi.json`, que el backend ya produce en `/openapi/v1.json`) mediante un script Node sin dependencias (`scripts/gen-api-docs.mjs`), siguiendo el precedente de tooling de la Spec 024. El acceso a Swagger desde el sidebar se implementa extendiendo el modelo declarativo de navegación del frontend (`navigation.ts` / `Sidebar.tsx`) con un ítem de **enlace externo** que abre la URL de Swagger en una pestaña nueva, con URL configurable por entorno. No se modifica ningún endpoint, entidad ni lógica de negocio.

## Technical Context

**Language/Version**: Node.js 22+ (generador de docs, vía `.node-version`); Markdown + Mermaid (documentación); TypeScript strict (cambio de frontend); .NET 10 (backend ya existente, sin cambios funcionales)

**Primary Dependencies**: Ninguna nueva. El generador usa solo módulos integrados de Node (`node:fs`, `node:path`). El frontend reutiliza React 19 + react-router + lucide-react ya presentes. El backend ya expone OpenAPI vía `Microsoft.AspNetCore.OpenApi` y `Swashbuckle.AspNetCore.SwaggerUI` (Spec 010)

**Storage**: N/A (documentación estática versionada en el repo; un snapshot `docs/openapi.json`)

**Testing**: Vitest + Testing Library (test del nuevo ítem de sidebar); verificación de regeneración del generador en CI (estilo `--verify-no-changes`); las suites existentes se reutilizan vía `npm run test:all` (Spec 024)

**Target Platform**: Documentación en repositorio (GitHub-flavored Markdown); frontend React (navegadores modernos); backend ASP.NET Core (Swagger en Development)

**Project Type**: Monorepo web (backend .NET + worker .NET + frontend React) + tooling y documentación en la raíz/`docs`

**Performance Goals**: N/A. La generación de docs es offline y de duración despreciable

**Constraints**: Documentación en español (Principio III); sin secretos embebidos (Postman usa variables `{{baseUrl}}`/`{{token}}`); URL base del backend configurable por entorno; referencia de endpoints y Postman sincronizados con la API real (regenerables, sin mantenimiento manual divergente); el cambio de frontend respeta TS strict, Biome, React Doctor y accesibilidad; **sin cambios** en endpoints, entidades ni lógica de negocio

**Scale/Scope**: ~31 endpoints en 4 grupos (Clients, Invoices, Settings, Workers) + health/openapi; 5 entidades de dominio; 6 artefactos de documentación; 1 generador Node; 1 ítem de sidebar nuevo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Arquitectura Limpia**: ✅ La documentación y el generador son tooling externo a las capas. El único cambio de código de producción es un ítem de navegación en la capa de presentación del frontend (un enlace), sin afectar dominio, aplicación ni infraestructura.
- **II. SOLID**: ✅ El ítem de Swagger se añade de forma declarativa a `NAV_ITEMS` (Open/Closed: añadir navegación = añadir una entrada, sin alterar la lógica de render más allá de soportar el tipo "enlace externo"). El generador tiene responsabilidad única (OpenAPI → docs).
- **III. Documentación en español (SDD)**: ✅ Spec, plan, research, data-model, contracts, quickstart y los seis entregables en español. La spec deriva del roadmap Spec 6.1 en formato GIVEN/WHEN/THEN. Se permite contenido OpenAPI en su forma original.
- **IV. Test-First / CI Gate**: ✅ El ítem de sidebar se cubre con un test de Vitest (presencia, etiqueta, enlace externo, `target`/`rel`). El generador se valida con un modo de verificación reproducible (regenerar no produce diff). No se añaden ni modifican pruebas existentes.
- **V. Frontend de Calidad**: ✅ Cambio mínimo en TS strict, Biome-compliant, React Doctor sin warnings, enlace accesible (texto/`aria`, `rel="noopener noreferrer"`), sin afectar dark mode ni el comportamiento colapsado/expandido del sidebar.
- **VI. Observable y Mantenible**: ✅ La feature **es** documentación: cumple "Diagramas de arquitectura en README" (Principio VI) con diagramas como código. Se registra un ADR para la decisión de snapshot OpenAPI + generación reproducible. El mecanismo del botón y del generador queda documentado en quickstart y contratos.

**Resultado**: PASS. Sin violaciones; la sección Complexity Tracking no aplica.

## Project Structure

### Documentation (this feature)

```text
specs/025-documentacion-api/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0 (/speckit-plan)
├── data-model.md        # Fase 1 (/speckit-plan)
├── quickstart.md        # Fase 1 (/speckit-plan)
├── contracts/
│   ├── documentation-deliverables.md  # Contrato: los 6 entregables y su mapeo a archivos
│   ├── api-docs-generator.md          # Contrato CLI del generador (entradas/salidas/verify)
│   ├── postman-collection.md          # Contrato de la colección y entorno Postman
│   └── sidebar-swagger-link.md        # Contrato UI del ítem de Swagger en el sidebar
└── tasks.md             # Fase 2 (/speckit-tasks — NO lo crea /speckit-plan)
```

### Source Code (repository root)

```text
docs/
├── README.md            # NUEVO — índice de la documentación del proyecto
├── architecture.md      # NUEVO — panorama de arquitectura + diagrama de capas/componentes (Mermaid)
├── data-model.md        # NUEVO — ERD (Mermaid) + entidades, enums y colecciones
├── api-reference.md     # NUEVO (GENERADO) — referencia de endpoints desde openapi.json
├── setup.md             # NUEVO — instrucciones de configuración local consolidadas
├── deployment.md        # NUEVO — guía de despliegue (Docker/VPS, entornos, variables)
├── openapi.json         # NUEVO — snapshot del documento OpenAPI (fuente de generación)
├── adr/
│   └── 0002-documentacion-openapi-generada.md   # NUEVO — decisión snapshot + generación reproducible
└── postman/
    ├── monolegal.postman_collection.json   # NUEVO (GENERADO) — colección importable
    └── monolegal.postman_environment.json  # NUEVO — plantilla de entorno (baseUrl, token)

scripts/
└── gen-api-docs.mjs     # NUEVO — generador Node (sin deps): openapi.json → api-reference.md + Postman

README.md                # MODIFICADO — sección "Documentación" enlaza a docs/ y a Swagger

frontend/src/components/layout/
├── navigation.ts        # MODIFICADO — soporta ítem de enlace externo; añade "API (Swagger)"
└── Sidebar.tsx          # MODIFICADO — renderiza ítems de enlace externo (<a target="_blank">)

frontend/.env.example     # MODIFICADO/NUEVO — VITE_SWAGGER_URL documentado (default /swagger)
frontend/vite.config.ts   # MODIFICADO — proxy de dev reenvía /swagger y /openapi al backend
```

**Structure Decision**: Monorepo web existente. La documentación escrita vive en `docs/` (junto al `docs/adr/` ya presente) y se enlaza desde el `README.md` raíz, cumpliendo el Principio VI. La referencia de endpoints y la colección de Postman se **generan** desde un snapshot OpenAPI versionado para mantener coherencia con la API real sin mantenimiento manual (FR-010). El tooling de generación se ubica en `scripts/` junto al orquestador de pruebas de la Spec 024, reutilizando el patrón "Node sin dependencias". El acceso a Swagger se añade al modelo declarativo de navegación existente del frontend, que es el único punto que controla el sidebar.

## Complexity Tracking

> No aplica. La Constitution Check pasó sin violaciones.
