# Quickstart: Verificación de Dependencias Backend

**Fase**: Fase 1 - Guía de validación
**Entrada**: [plan.md](plan.md), [contracts/dependency-matrix.md](contracts/dependency-matrix.md)

Esta guía valida de extremo a extremo que las dependencias de la Fase 0.2 están disponibles y correctamente ubicadas. No contiene código de implementación; las decisiones de diseño están en [research.md](research.md) y el detalle de tareas se generará en `tasks.md`.

---

## Prerequisitos

- .NET 10 SDK instalado (`global.json` fija `10.0.301`).
- Estructura de proyectos de la Fase 0.1 presente (`backend/backend.slnx` con 5 proyectos).
- Conectividad con el repositorio de paquetes NuGet.

Comprobar el SDK:

```powershell
dotnet --version   # Debe reportar 10.0.3xx
```

---

## Escenario 1: Restauración sin conflictos (V1 → SC-003)

```powershell
dotnet restore backend/backend.slnx
```

**Esperado**: `Restored ...` para los 5 proyectos, sin advertencias `NU1605`/`NU1107` de conflicto de versión.

---

## Escenario 2: Compilación de la solución (V2 → SC-002, FR-008)

```powershell
dotnet build backend/backend.slnx -c Release
```

**Esperado**: `Build succeeded. 0 Error(s)`.

---

## Escenario 3: Framework de pruebas + aserciones fluidas (V3 → SC-004, FR-005/FR-006)

Con una prueba trivial de verificación presente en `Tests` (que use una aserción fluida), ejecutar:

```powershell
dotnet test backend/backend.slnx
```

**Esperado**: el runner descubre y ejecuta al menos 1 prueba; `Passed!`. Si Shouldly falta, la prueba no compila → señal de brecha abierta.

---

## Escenario 4: Arranque de la API con Minimal APIs (FR-001, FR-004)

```powershell
dotnet run --project backend/Api
```

**Esperado**: la aplicación arranca, Serilog emite el log de inicio en consola, y `GET /health` responde. Confirma `Sdk.Web` (Minimal APIs) + Serilog disponibles. Detener con `Ctrl+C`.

---

## Verificación de invariantes de capa (I2, I3 → SC-005, FR-007)

Inspección rápida (sin ejecutar build):

- `backend/Domain/Domain.csproj` → **sin** ningún `<PackageReference>` de infraestructura.
- `FluentValidation` aparece **solo** en `backend/Application/Application.csproj`.
- `Shouldly` aparece **solo** en `backend/Tests/Tests.csproj`.

Detalle completo del estado esperado por proyecto en [contracts/dependency-matrix.md](contracts/dependency-matrix.md).

---

## Criterio de fase completa

La Fase 0.2 está completa cuando los Escenarios 1–4 pasan y las invariantes de capa se cumplen (mapeo a SC-001..SC-006 en [spec.md](spec.md)).
