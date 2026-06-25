# Data Model — Documentación Swagger/OpenAPI (spec 010)

Esta feature **no introduce nuevas entidades de dominio ni persistencia**. El "modelo de datos" relevante es el conjunto de elementos del **documento OpenAPI** que la página de documentación expone. Todos derivan de artefactos ya existentes (endpoints y DTOs de la spec 009).

## 1. Documento OpenAPI (recurso generado)

Estructura legible por máquina que describe la API completa. Generada automáticamente por `Microsoft.AspNetCore.OpenApi` a partir del grafo de endpoints.

| Elemento | Descripción | Fuente |
|----------|-------------|--------|
| `info` | Título y versión de la API (`Monolegal API`, `v1`) | Configuración `AddOpenApi` |
| `paths` | Mapa ruta → operaciones (una por método HTTP) | Endpoints registrados en `Program.cs` |
| `components.schemas` | Esquemas de los DTO de entrada/salida | Firmas de los handlers (inferencia) |
| `components.securitySchemes` | Esquema `Bearer` (JWT) para "Authorize" | Document transformer (D4) |

## 2. Operaciones documentadas

Cada operación expone método, ruta, resumen, descripción, parámetros, cuerpo (si aplica) y respuestas. Reflejan los contratos reales de la spec 009.

| OperationId | Método | Ruta | Parámetros / Cuerpo | Respuestas declaradas |
|-------------|--------|------|---------------------|------------------------|
| `ListInvoices` | GET | `/api/invoices` | query: `status?`, `page?`, `pageSize?` | `200` (PagedResponse), `400` (ValidationProblem) |
| `GetInvoiceById` | GET | `/api/invoices/{id}` | path: `id` | `200` (InvoiceDetailDto), `404` |
| `TransitionInvoice` | POST | `/api/invoices/transition/{id}` | path: `id`; body: `TransitionRequest` | `200` (InvoiceDetailDto), `400`, `404` |
| `GetInvoiceStats` | GET | `/api/invoices/stats` | — | `200` (InvoiceStatsDto) |

> Los endpoints adicionales ya registrados (`PayInvoice`, settings, workers) también aparecerán en el documento; su enriquecimiento de metadatos es opcional y coherente con lo anterior.

## 3. Esquemas (DTO) expuestos

Esquemas inferidos automáticamente desde los tipos ya definidos en `backend/Api/Endpoints/Invoices/InvoiceDtos.cs`. No se crean DTOs nuevos.

| Esquema | Campos | Notas |
|---------|--------|-------|
| `InvoiceListItemDto` | `id`, `clientId`, `amount`, `status`, `createdAt` | `status` como enum de cadenas en minúscula |
| `PagedResponse<InvoiceListItemDto>` | `data[]`, `total`, `pageSize` | Respuesta del listado |
| `InvoiceDetailDto` | `id`, `clientId`, `amount`, `status`, `createdAt`, `updatedAt`, `remindersCount`, `lastReminderSentAt`, `lastStatusTransitionAt` | Objeto completo |
| `TransitionRequest` | `newStatus` | Cuerpo de la transición |
| `InvoiceStatsDto` | `totalInvoices`, `byStatus` (mapa), `byClient` (mapa) | Estadísticas del dashboard |
| `InvoiceStatus` (enum) | `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado` | Serializado en minúscula vía `JsonStringEnumConverter` + `LowerCaseNamingPolicy` (ya configurado) |

## 4. Esquema de seguridad

| Nombre | Tipo | Esquema | Formato | Uso |
|--------|------|---------|---------|-----|
| `Bearer` | `http` | `bearer` | `JWT` | Habilita el botón "Authorize" en Swagger UI para "Try it out" sobre endpoints protegidos (FR-011). La autenticación efectiva se implementa en la spec de seguridad. |

## Reglas de validación / consistencia

- El documento OpenAPI MUST contener una operación por cada endpoint público registrado (FR-002): verificable comparando `paths` contra los `MapXxx` de `Program.cs`.
- Cada operación de los endpoints de facturas MUST declarar resumen, descripción y las respuestas de la tabla §2 (FR-003, FR-006).
- Los esquemas de la tabla §3 MUST estar presentes en `components.schemas` (FR-005).
- El documento y la UI MUST estar disponibles solo en entorno `Development` (D3).
