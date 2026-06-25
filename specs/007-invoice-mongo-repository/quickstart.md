# Quickstart: Validación del Repositorio MongoDB de Facturas

**Feature**: 007-invoice-mongo-repository | **Date**: 2026-06-24

Guía para validar de extremo a extremo que el repositorio de facturas cumple su contrato contra un MongoDB real. Los detalles de comportamiento están en [contracts/IInvoiceRepository.md](./contracts/IInvoiceRepository.md) y [data-model.md](./data-model.md).

## Prerrequisitos

- .NET 10 SDK instalado.
- MongoDB 8 en ejecución. Vía docker-compose:
  ```bash
  docker compose up -d mongo
  ```
- Variable de entorno `MONGODB_URI` apuntando a la instancia (mismo origen que la app). Si no se define, los tests de integración usan el default de desarrollo:
  `mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin`

## Validación 1 — Tests de contrato (fake en memoria)

Validan la semántica del contrato sin base de datos. No requieren Mongo.

```bash
cd backend
dotnet test Tests/Tests.csproj --filter "Category=Repository"
```

**Esperado**: pasan los casos de `GetByStatusAsync` (coincidencia única, vacío, múltiples) y `UpdateStatusAsync` (cambia objetivo, no afecta otras, no-op en id inexistente).

## Validación 2 — Tests de integración (MongoDB real)

Validan la traducción real a MongoDB de la implementación `MongoInvoiceRepository`.

```bash
cd backend
# Con docker-compose mongo arriba y MONGODB_URI definido:
dotnet test Tests/Tests.csproj --filter "Category=Integration"
```

**Esperado**:
- `AddAsync` persiste una factura recuperable por `Id`, por estado y por cliente (SC-007).
- `GetByStatusAsync` / `GetByClientIdAsync` devuelven solo coincidencias y `[]` cuando no hay (SC-001, SC-002, FR-008).
- `UpdateStatusAsync` actualiza estado + `LastStatusTransitionAt` y deja intactos otros campos/facturas (SC-003).
- `UpdateStatusAsync` sobre `id` inexistente modifica 0 documentos (SC-004).

## Validación 3 — Índices creados al arranque

```bash
# Tras arrancar la API/worker una vez contra Mongo:
docker compose exec mongo mongosh "$MONGODB_URI" --eval "db.Invoices.getIndexes()"
```

**Esperado**: aparecen los índices `Status_asc`, `ClientId_asc` y `LastStatusTransitionAt_asc` (SC-006). Re-arrancar la app no produce errores de índice duplicado (FR-010, creación idempotente).

## Validación 4 — Suite completa

```bash
cd backend
dotnet test
```

**Esperado**: todas las suites en verde (unit + contrato + integración), sin tests omitidos (`[Ignore]`/skip prohibidos por constitución).

## Criterios de aceptación cubiertos

| Validación | Criterios de éxito |
|------------|--------------------|
| 1 — Contrato | SC-001, SC-003, SC-004 (semántica) |
| 2 — Integración | SC-001, SC-002, SC-003, SC-004, SC-007 |
| 3 — Índices | SC-005 (soporte), SC-006 |
| 4 — Suite completa | Gate CI (Principio IV) |
