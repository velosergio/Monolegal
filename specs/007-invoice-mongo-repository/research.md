# Research: Repositorio MongoDB de Facturas

**Feature**: 007-invoice-mongo-repository | **Date**: 2026-06-24

Este documento resuelve las decisiones técnicas (los "NEEDS CLARIFICATION" del Technical Context). Dado que gran parte del código ya existe, varias decisiones consolidan y justifican lo ya implementado.

## D1 — Nombre del método de inserción: `InsertAsync` vs `AddAsync`

- **Decisión**: Conservar `AddAsync(Invoice, CancellationToken)` del contrato `IInvoiceRepository` existente, con semántica de inserción (un único documento nuevo).
- **Rationale**: El input de la spec menciona `InsertAsync(invoice)` de forma descriptiva. El contrato del dominio ya define `AddAsync` y está consumido por workers, endpoints y tests. Renombrarlo sería un cambio de superficie sin valor de negocio y rompería código y pruebas existentes (Principio II: cerrado para modificación innecesaria). La semántica solicitada (crear una factura) queda satisfecha.
- **Alternativas consideradas**: (a) Renombrar a `InsertAsync` — rechazada por churn y rotura de consumidores. (b) Añadir un alias `InsertAsync` — rechazada por duplicar la superficie del contrato (viola Interface Segregation con métodos redundantes).

## D2 — Estrategia de índices sobre `Status` y `ClientId`

- **Decisión**: Índices simples ascendentes, **no únicos**, sobre `Status` y `ClientId`, creados al arranque por `MongoIndexBuilder.EnsureIndexesAsync`. Se mantiene además el índice sobre `LastStatusTransitionAt` que sirve al worker.
- **Rationale**: Un cliente tiene múltiples facturas y muchas facturas comparten estado → los campos no son únicos. Los índices simples cubren las consultas `GetByStatusAsync` y `GetByClientIdAsync` cumpliendo el presupuesto ≤200 ms. La constitución pide índices en `Status`, `ClientId`, `CreatedAt`/campos frecuentes.
- **Alternativas consideradas**: (a) Índice compuesto `{ClientId, Status}` — rechazado por ahora: no hay consulta combinada en la spec; se añadiría si surge ese patrón. (b) Índices únicos — incorrecto semánticamente.

## D3 — Idempotencia de creación de índices

- **Decisión**: Usar `Indexes.CreateOneAsync` por modelo con `Name` explícito; envolver cada creación en try/catch que loguea warning y continúa el arranque.
- **Rationale**: `CreateOneAsync` es idempotente para definiciones idénticas (MongoDB no reconstruye si no cambia). El try/catch evita que un fallo de un índice tumbe el arranque (FR-010). Ya implementado en `MongoIndexBuilder`.
- **Alternativas consideradas**: `CreateManyAsync` — válido, pero la creación individual permite logging y tolerancia a fallos por índice.

## D4 — Actualización atómica de estado (`UpdateStatusAsync`)

- **Decisión**: Usar `UpdateOneAsync` con `Builders<Invoice>.Update.Set` sobre `Status`, `UpdatedAt` y `LastStatusTransitionAt`, filtrando por `Id`. No se reescribe el documento completo.
- **Rationale**: Satisface FR-003 (actualizar solo campos de estado/auditoría) y FR-007 (registrar el momento del cambio). Un `id` inexistente produce `MatchedCount = 0` sin error → FR-009/SC-004. Ya implementado.
- **Alternativas consideradas**: `ReplaceOneAsync` con la entidad completa — rechazado: reescribe todo el documento y arriesga condiciones de carrera con otros campos (RemindersCount, etc.).

## D5 — Estrategia de tests de integración del repositorio

- **Decisión**: Añadir `MongoInvoiceRepositoryIntegrationTests` que ejercitan la implementación real contra un MongoDB en ejecución, siguiendo el patrón existente: cadena desde `MONGODB_URI` (o default dev), `[Trait("Category", "Integration")]`, aislamiento por colección/base temporal o limpieza por test.
- **Rationale**: El Principio IV exige integration tests para contratos de repositorio. Hoy solo existe `InvoiceRepositoryContractTests` contra un fake en memoria, que no valida la traducción real a MongoDB (filtros, índices, actualización parcial). El patrón con `MONGODB_URI` ya está probado en `MongoConnectionTests`, evitando introducir nuevas dependencias.
- **Alternativas consideradas**: (a) Testcontainers / Mongo2Go — rechazado por ahora: añade dependencias y el patrón `MONGODB_URI` + docker-compose ya cubre CI/local de forma consistente con el resto de la suite. (b) Solo el fake en memoria — insuficiente: no cumple el principio de integration testing.

## D6 — Aislamiento de datos entre tests de integración

- **Decisión**: Cada test usa una colección con nombre único (p. ej. sufijo GUID) o limpia los documentos que inserta, de modo que las pruebas sean independientes y repetibles.
- **Rationale**: Evita interferencia entre tests y con datos de desarrollo; soporta ejecución en paralelo y re-ejecución (SC-001..SC-004 deben ser deterministas).
- **Alternativas consideradas**: Base de datos dedicada por ejecución — válida pero más pesada; la colección efímera por test es suficiente para el alcance.

## Resumen

Todas las incógnitas del Technical Context quedan resueltas. La implementación productiva (repositorio, índices, DI) ya cumple las decisiones D1–D4. El trabajo pendiente derivado de la investigación es D5/D6: la suite de integración del repositorio contra MongoDB real.
