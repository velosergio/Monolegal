# Implementation Plan: Vista de Configuración — Proveedor de Email, Plantillas, Prueba de Envío y Herramientas

**Branch**: `017-configuracion-resend-tools` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/017-configuracion-resend-tools/spec.md`

## Summary

Convertir `/configuracion` en el centro de administración de envío de correos: (1) seleccionar y configurar el **proveedor activo** (SMTP o Resend) con datos no secretos persistidos vía API y credenciales **solo por entorno**, con validación; (2) **editar plantillas** (asunto/cuerpo) con un conjunto canónico de variables y vista previa; (3) **enviar correo de prueba**; (4) **herramientas globales** (reenvío masivo de fallidos, saneamiento de atascados). El cambio de proveedor activo y de plantillas aplica **en runtime** (el worker de transiciones —que corre in-process con la API— lee la configuración del sistema en cada ciclo).

Enfoque técnico: extender la entidad `SystemSettings` con `EmailSettings` y `EmailTemplates`; introducir una abstracción `IEmailProvider` (SMTP/Resend) en Infrastructure y un `IEmailService` respaldado por configuración que selecciona proveedor y renderiza plantillas en cada envío; añadir endpoints Minimal API bajo `/api/settings/email/*` con FluentValidation; y ampliar la feature `settings` del frontend (React 19 + TanStack Query + shadcn/ui + toasts ya existentes).

## Technical Context

**Language/Version**: C# / .NET 10 (backend, worker); TypeScript 5 strict (frontend, React 19)

**Primary Dependencies**: Backend — ASP.NET Core 10 Minimal APIs, MongoDB.Driver, FluentValidation, MailKit (SMTP), Serilog, `HttpClient` (Resend API REST). Frontend — Vite, React Router, TanStack Query, shadcn/ui, Motion, Biome.

**Storage**: MongoDB. Colección `SystemSettings` (documento singleton `singleton-settings`), extendida con `Email` (config no secreta) y `EmailTemplates`. Estado de notificación por factura ya embebido en la colección `Invoices` (`LastNotificationOutcome`, etc.).

**Testing**: Backend — xUnit + Shouldly (Domain/Application) e integración con `WebApplicationFactory<Program>`. Frontend — Vitest + Testing Library (+ MSW para API). E2E — Playwright para la jornada de configuración.

**Target Platform**: Contenedores Docker (frontend, backend+worker in-process, MongoDB) sobre VPS Linux; navegadores modernos (escritorio/móvil).

**Project Type**: Aplicación web (frontend SPA + backend API con worker hosted service in-process).

**Performance Goals**: API ≤200ms p95 en operaciones de configuración bajo carga normal; frontend TTI < 2s, Lighthouse > 90; herramientas masivas acotadas y observables.

**Constraints**: Credenciales (API key Resend, contraseña SMTP) **solo por variables de entorno / secrets**, nunca en BD ni expuestas en UI. Cambio de proveedor/plantillas en **runtime** (sin reinicio). Toda la documentación y la UI en **español**. Sin nuevas dependencias de runtime en frontend (toasts ya existen).

**Scale/Scope**: Panel Admin-only de baja concurrencia; volumen de facturas en órdenes de miles; herramientas masivas operan sobre el subconjunto con notificación fallida/atascada.

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio (Constitución) | Cumplimiento en este plan | Estado |
|---|---|---|
| I. Arquitectura Limpia | Proveedores concretos (SMTP/Resend) en Infrastructure tras `IEmailProvider`; contrato `IEmailService` en Application; settings en Domain; endpoints en Api. Cambio de proveedor confinado a Infrastructure. | PASS |
| II. SOLID | `IEmailProvider` segregada; selección por `IEmailProviderFactory` (OCP: añadir proveedor sin tocar consumidores); DI por constructor. | PASS |
| III. SDD (español) | Spec + clarifs + estos artefactos en español; GIVEN/WHEN/THEN ya definidos. | PASS |
| IV. Test-First (>85%) | Plan define tests unit/integración/E2E antes de implementar (ver quickstart y tasks). | PASS (a verificar en implementación) |
| V. Frontend Calidad Producción | TS strict, Biome, React Doctor 100 honesto, dark mode, a11y (toasts accesibles ya existentes), responsive, presupuestos perf. | PASS |
| VI. Observable y Mantenible | Serilog estructurado en validación/prueba/herramientas (proveedor, conteos, duración, resultado); sin secretos en logs. | PASS |
| Stack tecnológico mandatado | Sin desviaciones; Resend vía `HttpClient` (sin SDK nuevo pesado). | PASS |
| Seguridad & Secrets | Credenciales solo por entorno; FluentValidation en inputs; Admin-only; nunca se loguea ni devuelve el secreto. | PASS |
| Performance & Escalabilidad | Endpoints stateless; herramientas masivas con consultas indexadas por outcome y paginación interna. | PASS |

**Sin violaciones**: la sección Complexity Tracking queda vacía.

## Project Structure

### Documentation (this feature)

```text
specs/017-configuracion-resend-tools/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 (decisiones de diseño)
├── data-model.md        # Phase 1 (entidades y reglas)
├── quickstart.md        # Phase 1 (guía de validación E2E)
├── contracts/           # Phase 1
│   ├── email-settings-api.md
│   ├── email-templates-api.md
│   ├── email-tools-api.md
│   └── ui-contracts.md
├── checklists/
│   └── requirements.md  # Creado en /speckit-specify
└── tasks.md             # Phase 2 (/speckit-tasks — NO lo crea este comando)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Entities/
│   │   └── SystemSettings.cs            # + EmailSettings, EmailTemplates, métodos Update/Reset
│   ├── Enums/
│   │   ├── EmailProvider.cs             # NUEVO: Smtp | Resend
│   │   └── NotificationType.cs          # (existente) tipos de plantilla
│   └── Email/
│       └── EmailTemplateVariables.cs    # NUEVO: catálogo canónico de variables admitidas
├── Application/
│   ├── Abstractions/
│   │   ├── IEmailService.cs             # (existente) contrato alto nivel
│   │   ├── IEmailProvider.cs            # NUEVO: SendAsync + ValidateAsync (bajo nivel)
│   │   ├── IEmailProviderFactory.cs     # NUEVO: resuelve proveedor activo
│   │   ├── IEmailCredentialStatus.cs    # NUEVO: estado de credencial (sin valor)
│   │   └── IEmailAdminService.cs        # NUEVO: reenvío masivo + saneamiento
│   ├── Email/
│   │   └── TemplateRenderer.cs          # NUEVO: sustitución + validación de variables
│   └── Validation/
│       ├── UpdateEmailSettingsValidator.cs   # NUEVO
│       ├── UpdateEmailTemplateValidator.cs    # NUEVO
│       └── SendTestEmailValidator.cs          # NUEVO
├── Infrastructure/
│   ├── Email/
│   │   ├── EmailOptions.cs              # + Resend (ApiKey por entorno), credenciales solo entorno
│   │   ├── SmtpEmailProvider.cs         # refactor de SmtpEmailService → IEmailProvider
│   │   ├── ResendEmailProvider.cs       # NUEVO: IEmailProvider vía HttpClient
│   │   ├── SettingsBackedEmailService.cs# NUEVO: IEmailService que lee settings por envío
│   │   ├── EmailProviderFactory.cs      # NUEVO
│   │   └── EmailTemplateProvider.cs     # refactor → lee plantillas de settings con fallback
│   ├── Repositories/
│   │   └── MongoInvoiceRepository.cs    # + GetByNotificationOutcomeAsync / consultas tools
│   └── Configuration/
│       └── DependencyInjection.cs       # registro de proveedores, factory, HttpClient, admin svc
└── Api/
    └── Endpoints/Settings/
        ├── GetEmailSettings.cs          # NUEVO
        ├── UpdateEmailSettings.cs       # NUEVO
        ├── ValidateEmailCredentials.cs  # NUEVO
        ├── GetEmailTemplates.cs         # NUEVO
        ├── UpdateEmailTemplate.cs       # NUEVO
        ├── ResetEmailTemplate.cs        # NUEVO
        ├── SendTestEmail.cs             # NUEVO
        ├── ResendFailedNotifications.cs # NUEVO (herramienta global)
        └── SanitizeStuckNotifications.cs# NUEVO (herramienta global)

frontend/src/features/settings/
├── components/
│   ├── SettingsPage.tsx                 # compone secciones (mantiene Apariencia)
│   ├── EmailProviderSection.tsx         # NUEVO
│   ├── EmailTemplatesSection.tsx        # NUEVO
│   ├── TestEmailSection.tsx             # NUEVO
│   └── AdminToolsSection.tsx            # NUEVO
├── api/
│   ├── getEmailSettings.ts / useEmailSettings.ts
│   ├── updateEmailSettings.ts / useUpdateEmailSettings.ts
│   ├── validateEmailCredentials.ts / useValidateEmailCredentials.ts
│   ├── emailTemplates.ts / useEmailTemplates.ts / useUpdateEmailTemplate.ts
│   ├── sendTestEmail.ts / useSendTestEmail.ts
│   └── emailTools.ts / useResendFailed.ts / useSanitizeStuck.ts
└── types.ts                             # NUEVO: tipos del dominio de configuración email
```

**Structure Decision**: Aplicación web (Opción 2). Se respeta la arquitectura por capas del backend (`Domain`/`Application`/`Infrastructure`/`Api`) y la organización por feature del frontend (`features/settings`). El worker de transiciones (que envía los correos) se ejecuta como hosted service **in-process con la API** (registrado en `DependencyInjection.AddInfrastructure`), por lo que `SettingsBackedEmailService` lee la configuración del sistema en cada envío y el cambio de proveedor aplica en runtime sin reinicio.

## Complexity Tracking

> Sin violaciones de la Constitución que justificar. Sección intencionalmente vacía.
