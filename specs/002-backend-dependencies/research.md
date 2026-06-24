# Investigación: Dependencias Backend

**Fase**: Fase 0 - Investigación
**Entrada**: [plan.md](plan.md), [spec.md](spec.md)

---

## Contexto

La Fase 0.1 (`001-project-setup`) ya instaló la mayoría de los paquetes. Esta investigación parte de una **auditoría del estado actual** de los `.csproj` y resuelve las decisiones abiertas para cerrar las brechas detectadas. No hay marcadores NECESITA CLARIFICACIÓN heredados de la spec.

### Estado actual auditado

| Capa | Paquetes presentes |
|------|--------------------|
| Domain | (ninguno — correcto) |
| Application | `Microsoft.Extensions.Logging.Abstractions` 10.0.9 |
| Infrastructure | `MongoDB.Driver` 3.4.0, `Serilog` 4.3.0, `Serilog.Extensions.Logging` 9.0.0, `FluentValidation` 12.1.1, `Microsoft.Extensions.Configuration.Abstractions` 10.0.0 |
| Api | `Microsoft.AspNetCore.OpenApi` 10.0.6, `Serilog.AspNetCore` 9.0.0 (`Sdk.Web`) |
| Tests | `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.1 |

---

## Decisión 1: Ubicación de FluentValidation

**Decisión**: Mover la referencia `FluentValidation` de `Infrastructure` a `Application`.

**Justificación**: Los validadores validan comandos y DTOs de la capa de Aplicación (casos de uso). Según Arquitectura Limpia (Principio I) y FR-003/FR-007 de la spec, la dependencia de validación pertenece a `Application`, junto al código que valida. `Infrastructure` ya referencia `Application`, por lo que cualquier registro de validadores en el contenedor DI (que vive en Infrastructure) sigue teniendo acceso transitivo a los tipos de FluentValidation sin necesidad de una referencia directa en Infrastructure.

**Alternativas consideradas**:
- *Mantener en Infrastructure*: rechazada — viola la dirección de dependencias; los validadores quedarían acoplados a la capa de infraestructura.
- *Duplicar en ambas capas*: rechazada — referencia redundante; basta con Application (Infrastructure la obtiene transitivamente).

**Versión**: `12.1.1` (la ya resuelta en el repositorio; sin cambio de versión, solo de capa).

---

## Decisión 2: Librería de aserciones en el proyecto Tests

**Decisión**: Añadir `Shouldly` al proyecto `Tests` como librería de aserciones legibles.

**Justificación**: La constitución exige aserciones legibles junto a xUnit para soportar el ciclo Red-Green-Refactor (Principio IV). Se elige `Shouldly` por ser **OSS sin restricciones de licencia** (a diferencia de FluentAssertions 8+, que pasó a licencia comercial de Xceed) y por producir mensajes de error descriptivos que incluyen el contexto de la expresión evaluada. La constitución y el roadmap se actualizaron para reflejar `xUnit + Shouldly` como el stack de testing backend oficial.

**Versión**: `4.x` (última línea mayor estable compatible con `net10.0` y xUnit 2.9.x). El pin menor exacto se fija al editar el `.csproj`, alineado con el resto de paquetes ya restaurados.

**Sintaxis**: aserciones por métodos de extensión, ej. `factura.Estado.ShouldBe("PENDIENTE")`, `total.ShouldBePositive()`, `accion.ShouldThrow<TransicionInvalidaException>()`.

**Alternativas consideradas**:
- *FluentAssertions*: rechazada — la versión 8+ requiere licencia comercial (Xceed); se descarta para evitar restricciones de licencia en el proyecto.
- *Aserciones nativas de xUnit (`Assert.*`)*: rechazada — mensajes de fallo menos descriptivos; la constitución prioriza legibilidad.

---

## Decisión 3: Verificación de Minimal APIs y ASP.NET Core 10

**Decisión**: No añadir paquetes; verificar que `Api.csproj` usa `Microsoft.NET.Sdk.Web` con `TargetFramework` `net10.0`, lo que provee Minimal APIs de forma built-in.

**Justificación**: Minimal APIs forman parte del framework compartido de ASP.NET Core; no es un paquete NuGet separado. `Program.cs` ya declara `WebApplication.CreateBuilder` y endpoints (`MapHealthChecks`, `MapOpenApi`), confirmando disponibilidad. La verificación es de arranque, no de instalación.

**Alternativas consideradas**:
- *Añadir paquetes MVC/controllers*: rechazada — la constitución prohíbe MVC completo.

---

## Decisión 4: Alcance de Serilog (sinks y configuración)

**Decisión**: Limitar esta fase a verificar la **disponibilidad** de Serilog (`Serilog` + `Serilog.Extensions.Logging` en Infrastructure, `Serilog.AspNetCore` en Api). La configuración de sinks estructurados JSON, enriquecedores y niveles se difiere.

**Justificación**: La spec acota explícitamente que esta fase instala/referencia dependencias; la configuración funcional (sinks JSON, contexto userId/facturaId) corresponde a fases posteriores (Suposiciones de la spec). `Program.cs` ya inicializa un logger de consola mínimo, suficiente para verificar que los tipos resuelven.

**Alternativas consideradas**:
- *Configurar sinks JSON ahora*: rechazada — fuera del alcance de "dependencias disponibles"; adelantaría trabajo de observabilidad.

---

## Decisión 5: Estrategia de verificación

**Decisión**: Verificar la fase con tres comprobaciones objetivas:
1. `dotnet restore` de `backend.slnx` sin conflictos de versión.
2. `dotnet build` de la solución con cero errores.
3. `dotnet test` ejecutando una prueba trivial de verificación que use una aserción de Shouldly (valida xUnit + Shouldly de extremo a extremo).

**Justificación**: Cubre directamente SC-002, SC-003 y SC-004, y es ejecutable de forma determinista en local y CI.

**Alternativas consideradas**:
- *Solo inspección de `.csproj`*: rechazada — no detecta conflictos de restauración ni fallos de runtime del runner (SC-003/SC-004).

---

## Resumen de Acciones Derivadas

| # | Acción | Capa/Proyecto | Tipo |
|---|--------|---------------|------|
| A1 | Mover `FluentValidation` 12.1.1 a Application | Application / Infrastructure | Reubicación |
| A2 | Añadir `Shouldly` 4.x | Tests | Nueva referencia |
| A3 | Verificar `Sdk.Web` + net10.0 (Minimal APIs) | Api | Verificación |
| A4 | Verificar Serilog disponible (3 paquetes) | Api + Infrastructure | Verificación |
| A5 | Verificar MongoDB.Driver disponible | Infrastructure | Verificación |
| A6 | restore + build + test verde | Solución | Verificación |

**Todas las decisiones resueltas. Sin NECESITA CLARIFICACIÓN pendientes.**
