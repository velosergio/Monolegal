# Plan de Implementación: Email Service Interface

**Branch**: `011-email-service-interface` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/011-email-service-interface/spec.md`

## Summary

Definir el contrato abstracto `IEmailService` en la capa de aplicación del backend para desacoplar a los consumidores (el worker de transiciones de la Fase 3) del proveedor de correo concreto. El contrato expone dos operaciones asíncronas: enviar un correo de recordatorio y enviar un correo de confirmación de pago, ambas recibiendo la dirección de correo del cliente y la entidad `Invoice` asociada.

Enfoque técnico: esta spec entrega **únicamente la abstracción** (interfaz C#), siguiendo el precedente de `IDevDataSeeder` (`Backend.Application.Abstractions`). No incluye implementación concreta (proveedor SMTP/API, plantillas, configuración) — eso corresponde a la Spec 3.3. Para satisfacer Test-First y el criterio de sustituibilidad (CE-002), se añade una prueba de contrato que verifica que el contrato puede ser sustituido por una implementación falsa (fake) e invocado de forma asíncrona con los datos esperados. Se respeta la Arquitectura Limpia: la interfaz vive en `Application`, y cualquier cambio de proveedor de correo quedará confinado a `Infrastructure` (Spec 3.3).

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`, SDK 10.0.301)

**Primary Dependencies**: Ninguna nueva. Solo la BCL (`System.Threading.Tasks`, `System.Threading`) y la entidad de dominio `Monolegal.Domain.Entities.Invoice` (ya existente, spec 005). El proveedor de correo real se introduce en la Spec 3.3.

**Storage**: N/A — el contrato no persiste datos. La actualización de `LastReminderSentAt`/`RemindersCount` es responsabilidad del flujo consumidor (Spec 3.3), no del contrato.

**Testing**: xUnit + Shouldly. Prueba de contrato en `Monolegal.Application.Tests` usando una implementación falsa (fake) de `IEmailService` que registra las invocaciones y demuestra la sustituibilidad e invocación asíncrona.

**Target Platform**: Servicio web/worker Linux (contenedor Docker), backend administrativo.

**Project Type**: Web service por capas (Domain/Application/Infrastructure/Api). Esta feature toca exclusivamente la capa `Application`.

**Performance Goals**: N/A — la definición de un contrato no tiene presupuesto de rendimiento. Las operaciones se declaran asíncronas (`Task`) para que la futura implementación no bloquee al invocador.

**Constraints**: Segregación de interfaces (ISP): contrato pequeño y cohesivo (solo notificaciones por correo). Inversión de dependencias (DIP): consumidores dependen de la abstracción. Ambas operaciones deben ser asíncronas. La documentación se mantiene en español (Constitución III).

**Scale/Scope**: 1 archivo nuevo de interfaz (`IEmailService.cs`) con 2 métodos, 1 archivo de prueba de contrato con un fake. Sin wiring de DI en esta spec (no hay implementación concreta que registrar todavía).

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | La interfaz vive en `Application`; consumidores (Infrastructure/Api) dependen de la abstracción. Un futuro cambio de proveedor de correo queda confinado a `Infrastructure` (Spec 3.3), nunca se propaga a capas internas. | ✅ PASS |
| II. SOLID | ISP: contrato cohesivo y pequeño (solo correo). DIP: se depende de la abstracción, no de la concreción. OCP: nuevas implementaciones se agregan sin modificar el contrato. | ✅ PASS |
| III. SDD (specs en español) | Spec 011 escrita y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Se escribe primero una prueba de contrato (fake substituible, invocación asíncrona) que demuestra CE-002 antes de definir la interfaz. Una interfaz no tiene lógica ejecutable propia; la cobertura aplica a la implementación de la Spec 3.3. | ✅ PASS (compromiso) |
| V. Frontend Producción | No aplica: feature de backend (contrato de dominio/aplicación). | ➖ N/A |
| VI. Observable y Mantenible | El contrato se documenta con XML-doc en español; el logging estructurado del envío real corresponde a la implementación (Spec 3.3/3.4). DI por constructor se aplicará al inyectar la implementación. | ✅ PASS |
| Stack tecnológico | Sin dependencias nuevas; solo BCL y la entidad de dominio existente. | ✅ PASS |
| Seguridad | El contrato no maneja secretos. La configuración del proveedor (credenciales SMTP/API) será por variables de entorno en la Spec 3.3. | ✅ PASS |
| Performance | N/A para una definición de contrato; operaciones asíncronas para no bloquear. | ✅ PASS |

**Resultado del gate**: PASS. Ningún principio NO NEGOCIABLE se incumple. El elemento diferido (implementación concreta, logging de envío) corresponde explícitamente a la Spec 3.3 y está documentado en los Assumptions de la spec.

## Project Structure

### Documentation (this feature)

```text
specs/011-email-service-interface/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas
├── data-model.md        # Phase 1 — datos que cruzan el contrato (Invoice, email)
├── quickstart.md        # Phase 1 — guía de validación del contrato
├── contracts/           # Phase 1 — contrato de la interfaz
│   └── IEmailService.md      # Contrato IEmailService (operaciones, parámetros, errores)
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
backend/
└── Application/
    └── Abstractions/
        ├── IDevDataSeeder.cs        # (existente) precedente de ubicación de contratos
        └── IEmailService.cs         # (NUEVO) contrato de notificaciones por correo
                                      #   - Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken)
                                      #   - Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken)

backend/Tests/Monolegal.Application.Tests/Email/
└── EmailServiceContractTests.cs     # (NUEVO) fake substituible + invocación asíncrona (CE-002)
```

**Structure Decision**: Web service por capas ya establecido en `backend/`. Esta feature añade exclusivamente un contrato en la capa `Application` (`Backend.Application.Abstractions`), siguiendo el precedente de `IDevDataSeeder`. No se toca `Domain`, `Infrastructure` ni `Api`, y no se registra DI todavía (no existe implementación concreta hasta la Spec 3.3), respetando la dirección de dependencias de la Arquitectura Limpia.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. El alcance se limita a una abstracción; la implementación concreta del proveedor de correo se difiere a la Spec 3.3, lo que mantiene esta feature mínima y cohesiva.
