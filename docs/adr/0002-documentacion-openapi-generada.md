# ADR 0002 — Documentación de API generada desde un snapshot OpenAPI

**Estado**: Aceptada · **Fecha**: 2026-06-30 · **Feature**: 025 — Documentación de API y del Proyecto

## Contexto

El proyecto finalizado necesita una referencia de endpoints y una colección de Postman como parte
de su documentación (roadmap Spec 6.1). El backend ya genera un documento OpenAPI en
`/openapi/v1.json` (spec 010), pero está restringido a `Development` y se produce en tiempo de
ejecución. Mantener a mano una referencia de endpoints y una colección de Postman las
desincronizaría inevitablemente de la API real (la spec lo exige en FR-010).

## Decisión

1. **Versionar un snapshot** del documento OpenAPI en `docs/openapi.json`, obtenido de
   `/openapi/v1.json` con el backend en Development.
2. **Generar** `docs/api-reference.md` y `docs/postman/monolegal.postman_collection.json` a partir
   de ese snapshot mediante `scripts/gen-api-docs.mjs`, un script **Node sin dependencias**
   (solo módulos integrados), expuesto como `npm run docs:api`.
3. Proveer un **modo de verificación** (`npm run docs:api:verify`) que regenera y compara con lo
   versionado, fallando si difieren, para usarlo como check de CI.

## Justificación

- **Coherencia con la API real** (FR-010): los artefactos derivan de la fuente de verdad (OpenAPI),
  no de edición manual.
- **Reproducibilidad y verificabilidad**: salida determinista + modo `--verify` permiten detectar
  documentación obsoleta en CI, siguiendo el patrón `--verify-no-changes` ya usado para formato.
- **Sin dependencias nuevas**: respeta la política de mínimas dependencias de la Constitución y
  reutiliza el patrón "Node sin deps" del orquestador de pruebas (feature 024,
  `scripts/test-all.mjs`).
- **No requiere backend para construir docs**: el snapshot desacopla la generación de un servicio en
  ejecución; solo se necesita el backend para **refrescar** el snapshot.

## Alternativas consideradas

- **Generar contra el backend en vivo en cada build**: introduce dependencia de un servicio
  corriendo; se reserva solo para refrescar el snapshot.
- **Librería `openapi-to-postman`**: potente pero añade una dependencia y produce colecciones muy
  verbosas; para esta API (rutas planas, cuerpos JSON, auth Bearer) un mapeo dirigido es suficiente.
- **Referencia y colección a mano**: descartada por violar FR-010 (se desincronizan).

## Consecuencias

- Al cambiar endpoints hay que **refrescar el snapshot** (`curl .../openapi/v1.json -o
  docs/openapi.json`) y **regenerar** (`npm run docs:api`); el check de CI (`docs:api:verify`)
  recuerda hacerlo.
- `docs/api-reference.md` y la colección Postman son **archivos generados**: no se editan a mano.
- La colección usa variables `{{baseUrl}}`/`{{token}}` sin secretos embebidos.
