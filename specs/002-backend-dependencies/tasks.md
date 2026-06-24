---
description: "Lista de tareas para la implementación de Dependencias Backend"
---

# Tasks: Dependencias Backend

**Input**: Documentos de diseño en `/specs/002-backend-dependencies/`

**Prerequisites**: [plan.md](plan.md) (requerido), [spec.md](spec.md) (historias de usuario), [research.md](research.md), [data-model.md](data-model.md), [contracts/dependency-matrix.md](contracts/dependency-matrix.md)

**Tests**: Esta feature es de configuración de dependencias. Las "pruebas" son **smoke tests de disponibilidad** que verifican que cada paquete resuelve y sus tipos son utilizables (derivadas de los Escenarios de Aceptación y de [quickstart.md](quickstart.md)). Se incluyen porque son el mecanismo de verificación de la propia feature.

**Organización**: Tareas agrupadas por historia de usuario para implementación y verificación independiente. Todas las historias son P1 (cada una habilita una capacidad de la constitución).

## Formato: `[ID] [P?] [Story] Descripción`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US5)
- Rutas de archivo exactas incluidas en cada descripción

## Convenciones de Rutas

Proyecto backend multi-capa en `backend/` (ver [plan.md](plan.md)):
`backend/Domain`, `backend/Application`, `backend/Infrastructure`, `backend/Api`, `backend/Tests`. Solución: `backend/backend.slnx`.

---

## Phase 1: Setup (Infraestructura Compartida)

**Propósito**: Establecer línea base reproducible antes de modificar dependencias.

- [X] T001 Verificar que el SDK .NET 10 está disponible y coincide con `global.json` ejecutando `dotnet --version` (esperado `10.0.3xx`) en la raíz del repositorio
- [X] T002 [P] Restaurar dependencias de la solución con `dotnet restore backend/backend.slnx` y confirmar ausencia de advertencias `NU1605`/`NU1107`
- [X] T003 Compilación baseline con `dotnet build backend/backend.slnx -c Release` para registrar el estado inicial antes de los cambios

**Checkpoint**: Solución restaura y compila en su estado actual (pre-cambios).

---

## Phase 2: Foundational (Prerequisitos Bloqueantes)

**Propósito**: Habilitar el framework de aserciones, prerequisito transversal de todas las smoke tests de las historias.

**⚠️ CRÍTICO**: Ninguna smoke test de US1–US5 puede compilarse hasta completar esta fase.

- [X] T004 Añadir `<PackageReference Include="Shouldly" Version="4.*" />` al `<ItemGroup>` de paquetes en `backend/Tests/Tests.csproj` (acción A2 de research.md)
- [X] T005 [P] Crear carpeta `backend/Tests/Dependencies/` para alojar las smoke tests de disponibilidad de dependencias
- [X] T006 Restaurar y verificar que Shouldly resuelve con `dotnet restore backend/Tests/Tests.csproj` (sin conflictos de versión)

**Checkpoint**: Framework de aserciones disponible — las historias pueden implementarse en paralelo.

---

## Phase 3: User Story 1 - Persistencia de Datos Disponible (Priority: P1) 🎯 MVP

**Goal**: El driver de base de datos documental está referenciado en Infrastructure y sus tipos son instanciables.

**Independent Test**: Un test en Infrastructure/Tests instancia un cliente de base de datos sin errores de resolución de tipos; la solución compila con la dependencia resuelta.

### Implementación US1

- [X] T007 [US1] Verificar que `MongoDB.Driver` (3.4.0) está referenciado en `backend/Infrastructure/Infrastructure.csproj` (invariante I1/data-model)
- [X] T008 [P] [US1] Smoke test de disponibilidad: instanciar `MongoClient` y obtener `IMongoDatabase` en `backend/Tests/Dependencies/MongoDriverAvailabilityTests.cs`, aseverando con Shouldly que los tipos resuelven (no requiere servidor MongoDB en ejecución)

**Checkpoint**: US1 funcional — persistencia documental disponible y verificada.

---

## Phase 4: User Story 2 - Validación de Entradas Disponible (Priority: P1)

**Goal**: La librería de validación está referenciada en la capa Application (no en Infrastructure), respetando la dirección de dependencias.

**Independent Test**: Declarar un `AbstractValidator<T>` que compila y ejecuta reglas; `FluentValidation` aparece solo en Application.

### Implementación US2

- [X] T009 [US2] Mover la referencia `<PackageReference Include="FluentValidation" Version="12.1.1" />` desde `backend/Infrastructure/Infrastructure.csproj` hacia el `<ItemGroup>` de paquetes de `backend/Application/Application.csproj` (acción A1 de research.md)
- [X] T010 [US2] Verificar que `backend/Infrastructure/Infrastructure.csproj` ya **no** referencia `FluentValidation` directamente (la obtiene transitivamente vía Application) — invariante I3/RV-3
- [X] T011 [P] [US2] Smoke test de disponibilidad: declarar un `AbstractValidator<T>` trivial y validar un objeto de prueba en `backend/Tests/Dependencies/FluentValidationAvailabilityTests.cs`, aseverando el resultado con Shouldly

**Checkpoint**: US2 funcional — validación disponible en la capa correcta.

---

## Phase 5: User Story 3 - Logging Estructurado Disponible (Priority: P1)

**Goal**: Serilog está referenciado en Infrastructure y Api y es configurable en el arranque.

**Independent Test**: Construir una `LoggerConfiguration` sin errores de tipos; la API arranca con Serilog como proveedor de logging.

### Implementación US3

- [X] T012 [US3] Verificar que `Serilog` (4.3.0) y `Serilog.Extensions.Logging` (9.0.0) están en `backend/Infrastructure/Infrastructure.csproj`, y `Serilog.AspNetCore` (9.0.0) en `backend/Api/Api.csproj`
- [X] T013 [P] [US3] Smoke test de disponibilidad: construir un `LoggerConfiguration().WriteTo.Console().CreateLogger()` en `backend/Tests/Dependencies/SerilogAvailabilityTests.cs`, aseverando con Shouldly que el logger se crea (no nulo)

**Checkpoint**: US3 funcional — logging estructurado disponible.

---

## Phase 6: User Story 4 - Framework de API con APIs Mínimas Disponible (Priority: P1)

**Goal**: La capa Api usa `Sdk.Web` con `net10.0` (Minimal APIs built-in) y arranca exponiendo un endpoint mínimo.

**Independent Test**: La aplicación web arranca y `GET /health` responde, confirmando Minimal APIs disponibles.

### Implementación US4

- [X] T014 [US4] Verificar que `backend/Api/Api.csproj` declara SDK `Microsoft.NET.Sdk.Web`, `TargetFramework` `net10.0` y referencia `Microsoft.AspNetCore.OpenApi` (10.0.6)
- [X] T015 [US4] Verificar el arranque con `dotnet run --project backend/Api`: confirmar que el endpoint mínimo `GET /health` declarado en `backend/Api/Program.cs` responde y que Serilog emite el log de inicio (detener con Ctrl+C)

**Checkpoint**: US4 funcional — Minimal APIs disponibles y endpoint mínimo operativo.

---

## Phase 7: User Story 5 - Framework de Pruebas Disponible (Priority: P1)

**Goal**: El proyecto Tests dispone de xUnit + Shouldly y el runner descubre y ejecuta pruebas con aserciones legibles.

**Independent Test**: `dotnet test` descubre y ejecuta una prueba que usa una aserción Shouldly con resultado verde.

### Implementación US5

- [X] T016 [US5] Verificar que `backend/Tests/Tests.csproj` referencia `Microsoft.NET.Test.Sdk` (17.14.1), `xunit` (2.9.3), `xunit.runner.visualstudio` (3.1.1) y `Shouldly` (4.x, añadido en T004), con `IsTestProject=true`
- [X] T017 [P] [US5] Smoke test del framework: prueba `[Fact]` trivial que use una aserción Shouldly (ej. `resultado.ShouldBe(esperado)`) en `backend/Tests/Dependencies/TestFrameworkAvailabilityTests.cs`, confirmando descubrimiento y ejecución por el runner

**Checkpoint**: US5 funcional — framework de pruebas + aserciones legibles operativo.

---

## Phase 8: Polish & Verificación Transversal

**Propósito**: Validar la fase completa contra los criterios de éxito de la spec.

- [X] T018 Ejecutar `dotnet test backend/backend.slnx` y confirmar que las 4 smoke tests (T008, T011, T013, T017) pasan en verde (SC-004)
- [X] T019 Verificar invariantes de capa de [data-model.md](data-model.md): Domain sin paquetes de infraestructura (RV-1/SC-005), referencias de proyecto `Api→Infrastructure→Application→Domain` (RV-2), `FluentValidation` solo en Application (RV-3), dependencias de testing solo en Tests (RV-4)
- [X] T020 [P] Ejecutar los escenarios 1–4 de [quickstart.md](quickstart.md) (restore, build, test, arranque) y confirmar resultados esperados (SC-002, SC-003, SC-006)
- [X] T021 [P] Verificar que las 6 dependencias objetivo están referenciadas en su capa según [contracts/dependency-matrix.md](contracts/dependency-matrix.md) (SC-001, FR-010)

---

## Dependencies & Execution Order

### Dependencias de Fase

- **Setup (Phase 1)**: Sin dependencias — inicia de inmediato
- **Foundational (Phase 2)**: Depende de Setup — **BLOQUEA** todas las smoke tests de las historias
- **User Stories (Phase 3–7)**: Dependen de Foundational
  - Pueden ejecutarse en paralelo (tocan archivos distintos: cada historia su `.csproj` y su archivo de smoke test)
  - O secuencialmente; orden recomendado US1 → US2 → US3 → US4 → US5
- **Polish (Phase 8)**: Depende de que todas las historias deseadas estén completas

### Dependencias entre Historias

- Todas las historias son independientes entre sí tras Foundational. Cada una modifica/verifica un `.csproj` distinto y un archivo de smoke test propio.
- **Única dependencia transversal**: las smoke tests (T008, T011, T013, T017) requieren `Shouldly` (T004, en Foundational).
- **US2** es la única que **modifica** código (mueve FluentValidation); el resto son verificaciones más su smoke test, salvo el alta de Shouldly en Foundational.

### Dentro de Cada Historia

- Verificación/modificación del `.csproj` antes de su smoke test.

### Oportunidades de Paralelización

- T002 (restore) en paralelo dentro de Setup.
- Tras Foundational, las smoke tests T008, T011, T013, T017 son `[P]` (archivos distintos en `backend/Tests/Dependencies/`).
- Las cinco historias pueden repartirse entre desarrolladores en paralelo.
- En Polish, T020 y T021 son `[P]`.

---

## Parallel Example: Smoke tests tras Foundational

```bash
# Una vez completada la Phase 2 (Shouldly disponible), lanzar las smoke tests en paralelo:
Task: "Smoke test MongoDB.Driver en backend/Tests/Dependencies/MongoDriverAvailabilityTests.cs"
Task: "Smoke test FluentValidation en backend/Tests/Dependencies/FluentValidationAvailabilityTests.cs"
Task: "Smoke test Serilog en backend/Tests/Dependencies/SerilogAvailabilityTests.cs"
Task: "Smoke test framework xUnit+Shouldly en backend/Tests/Dependencies/TestFrameworkAvailabilityTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1: Setup
2. Completar Phase 2: Foundational (CRÍTICO — habilita verificación)
3. Completar Phase 3: US1 (persistencia documental disponible)
4. **DETENER y VALIDAR**: ejecutar la smoke test de US1 de forma independiente
5. Continuar con las siguientes historias

> Nota: en esta feature, US5 (framework de pruebas) es el **habilitador de verificación**; su paquete clave (Shouldly) se adelanta a Foundational para no bloquear las smoke tests de las demás historias.

### Incremental Delivery

1. Setup + Foundational → base lista (framework de aserciones disponible)
2. US1 → smoke test verde → persistencia lista (MVP)
3. US2 → mover FluentValidation → validación en capa correcta
4. US3 → logging disponible
5. US4 → Minimal APIs disponibles
6. US5 → framework de pruebas confirmado
7. Polish → verificación transversal contra SC-001..SC-006

### Parallel Team Strategy

Tras Foundational, repartir: Dev A→US1, Dev B→US2, Dev C→US3, Dev D→US4/US5. Cada historia integra de forma independiente (archivos distintos).

---

## Notes

- `[P]` = archivos distintos, sin dependencias pendientes.
- La mayoría de las historias son **verificación** (la Fase 0.1 ya instaló los paquetes); las dos acciones que **modifican** archivos son T004 (añadir Shouldly) y T009 (mover FluentValidation).
- Las smoke tests no requieren servicios externos (MongoDB en ejecución, etc.); solo comprueban que los tipos resuelven y son utilizables.
- Documentación de requisitos y comentarios de specs en español (constitución, Principio III).
- Hacer commit tras cada tarea o grupo lógico, referenciando la spec (ej. `feat(spec-0.2): mover FluentValidation a Application`).
