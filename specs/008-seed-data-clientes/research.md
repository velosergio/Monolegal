# Research: Seed Data - 3 Clientes Mínimo

**Feature**: `008-seed-data-clientes` | **Fecha**: 2026-06-25

Este documento resuelve las incógnitas técnicas del plan. No quedaron marcadores `NEEDS CLARIFICATION` en el Technical Context; los puntos abajo documentan decisiones de diseño y sus alternativas.

## D1 — Representación de "Cliente" (no existe entidad Client)

- **Decisión**: Representar los 3 clientes como tres valores de `ClientId` distintos y estables (constantes en `SeedDataDefinition`). No se crea una entidad ni colección `Cliente`.
- **Justificación**: El modelo de dominio actual (`Invoice`) referencia al cliente sólo por `ClientId` (string); no existe `Cliente.cs` ni colección `Clients`. Introducir una entidad nueva excedería el alcance de la spec (que pide datos sembrados, no un nuevo agregado) y violaría "sin spec creep".
- **Alternativas consideradas**:
  - *Crear entidad/colección Cliente*: rechazada — fuera de alcance, no requerida por la spec ni por specs previas (005/006/007).
  - *IDs aleatorios por ejecución*: rechazada — rompe la reproducibilidad y la idempotencia; se usan IDs estables.

## D2 — Ubicación de la lógica de siembra (capa)

- **Decisión**: Orquestación en **Application** (`DevDataSeeder` + `SeedDataDefinition`); persistencia y verificación de vacuidad en **Infrastructure** (`MongoInvoiceRepository`); disparo en **Api/Infrastructure** vía `IHostedService`.
- **Justificación**: Cumple Arquitectura Limpia (Principio I): la definición del dataset es lógica de aplicación reutilizable y testeable sin Mongo; la persistencia queda aislada tras `IInvoiceRepository`. Alineado con el patrón existente (`InvoiceTransitionsWorker` como hosted service, repos en Infrastructure).
- **Alternativas consideradas**:
  - *Todo en Infrastructure*: rechazada — mezclaría reglas de negocio (qué sembrar) con detalle de persistencia.
  - *Script externo (mongosh)*: rechazada — duplicaría reglas de dominio fuera del código y no respetaría invariantes de la entidad `Invoice`.

## D3 — Verificación de "base vacía" e idempotencia

- **Decisión**: Añadir `Task<long> CountAsync(CancellationToken)` a `IInvoiceRepository` y su implementación Mongo (`CountDocumentsAsync`). El seeder siembra sólo si `CountAsync == 0`; en caso contrario omite y registra el motivo.
- **Justificación**: La idempotencia (RF-009) y la condición de vacuidad (RF-008) requieren una verificación previa barata. `CountDocumentsAsync` sobre colección vacía es O(1) lógico y se apoya en metadatos. Es un método cohesivo y reutilizable.
- **Alternativas consideradas**:
  - *`AnyAsync`/`Find().Limit(1)`*: válida, pero `CountAsync` es más expresivo para el logging de conteos y reutilizable en tests/aserciones.
  - *Upsert por Id determinista*: rechazada — añade complejidad y no cubre el requisito explícito de "sólo si está vacía".

## D4 — Construcción de facturas en estados específicos con auditoría coherente

- **Decisión**: Construir cada `Invoice` con `new Invoice(clientId, amount)` y llevarla al estado objetivo con `UpdateStatus(...)`; para estados con recordatorios (`primerrecordatorio`, `segundorecordatorio`) invocar `RecordReminderSent()` la cantidad coherente de veces (1 y 2 respectivamente). `pending` queda con `RemindersCount = 0`.
- **Justificación**: Respeta los invariantes y la API pública de la entidad (Principio I/II); no requiere reflexión ni acceso interno. Las fechas (`LastStatusTransitionAt`, `LastReminderSentAt`) quedan en "ahora", aceptable para datos de desarrollo (RF-007 pide coherencia, no antigüedad simulada).
- **Alternativas consideradas**:
  - *Insertar BSON crudo con fechas retrodatadas*: rechazada — evade invariantes de dominio y acopla el seeder al esquema de persistencia.
  - *Exponer setters internos vía InternalsVisibleTo*: rechazada — el seeder no es ensamblado de test; ampliaría la superficie interna sin necesidad.

## D5 — Disparo del seeder y gate de entorno

- **Decisión**: Registrar un `DevDataSeederHostedService` que invoca al seeder en `StartAsync`, **registrado en DI sólo cuando `builder.Environment.IsDevelopment()`** en `Program.cs`. En producción ni siquiera se registra.
- **Justificación**: Seguridad (no sembrar en producción) y simplicidad; sigue el patrón de hosted services ya usado (`MongoConnectionVerifier`, `InvoiceTransitionsWorker`). El gate en el registro (no sólo en runtime) elimina el riesgo de ejecución accidental.
- **Alternativas consideradas**:
  - *Endpoint manual `POST /dev/seed`*: opcional y complementario; documentado en el contrato como extensión, pero no requerido. El disparo al arranque cubre el escenario GIVEN/WHEN/THEN.
  - *Flag de configuración `Seed:Enabled`*: rechazada como mecanismo único — el gate por entorno es más seguro; puede añadirse como refuerzo.

## D6 — Estrategia de pruebas

- **Decisión**: 
  - *Unit (Application.Tests)*: `DevDataSeeder` contra un `IInvoiceRepository` fake/sustituto — verifica distribución 3/2/3, estados variados de Cliente A, cobertura de `primerrecordatorio`/`segundorecordatorio`, y que con `CountAsync > 0` no siembra (idempotencia).
  - *Integración (Infrastructure)*: ejecutar el seeder real contra `MongoIntegrationFixture` (base efímera); aserciones por conteo y distribución; segunda ejecución → conteos idénticos.
- **Justificación**: Cumple Principio IV (test-first, integración cross-layer) y reutiliza el fixture existente (`MongoIntegrationFixture`, `[Trait("Category","Integration")]`).
- **Alternativas consideradas**:
  - *Sólo integración*: rechazada — los tests unitarios validan la lógica de distribución sin depender de Mongo y son más rápidos/estables.

## Resumen de cambios de superficie

| Componente | Cambio |
|------------|--------|
| `IInvoiceRepository` | + `CountAsync(CancellationToken)` |
| `MongoInvoiceRepository` | Implementa `CountAsync` (`CountDocumentsAsync`) |
| `Application/Abstractions/IDevDataSeeder` | Nuevo contrato |
| `Application/Seeding/DevDataSeeder` + `SeedDataDefinition` | Nueva orquestación + dataset fijo |
| `Infrastructure/Hosting/DevDataSeederHostedService` | Nuevo disparador dev-only |
| `Api/Program.cs` + `DependencyInjection` | Registro condicional (Development) |
