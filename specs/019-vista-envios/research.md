# Research — Vista de Envíos (spec 019)

Fase 0. Resuelve las decisiones técnicas derivadas del spec (6 clarificaciones ya cerradas) y del estado real del código. No quedan marcadores NEEDS CLARIFICATION.

## D1 — Modelo del estado de envío y del contador de reintentos

**Decisión**: El estado de envío de la vista se **deriva** del estado de notificación ya embebido en `Invoice` (`LastNotificationOutcome`, `LastNotificationAt`, `LastNotificationError`). Se añade un único campo persistido nuevo: `NotificationRetryCount` (int). Mapeo a la UI:

| `LastNotificationOutcome` | Estado UI |
|---------------------------|-----------|
| `None` | pendiente |
| `Sent` | enviado |
| `Failed` | fallido |
| `Skipped` | omitido |

El estado **"reintentando" no se persiste**: es transitorio en el cliente mientras una mutación de reenvío/reintento está en curso (clarificación: el worker no reintenta automáticamente).

**Rationale**: Minimiza el cambio de dominio y reutiliza la maquinaria de notificación existente (specs 013/017). Coincide con la clarificación Q1 (contador persistido) y con que "reintentando" es transitorio.

**Alternativas consideradas**: (a) Colección/entidad de "envíos" separada con historial completo — rechazada por sobre-ingeniería; el dominio ya guarda el último resultado y el roadmap no pide historial por intento. (b) Estado "reintentando" persistido con cola — rechazado: no hay reintento automático (clarificación Q1=A).

## D2 — Semántica del contador `NotificationRetryCount`

**Decisión**:
- Se **reinicia a 0** cuando la factura entra en un **nuevo estado notificable** (dentro de `Invoice.UpdateStatus`, cuando el estado destino es notificable). Cuenta los reintentos del **aviso vigente**.
- El **primer** intento de notificación del aviso (el que dispara el worker en la transición) **no incrementa** el contador (queda en 0).
- Cada **reintento posterior** (reenvío por factura `POST /resend` o reenvío masivo `resend-failed`) **incrementa** el contador en 1, con éxito o fallo.

**Implementación**: el incremento ocurre en la capa de reenvío (`InvoiceShipmentService.ResendAsync` y `EmailAdminService.ResendFailedAsync`), no dentro de `RecordNotificationResult` (que se sigue llamando también en la primera notificación del worker). Se añade `Invoice.RecordNotificationRetry()` que incrementa el contador, invocado por las rutas de reenvío justo antes/después de re-notificar.

**Rationale**: Mantiene `RecordNotificationResult` con una sola responsabilidad (registrar resultado) y deja el conteo de reintentos a las operaciones que sí son reintentos. Coincide con la clarificación Q3 (reset al nuevo estado notificable).

**Alternativas**: contar dentro de `RecordNotificationResult` con una bandera `isRetry` — rechazado por ensuciar la firma; contador acumulativo de por vida — rechazado por la clarificación Q3.

## D3 — Cancelar envío = marcar como omitido

**Decisión**: `POST /api/invoices/{id}/cancel-notification` marca como **omitida** (`NotificationOutcome.Skipped`) una factura cuyo estado de notificación sea **pendiente** (`LastNotificationOutcome == None`) y esté en un estado notificable. Usa `RecordNotificationResult(type, Skipped, now, "cancelado por el administrador")`, conserva el registro y persiste. Si la factura no está pendiente, devuelve 409 (no aplicable).

**Rationale**: Coincide con la clarificación Q2. El envío es síncrono (no hay cola que cancelar); marcar `Skipped` evita que el worker la procese, reutilizando el mismo significado de `Skipped` que ya tiene el dominio (sin plantilla / sin destinatario → omitido).

**Alternativas**: cancelar una petición HTTP en vuelo (sin efecto de dominio) — rechazado por la clarificación Q2; nuevo estado de dominio "Cancelado" — rechazado por no introducir un enum nuevo cuando `Skipped` ya expresa "no notificar".

## D4 — Reenvío por factura

**Decisión**: `POST /api/invoices/{id}/resend` reutiliza `IInvoiceTransitionNotifier.NotifyTransitionAsync(invoice, invoice.Status)` (mismo patrón que `EmailAdminService.ResendFailedAsync`), incrementa `NotificationRetryCount`, persiste con `UpdateAsync` y devuelve el ítem de envío actualizado. Es fail-soft: un fallo de envío se registra como `Failed` (no lanza 500). Devuelve 404 si la factura no existe; 409/200 con `outcome` si no es notificable (`Skipped`).

**Rationale**: Reutiliza exactamente la lógica de plantilla/resolución de correo/registro ya probada. Una nueva responsabilidad acotada en `InvoiceShipmentService` mantiene SOLID.

**Alternativas**: lógica de envío nueva en el endpoint — rechazada (duplicación, viola DRY/SRP).

## D5 — Listado de envíos: endpoint, filtro y búsqueda por correo

**Decisión**: Nuevo endpoint dedicado `GET /api/invoices/shipments?sendStatus=&search=&page=&pageSize=` que devuelve `PagedResponse<ShipmentListItemDto>`. Alcance (Q3): solo facturas en **estados notificables** (`PrimerRecordatorio`, `SegundoRecordatorio`, `Pagado`, `Desactivado`).

- **Filtro `sendStatus`**: pending/sent/failed/skipped → `NotificationOutcome` None/Sent/Failed/Skipped (ausente = todos).
- **Búsqueda `search`**: coincidencia parcial case-insensitive contra **nombre de cliente o correo de cliente**. Como el correo no se almacena en `Invoice` sino en `Client`, la búsqueda se resuelve en dos pasos: (1) `IClientRepository` filtra los `clientId` cuyo nombre o email contienen el término; (2) el repo de facturas filtra por esos `clientId` + estados notificables + `sendStatus`. Sin término, se omite el paso (1).
- **Email por fila**: se resuelve por los `clientId` distintos de la página (patrón anti N+1 ya usado en `ListInvoices`), reutilizando `IClientRepository`/resolución de correo.
- **Orden**: por `LastNotificationAt` desc con desempate por `CreatedAt` desc (las pendientes sin intento van por `CreatedAt`).
- **Paginación**: misma convención que `ListInvoices` (page≥1, pageSize≤50, defaults 1/10), validada con FluentValidation.

**Rationale**: Endpoint separado mantiene la responsabilidad de "envíos" aislada de `GET /api/invoices` (que sirve la tabla de facturas con otras columnas) y evita inflar el DTO de facturas. La búsqueda en dos pasos respeta la separación de colecciones de Mongo y mantiene la paginación correcta del lado servidor.

**Alternativas**: (a) extender `GET /api/invoices` con campos de notificación y filtro por outcome — rechazado: mezcla dos vistas con distinto contrato y semántica de filtro; (b) buscar email con `$lookup`/agregación entre colecciones — viable pero más complejo; se difiere salvo que el volumen lo exija (el filtrado por clientId cubre el caso con el índice existente).

## D6 — Reintentar fallidos (global)

**Decisión**: La acción "Reintentar fallidos" de la vista **reutiliza** el endpoint existente `POST /api/settings/email/tools/resend-failed` (spec 017) sin cambios de contrato. Solo se añade que `ResendFailedAsync` incremente `NotificationRetryCount` por cada factura reintentada (alineado con D2).

**Rationale**: Clarificación Q2 (global) y reuso máximo. El frontend de envíos consume el mismo cliente/mutación que ya existe en `features/settings/api/emailTools.ts`.

**Alternativas**: endpoint nuevo acotado al subconjunto filtrado — rechazado por la clarificación (alcance global).

## D7 — Índices y rendimiento (MongoDB)

**Decisión**: Ya existe índice por `LastNotificationOutcome` (spec 017) y por `Status`/`CreatedAt` (constitución). Para el listado de envíos se añade, si hace falta, un índice compuesto `{ Status, LastNotificationOutcome, LastNotificationAt }` para soportar el filtro por estado notificable + sendStatus + orden. Se valida con `explain` que no haya COLLSCAN bajo carga normal.

**Rationale**: Cumple el presupuesto de ≤200ms y la regla de "índices en campos frecuentemente consultados".

## D8 — Frontend: estructura, estado y "reintentando" transitorio

**Decisión**: Nueva feature `features/shipments` espejo de `features/invoices`:
- `useShipments(params)` con `useQuery` + `keepPreviousData` (sin parpadeo al filtrar/paginar).
- Mutaciones `useResendInvoice(id)` y `useCancelNotification(id)` que invalidan `['shipments']` (y `['invoices']`/`['invoice-stats']` por consistencia). "Reintentar fallidos" reutiliza `useResendFailed` de settings.
- **"reintentando"** se representa con el estado `isPending` de la mutación de reenvío por fila (badge transitorio), sin dato de servidor.
- Insignias por estado con `ShipmentStatusBadge` (color + etiqueta textual, no solo color).
- Skeletons (`ShipmentsTableSkeleton`), empty states diferenciados ("sin envíos" vs "sin coincidencias"), toasts vía `ToastProvider`/`useToast` existentes, respeto a "reducir movimiento".
- Ruta `/envios` lazy en `App.tsx` + entrada en `navigation.ts`.

**Rationale**: Reutiliza los componentes de feedback (`ToastProvider`, `ErrorBoundary`), el patrón de `useInvoicesViewState` y el layout existentes; cumple el estándar de la Fase 4 y mantiene code splitting por ruta.

**Alternativas**: meter envíos dentro de `features/invoices` — rechazado por límites de feature (Constitución I); estado global propio — innecesario, TanStack Query cubre el server state.

## D9 — Testing

**Decisión**: TDD por capa.
- **Domain**: reset de `NotificationRetryCount` al entrar en estado notificable; `RecordNotificationRetry` incrementa; `RecordNotificationResult` no toca el contador.
- **Application**: `InvoiceShipmentService.ResendAsync` (incremento + outcome) y `CancelAsync` (None→Skipped; rechazo si no pendiente); listado en dos pasos (filtro por clientId).
- **Infrastructure**: `MongoInvoiceRepository.GetShipmentsPagedAsync` (filtro, búsqueda, orden, paginación, total).
- **Api**: contratos de los 3 endpoints (200/400/404/409) y reuso de `resend-failed`.
- **Frontend**: render de tabla/columnas, badges por estado, filtro+búsqueda, empty/loading states, mutaciones con toast (MSW), a11y por teclado.

**Rationale**: Constitución IV (Test-First, ≥85%).
