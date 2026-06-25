# Plan de Implementación: Worker — Transiciones de Estado

**Branch**: `012-worker-state-transitions` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/012-worker-state-transitions/spec.md`

## Summary

Consolidar el **proceso en segundo plano (hosted service)** que evalúa periódicamente las facturas activas y aplica las transiciones automáticas de estado por tiempo (`Pending → PrimerRecordatorio → SegundoRecordatorio → Desactivado`), delegando en las reglas de dominio ya existentes (spec 006), leyendo los umbrales de días desde la configuración administrable y registrando cada ejecución con Serilog.

Enfoque técnico: **la mayor parte de la maquinaria ya existe** y se reutiliza sin reescribirse — `InvoiceTransitionsWorker` (Infrastructure, `BackgroundService`), `InvoiceTransitionService` (Domain), `IInvoiceRepository.GetTransitionableAsync` + `MongoInvoiceRepository`, `ISystemSettingsRepository` (umbrales de días) y el endpoint manual `POST /api/workers/trigger-transitions`. Esta spec cierra las **brechas** que el worker actual no cubre frente a los requisitos 012:

1. **Intervalo configurable** (FR-001/FR-002, SC-005): hoy el intervalo es una constante embebida (`static readonly TimeSpan = 1h`). Se externaliza a configuración (`IOptions`/variable de entorno) con un default razonable.
2. **Aislamiento de errores por factura** (FR-007, SC-004): hoy un fallo en una factura aborta el lote del ciclo. Se envuelve cada factura en su propio `try/catch` para que el lote continúe.
3. **Conteo de errores en el resumen** (FR-008): el log de fin de ciclo registra evaluadas/transicionadas/duración; falta el contador de errores.
4. **Pruebas directas del worker** (Test-First): hoy las pruebas validan un handler simulado en memoria; se añaden pruebas sobre `RunCycleAsync` real (aislamiento de errores, resumen estructurado, repositorio vacío, intervalo configurable).

Lo demás (detección de candidatos, exclusión de estados terminales, delegación a reglas de dominio, persistencia, apagado ordenado, no solapamiento por ejecución secuencial, log por transición) **ya cumple** y solo se verifica.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)

**Primary Dependencies**: `Microsoft.Extensions.Hosting` (`BackgroundService`), `Microsoft.Extensions.Options` / `Microsoft.Extensions.Configuration` (intervalo configurable), Serilog (logging estructurado), MongoDB.Driver (persistencia, vía repositorios existentes). Sin dependencias nuevas.

**Storage**: MongoDB. El worker no persiste estado propio; lee facturas y settings y actualiza facturas a través de los repositorios existentes. No hay estado en memoria entre ciclos (Constitución: worker sin estado).

**Testing**: xUnit + Shouldly. Pruebas de aplicación/infra sobre `RunCycleAsync` con repositorios en memoria (fakes ya disponibles en `InvoiceWorkerTests`/`Support`). El acceso al método `internal RunCycleAsync` requiere `InternalsVisibleTo("Tests")` en el ensamblado `Infrastructure`.

**Target Platform**: Servicio Linux en contenedor Docker (proceso de larga vida). El worker corre hoy dentro del host del API vía `AddHostedService<InvoiceTransitionsWorker>()`; opcionalmente puede ejecutarse en un contenedor worker separado reutilizando el mismo registro de Infrastructure.

**Project Type**: Web service por capas (Domain/Application/Infrastructure/Api). Esta feature toca `Infrastructure` (worker + opciones) y la configuración de DI; reutiliza `Domain` sin modificarlo.

**Performance Goals**: El ciclo debe procesar el lote de facturas transicionables sin bloquear el apagado. No hay presupuesto p95 estricto (proceso de fondo); el objetivo es completar cada ciclo holgadamente dentro del intervalo configurado para evitar solapamiento.

**Constraints**: Sin estado en memoria (escalable horizontalmente / reiniciable). Apagado ordenado ante `CancellationToken`. Un fallo por factura no debe abortar el lote. Logging estructurado JSON (Serilog). Documentación en español (Constitución III).

**Scale/Scope**: Decenas–miles de facturas activas por ciclo. Cambios acotados: 1 clase de opciones nueva, edición del worker (intervalo + try/catch por factura + contador de errores), 1 línea de `InternalsVisibleTo`, registro de opciones en DI y nuevas pruebas del worker.

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | Las reglas de transición viven en `Domain` (`InvoiceTransitionService`); el worker en `Infrastructure` orquesta y persiste; no hay lógica de negocio nueva fuera de `Domain`. Un cambio de proveedor de scheduling/persistencia queda confinado a `Infrastructure`. | ✅ PASS |
| II. SOLID | SRP: el worker solo orquesta el ciclo; las reglas son del servicio de dominio. DIP: depende de `IInvoiceRepository`/`ISystemSettingsRepository` por constructor. OCP: el intervalo se inyecta vía opciones sin modificar la clase para cambiar la frecuencia. | ✅ PASS |
| III. SDD (specs en español) | Spec 012 escrita y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Se añaden pruebas que primero fallan sobre las brechas (aislamiento de error por factura, contador de errores, intervalo configurable) antes de modificar el worker. | ✅ PASS |
| V. Frontend Producción | No aplica: feature de backend/worker. | ➖ N/A |
| VI. Observable y Mantenible | Serilog estructurado por ciclo (timestamp, evaluadas, transicionadas, errores, duración) y por transición (id, estado anterior/nuevo). DI por constructor. | ✅ PASS |
| Stack tecnológico | `BackgroundService` + Serilog + MongoDB Driver, todos ya en uso; sin dependencias nuevas. | ✅ PASS |
| Seguridad | El worker no maneja secretos; la cadena de conexión proviene de variables de entorno (spec 004). El intervalo es config no sensible. | ✅ PASS |
| Performance & Escalabilidad | Sin estado en memoria → replicable horizontalmente. Ejecución secuencial por instancia evita procesar el mismo lote dos veces dentro de una instancia. | ✅ PASS |

**Resultado del gate**: PASS. Ningún principio NO NEGOCIABLE se incumple.

> **Nota de concurrencia multi-réplica**: con múltiples réplicas del worker podrían evaluarse las mismas facturas en paralelo. La idempotencia de la transición (releer estado + condición por días) y la actualización por documento mitigan duplicados de efecto de negocio; un bloqueo distribuido es una mejora futura fuera del alcance de esta spec (documentado en research.md D4).

## Project Structure

### Documentation (this feature)

```text
specs/012-worker-state-transitions/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas (intervalo, errores, scheduling)
├── data-model.md        # Phase 1 — entidades y configuración que usa el worker
├── quickstart.md        # Phase 1 — guía de validación del worker
├── contracts/
│   └── invoice-transitions-worker.md   # Contrato de comportamiento del worker + config + endpoint trigger
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Services/InvoiceTransitionService.cs      # (existente) reglas de transición — REUTILIZADO
│   ├── Entities/Invoice.cs                        # (existente) Status, LastStatusTransitionAt, etc.
│   ├── Entities/SystemSettings.cs                 # (existente) InvoiceTransitionsConfig (umbrales de días)
│   └── Repositories/IInvoiceRepository.cs         # (existente) GetTransitionableAsync — REUTILIZADO
├── Infrastructure/
│   ├── Workers/
│   │   ├── InvoiceTransitionsWorker.cs            # (EDITADO) intervalo configurable + try/catch por factura + contador errores
│   │   └── InvoiceTransitionsWorkerOptions.cs     # (NUEVO) opciones: IntervalMinutes (con default) + RunOnStartup
│   ├── Repositories/MongoInvoiceRepository.cs     # (existente) GetTransitionableAsync — REUTILIZADO
│   └── Configuration/DependencyInjection.cs       # (EDITADO) bind de opciones del worker desde IConfiguration
└── (Infrastructure assembly)                       # (EDITADO) [assembly: InternalsVisibleTo("Tests")] para probar RunCycleAsync

backend/Tests/Monolegal.Application.Tests/
└── InvoiceTransitionsWorkerCycleTests.cs          # (NUEVO) pruebas sobre RunCycleAsync real (errores aislados, resumen, vacío, intervalo)
```

**Structure Decision**: Web service por capas ya establecido. El worker se mantiene en `Infrastructure/Workers` (orquestación + acceso a infraestructura) y delega las reglas en `Domain`. El intervalo se externaliza mediante una clase de opciones en `Infrastructure` enlazada desde `IConfiguration` (variable de entorno / sección de config), preservando la dirección de dependencias de la Arquitectura Limpia.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. El alcance se limita a externalizar la configuración del intervalo, endurecer el manejo de errores por factura, completar la observabilidad (conteo de errores) y añadir pruebas directas del worker, reutilizando todas las reglas y repositorios existentes.
