# Plan de Implementación: Entidad Invoice

**Rama**: `005-invoice-entity` | **Fecha**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

**Entrada**: Especificación de la funcionalidad desde `/specs/005-invoice-entity/spec.md`

## Resumen

Creación de la entidad de dominio `Invoice` que servirá como base central de datos para el módulo de facturación, soportando el ciclo de vida de pagos mediante el enumerador `InvoiceStatus` y adhiriéndose estrictamente a las reglas de Clean Architecture del proyecto.

## Contexto Técnico

**Lenguaje/Versión**: C# / .NET 10 (Backend)
**Dependencias Principales**: FluentValidation (para validación en dominio/aplicación)
**Almacenamiento**: MongoDB (Driver C#) - Aislado en Infrastructure
**Testing**: xUnit + Shouldly
**Plataforma Destino**: Docker Compose / Linux VPS
**Tipo de Proyecto**: Web API (ASP.NET Core Minimal APIs)
**Arquitectura**: Clean Architecture (Domain, Application, Infrastructure, Api)
**Reglas Obligatorias**: SOLID, documentar en español, test-first.

## Revisión de Constitución

*GATE: Superado.*

- **Arquitectura Limpia**: Se garantizará que la entidad `Invoice` resida en la capa `Domain` sin ninguna dependencia externa (específicamente sin acoplamiento a MongoDB).
- **Desarrollo Test-First**: La implementación comenzará con pruebas unitarias en `xUnit` + `Shouldly` validando el estado inicial, asignaciones y excepciones.
- **SDD y Documentación**: Toda la planificación y artefactos se generarán en español.

## Estructura del Proyecto

### Documentación (esta funcionalidad)

```text
specs/005-invoice-entity/
├── plan.md              # Este archivo
├── research.md          # Decisiones técnicas y de diseño
├── data-model.md        # Modelo de datos detallado
├── quickstart.md        # Guía para probar la implementación
├── contracts/           # Interfaces expuestas (ej. IInvoiceRepository)
└── tasks.md             # Tareas de implementación
```

### Código Fuente (raíz del repositorio)

```text
backend/
├── src/
│   ├── Monolegal.Domain/
│   │   ├── Entities/
│   │   │   └── Invoice.cs
│   │   ├── Enums/
│   │   │   └── InvoiceStatus.cs
│   │   └── Repositories/
│   │       └── IInvoiceRepository.cs
└── tests/
    └── Monolegal.Domain.Tests/
        └── Entities/
            └── InvoiceTests.cs
```

**Decisión de Estructura**: Se utilizará la estructura Backend Clean Architecture, situando la entidad, enumerador e interfaces de repositorio en la capa `Domain`, y las pruebas en el proyecto de pruebas unitarias asociado a `Domain`.

## Seguimiento de Complejidad

Ninguna violación a la constitución o desviación detectada.
