# Research — Email Service Interface (Spec 011)

Decisiones técnicas para definir el contrato `IEmailService`. Esta spec entrega solo la abstracción; la implementación concreta se difiere a la Spec 3.3.

## D1 — Ubicación del contrato (capa y namespace)

- **Decisión**: Definir `IEmailService` en la capa `Application`, namespace `Backend.Application.Abstractions`, archivo `backend/Application/Abstractions/IEmailService.cs`.
- **Rationale**: Existe el precedente directo `IDevDataSeeder` en `Backend.Application.Abstractions`. El envío de notificaciones es una capacidad de aplicación (orquesta dominio + infraestructura). Ubicarla en `Application` permite que el worker (Infrastructure) y futuros endpoints dependan de la abstracción, mientras el proveedor concreto vive en `Infrastructure` (Constitución I: "cambio de proveedor email no debe propagarse más allá de Infrastructure").
- **Alternativas consideradas**:
  - `Domain` (junto a `IInvoiceRepository`): rechazada — el envío de correo es una preocupación de aplicación/infraestructura, no una regla de dominio pura; mezclarla en Domain acoplaría el dominio a una capacidad de I/O externa.
  - `Infrastructure`: rechazada — colocar el contrato junto a su implementación invertiría la dirección de dependencias (los consumidores en Application/Api no deben depender de Infrastructure).

## D2 — Firma de los métodos y `CancellationToken`

- **Decisión**: 
  - `Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)`
  - `Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default)`
- **Rationale**: El roadmap (Spec 3.1) define `Task SendReminderAsync(string clientEmail, Invoice invoice)`. Se añade `CancellationToken cancellationToken = default` para alinear con la convención uniforme del proyecto (todos los contratos asíncronos lo incluyen: `IInvoiceRepository`, `IDevDataSeeder`, `ISystemSettingsRepository`). El parámetro opcional preserva la firma del roadmap para invocaciones simples y habilita cancelación cooperativa cuando el worker se apaga (`stoppingToken`).
- **Alternativas consideradas**: Omitir `CancellationToken` para coincidir literalmente con el roadmap — rechazada por inconsistencia con el resto de la base de código y por perder el apagado cooperativo del worker.

## D3 — Tipo de retorno

- **Decisión**: Retornar `Task` (no `Task<bool>` ni `Task<resultado>`).
- **Rationale**: La Spec 3.1 solo define el envío. El roadmap (Spec 3.3) indica registrar éxito/error en BD como parte de la implementación de envío; modelar el resultado detallado pertenece a esa spec. Para el contrato base, `Task` es suficiente: el éxito se representa por completar sin excepción y el fallo por excepción (ver D4).
- **Alternativas consideradas**: `Task<EmailResult>` con estado detallado — rechazada por ahora (YAGNI / fuera del alcance de 3.1); puede extenderse en 3.3 sin romper consumidores si se introduce una sobrecarga o un contrato de resultado separado.

## D4 — Manejo de errores en el contrato

- **Decisión**: El contrato no define un tipo de resultado de error; las implementaciones señalan fallos mediante excepciones. El contrato documenta (XML-doc) que `clientEmail` no debe ser nulo/vacío y que un fallo del proveedor se propaga como excepción.
- **Rationale**: Mantiene el contrato agnóstico del proveedor (RF-006). La validación de formato de correo y el manejo de fallos de red/proveedor son responsabilidad de la implementación (Spec 3.3) y del invocador.
- **Alternativas consideradas**: Patrón Result/Either — válido pero excede el alcance de 3.1; se documenta como posible evolución en 3.3.

## D5 — Estrategia de pruebas (Test-First sobre una interfaz)

- **Decisión**: Escribir una prueba de contrato en `Monolegal.Application.Tests/Email/EmailServiceContractTests.cs` que: (a) defina un `FakeEmailService : IEmailService` que registre las invocaciones, (b) lo use como `IEmailService` (sustituibilidad — Liskov/DIP, CE-001/CE-002), y (c) verifique que ambas operaciones se invocan de forma asíncrona y reciben el `clientEmail` y la `Invoice` esperados.
- **Rationale**: Una interfaz no tiene lógica ejecutable propia, pero el criterio de éxito clave (CE-002: sustituible por un fake en pruebas) es verificable y se escribe antes de declarar la interfaz, respetando Test-First. La cobertura de lógica real corresponderá a la implementación de la Spec 3.3.
- **Alternativas consideradas**: No escribir pruebas en esta spec — rechazada por la Constitución IV (Test-First NO NEGOCIABLE) y porque la sustituibilidad es un criterio de éxito explícito.

## D6 — Registro en el contenedor de DI

- **Decisión**: **No** registrar `IEmailService` en DI en esta spec.
- **Rationale**: No existe implementación concreta que registrar hasta la Spec 3.3. Registrar la abstracción sin implementación causaría fallos de resolución en tiempo de ejecución para cualquier consumidor. El wiring (`services.AddSingleton<IEmailService, ...>()`) se hará en la Spec 3.3 junto a la implementación.
- **Alternativas consideradas**: Registrar un no-op temporal — rechazada por introducir comportamiento silencioso engañoso (correos "enviados" que no se envían) fuera de un contexto de prueba.
