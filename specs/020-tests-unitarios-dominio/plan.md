# Implementation Plan: Tests Unitarios del Dominio

**Branch**: `main` (sin rama dedicada) | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/020-tests-unitarios-dominio/spec.md`

## Summary

Consolidar y completar la suite de pruebas unitarias de la capa de dominio (`backend/Domain`) hasta garantizar de forma verificable el umbral de cobertura ≥ 85% exigido por la constitución (Principio IV), usando xUnit + Shouldly con ejecución aislada (sin BD/red).

El comportamiento de mayor riesgo que pide la spec —matriz de transiciones de estado (US1) y creación/validación de facturas (US2)— **ya está cubierto por encima del umbral** (Invoice 95.1%, InvoiceTransitionService 98.4%, Client/InvoiceItem/StatusChange 100%). La línea base medida del **proyecto de dominio completo es 62.06% de líneas**, arrastrada por clases de la capa `Domain/Email` sin ninguna prueba. Por tanto, el trabajo real del plan es: (1) cerrar los huecos de cobertura concentrados en `Domain/Email` y en los bordes de `SystemSettings`/`SmtpSettings`, (2) reforzar explícitamente los casos de transición prohibida y validación de creación que formaliza la spec, (3) hacer la cobertura del 85% un gate automático y reproducible, y (4) limpiar el andamiaje (`UnitTest1.cs`).

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)

**Primary Dependencies**: xUnit 2.9.3, Shouldly 4.3.0, coverlet.collector 10.0.1, Microsoft.NET.Test.Sdk 18.7.0

**Storage**: N/A — las pruebas de dominio son puras, sin persistencia (MongoDB queda fuera del SUT)

**Testing**: xUnit (runner `xunit.runner.visualstudio`), aserciones con Shouldly, cobertura vía coverlet (`--collect:"XPlat Code Coverage"`, formato Cobertura)

**Target Platform**: Proyecto de pruebas `backend/Tests/Monolegal.Domain.Tests`, ejecutado en dev y en el gate de CI

**Project Type**: Backend (web service) con arquitectura limpia; esta feature toca únicamente el proyecto de pruebas del dominio y su configuración de cobertura

**Performance Goals**: Suite completa < 10 s en máquina de desarrollo (línea base actual: 105 pruebas en 183 ms) — evidencia de aislamiento total

**Constraints**: Sin dependencias externas (BD/red/FS); sin pruebas omitidas (`Skip`/`[Ignore]`); cobertura de líneas del dominio ≥ 85%

**Scale/Scope**: 1 proyecto de pruebas; SUT = `backend/Domain` (entidades, enums, servicios de dominio y `Domain/Email`); ~17 archivos fuente de dominio

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|-----------|--------|
| I. Arquitectura Limpia | Las pruebas viven en un proyecto separado que sólo referencia `Domain.csproj`; no se cruzan capas. El SUT es la capa más interna. | ✅ |
| II. SOLID | La feature no introduce producción nueva; las pruebas validan contratos existentes (sustituibilidad de estados, invariantes de entidad). | ✅ |
| III. SDD | Spec GIVEN/WHEN/THEN escrita y aprobada (spec 020); este plan deriva de ella. Documentación en español. | ✅ |
| IV. Test-First (NO NEGOCIABLE) | Núcleo de esta feature: elevar y blindar cobertura ≥ 85% con gate automático; sin skips. | ✅ (objetivo directo) |
| V. Frontend de calidad | No aplica (feature backend). | N/A |
| VI. Observable/Mantenible | Reporte de cobertura reproducible por PR; pruebas legibles (Shouldly) y deterministas (tiempo inyectado). | ✅ |

**Veredicto**: PASA. Sin violaciones. No se requiere sección de Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/020-tests-unitarios-dominio/
├── plan.md              # Este archivo
├── research.md          # Phase 0: decisiones (línea base de cobertura, gate, alcance)
├── data-model.md        # Phase 1: sujetos de prueba e invariantes del dominio
├── quickstart.md        # Phase 1: cómo ejecutar la suite y verificar ≥85%
├── contracts/
│   └── test-inventory.md # Phase 1: inventario de casos por clase (Given/When/Then)
├── checklists/
│   └── requirements.md   # Checklist de calidad de la spec (ya creado)
└── tasks.md             # Phase 2 (/speckit-tasks — NO lo crea /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/                              # SUT — sin cambios de producción esperados
│   ├── Entities/                        # Invoice, InvoiceItem, Client, StatusChange, SystemSettings…
│   ├── Enums/                           # InvoiceStatus, NotificationOutcome, …
│   ├── Services/                        # InvoiceTransitionService
│   └── Email/                           # EmailTemplateRenderer, EmailTemplateVariables  <- hueco principal
└── Tests/
    └── Monolegal.Domain.Tests/          # Proyecto de pruebas (objetivo del trabajo)
        ├── Entities/                    # InvoiceTests, InvoiceItemsTests, ClientTests
        ├── Services/                    # InvoiceTransitionServiceTests
        ├── Email/                       # <- NUEVO: EmailTemplateRendererTests, EmailTemplateVariablesTests
        ├── InvoiceStatusTransitionsTests.cs
        ├── InvoiceManualTransitionTests.cs
        ├── InvoicePaymentTests.cs
        ├── InvoiceNotificationRetryTests.cs
        ├── SystemSettingsEmailTests.cs  # <- ampliar bordes (SmtpSettings/SystemSettings)
        └── UnitTest1.cs                 # <- eliminar (andamiaje vacío)
```

**Structure Decision**: Se reutiliza el proyecto de pruebas existente `backend/Tests/Monolegal.Domain.Tests` (ya configurado con xUnit + Shouldly + coverlet). El trabajo añade una carpeta `Email/` para las clases sin cobertura, amplía los bordes de `SystemSettings`/`SmtpSettings`, formaliza los casos de transición prohibida/validación de la spec y añade un gate de cobertura. No se prevé crear código de producción; si algún hueco de cobertura revela código muerto, se documentará como hallazgo en lugar de añadir pruebas artificiales.

## Complexity Tracking

> No aplica — Constitution Check sin violaciones.
