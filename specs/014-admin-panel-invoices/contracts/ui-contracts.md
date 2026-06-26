# Contratos de UI — Panel de Administración

**Feature**: `014-admin-panel-invoices` | **Fase**: 1 (Design)

Contratos de los componentes/página del panel: responsabilidad, props públicas, estados observables y requisitos de accesibilidad. Sirven de base para las pruebas (Vitest + Testing Library) y la implementación. No incluyen código de implementación.

---

## AppShell

- **Responsabilidad**: Componer el layout (navbar + sidebar + footer + área principal) y aplicar el comportamiento responsive.
- **Props**: `{ children: ReactNode }`.
- **Contrato observable**:
  - Renderiza navbar (con logo Monolegal), navegación lateral, pie de página y `children` en el área principal.
  - En escritorio (≥ breakpoint), la navegación lateral es persistente; en móvil se colapsa tras un disparador accesible.
  - No provoca desbordamiento horizontal en anchos de móvil ni escritorio.
- **A11y**: landmarks `header`/`nav`/`main`/`footer`; el disparador del menú móvil tiene `aria-label` y `aria-expanded`.
- **Cubre**: FR-001, FR-003, FR-004; SC-005.

## Navbar

- **Responsabilidad**: Barra superior con marca y disparador del menú móvil.
- **Contrato**: muestra el logo/marca "Monolegal" siempre visible; en móvil muestra el botón de menú.
- **A11y**: el logo es un enlace/encabezado con texto accesible; botón de menú operable por teclado.
- **Cubre**: FR-001, FR-019.

## Sidebar

- **Responsabilidad**: Navegación entre secciones del panel.
- **Props**: `{ activeSection: 'invoices' }` (por ahora fija).
- **Contrato**:
  - Ítems: **Facturas** (activo, resaltado), **Dashboard** (deshabilitado, "próximamente"), **Configuración** (deshabilitado, "próximamente").
  - Los ítems deshabilitados no son accionables (sin navegación) y se comunican como tal a tecnologías de asistencia (`aria-disabled`).
- **A11y**: `nav` con lista; ítem activo con `aria-current="page"`; foco visible.
- **Cubre**: FR-002, FR-006, FR-019.

## Footer

- **Contrato**: muestra nombre de la app y versión/año.
- **Cubre**: FR-003.

---

## InvoicesPage

- **Responsabilidad**: Orquestar el estado de vista (filtro/búsqueda/página), obtener datos (`useInvoices`) y renderizar el sub-árbol (filtros, tabla/skeleton/vacío/error, paginación) con animación de transición.
- **Estados observables**:
  - **loading**: muestra `InvoicesTableSkeleton` (no pantalla en blanco) — FR-010/SC-002.
  - **success (con datos)**: muestra `InvoicesTable` con entrada animada — FR-007/FR-017.
  - **empty**: muestra `InvoicesEmptyState` con mensaje claro; filtros visibles — FR-015.
  - **error**: muestra mensaje legible + botón "Reintentar"; el shell sigue usable — FR-016.
- **Reglas**: cambiar estado/búsqueda reinicia la página a 1 (FR-014); usa `keepPreviousData` para evitar parpadeo (FR-017).
- **Cubre**: FR-007..FR-017.

## InvoicesTable

- **Props**: `{ invoices: Invoice[] }`.
- **Contrato**: tabla (`table` shadcn) con columnas **ID** (abreviado), **Cliente** (`clientId`), **Monto** (moneda MXN), **Estado** (`StatusBadge`), **Última Acción** (fecha/hora local es-MX). Acción "Pagar" se conserva para estados no terminales.
- **Edge**: estado desconocido ⇒ badge neutro con valor en bruto; ausencia de fecha ⇒ marcador consistente sin romper alineación.
- **A11y**: cabeceras `th` asociadas; tabla con `caption`/`aria-label`; en móvil permite *scroll* horizontal contenido.
- **Cubre**: FR-007, FR-008, FR-009, FR-019; edge cases de estado/fecha.

## InvoicesTableSkeleton

- **Contrato**: réplica de la estructura de la tabla (cabeceras + N filas de placeholders con `skeleton`); igual número de columnas para evitar *layout shift*.
- **Cubre**: FR-010, FR-017, SC-002.

## StatusFilter

- **Props**: `{ value: InvoiceStatus | 'all'; onChange: (v) => void }`.
- **Contrato**: `select` (shadcn/Radix) con opción "Todos" + un ítem por estado filtrable; al cambiar, notifica y la página se reinicia.
- **A11y**: `select` accesible por teclado, etiqueta asociada.
- **Cubre**: FR-011, FR-014, FR-019.

## ClientSearch

- **Props**: `{ value: string; onChange: (v) => void }`.
- **Contrato**: `input` de texto con etiqueta; el valor se *debounce* (~300ms) antes de propagarse a la consulta; ignora mayúsculas/espacios en extremos; al cambiar (debounced) reinicia la página.
- **Observable**: teclear no produce una petición por pulsación (debounce) ni parpadeo (FR-017/US3-3).
- **Cubre**: FR-012, FR-014, FR-019.

## InvoicesPagination

- **Props**: `{ page: number; totalPages: number; onPageChange: (p) => void }`.
- **Contrato**: controles anterior/siguiente (y/o números) con indicación de página/total; deshabilita extremos (`canPrev`/`canNext`); nunca permite páginas inválidas.
- **A11y**: botones con `aria-label`; estado deshabilitado expuesto.
- **Cubre**: FR-013, FR-019.

## StatusBadge

- **Props**: `{ status: InvoiceStatus }`.
- **Contrato**: `badge` con color por estado y etiqueta en español; estado no mapeado ⇒ estilo neutro + valor en bruto.
- **Cubre**: FR-009; edge case estado desconocido.

## InvoicesEmptyState

- **Contrato**: mensaje claro ("No hay facturas para mostrar" / "Sin coincidencias") manteniendo visibles filtro y búsqueda.
- **Cubre**: FR-015.

## ErrorBoundary

- **Contrato**: captura errores de render del sub-árbol de la página y muestra una vista de degradación elegante sin tumbar el shell.
- **Cubre**: FR-016, Constitución VI.

---

## Hooks

### useInvoices

- **Entrada**: `{ status, search, page, pageSize }`.
- **Salida**: `{ data?: PagedInvoices, isPending, isError, error, refetch, isPlaceholderData }`.
- **Contrato**: `useQuery` con `queryKey: ['invoices', params]`, `placeholderData: keepPreviousData`. No lanza; expone estados.

### useInvoicesViewState

- **Salida**: `{ status, search, page, setStatus, setSearch, setPage }` con la regla de reinicio de página al cambiar `status`/`search`.

### useDebouncedValue

- **Contrato**: `useDebouncedValue(value, delayMs)` devuelve el último valor estabilizado tras `delayMs`.

### useReducedMotion (de Motion) + variantes

- **Contrato**: las animaciones consultan la preferencia de movimiento reducido y se atenúan/omiten (FR-018/SC-008). Variantes y duraciones centralizadas en `lib/motion.ts`.

---

## Requisitos de calidad transversales (todas las piezas)

- TypeScript strict sin `any`; Biome *compliant*.
- **React Doctor 100/100 honesto** (FR-021/SC-006): sin supresiones nuevas.
- Operable por teclado, foco visible, roles/labels correctos (FR-019; WCAG A).
- Animaciones suaves sin *layout shift* perceptible (FR-017) y respetando reduce-motion (FR-018).
