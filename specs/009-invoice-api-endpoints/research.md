# Investigación — Endpoints API de Facturas (Phase 0)

Resolución de decisiones técnicas previas al diseño. No quedaron marcadores `NEEDS CLARIFICATION` en el contexto técnico; las decisiones siguientes consolidan el enfoque sobre el código existente.

## D1 — Representación de `InvoiceStatus` en el contrato HTTP

**Decision**: Serializar/deserializar `InvoiceStatus` como cadena en **minúscula sin separadores** mediante un `JsonStringEnumConverter` con política de nomenclatura a minúsculas, registrado globalmente vía `ConfigureHttpJsonOptions`. El parseo del query param `status` (cadena suelta, no JSON) se hace con un mapeo case-insensitive centralizado en `InvoiceStatusApi` (Api).

**Rationale**: Los nombres del enum coinciden exactamente con las cadenas esperadas al pasarlos a minúscula: `Pending→pending`, `PrimerRecordatorio→primerrecordatorio`, `SegundoRecordatorio→segundorecordatorio`, `Desactivado→desactivado`, `Pagado→pagado`. No requiere atributos por miembro ni tablas duplicadas. Un único punto de verdad para entrada (query/body) y salida (respuestas).

**Impacto en código existente**: `PayInvoice` actualmente devuelve `status` como número (no hay converter configurado). Al registrar el converter global, su salida pasará a cadena en minúscula — cambio deseable y consistente con el nuevo contrato; se debe verificar/ajustar su test si afirma sobre el valor numérico.

**Alternatives considered**:
- Atributo `[EnumMember]`/`JsonConverter` por miembro: más verboso, redundante dado que la regla de minúsculas es uniforme.
- DTO con `string Status` mapeado manualmente endpoint por endpoint: duplica lógica y rompe DRY.

## D2 — Listado paginado con conteo total

**Decision**: Añadir a `IInvoiceRepository`:
`Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(InvoiceStatus? status, int page, int pageSize, CancellationToken ct)`.
La implementación Mongo aplica filtro opcional por `Status`, ordena por `CreatedAt` descendente, usa `Skip((page-1)*pageSize).Limit(pageSize)` para los ítems y `CountDocumentsAsync(filtro)` para `Total`.

**Rationale**: Un solo método devuelve los datos de la página y el total de coincidencias (FR-001/FR-004), evitando dos llamadas descoordinadas desde el endpoint y manteniendo el cálculo del total alineado con el filtro. El orden `CreatedAt` descendente satisface FR-005a (Q3) y se apoya en el índice existente.

**Alternatives considered**:
- Dos métodos separados (`GetPagedAsync` + `CountAsync(status)`): aceptable, pero acopla al endpoint la coordinación; se descarta por cohesión.
- Paginación en memoria sobre `GetByStatusAsync`: viola "sin queries sin límite" (Constitución, Performance).

## D3 — Estadísticas agregadas (`byStatus`, `byClient`)

**Decision**: Añadir métodos de agregación al repositorio:
`Task<IReadOnlyDictionary<InvoiceStatus,long>> CountByStatusAsync(ct)` y
`Task<IReadOnlyDictionary<string,long>> CountByClientAsync(ct)`,
implementados con `$group` del pipeline de agregación de MongoDB. El total reutiliza `CountAsync()` ya existente. El endpoint compone el DTO de estadísticas.

**Rationale**: La agregación se resuelve en la base de datos (eficiente, respeta presupuesto de 200 ms) y permanece encapsulada en Infrastructure (Principio I). `CountByClientAsync` agrupa por `ClientId` (Q de agrupación, ya documentada en Assumptions). La invariante "suma de `byStatus` == `totalInvoices`" (FR-016) se garantiza porque ambas cuentan la misma colección sin filtro.

**Alternatives considered**:
- Traer todas las facturas y agrupar en memoria: simple pero escala mal y viola el principio de paginación/límite.
- Mantener contadores materializados: complejidad e invalidación innecesarias para el volumen esperado.

## D4 — Transición manual de estado

**Decision**: Extender `InvoiceTransitionService` con `void ApplyManualTransition(Invoice invoice, InvoiceStatus newStatus)`, que valida la transición solicitada contra la matriz de transiciones permitidas de la spec 006 y aplica `invoice.UpdateStatus(...)`; si `newStatus == Pagado` delega en `ApplyPayment`. Toda transición no permitida lanza `InvalidOperationException`.

Matriz de transiciones permitidas (spec 006):

| Estado actual | Destinos permitidos |
|---------------|---------------------|
| `Pending` | `PrimerRecordatorio`, `Pagado` |
| `PrimerRecordatorio` | `SegundoRecordatorio`, `Pagado` |
| `SegundoRecordatorio` | `Desactivado`, `Pagado` |
| `Desactivado` | `Pagado` |
| `Pagado` | (ninguno) |

**Rationale**: Centraliza las reglas de transición en el dominio (Principio I/II), reutiliza la lógica de pago existente y permite que el endpoint traduzca el resultado: éxito → `200`, `InvalidOperationException` → `400`. Consistente con `PayInvoice`, que ya captura `InvalidOperationException`.

**Mapeo de códigos HTTP en el endpoint de transición**:
- Factura inexistente o id con formato inválido → `404` (Q4).
- `newStatus` ausente o no perteneciente al conjunto de estados válidos → `400` (FR-017/FR-018, validación FluentValidation).
- Transición no permitida por la matriz → `400` (FR-013).
- Éxito → `200` con la factura actualizada.

**Alternatives considered**:
- Reutilizar `TryApplyTransition` (basado en tiempo): no aplica; la transición manual no depende de días transcurridos.
- Poner las reglas en el endpoint: viola Arquitectura Limpia y SOLID.

## D5 — Validación de entradas (FluentValidation)

**Decision**: Validadores en `Application/Validation`: `ListInvoicesQueryValidator` (`page ≥ 1`, `1 ≤ pageSize ≤ 50`, `status` ∈ estados válidos cuando esté presente) y `TransitionInvoiceRequestValidator` (`newStatus` requerido y ∈ estados válidos). El endpoint ejecuta la validación y devuelve `400` con detalle ante fallo.

**Rationale**: FluentValidation es el estándar mandado por la Constitución y ya está referenciado en `Application.csproj`. Implementa FR-006 (defaults solo en ausencia; valor presente inválido → 400), FR-003a (tope 50), FR-017 y FR-018.

**Alternatives considered**:
- Validación manual inline: dispersa reglas y dificulta tests; se descarta.

## D6 — Estrategia de pruebas

**Decision**: Tests de aplicación en `Tests/Monolegal.Application.Tests/Endpoints` con un `InMemoryInvoiceRepository` (extendido con los nuevos métodos) replicando el handler de cada endpoint, siguiendo el patrón ya establecido en `PayInvoiceTests`. Casos: filtro por estado, paginación (incluye página fuera de rango → vacío + total real), orden por `CreatedAt` desc, defaults vs inválidos, tope `pageSize`, detalle 200/404, transición válida/ inválida/ inexistente, e invariante de estadísticas (`Σ byStatus == total`).

**Rationale**: Mantiene velocidad y aislamiento (sin MongoDB), cumple Test-First y ≥85% cobertura. La validación con base real puede cubrirse con `MongoIntegrationFixture` para los métodos de repositorio nuevos (paginación/agregación) si se desea verificación de contrato Mongo.

**Alternatives considered**:
- Solo tests E2E vía HTTP (`WebApplicationFactory`): valiosos pero más lentos; se priorizan tests de aplicación para el grueso de la cobertura, dejando E2E para jornadas críticas (Constitución IV).
