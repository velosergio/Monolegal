# Implementation Plan: invoice-status-transitions

**Branch**: `006-invoice-status-transitions` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-invoice-status-transitions/spec.md`

## Summary

Implementar un motor de transiciones de estado de facturas que permita el paso automático entre recordatorios basándose en tiempos de gracia configurables por el administrador desde una vista de configuración, y que soporte pagos manuales o automáticos.

## Technical Context

**Language/Version**: C# 10 (Backend), TypeScript Strict (Frontend)

**Primary Dependencies**: ASP.NET Core Minimal APIs, React 19+, Vite, TanStack Query, MongoDB Driver

**Storage**: MongoDB

**Testing**: xUnit + Shouldly (Backend), Vitest + Testing Library (Frontend), Playwright (E2E)

**Target Platform**: Web (Docker Compose: frontend, backend, worker, MongoDB)

**Project Type**: Web Application

**Performance Goals**: API endpoints stateless, queries ≤200ms

**Constraints**: Clean Architecture estricta, SOLID, sin EF, logs estructurados con Serilog

**Scale/Scope**: Paginación forzada, índices en base de datos.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Backend utiliza Clean Architecture (Domain/Application/Infrastructure/Api).
- [x] Cumple con SOLID y Dependency Inversion.
- [x] Desarrollo Test-First con cobertura ≥85%.
- [x] Frontend en React con Vite, TypeScript strict, TanStack Query.
- [x] Sin directivas de saltos de test (No `[Ignore]` / `.skip()`).

## Project Structure

### Documentation (this feature)

```text
specs/006-invoice-status-transitions/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── Monolegal.Domain/
│   ├── Monolegal.Application/
│   ├── Monolegal.Infrastructure/
│   └── Monolegal.Api/
└── tests/
    ├── Monolegal.Domain.Tests/
    └── Monolegal.Application.Tests/

frontend/
├── src/
│   ├── features/
│   │   ├── invoices/
│   │   └── settings/
│   ├── components/
│   └── lib/
└── tests/
```

**Structure Decision**: El proyecto sigue la estructura Clean Architecture definida en la Constitución, separando frontend y backend, y agrupando por features en el frontend.
