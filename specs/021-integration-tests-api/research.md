# Research: Tests de Integración de la API

**Feature**: 021-integration-tests-api | **Fecha**: 2026-06-29

Decisiones técnicas que resuelven el Technical Context del plan. No quedan marcadores `NEEDS CLARIFICATION`.

## D1. Mecanismo de arranque del host de pruebas

- **Decisión**: Usar `WebApplicationFactory<Program>` (paquete `Microsoft.AspNetCore.Mvc.Testing`, ya referenciado en `Tests.csproj`) con una subclase interna por clase de prueba que sobreescribe `ConfigureWebHost`. Se obtiene un `HttpClient` vía `factory.CreateClient()` y se ejercitan los endpoints con peticiones HTTP reales sobre el `TestServer` en memoria.
- **Rationale**: Es el patrón oficial de ASP.NET Core para integration testing y ya está probado en el repositorio (`InvoiceCrudEndpointsTests`, `ResendInvoiceEndpointTests`, `ListShipmentsEndpointTests`, tests de email). `Program.cs` ya expone `public partial class Program { }` (línea 158), requisito del genérico. Reutilizar el patrón minimiza riesgo y mantiene consistencia.
- **Alternativas consideradas**:
  - *Reimplementar el flujo del handler en el test* (estilo actual de `TransitionInvoiceTests`): rechazada — no cubre enrutamiento/binding/serialización ni la traducción real a códigos HTTP; es justamente la brecha que esta feature cierra.
  - *Levantar Kestrel en un puerto real*: rechazada — más lento y frágil, innecesario cuando `TestServer` ofrece el pipeline completo en memoria.

## D2. Sustitución de dependencias de infraestructura

- **Decisión**: En `ConfigureWebHost`, fijar `UseEnvironment("Development")`, definir `MONGODB_URI` con un valor dummy y, en `ConfigureServices`, `RemoveAll<IHostedService>()` (worker de transiciones) y reemplazar `IInvoiceRepository`, `IClientRepository`, `ISystemSettingsRepository`, `IClientEmailResolver` y `IInvoiceTransitionNotifier` por los dobles en memoria de `Support/`.
- **Rationale**: Aísla la suite de MongoDB y de servicios en segundo plano, garantizando determinismo y velocidad. El valor dummy de `MONGODB_URI` evita que el registro de Mongo falle al construir el host, mientras los repositorios reales nunca se usan (quedan sustituidos). Es exactamente lo que hace `InvoiceCrudEndpointsTests`.
- **Alternativas consideradas**:
  - *Usar `MongoIntegrationFixture` (Mongo real)*: rechazada para esta suite — añade dependencia de servicio externo y latencia sin valor adicional para verificar el **contrato HTTP**; la integración con Mongo ya está cubierta por los tests de repositorio (spec 007).
  - *No remover el notificador*: rechazada — el notificador real intentaría enviar email; se sustituye por `FakeTransitionNotifier` para que la transición sea observable sin efectos externos.

## D3. Aislamiento y determinismo entre pruebas

- **Decisión**: Cada test construye su propia instancia de `Factory` (con sus repositorios en memoria nuevos) dentro de un `using`, sembrando datos mediante helpers `SeedClient`/`SeedInvoice`. No se comparte estado entre tests.
- **Rationale**: Instancia por test ⇒ datos limpios y deterministas, sin dependencia del orden de ejecución (FR-012, FR-013, SC-004). El coste de arranque del host en memoria es bajo.
- **Alternativas consideradas**:
  - *`IClassFixture` con estado compartido*: rechazada — introduce acoplamiento entre casos y riesgo de contaminación; el aislamiento estricto es preferible para una suite de regresión de contrato.

## D4. Datos de prueba y construcción de facturas

- **Decisión**: Reutilizar `InvoiceTestFactory.Create(clientId, amount, status)` y `Invoice.Create(...)` para sembrar facturas en estados conocidos; usar `OverrideCreatedAt` cuando un test verifique orden por `createdAt`. Sembrar clientes con `Client(name, email)` para que la resolución de nombre del endpoint funcione.
- **Rationale**: Los helpers ya encapsulan la construcción válida de entidades de dominio; mantienen los tests legibles y centralizan cambios de contrato del dominio.
- **Alternativas consideradas**: *Construcción manual inline en cada test*: rechazada — duplica lógica y dificulta el mantenimiento.

## D5. Aserciones sobre la respuesta HTTP

- **Decisión**: Afirmar `response.StatusCode` con Shouldly y, para los cuerpos, parsear con `JsonDocument`/`ReadFromJsonAsync` verificando las propiedades del contrato (`data`, `total`, `pageSize` en el listado; `status`, `id`, `amount` en detalle; `totalInvoices`, `byStatus`, `byClient` en stats). Los estados se serializan como cadenas en minúscula (JsonStringEnumConverter global).
- **Rationale**: Verifica el contrato tal como lo ve un cliente externo (incluida la serialización), que es el objetivo de los integration tests. Coincide con el estilo de `InvoiceCrudEndpointsTests`.
- **Alternativas consideradas**: *Deserializar a los DTO internos del API*: aceptable pero acopla el test a tipos internos; se prefiere navegar el JSON por nombre de propiedad para validar el contrato público, salvo donde deserializar a un tipo simple mejore la legibilidad.

## D6. Validación de paginación/estado inválidos vía HTTP

- **Decisión**: Para `400` de paginación, enviar `GET /api/invoices?page=0` y `?pageSize=51`; para estado inválido, `?status=foo`. Verificar `400` (ValidationProblem). Para el `400` por `pageSize`/`page` no numérico, el binding de minimal API produce `400` automáticamente (parámetro `int?` con valor no parseable).
- **Rationale**: Ejercita la ruta real de validación (validator + binding), no sólo el validador aislado (que ya cubre `ListInvoicesTests` a nivel unitario). Cierra la brecha de integración.
- **Alternativas consideradas**: *Sólo probar el validador*: rechazada — ya existe a nivel unitario; el valor nuevo está en verificar la traducción a `400` por HTTP.

## D7. Identificador inexistente vs. formato inválido (404 uniforme)

- **Decisión**: Probar tanto un id inexistente bien formado (`"no-existe"`) como uno con formato arbitrario, esperando `404` en detalle y transición. El doble en memoria devuelve `null` para ids ausentes, replicando el comportamiento "no encontrado" uniforme del endpoint.
- **Rationale**: El contrato (spec 009, Q4) trata ambos casos como `404`; el endpoint no distingue formato. La suite confirma el `404` sin error no controlado (FR-006/FR-007 de spec 021).
- **Alternativas consideradas**: *Distinguir 400 por formato*: rechazada — contradice el contrato acordado en spec 009.

## Resumen de dependencias y patrones

- Patrón de integración: `WebApplicationFactory<Program>` + dobles en memoria de `Support/` (sin Mongo, sin hosted services).
- Sin nuevas dependencias NuGet (todo ya presente en `Tests.csproj`).
- Sin cambios en código de producción.
