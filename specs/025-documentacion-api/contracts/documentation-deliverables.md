# Contrato — Entregables de documentación (Spec 6.1 / 025)

Define los seis entregables del roadmap y su mapeo a archivos verificables. Cada entregable es "cumplido" cuando el archivo existe, está en español y satisface su criterio.

| # | Entregable (roadmap) | Archivo | Criterio de aceptación | FR |
|---|----------------------|---------|------------------------|----|
| 1 | Architecture overview | `docs/architecture.md` | Describe capas (Domain/Application/Infrastructure/Api) y componentes (backend, worker, frontend, MongoDB) con ≥ 1 diagrama Mermaid de relaciones | FR-001 |
| 2 | Entity relationship diagram (ERD) | `docs/data-model.md` | Contiene un `erDiagram` Mermaid con Client, Invoice, InvoiceItem, StatusChange (+ SystemSettings) y sus relaciones y atributos clave | FR-002 |
| 3 | API endpoint reference | `docs/api-reference.md` (generado) | Lista el 100% de los endpoints con método, ruta, propósito y forma de petición/respuesta; coherente con `docs/openapi.json` | FR-003, FR-010 |
| 4 | Setup instructions | `docs/setup.md` | Permite levantar backend, worker, frontend y MongoDB en local; incluye prerrequisitos y variables de entorno | FR-004 |
| 5 | Deployment guide | `docs/deployment.md` | Pasos y requisitos para producción (Docker/VPS, entornos, variables, exposición de Swagger) | FR-005 |
| 6 | Colección Postman | `docs/postman/monolegal.postman_collection.json` (generado) + `monolegal.postman_environment.json` | Importable, cubre los endpoints principales, variables `{{baseUrl}}`/`{{token}}`, sin secretos | FR-006, FR-009 |

Adicional (descubribilidad):

| Extra | Archivo(s) | Criterio | FR |
|-------|-----------|----------|----|
| Swagger UI desde el sidebar | `frontend/src/components/layout/navigation.ts`, `Sidebar.tsx` | Ítem visible que abre Swagger UI; ver `sidebar-swagger-link.md` | FR-007, FR-008, FR-009 |
| Índice de documentación | `docs/README.md` + sección en `README.md` raíz | Enlaza los seis entregables y el acceso a Swagger | FR-001..006 |
| ADR de la decisión de generación | `docs/adr/0002-documentacion-openapi-generada.md` | Justifica snapshot OpenAPI + generación reproducible | Principio VI |

## Invariantes

- Toda la documentación está en español (FR-011); se permite el contenido OpenAPI en su forma original.
- Ningún archivo contiene credenciales ni secretos (FR-006, SC-006).
- Los entregables generados (#3, #6) derivan de `docs/openapi.json` y se pueden regenerar sin diff (ver `api-docs-generator.md`).
- Ningún cambio altera endpoints, entidades ni lógica de negocio (FR-012, SC-007).
