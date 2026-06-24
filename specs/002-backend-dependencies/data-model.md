# Modelo de Datos: Dependencias Backend

**Fase**: Fase 1 - Diseño
**Entrada**: [plan.md](plan.md), [research.md](research.md)

---

## Descripción

Esta fase no introduce entidades de negocio (Invoice, Client, etc. se definen en fases posteriores de Dominio). El "modelo de datos" aquí es la **matriz dependencia→capa**: el artefacto de configuración que define qué paquete vive en qué capa y por qué, respetando la dirección de dependencias de Arquitectura Limpia.

---

## Matriz Dependencia → Capa

| Dependencia | Capa permitida | Rol | Versión objetivo | Estado |
|-------------|----------------|-----|------------------|--------|
| Minimal APIs (built-in `Sdk.Web`) | Api | Exposición de endpoints HTTP sin MVC | net10.0 | ✅ Presente |
| `Microsoft.AspNetCore.OpenApi` | Api | Documentación OpenAPI | 10.0.6 | ✅ Presente |
| `Serilog.AspNetCore` | Api | Integración logging con el host web | 9.0.0 | ✅ Presente |
| `MongoDB.Driver` | Infrastructure | Persistencia documental | 3.4.0 | ✅ Presente |
| `Serilog` | Infrastructure | Logging estructurado (núcleo) | 4.3.0 | ✅ Presente |
| `Serilog.Extensions.Logging` | Infrastructure | Puente con `ILogger` de .NET | 9.0.0 | ✅ Presente |
| `FluentValidation` | **Application** | Validación de comandos/DTOs | 12.1.1 | ⚠️ A mover desde Infrastructure |
| `Microsoft.Extensions.Logging.Abstractions` | Application | Abstracción de logging | 10.0.9 | ✅ Presente |
| `Microsoft.NET.Test.Sdk` | Tests | Host de ejecución de pruebas | 17.14.1 | ✅ Presente |
| `xunit` | Tests | Framework de pruebas | 2.9.3 | ✅ Presente |
| `xunit.runner.visualstudio` | Tests | Runner / descubrimiento | 3.1.1 | ✅ Presente |
| **`Shouldly`** | **Tests** | Aserciones legibles | 4.x | ❌ A añadir |

> **Domain**: deliberadamente sin entradas. Permanece libre de dependencias de infraestructura (invariante).

---

## Reglas de Validación del Modelo (invariantes de Arquitectura Limpia)

- **RV-1**: `Domain.csproj` no contiene ningún `<PackageReference>` de infraestructura (MongoDB, Serilog, FluentValidation). Solo BCL.
- **RV-2**: La dirección de referencias entre proyectos es `Api → Infrastructure → Application → Domain` (las externas dependen de las internas, nunca al revés).
- **RV-3**: `FluentValidation` se referencia únicamente en `Application` (no en Infrastructure ni Api directamente).
- **RV-4**: Las dependencias de testing (`Shouldly`, `xunit`, Test SDK) se referencian únicamente en `Tests`.
- **RV-5**: La versión mayor de cada paquete es coherente con el stack de la constitución (ASP.NET Core / .NET 10, MongoDB Driver 3.x, Serilog 4.x, FluentValidation 12.x, xUnit 2.x, Shouldly 4.x).

---

## Transiciones de Estado

No aplica — las dependencias son referencias estáticas declaradas en los `.csproj`; no existen transiciones de estado en runtime para este modelo.
