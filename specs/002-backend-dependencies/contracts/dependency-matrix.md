# Contrato: Matriz de Dependencias Backend

**Fase**: Fase 1 - Contratos
**Entrada**: [data-model.md](../data-model.md), [research.md](../research.md)

---

## Naturaleza del contrato

Esta feature es **interna** (configuración de build/dependencias): no expone APIs, endpoints ni esquemas de comandos a usuarios o sistemas externos. El "contrato" verificable es el **estado final esperado de los `.csproj`**, que cualquier proceso (desarrollador, CI) puede comprobar de forma determinista.

---

## C1: Estado esperado por proyecto (post-fase)

### `Domain/Domain.csproj`
- **DEBE** declarar `TargetFramework` = `net10.0`.
- **NO DEBE** contener ningún `<PackageReference>` de infraestructura.

### `Application/Application.csproj`
- **DEBE** referenciar `Domain`.
- **DEBE** contener `<PackageReference Include="FluentValidation" Version="12.1.1" />`.
- **DEBE** conservar `Microsoft.Extensions.Logging.Abstractions`.

### `Infrastructure/Infrastructure.csproj`
- **DEBE** referenciar `Domain` y `Application`.
- **DEBE** contener `MongoDB.Driver`, `Serilog`, `Serilog.Extensions.Logging`.
- **NO DEBE** contener `FluentValidation` (movido a Application).

### `Api/Api.csproj`
- **DEBE** usar SDK `Microsoft.NET.Sdk.Web` (provee Minimal APIs).
- **DEBE** referenciar `Domain`, `Application`, `Infrastructure`.
- **DEBE** contener `Microsoft.AspNetCore.OpenApi` y `Serilog.AspNetCore`.

### `Tests/Tests.csproj`
- **DEBE** tener `IsTestProject` = `true`.
- **DEBE** contener `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`.
- **DEBE** contener `<PackageReference Include="Shouldly" ... />` (versión 4.x).

---

## C2: Contrato de verificación ejecutable

| ID | Comando | Resultado esperado | Criterio spec |
|----|---------|--------------------|---------------|
| V1 | `dotnet restore backend/backend.slnx` | Restauración exitosa, sin advertencias de conflicto de versión | SC-003, FR-009 |
| V2 | `dotnet build backend/backend.slnx -c Release` | `Build succeeded`, 0 errores | SC-002, FR-008 |
| V3 | `dotnet test backend/backend.slnx` | El runner descubre y ejecuta ≥1 prueba; resultado verde | SC-004, FR-005/FR-006 |

---

## C3: Aserciones de invariantes (verificables por inspección)

| ID | Aserción | Criterio spec |
|----|----------|---------------|
| I1 | Las 6 dependencias objetivo están referenciadas en su capa | SC-001, FR-001..FR-006 |
| I2 | `Domain.csproj` no tiene paquetes de infraestructura | SC-005, FR-007 |
| I3 | `FluentValidation` aparece solo en `Application` | FR-007 |
| I4 | Versiones mayores coherentes con la constitución | FR-010 |
