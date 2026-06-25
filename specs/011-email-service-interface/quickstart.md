# Quickstart — Validación de `IEmailService` (Spec 011)

Guía para validar que el **contrato** `IEmailService` queda correctamente definido y es sustituible. Esta spec no incluye implementación concreta (ver Spec 3.3), por lo que la validación se centra en la forma del contrato y su sustituibilidad en pruebas.

## Prerrequisitos

- SDK .NET 10 (`net10.0`) instalado.
- Solución backend restaurada: `dotnet restore` en `backend/`.
- Entidad `Invoice` disponible (spec 005, ya presente en `backend/Domain/Entities/Invoice.cs`).

## Artefactos esperados tras la implementación

- `backend/Application/Abstractions/IEmailService.cs` — la interfaz con las dos operaciones (ver `contracts/IEmailService.md`).
- `backend/Tests/Monolegal.Application.Tests/Email/EmailServiceContractTests.cs` — prueba de contrato con un fake substituible.

## Escenario de validación 1 — Sustituibilidad del contrato (CE-002)

**Objetivo**: Demostrar que `IEmailService` puede ser sustituido por una implementación falsa e invocado de forma asíncrona recibiendo los datos esperados.

Esquema de la prueba (referencia, los detalles van en `tasks.md`/implementación):

1. Definir un `FakeEmailService : IEmailService` que registre cada invocación (correo + factura + tipo de operación) y devuelva `Task.CompletedTask`.
2. Asignarlo a una variable de tipo `IEmailService` (verifica sustituibilidad — Liskov/DIP).
3. Crear una `Invoice` de prueba (`new Invoice(clientId, amount)`).
4. Invocar `await emailService.SendReminderAsync("cliente@correo.com", invoice)` y `await emailService.SendPaymentConfirmationAsync(...)`.

**Resultado esperado**:
- Ambas llamadas completan sin excepción.
- El fake registra el `clientEmail` y la `Invoice` exactos que se pasaron.
- Se confirma que ambas operaciones devuelven `Task` (son `await`-ables).

## Comandos de validación

Compilar el contrato y ejecutar las pruebas de la capa Application:

```bash
cd backend
dotnet build
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~EmailServiceContract"
```

**Salida esperada**: compilación exitosa y la(s) prueba(s) de contrato en verde.

## Criterios de aceptación de la validación

- [ ] `IEmailService` existe en `Backend.Application.Abstractions` con las dos operaciones asíncronas (RF-001, RF-002, RF-003).
- [ ] Ambas operaciones devuelven `Task` y aceptan `clientEmail` + `Invoice` (RF-004).
- [ ] El contrato no referencia ningún proveedor de correo concreto (RF-006, C-002).
- [ ] Una implementación falsa puede sustituir al contrato y ser invocada en una prueba (CE-002, C-004).

## Fuera de alcance de este quickstart

Envío real de correos, plantillas, configuración del proveedor, registro en DI y persistencia del resultado — todo ello se valida en la Spec 3.3.
