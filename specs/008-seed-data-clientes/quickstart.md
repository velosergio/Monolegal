# Quickstart: Seed Data - 3 Clientes Mínimo

**Feature**: `008-seed-data-clientes` | **Fecha**: 2026-06-25

Guía para ejecutar y validar el seeder de datos de desarrollo. Detalles de modelo y contrato en [data-model.md](./data-model.md) y [contracts/dev-data-seeder.md](./contracts/dev-data-seeder.md).

## Prerrequisitos

- MongoDB en ejecución: `docker compose up -d mongo`
- Variable `MONGODB_URI` definida (o usar el default de desarrollo del proyecto).
- Backend compilable: `dotnet build backend/backend.csproj`
- Entorno de desarrollo: `ASPNETCORE_ENVIRONMENT=Development` (gate del seeder).

## Escenario 1 — Sembrar sobre base vacía (camino feliz)

1. Asegurar base limpia (colección `Invoices` vacía o base de desarrollo recién creada).
2. Arrancar la API en desarrollo:
   ```bash
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/Api
   ```
3. **Resultado esperado** (log Serilog): `Sembrado=true ... Clientes=3 Facturas=8`.
4. Verificar en MongoDB:
   ```bash
   # 8 facturas en total
   mongosh "$MONGODB_URI" --eval 'db.Invoices.countDocuments({})'   # → 8
   # 3 clientes distintos
   mongosh "$MONGODB_URI" --eval 'db.Invoices.distinct("ClientId").length'  # → 3
   # cobertura de estados (10=PrimerRecordatorio, 11=SegundoRecordatorio)
   mongosh "$MONGODB_URI" --eval 'db.Invoices.countDocuments({Status:10})'  # ≥ 1
   mongosh "$MONGODB_URI" --eval 'db.Invoices.countDocuments({Status:11})'  # ≥ 1
   ```

## Escenario 2 — Idempotencia (segunda ejecución)

1. Con datos ya sembrados, reiniciar la API en desarrollo.
2. **Resultado esperado**: `Sembrado=false Motivo="datos existentes"`.
3. Verificar que los conteos no cambian: `countDocuments({})` sigue en **8**.

## Escenario 3 — Gate de entorno (no sembrar en producción)

1. Arrancar con `ASPNETCORE_ENVIRONMENT=Production` sobre una base vacía.
2. **Resultado esperado**: el seeder no se registra ni ejecuta; sin facturas creadas.

## Validación por tests

```bash
# Unit (distribución e idempotencia, sin Mongo)
dotnet test backend/Tests/Monolegal.Application.Tests --filter "FullyQualifiedName~Seeding"

# Integración (Mongo real efímero) — requiere docker compose up -d mongo
dotnet test backend/Tests/Tests.csproj --filter "Category=Integration&FullyQualifiedName~DevDataSeeder"
```

**Criterios cubiertos**: CE-001…CE-006 (ver [contracts/dev-data-seeder.md](./contracts/dev-data-seeder.md)).

## Mapa de estados (referencia para consultas)

| Estado | Valor numérico (enum) |
|--------|------------------------|
| Pending | 1 |
| Pagado | 2 |
| PrimerRecordatorio | 10 |
| SegundoRecordatorio | 11 |
| Desactivado | 12 |
