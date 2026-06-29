# Data Model: Tests de Integración de la API

**Feature**: 021-integration-tests-api | **Fecha**: 2026-06-29

Esta feature no introduce entidades de dominio nuevas. Documenta (a) los **datos de prueba** que la suite siembra y (b) las **formas de respuesta** del contrato HTTP que las pruebas verifican. Las definiciones canónicas viven en el dominio (specs 005/006/015) y en los DTO del API (spec 009); aquí sólo se referencian para anclar las aserciones.

## 1. Entidades de datos de prueba (sembradas en memoria)

### Invoice (Factura) — sujeto de prueba

| Campo | Tipo | Uso en las pruebas |
|-------|------|--------------------|
| `Id` | string | Clave para detalle/transición; un id ausente ⇒ `404`. |
| `ClientId` | string | Vinculа la factura a un cliente sembrado; base del filtro `search`. |
| `Amount` | decimal | Verificado en detalle (derivado de items). |
| `Status` | InvoiceStatus | Estado de origen para transiciones permitidas/prohibidas y filtro del listado. |
| `CreatedAt` | DateTime | Verifica el orden descendente del listado (`OverrideCreatedAt`). |
| `Items` | InvoiceItem[] | Necesarios para construir una factura válida (`Invoice.Create`). |

- **Construcción**: `InvoiceTestFactory.Create(clientId, amount, status)` o `Invoice.Create(clientId, items, dueDate)`.
- **Reglas de creación heredadas del dominio** (no se re-testean aquí, se asumen): cliente obligatorio, ≥1 item, monto = Σ subtotales.

### Client (Cliente) — dato de apoyo

| Campo | Tipo | Uso en las pruebas |
|-------|------|--------------------|
| `Id` | string | Referenciado por `Invoice.ClientId`. |
| `Name` | string | El endpoint resuelve el nombre legible (`ClientName`/clave de `byClient`). |
| `Email` | string | Requerido para construir el cliente; no central en estas pruebas. |

- **Construcción**: `new Client(name, email)`; sembrado con `SeedClient`.

## 2. Estados de factura (InvoiceStatus) — valores de API

Cadenas en minúscula intercambiadas por HTTP (JsonStringEnumConverter global):

`pending` · `primerrecordatorio` · `segundorecordatorio` · `desactivado` · `pagado`

Matriz de transiciones relevante para los casos de transición (origen → destinos permitidos):

- `pending` → `primerrecordatorio`, `pagado`
- `primerrecordatorio` → `segundorecordatorio`, `pagado`
- `segundorecordatorio` → `desactivado`, `pagado`
- `desactivado` → `pagado`
- `pagado` → ∅ (terminal)

## 3. Formas de respuesta verificadas (contrato HTTP)

### Listado — `PagedResponse<InvoiceListItemDto>`

```jsonc
{
  "data": [
    { "id": "...", "clientId": "...", "clientName": "...", "amount": 350.0,
      "status": "pending", "createdAt": "...", "lastStatusTransitionAt": "..." }
  ],
  "total": 25,      // total de coincidencias del filtro, independiente de la página
  "pageSize": 10
}
```

Aserciones: `200 OK`; `data` ≤ `pageSize`; `total` refleja todas las coincidencias; con `?status=` sólo aparecen facturas de ese estado.

### Detalle — `InvoiceDetailDto`

Objeto completo con `id`, `clientId`, `clientName`, `amount`, `dueDate`, `items[]`, `status`, `createdAt`, `updatedAt`, `statusHistory[]`, `allowedTransitions[]`. Aserciones: `200 OK` + objeto completo para id existente; `404 Not Found` para id inexistente o de formato inválido.

### Transición — `InvoiceDetailDto` (en éxito)

Aserciones: `200 OK` con `status` = nuevo estado (transición permitida); `400 Bad Request` (transición prohibida o cuerpo inválido) sin cambio de estado; `404 Not Found` (id inexistente). Cuerpo de petición: `TransitionRequest { newStatus }`.

### Estadísticas — `InvoiceStatsDto`

```jsonc
{ "totalInvoices": 5, "byStatus": { "pending": 3, "pagado": 2 }, "byClient": { "Acme": 5 } }
```

Aserciones: `200 OK`; Σ(`byStatus`) == `totalInvoices`; con base vacía ⇒ `totalInvoices` 0 y agregados vacíos (no error).

## 4. Dobles de infraestructura (estado de prueba)

| Abstracción | Doble | Rol |
|-------------|-------|-----|
| `IInvoiceRepository` | `InMemoryInvoiceRepository` | Almacén de facturas; implementa `GetPagedAsync`, `CountByStatusAsync`, etc. |
| `IClientRepository` | `InMemoryClientRepository` | Resolución de nombre de cliente. |
| `ISystemSettingsRepository` | `InMemorySystemSettingsRepository` | Evita acceso a Mongo para settings. |
| `IClientEmailResolver` | `FakeClientEmailResolver` | Email de cliente determinista. |
| `IInvoiceTransitionNotifier` | `FakeTransitionNotifier` | Hace observable la transición sin enviar email. |
| `IHostedService` | (removido) | Desactiva el worker de transiciones en pruebas. |
