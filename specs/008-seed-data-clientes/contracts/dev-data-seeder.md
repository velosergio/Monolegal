# Contrato: DevDataSeeder

**Feature**: `008-seed-data-clientes` | **Fecha**: 2026-06-25

Esta feature no expone una API HTTP pública. Su contrato es la **abstracción interna** del seeder y las **garantías sobre los datos** resultantes. El disparo es un `IHostedService` ejecutado al arranque, sólo en entorno de desarrollo.

## Contrato de la abstracción `IDevDataSeeder` (capa Application)

```csharp
namespace Monolegal.Application.Abstractions;

public interface IDevDataSeeder
{
    /// <summary>
    /// Siembra el dataset mínimo de desarrollo (3 clientes, 8 facturas) SOLO si la
    /// colección de facturas está vacía. Idempotente: si ya existen datos, omite la
    /// siembra sin modificar registros. Devuelve un resultado observable.
    /// </summary>
    Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default);
}
```

### Precondiciones / Postcondiciones

| Condición | Garantía |
|-----------|----------|
| **Pre**: colección `Invoices` vacía (`CountAsync == 0`) | **Post**: 3 `ClientId` distintos, 8 facturas con la distribución de `data-model.md`; `SeedResult.Seeded == true`, `ClientsCreated == 3`, `InvoicesCreated == 8`. |
| **Pre**: colección NO vacía (`CountAsync > 0`) | **Post**: sin inserciones; `SeedResult.Seeded == false`, conteos previos intactos. |
| **Pre**: MongoDB no disponible | **Post**: la operación falla de forma explícita (excepción propagada / logueada); no deja datos a medio insertar (cada `AddAsync` es atómico por documento). |

## Extensión del repositorio `IInvoiceRepository`

```csharp
/// <summary>
/// Devuelve la cantidad total de facturas en la colección. Usado para verificar
/// la condición de "base vacía" e idempotencia del seeder.
/// </summary>
Task<long> CountAsync(CancellationToken cancellationToken = default);
```

## Contrato de disparo (Infrastructure / Api)

- `DevDataSeederHostedService : IHostedService` invoca `IDevDataSeeder.SeedAsync` en `StartAsync`.
- **Registrado en DI únicamente cuando `IHostEnvironment.IsDevelopment()` es verdadero.** En producción no se registra ni ejecuta.
- Registra con Serilog (structured logging) el `SeedResult`: campos `Seeded`, `Reason`, `ClientsCreated`, `InvoicesCreated`.

### Ejemplo de log esperado (sembrado)

```
[INF] Seed de desarrollo completado. Sembrado=true Motivo="base vacía" Clientes=3 Facturas=8
```

### Ejemplo de log esperado (omitido)

```
[INF] Seed de desarrollo omitido. Sembrado=false Motivo="datos existentes" Clientes=0 Facturas=0
```

## Garantías verificables (mapeo a criterios de éxito)

| Garantía | Criterio |
|----------|----------|
| 3 `ClientId` distintos tras sembrar | CE-001 |
| Distribución 3 / 2 / 3 (= 8) | CE-002 |
| ≥1 `PrimerRecordatorio` y ≥1 `SegundoRecordatorio` | CE-003 |
| Cliente A con ≥2 estados distintos | CE-004 |
| Doble ejecución no incrementa conteos | CE-005 |
| Todos los estados ∈ `InvoiceStatus` válido | CE-006 |

## Extensión opcional (no requerida)

Un endpoint dev-only `POST /dev/seed` podría exponer `SeedAsync` para disparo manual durante el desarrollo. Queda fuera del alcance mínimo; si se implementa, debe compartir el mismo gate de entorno.
