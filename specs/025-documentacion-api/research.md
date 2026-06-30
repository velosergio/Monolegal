# Research — Documentación de API y del Proyecto (Spec 6.1 / 025)

Fase 0. Resuelve las decisiones técnicas necesarias para producir los seis entregables de documentación y el acceso a Swagger desde el sidebar, conforme a la spec y a la Constitución. No quedan marcadores NEEDS CLARIFICATION.

## D1 — Formato y ubicación de la documentación escrita

- **Decisión**: Documentos Markdown (GitHub-flavored) en `docs/`, junto al `docs/adr/` existente, enlazados desde el `README.md` raíz. Un documento por entregable: `architecture.md`, `data-model.md`, `api-reference.md`, `setup.md`, `deployment.md`, más `docs/README.md` como índice.
- **Rationale**: El repo ya usa `docs/adr/` y un `README.md` rico; mantener la documentación versionada cumple el Principio VI ("Diagramas de arquitectura en README") y permite revisarla en PR. Markdown no requiere herramientas ni build adicional y se renderiza directamente en GitHub.
- **Alternativas consideradas**: (a) Sitio de documentación estático (Docusaurus/MkDocs) — descartado por dependencia y pipeline extra para un proyecto finalizado; (b) Todo en un único `README.md` gigante — descartado por legibilidad y porque mezcla quick-start con referencia detallada.

## D2 — Diagramas (arquitectura y ERD)

- **Decisión**: Diagramas como código con **Mermaid** embebido en los `.md` (`graph`/`flowchart` para componentes y capas; `erDiagram` para el modelo de datos).
- **Rationale**: Versionable y diffeable (texto), se renderiza nativamente en GitHub y en muchos visores Markdown, y no requiere binarios ni exportar imágenes. Coincide con la asunción de "diagramas como código" de la spec.
- **Alternativas consideradas**: Imágenes PNG/SVG exportadas (draw.io) — descartadas por no ser diffeables y requerir regenerar manualmente; PlantUML — descartado por requerir runtime Java/servidor de render.

## D3 — Origen de la referencia de endpoints y de la colección de Postman

- **Decisión**: Generarlas de forma **reproducible** a partir de un snapshot versionado del documento OpenAPI, `docs/openapi.json` (el backend ya lo expone en `/openapi/v1.json`, Spec 010), mediante un script Node sin dependencias: `scripts/gen-api-docs.mjs` → produce `docs/api-reference.md` y `docs/postman/monolegal.postman_collection.json`.
- **Rationale**: Satisface FR-010 (mantener la referencia y Postman sincronizadas con la API real sin mantenimiento manual divergente). El snapshot versionado hace la generación reproducible y verificable en CI (regenerar no debe producir diff, estilo `dotnet format --verify-no-changes`), sin exigir un backend levantado para construir la documentación. Reutiliza el patrón "Node sin dependencias" de la Spec 024 (`scripts/test-all.mjs`), respetando la política de mínimas dependencias de la Constitución.
- **Alternativas consideradas**:
  - **Generar contra el backend en vivo** en cada build — descartado como mecanismo principal: introduce dependencia de un servicio corriendo; sí se usa para **refrescar** el snapshot (`docs/openapi.json`) cuando cambian endpoints.
  - **Librería `openapi-to-postman`** — potente pero añade una dependencia npm y produce colecciones muy verbosas; para esta API (rutas planas, cuerpos JSON, auth Bearer) un mapeo dirigido es suficiente y más controlable.
  - **Escribir la referencia y la colección a mano** — descartado: viola FR-010 (se desincroniza con la API).

## D4 — Acceso a Swagger UI desde el sidebar

- **Contexto verificado**: El frontend (`vite.config.ts`) habla con el backend mediante un **proxy de `/api`** (dev: `http://localhost:5155`; Docker: `http://backend:5000`); no maneja una base URL de API en el cliente. Swagger UI se sirve en el **backend** en `/swagger` y el documento en `/openapi/v1.json`, y está **restringido a Development** (`Program.cs`: `if (app.Environment.IsDevelopment())`, Spec 010 D3). El sidebar (`navigation.ts`/`Sidebar.tsx`) hoy solo soporta rutas internas (`NavLink`) e ítems `disabled`.
- **Decisión**:
  1. Extender el modelo `NavItem` con un tipo de **enlace externo** (p. ej. `external: true` + `href`). El `Sidebar` renderiza esos ítems como `<a href target="_blank" rel="noopener noreferrer">` en lugar de `NavLink`, manteniendo estilos y comportamiento colapsado/expandido.
  2. La URL de Swagger es **configurable** vía `import.meta.env.VITE_SWAGGER_URL`, con valor por defecto `/swagger`.
  3. Extender el **proxy de dev** de Vite para reenviar también `/swagger` y `/openapi` al backend, de modo que el enlace por defecto `/swagger` funcione en el mismo origen en desarrollo (igual que ya ocurre con `/api`).
- **Rationale**: Un enlace externo abierto en pestaña nueva es la forma natural de exponer una UI servida por otro componente (el backend) sin acoplar el router del frontend. Hacer la URL configurable evita asumir un host fijo (FR-009) y permite apuntar a un Swagger expuesto en producción si el equipo decide habilitarlo. Reenviar `/swagger` y `/openapi` por el proxy de dev replica el patrón ya existente de `/api`, logrando experiencia de un solo clic en local sin configuración extra.
- **Producción**: Por Spec 010 D3, Swagger está deshabilitado en producción por defecto. La guía de despliegue documenta cómo (y con qué implicaciones de seguridad) exponerlo, y que el botón puede ocultarse o deshabilitarse si `VITE_SWAGGER_URL` queda vacío. Un casevacío hace que el ítem no se muestre (degradación elegante), evitando un enlace roto.
- **Alternativas consideradas**: (a) Incrustar Swagger dentro del frontend (iframe o ruta interna) — descartado: duplica/acopla la UI ya servida por el backend y complica auth/orígenes; (b) Hardcodear `http://localhost:5155/swagger` — descartado por no ser multi-entorno (FR-009).

## D5 — Sincronización y verificación en CI

- **Decisión**: El generador admite un **modo verificación** (regenerar a un temporal y comparar con lo versionado; salir distinto de cero si difieren), integrable como check de CI. Refrescar el snapshot `docs/openapi.json` es un paso explícito y documentado (obtenerlo de `/openapi/v1.json` con el backend en dev) cuando cambian endpoints.
- **Rationale**: Garantiza que `api-reference.md` y la colección de Postman no se desincronicen del snapshot (FR-010), siguiendo el patrón de "verify-no-changes" ya usado para formato. Mantiene la documentación como artefacto confiable del proyecto finalizado.
- **Alternativas consideradas**: No verificar — descartado: permitiría que la doc generada quedara obsoleta sin detección.

## D6 — Política de secretos en Postman y ejemplos

- **Decisión**: La colección usa variables `{{baseUrl}}` y `{{token}}`; se entrega un `monolegal.postman_environment.json` con placeholders (sin valores reales). Los ejemplos de cuerpos provienen de los esquemas OpenAPI, sin credenciales.
- **Rationale**: Cumple FR-006/FR-009 y la política de secretos de la Constitución (credenciales solo por entorno, nunca embebidas).
- **Alternativas consideradas**: Incluir un token de ejemplo "de prueba" — descartado: aun siendo ficticio, fomenta malas prácticas y puede confundirse con uno válido.

## Resumen de decisiones

| Tema | Decisión |
|------|----------|
| Formato docs | Markdown en `docs/`, índice + 5 documentos, enlazados desde README |
| Diagramas | Mermaid (capas/componentes + `erDiagram`) |
| Referencia API + Postman | Generadas desde `docs/openapi.json` con `scripts/gen-api-docs.mjs` (Node, sin deps) |
| Botón Swagger | Ítem de enlace externo en sidebar, `VITE_SWAGGER_URL` (default `/swagger`), proxy dev extendido |
| Producción Swagger | Deshabilitado por defecto (Spec 010 D3); documentado en deployment; botón oculto si URL vacía |
| Sincronización | Modo verificación en CI; refresco explícito del snapshot OpenAPI |
| Secretos Postman | Variables `{{baseUrl}}`/`{{token}}`, entorno con placeholders |
