# Plan de Implementación: Dependencias Backend

**Rama**: `002-backend-dependencies` | **Fecha**: 2026-06-24 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de feature de `/specs/002-backend-dependencies/spec.md`

## Resumen

Garantizar que el backend ASP.NET Core 10 dispone de todas las dependencias requeridas por la constitución, ubicadas en la capa correcta según la dirección de dependencias de Arquitectura Limpia, y verificadas mediante compilación y ejecución de pruebas. El scaffolding de proyectos de la Fase 0.1 ya instaló la mayoría de los paquetes; esta fase **cierra las brechas detectadas** y formaliza la matriz dependencia→capa como contrato verificable.

**Brechas detectadas en el estado actual** (auditoría de los `.csproj`):

1. **Librería de aserciones ausente** en el proyecto `Tests` (solo están xUnit + Test SDK). Bloquea el estilo de aserciones legibles exigido por la constitución (Test-First). Se adopta `Shouldly` (OSS, sin restricción de licencia).
2. **FluentValidation mal ubicado**: referenciado en `Infrastructure` (`12.1.1`) pero la spec (FR-003) y Arquitectura Limpia lo ubican en `Application`, donde residen los validadores de comandos/DTOs.

El resto de dependencias (MongoDB Driver, Serilog, Minimal APIs vía `Sdk.Web`, xUnit) ya están presentes y solo requieren verificación.

## Contexto Técnico

**Lenguaje/Versión**: ASP.NET Core 10 / .NET 10 SDK (`global.json` fija `10.0.301`, rollForward `latestFeature`)

**Dependencias Primarias** (objetivo de esta fase, por capa):

- **Api** (`Sdk.Web`): Minimal APIs (built-in), `Microsoft.AspNetCore.OpenApi` 10.0.6, `Serilog.AspNetCore` 9.0.0
- **Infrastructure**: `MongoDB.Driver` 3.4.0, `Serilog` 4.3.0, `Serilog.Extensions.Logging` 9.0.0
- **Application**: `FluentValidation` 12.1.1 (a mover desde Infrastructure), `Microsoft.Extensions.Logging.Abstractions`
- **Domain**: sin dependencias de infraestructura (pura)
- **Tests**: `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.1, **`Shouldly` (a añadir)**

**Almacenamiento**: MongoDB (driver referenciado; configuración de conexión es Fase 0.4)

**Testing**: xUnit + Shouldly

**Plataforma Objetivo**: Docker containers (desarrollo local + VPS Linux); runtime .NET 10

**Tipo de Proyecto**: Web application backend multi-capa (Domain/Application/Infrastructure/Api + Tests)

**Objetivos de Performance**: N/A para esta fase (sin lógica de runtime nueva). La restauración + compilación de la solución debe completarse en una ejecución sin errores.

**Restricciones**:

- Dirección de dependencias de Arquitectura Limpia: capas externas dependen de internas, nunca al revés. Domain permanece sin paquetes de infraestructura.
- Versiones mayores alineadas con el stack de la constitución.
- Sin introducir EF Core ni MVC completo (solo MongoDB Driver y Minimal APIs).

**Escala/Alcance**: 5 proyectos en la solución (`backend.slnx`); 6 dependencias objetivo; 2 brechas a cerrar.

## Revisión de Constitución

*PUERTA: Debe pasar antes de investigación de Fase 0. Re-chequear después de diseño de Fase 1.*

### Alineación con Principios

✅ **I. Arquitectura Limpia (NO NEGOCIABLE)**: El plan refuerza la dirección de dependencias — mueve FluentValidation a Application (capa de validadores), mantiene MongoDB/Serilog en Infrastructure y Domain libre de infraestructura. **CUMPLE** (de hecho, corrige una desviación previa).

✅ **II. Principios SOLID (NO NEGOCIABLE)**: Las dependencias se referencian por capa para que cada capa dependa solo de abstracciones permitidas. Sin impacto negativo. **CUMPLE**.

✅ **III. Desarrollo Dirigido por Especificaciones**: Esta fase deriva de la spec 0.2 en formato GIVEN/WHEN/THEN; documentación en español. **CUMPLE**.

✅ **IV. Desarrollo Test-First (NO NEGOCIABLE)**: Cerrar la brecha de la librería de aserciones (Shouldly) habilita el ciclo Red-Green-Refactor con aserciones legibles desde el inicio. **CUMPLE** (esta fase es prerequisito directo del principio).

➖ **V. Frontend de Calidad Producción**: No aplica (fase exclusivamente backend).

✅ **VI. Código Observable y Mantenible**: Serilog (structured logging) ya referenciado en Api + Infrastructure; esta fase lo verifica. **CUMPLE**.

✅ **Stack Tecnológico**: ASP.NET Core 10, Minimal APIs, MongoDB Driver (sin EF), FluentValidation, Serilog, xUnit + Shouldly — todas contempladas. **CUMPLE**.

### Resultado de la Puerta

**✅ APROBADO** — Sin violaciones. La sección de Complejidad no requiere justificaciones.

## Estructura del Proyecto

### Documentación (esta feature)

```text
specs/002-backend-dependencies/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Salida Fase 0 (/speckit-plan)
├── data-model.md        # Salida Fase 1 — matriz dependencia→capa (/speckit-plan)
├── quickstart.md        # Salida Fase 1 — guía de verificación (/speckit-plan)
├── contracts/           # Salida Fase 1 — contrato de dependencias (/speckit-plan)
│   └── dependency-matrix.md
└── tasks.md             # Salida Fase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Código Fuente (raíz del repositorio)

```text
backend/
├── backend.slnx                 # Solución (5 proyectos)
├── Domain/
│   └── Domain.csproj            # net10.0, sin dependencias de infraestructura
├── Application/
│   └── Application.csproj        # + FluentValidation (movido desde Infrastructure)
├── Infrastructure/
│   └── Infrastructure.csproj     # MongoDB.Driver, Serilog, Serilog.Extensions.Logging
├── Api/
│   └── Api.csproj                # Sdk.Web (Minimal APIs), OpenApi, Serilog.AspNetCore
└── Tests/
    └── Tests.csproj             # Test SDK, xunit, runner, + Shouldly (añadir)
```

**Decisión de Estructura**: Se mantiene la estructura multi-capa establecida en la Fase 0.1. Esta fase solo modifica las secciones `<PackageReference>` de los `.csproj` afectados (`Application`, `Infrastructure`, `Tests`); no crea proyectos ni directorios nuevos.

## Seguimiento de Complejidad

> Sin violaciones de la Revisión de Constitución. No se requieren justificaciones de complejidad.
