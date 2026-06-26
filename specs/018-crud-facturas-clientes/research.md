# Phase 0 — Research: CRUD de Facturas y Clientes

Decisiones técnicas que resuelven los puntos abiertos del Technical Context. No quedan `NEEDS CLARIFICATION`: las ambigüedades funcionales se cerraron en la sesión de `/speckit-clarify` (ver `spec.md` → Clarifications).

---

## D1 — Monto derivado de los items (no capturado)

- **Decisión**: `Invoice.Amount` se recalcula en el dominio como `sum(item.Subtotal)` donde `Subtotal = Quantity × UnitPrice`. El setter de `Amount` se vuelve privado/derivado; ningún endpoint acepta `amount` en el cuerpo. El frontend muestra el total de solo lectura.
- **Rationale**: Una sola fuente de verdad (los items) elimina la posibilidad de divergencia entre total y líneas (clarificación Q1=A). Coherente con SOLID (la factura es responsable de su propio total).
- **Alternativas consideradas**: (B) capturar monto y validar igualdad exacta — rechazada por redundancia y riesgo de error de redondeo; (C) items informativos — rechazada porque permite inconsistencia.

## D2 — Estructura de los items: descripción + cantidad + precio unitario

- **Decisión**: `InvoiceItem` como value object inmutable embebido en el documento `Invoice` (lista). Campos: `Description` (string, requerido), `Quantity` (decimal/int > 0), `UnitPrice` (decimal > 0). `Subtotal` es propiedad calculada. Se embebe (no colección aparte) siguiendo el patrón ya usado con `StatusHistory` (lista embebida en `Invoice`).
- **Rationale**: Los items no tienen ciclo de vida propio ni se consultan fuera de su factura → documento embebido (clarificación Q5=B). Reutiliza el patrón de serialización existente para listas embebidas.
- **Alternativas consideradas**: colección `InvoiceItems` separada — rechazada por sobre-normalización sin caso de consulta independiente.

## D3 — Fecha de vencimiento (`DueDate`)

- **Decisión**: `Invoice.DueDate` como `DateTime` (UTC). Requerido en creación/edición (validación). Para facturas legacy (sin el campo) se hace backfill (ver D6). Se almacena como fecha absoluta; la validación exige que sea una fecha válida (no se impone que sea futura, para permitir registrar facturas ya vencidas).
- **Rationale**: El vencimiento es un dato de negocio relevante para recordatorios; mantenerlo no nulo simplifica el worker existente. Permitir fechas pasadas evita bloquear la carga de cartera histórica.
- **Alternativas consideradas**: `DateTime?` nullable permanente — rechazada porque deja el campo sin garantía para nuevas facturas; mejor nullable solo en almacenamiento legacy + requerido por validación.

## D4 — Entidad `Client` y relación con `Invoice.ClientId`

- **Decisión**: Nueva entidad de dominio `Client` (`Id`, `Name`, `Email`, `Phone?`, `Address?`, `CreatedAt`, `UpdatedAt`) en colección `Clients`. `Invoice.ClientId` sigue siendo el `string` que referencia `Client.Id` (sin cambio de tipo ni de relación, sin `$lookup`/joins). La creación/edición de factura valida que el `ClientId` exista en `Clients`.
- **Rationale**: Mantener `ClientId` como referencia simple evita romper el modelo existente (worker, stats, seeder, índices `ClientId_asc`). MongoDB no fuerza FKs; la integridad referencial se valida en la capa de aplicación (endpoint) y se protege en el borrado de cliente (D7).
- **Alternativas consideradas**: embeber el cliente en la factura — rechazada por duplicación y dificultad de editar datos del cliente; cambiar `ClientId` a `ObjectId` — innecesario, rompería documentos existentes.

## D5 — Unicidad del email del cliente

- **Decisión**: Índice **único** en `Clients.Email` (case-insensitive vía collation `{ locale: 'en', strength: 2 }` o normalización a minúsculas antes de persistir). La validación de aplicación (FluentValidation) comprueba unicidad consultando el repositorio antes de insertar/actualizar y devuelve `ValidationProblem` (400) con mensaje claro; el índice único es la red de seguridad ante condiciones de carrera (traducción de `DuplicateKey` → 409/400).
- **Rationale**: Clarificación Q2=A. Doble defensa (validación + índice) sigue el patrón de robustez de la constitución. El email se usa además para resolver destinatarios de notificación (ver D8).
- **Alternativas consideradas**: solo validación de aplicación — rechazada por vulnerable a carreras; solo índice único — rechazada porque el mensaje de error sería poco amigable.

## D6 — Migración/backfill de facturas existentes

- **Decisión**: `HostedService` idempotente `InvoiceItemsBackfillMigration`, siguiendo el patrón de `StatusHistoryBackfillMigration` ya registrado. Para cada factura sin items: sintetiza una línea única `{ Description: "Concepto", Quantity: 1, UnitPrice: Amount }` (preservando el `Amount` actual) y asigna `DueDate = CreatedAt + 30 días` si falta. Se ejecuta al arranque y es seguro reejecutar (solo toca documentos sin items/dueDate).
- **Rationale**: Garantiza que las facturas previas cumplan el nuevo invariante (monto = suma de items) sin pérdida de datos, reutilizando el mecanismo de migración existente y observable.
- **Alternativas consideradas**: deserialización tolerante sin migración — rechazada porque deja documentos en estado inválido respecto al invariante; migración manual — rechazada por no ser reproducible.

## D7 — Borrado de factura (hard delete) y de cliente (con guard)

- **Decisión**: `IInvoiceRepository.DeleteAsync(id)` (hard delete vía `DeleteOneAsync`), permitido en cualquier estado (clarificación Q4=A). El borrado de cliente comprueba primero si existen facturas asociadas mediante `IInvoiceRepository.GetByClientIdAsync(clientId)` (o un nuevo `CountByClientIdAsync`); si hay ≥1, el endpoint devuelve `409 Conflict`/`400` con mensaje explicativo y NO elimina (RF-018).
- **Rationale**: Hard delete confirmado para facturas (RF-010). El guard de cliente protege integridad referencial que Mongo no fuerza.
- **Alternativas consideradas**: soft delete — descartado por el usuario; cascada al borrar cliente — rechazada por riesgo de pérdida masiva no solicitada.

## D8 — Resolución de email del cliente (`IClientEmailResolver`)

- **Decisión**: Añadir `ClientRepositoryEmailResolver` respaldado por la colección `Clients` (resuelve `ClientId → Email`), con *fallback* al `ConfiguredClientEmailResolver` actual (config `ClientEmails`) para no romper entornos sin clientes migrados. Se registra como implementación principal en DI.
- **Rationale**: Una vez que el cliente tiene email persistido, la resolución debe leer de la fuente de verdad. El fallback preserva compatibilidad con la spec 013 sin regresiones.
- **Alternativas consideradas**: reemplazar por completo el resolver de config — rechazado para no romper la spec 013 en ausencia de datos; dejarlo intacto — rechazado porque ignoraría el email recién gestionado.

## D9 — Listado de clientes: paginación y búsqueda server-side

- **Decisión**: `IClientRepository.GetPagedAsync(search, page, pageSize)` con búsqueda case-insensitive "contains" sobre `Name` y `Email` (regex escapada, patrón idéntico a `MongoInvoiceRepository.GetPagedAsync`), orden por `Name` ascendente, devolviendo `(Items, Total)`. El endpoint `GET /api/clients` reutiliza el `PagedResponse<T>` existente y un `ListClientsQueryValidator` (page/pageSize/limites) espejo de `ListInvoicesQueryValidator`.
- **Rationale**: La constitución exige paginación forzada y queries acotadas; replicar el patrón ya probado de facturas reduce riesgo y mantiene consistencia.
- **Alternativas consideradas**: filtrado en cliente — rechazado por no escalar y violar "sin queries sin límite".

## D10 — Patrones de frontend (TanStack Query + toasts)

- **Decisión**: Mutaciones `useCreateInvoice`/`useUpdateInvoice`/`useDeleteInvoice` y equivalentes de cliente, cada una con `onSuccess` que invalida de forma dirigida las claves afectadas: facturas → `['invoices']` + `['invoice-stats']` (+ detalle); clientes → `['clients']`. Toasts de éxito/error vía `ToastProvider` existente. Formularios en modal con validación espejo (zod o validación manual coherente con backend). El total de la factura se muestra de solo lectura y se recalcula en vivo desde los items. Estados de error preservan el contenido del formulario (RF-009).
- **Rationale**: Reutiliza exactamente el patrón de `useTransitionInvoice` (invalidación dirigida, research D5 de spec 015) garantizando el refresco automático de tabla y dashboard (RF-008) sin recargar.
- **Alternativas consideradas**: refetch global — rechazado por sobre-fetching; optimistic updates — opcional, no requerido por los criterios de éxito (se deja como mejora futura).

## D11 — Seeder de desarrollo coherente con `Clients`

- **Decisión**: Extender el seeder (spec 008) para crear también los 3 documentos `Client` (A/B/C) con los `Id` estables existentes (`seed-cliente-a/b/c`) y emails coherentes con la sección `ClientEmails`, además de poblar items/dueDate en las facturas sembradas. Mantiene la idempotencia (solo siembra con base vacía).
- **Rationale**: La creación de factura valida que el cliente exista (caso límite de la spec); sin documentos `Client`, el entorno de desarrollo quedaría inconsistente. Conserva la fuente de verdad de los tests de distribución del seeder.
- **Alternativas consideradas**: no tocar el seeder — rechazada porque dejaría facturas sembradas sin items y sin cliente real, rompiendo las nuevas validaciones.
