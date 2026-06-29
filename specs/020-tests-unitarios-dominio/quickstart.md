# Quickstart — Verificación de los tests del dominio (spec 020)

Guía reproducible para ejecutar la suite del dominio y validar el umbral de cobertura ≥ 85%. No contiene código de implementación (eso va en `tasks.md`).

## Prerrequisitos

- SDK de .NET 10 instalado (`dotnet --version` ≥ 10).
- Restaurar dependencias del backend: `dotnet restore backend` (la primera vez).

## Ejecutar la suite (sin cobertura)

Desde la raíz del repositorio:

```powershell
dotnet test backend/Tests/Monolegal.Domain.Tests/Monolegal.Domain.Tests.csproj
```

**Esperado**: todas las pruebas en verde, **0 omitidas**, duración < 10 s.

## Ejecutar con cobertura (gate del 85%)

```powershell
dotnet test backend/Tests/Monolegal.Domain.Tests/Monolegal.Domain.Tests.csproj `
  --collect:"XPlat Code Coverage" `
  --results-directory backend/Tests/Monolegal.Domain.Tests/TestResults
```

Genera `TestResults/<guid>/coverage.cobertura.xml`.

### Verificar el porcentaje (line-rate ≥ 0.85)

```powershell
$cov = Get-ChildItem backend/Tests/Monolegal.Domain.Tests/TestResults -Recurse -Filter coverage.cobertura.xml |
       Sort-Object LastWriteTime | Select-Object -Last 1
[xml]$x = Get-Content $cov.FullName
$rate = [double]$x.coverage.'line-rate'
"Line coverage: {0:P2}" -f $rate
if ($rate -lt 0.85) { throw "Cobertura del dominio por debajo del 85%" }
```

**Esperado tras implementar**: `Line coverage` ≥ 85.00% (línea base previa: 62.06%).

## Escenarios de validación mapeados a la spec

| Escenario (spec) | Cómo validar | Resultado esperado |
|------------------|--------------|--------------------|
| US1 — transiciones válidas/prohibidas (SC-002) | revisar que `InvoiceManualTransitionTests` cubre, por `[Theory]`, ≥1 permitida y ≥1 prohibida por cada estado de origen | todos los orígenes con ambos casos |
| US1 — transición por tiempo (FR-003) | casos de plazo no cumplido (`false`) y cumplido (`true`) | ambos presentes y en verde |
| US2 — creación válida (FR-004) | `InvoiceTests` afirma estado inicial y `Amount == Σ Subtotal` | verde |
| US2 — validaciones (SC-003) | rechazo de cliente vacío, monto ≤0, items vacía | 3 casos en verde |
| US3 — umbral (SC-001) | script de verificación de `line-rate` arriba | ≥ 0.85 |
| Higiene (SC-005) | salida de `dotnet test` | `Omitido: 0`, `Con error: 0` |
| Aislamiento (SC-004) | duración de la suite | < 10 s |

## Referencias

- Inventario de casos: [contracts/test-inventory.md](./contracts/test-inventory.md)
- Sujetos e invariantes: [data-model.md](./data-model.md)
- Decisiones: [research.md](./research.md)
