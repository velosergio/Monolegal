# Contratos de UI — Modal de Detalle y Dashboard (spec 015)

Contratos de los componentes/páginas: responsabilidad, props, estados y accesibilidad. No incluyen implementación.

---

## Modal de detalle de factura (`features/invoices`)

### `useSelectedInvoice()` *(hook)*

- **Responsabilidad**: leer/escribir la factura seleccionada en el *search param* `?factura=<id>`.
- **API**: `{ selectedId: string | null, open(id): void, close(): void }` sobre `useSearchParams` (react-router v7).
- **A11y/UX**: `close()` limpia el param; el foco retorna a la fila de origen.

### `InvoiceDetailModal`

- **Responsabilidad**: orquestar el contenido del diálogo a partir de `selectedId` (detalle + historial + cambio de estado).
- **Props**: `{ invoiceId: string | null; onClose: () => void }`.
- **Datos**: `useInvoiceDetail(invoiceId)` (`useQuery ['invoice', id]`, *enabled* solo si hay id).
- **Estados**:
  - Cargando → `InvoiceDetailSkeleton` (forma del contenido).
  - Éxito → `InvoiceDetailFields` + `StatusHistoryTimeline` + `ChangeStatusControl`.
  - Error → mensaje legible + acción de reintento (no rompe el listado de fondo).
  - `404` → mensaje "factura no encontrada".
- **A11y**: shadcn `dialog` (Radix) → *focus trap*, cierre por `Escape`/overlay/botón, `aria-labelledby`/`aria-describedby`, retorno de foco. Animación de apertura/cierre respeta `prefers-reduced-motion`.

### `InvoiceDetailFields`

- **Props**: `{ invoice: InvoiceDetail }`.
- **Render**: todos los campos (id legible/abreviado, cliente, monto como moneda, estado como `StatusBadge`, fechas en español, nº de recordatorios, último recordatorio, última transición).

### `StatusHistoryTimeline`

- **Props**: `{ history: StatusChange[]; createdAt: string }`.
- **Render**: línea de tiempo ordenada (más reciente primero) con `from → to`, fecha/hora legible y origen (`automático`/`manual`).
- **Vacío**: si `history` está vacío → muestra el evento de creación derivado de `createdAt` (FR-010).

### `ChangeStatusControl`

- **Props**: `{ invoiceId: string; allowedTransitions: InvoiceStatus[]; currentStatus: InvoiceStatus }`.
- **Render**: si `allowedTransitions` vacío → botón oculto/deshabilitado con indicación; si no → selector de destinos permitidos + botón "Cambiar Estado".
- **Acción**: `useTransitionInvoice` (`useMutation`) → invalida `['invoice',id]`, `['invoices']`, `['invoice-stats']` (research D5). El cambio manual **notifica al cliente** reutilizando la notificación de transición existente (FR-017a); un fallo de envío no revierte el cambio.
- **Estados**: ocupado durante la mutación (evita doble envío); error `400` → mensaje legible sin alterar el estado mostrado.
- **A11y**: selector y botón operables por teclado, foco visible, `aria` correctos.

### `InvoicesTable` *(EDITADO)* / `InvoicesPage` *(EDITADO)*

- Fila activable (clic y teclado) que llama `open(invoice.id)`.
- `InvoicesPage` monta `InvoiceDetailModal` con `invoiceId = selectedId`.

---

## Dashboard (`features/dashboard`)

### `DashboardPage`

- **Responsabilidad**: orquestar tarjetas + gráficos + último refresh.
- **Datos**: `useInvoiceStats()` (`useQuery ['invoice-stats']`).
- **Estados**:
  - Cargando → `DashboardSkeleton` (forma de tarjetas/gráficos).
  - Éxito → `StatCard`(s) + `StatusDistributionChart` + `ClientDistributionChart` + `LastRefreshIndicator`.
  - Vacío (`totalInvoices === 0`) → `DashboardEmptyState` (ceros legibles).
  - Error → mensaje legible + reintento (no rompe el panel).
- **A11y**: encabezados/landmarks correctos; gráficos con texto alternativo/labels.

### `StatCard`

- **Props**: `{ label: string; value: number | string; icon?: LucideIcon }`.
- **Render**: métrica destacada (total, nº de estados, nº de clientes), legible y con tema claro/oscuro.

### `StatusDistributionChart` / `ClientDistributionChart`

- **Props**: `{ data: { label: string; value: number; color?: string }[] }`.
- **Render**: gráfico SVG con animación Motion de entrada (barras/donut), respetando `prefers-reduced-motion`.
- **`ClientDistributionChart`**: recibe top-N + "Otros" (derivado en `DashboardPage`).

### `LastRefreshIndicator`

- **Props**: `{ updatedAt: number; onRefresh: () => void; isRefreshing: boolean }` (`dataUpdatedAt` + control manual).
- **Render**: texto en español ("Actualizado: …" / "hace N min") + **botón manual de actualizar** (`refetch`). Sin auto-refresco periódico/polling (FR-021a). El dashboard carga al montar.

---

## Navegación y ruteo

- `navigation.ts`: entrada **Dashboard** → `disabled: false` (habilitada, ya no "próximamente").
- `App.tsx`: nueva `<Route path="/dashboard">` con `DashboardPage` *lazy* + `Suspense` (skeleton). La sección activa se resalta (estado actual de la navegación).

---

## Calidad transversal

- TS strict sin `any`; Biome *compliant*; **React Doctor 100/100 honesto** (sin supresiones nuevas).
- Todas las animaciones (modal, gráficos) respetan `prefers-reduced-motion`.
- Operable por teclado con foco visible; modal con *focus trap* y retorno de foco.
