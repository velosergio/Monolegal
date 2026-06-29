# Contrato: Matriz de Tests de Integración del API

**Feature**: 021-integration-tests-api | **Fecha**: 2026-06-29

El "contrato" de esta feature es el conjunto de **casos de prueba de integración** que verifican el comportamiento HTTP observable de los endpoints de facturas. Cada caso mapea a peticiones HTTP reales contra `WebApplicationFactory<Program>` y a un código/forma de respuesta esperados. Trazabilidad: columna FR/US → spec.md (021).

## US1 — `GET /api/invoices` (listado, filtro, paginación)

| # | Setup | Petición | Esperado | FR |
|---|-------|----------|----------|-----|
| 1 | Sembrar N facturas | `GET /api/invoices` | `200`; cuerpo con `data`, `total`, `pageSize` | FR-003 |
| 2 | Facturas en estados mixtos | `GET /api/invoices?status=primerrecordatorio` | `200`; `data` sólo `primerrecordatorio`; `total` = #coincidencias | FR-004 |
| 3 | 25 facturas | `GET /api/invoices?page=1&pageSize=10` | `200`; `data.length` = 10; `total` = 25 | FR-003 |
| 4 | — | `GET /api/invoices?status=foo` | `400` | FR-005 |
| 5 | — | `GET /api/invoices?page=0` | `400` | FR-005 |
| 6 | — | `GET /api/invoices?pageSize=51` | `400` | FR-005 |

## US2 — `GET /api/invoices/{id}` y `404` por identificador

| # | Setup | Petición | Esperado | FR |
|---|-------|----------|----------|-----|
| 7 | Sembrar factura conocida | `GET /api/invoices/{id}` | `200`; objeto completo (`id`, `status`, `amount`, `items`) | FR-006 |
| 8 | Base vacía | `GET /api/invoices/no-existe` | `404` | FR-006 |
| 9 | Base vacía | `GET /api/invoices/{idFormatoInvalido}` | `404` (uniforme, sin error 500) | FR-007 |

## US3 — `POST /api/invoices/transition/{id}` (validación de transición)

| # | Setup | Petición | Esperado | FR |
|---|-------|----------|----------|-----|
| 10 | Factura `primerrecordatorio` | body `{ "newStatus": "segundorecordatorio" }` | `200`; `status` actualizado y persistido | FR-008 |
| 11 | Factura `pending` | body `{ "newStatus": "desactivado" }` (prohibida) | `400`; estado sin cambios | FR-009 |
| 12 | Factura `pending` | body `{ "newStatus": "foo" }` | `400` | FR-010 |
| 13 | Factura `pending` | body `{ }` (sin `newStatus`) | `400` | FR-010 |
| 14 | Base vacía | `POST .../transition/no-existe` body válido | `404` | FR-011 |

## US4 — Infraestructura de pruebas y casos de borde

| # | Setup | Petición | Esperado | FR |
|---|-------|----------|----------|-----|
| 15 | Base vacía | `GET /api/invoices/stats` | `200`; `totalInvoices` 0; agregados vacíos | FR-002 |
| 16 | Facturas en varios estados | `GET /api/invoices/stats` | `200`; Σ(`byStatus`) == `totalInvoices` | FR-002 |
| 17 | 5 facturas | `GET /api/invoices?page=99&pageSize=10` | `200`; `data` vacío; `total` = 5 | FR-003 |
| 18 | Dos clases/instancias de fábrica | Ejecutar suite | Datos aislados; resultado repetible | FR-012, FR-013 |

## Invariantes transversales del contrato

- **Códigos de estado**: nunca `500` ante entradas inválidas controladas (estado/paginación/cuerpo/id) — siempre `400`/`404`.
- **Serialización de estado**: los valores de `status` viajan en minúscula (`pending`, `primerrecordatorio`, ...).
- **Aislamiento**: cada test parte de repositorios en memoria nuevos; sin dependencia del orden (FR-012/FR-013).
- **Sin efectos externos**: el notificador es un doble; ninguna prueba envía email ni toca MongoDB.
- **Sin skips**: ningún caso se marca como omitido (Principio IV / FR-017).
