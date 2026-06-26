# Research — Panel de Administración (Layout Base + Listado de Facturas)

**Feature**: `014-admin-panel-invoices` | **Fase**: 0 (Outline & Research)

Este documento resuelve las incógnitas técnicas del plan. Cada decisión sigue el formato Decisión / Justificación / Alternativas consideradas.

---

## D1 — Enrutamiento: sin router (sección Facturas activa)

- **Decisión**: No introducir librería de enrutamiento en esta feature. `AppShell` renderiza directamente la página de Facturas; la navegación lateral marca **Facturas** como activa y **Dashboard/Configuración** como deshabilitadas ("próximamente"). La estructura se diseña para admitir enrutamiento *lazy* por ruta cuando lleguen 4.3–4.5 sin reescritura.
- **Justificación**: La clarificación de la spec dejó solo Facturas como sección funcional. Añadir un router para una única ruta es complejidad innecesaria y peso de bundle; la Constitución pide bundle principal < 50KB gzip. Mantener el shell desacoplado de la página permite incorporar `react-router`/rutas *lazy* después.
- **Alternativas consideradas**:
  - *react-router-dom ahora*: rechazado por sobre-ingeniería para una sola ruta funcional; se difiere a cuando existan múltiples secciones.
  - *Estado de sección en memoria con múltiples páginas*: innecesario; las demás secciones están deshabilitadas.

## D2 — Búsqueda global por cliente: extender el endpoint server-side

- **Decisión**: Añadir el parámetro opcional **`search`** a `GET /api/invoices`. Se propaga `ListInvoicesQuery.Search` → `IInvoiceRepository.GetPagedAsync(status, clientSearch, page, pageSize)` → filtro Mongo que combina (AND) el filtro de estado con una coincidencia **case-insensitive** sobre `ClientId`. El total devuelto refleja el filtro+búsqueda aplicados (paginación coherente).
- **Justificación**: La spec exige búsqueda **global** (todo el dataset, no solo la página). Con paginación server-side, hacerla en cliente rompería resultados; por eso se resuelve en el servidor, respetando "paginación forzada / sin queries sin límite" (Constitución). El filtro tipado del driver Mongo evita inyección.
- **Detalles**:
  - Normalización en Application: `Trim()`; cadena vacía/whitespace ⇒ tratado como ausente (sin filtro de búsqueda). Longitud máxima razonable (p. ej. 100) validada para evitar regex abusivas.
  - Coincidencia: por defecto *contains* case-insensitive sobre `ClientId` (regex anclada/escapada del lado del repositorio para evitar metacaracteres del usuario). Índice existente en `ClientId` ayuda en prefijos; *contains* puede no usar índice — aceptable al volumen objetivo y acotado por paginación.
- **Alternativas consideradas**:
  - *Búsqueda en cliente sobre la página actual*: rechazada — contradice "búsqueda global" (la clarificación lo excluyó explícitamente).
  - *Traer todo el dataset al cliente*: rechazada por performance/escalabilidad (miles de facturas).
  - *Servicio de búsqueda externo*: sobredimensionado para el alcance.

## D3 — Columna "Última Acción": añadir `LastStatusTransitionAt` al DTO de listado

- **Decisión**: Extender `InvoiceListItemDto` con `LastStatusTransitionAt` (UTC). La tabla usa ese campo para "Última Acción". `CreatedAt` permanece disponible.
- **Justificación**: El DTO de listado actual solo expone `CreatedAt`, pero la columna "Última Acción" y el tipo `Invoice` del frontend (`lastStatusTransitionAt`) requieren la última transición de estado. El campo ya está persistido en el documento (`Invoice.LastStatusTransitionAt`); exponerlo es trivial y evita un desfase contrato↔UI.
- **Alternativas consideradas**:
  - *Usar `CreatedAt` como "Última Acción"*: rechazada — semánticamente incorrecto (no refleja la última transición).
  - *Llamar al endpoint de detalle por fila*: rechazada — N+1 innecesario.

## D4 — Animaciones con Motion + accesibilidad de movimiento

- **Decisión**: Usar `motion/react` para: (a) entrada del contenido al pasar de skeleton a datos (fade/translate corto), (b) apertura/cierre del menú lateral móvil (`sheet`), (c) transiciones sutiles al cambiar página/filtro. Centralizar variantes y duraciones en un módulo (`lib/motion.ts`) y respetar `prefers-reduced-motion` mediante `useReducedMotion()` de Motion, además de la regla CSS base ya presente en `index.css`.
- **Justificación**: Motion es el estándar mandatado; `useReducedMotion` permite atenuar/omitir animaciones (FR-018/SC-008). Variantes centralizadas mantienen consistencia (FR-017/FR-020) y facilitan el cumplimiento de React Doctor (sin animaciones ad-hoc dispersas).
- **Alternativas consideradas**:
  - *CSS transitions puras*: insuficientes para orquestar entrada/salida de listas y menú con control fino.
  - *Animar cada componente por separado*: rechazado por inconsistencia y riesgo de *layout shift*.

## D5 — Datos sin parpadeo: TanStack Query + debounce

- **Decisión**: `useInvoices` usa `useQuery` con `queryKey: ['invoices', { status, search, page, pageSize }]` y `placeholderData: keepPreviousData` para que, al cambiar página/filtro/búsqueda, la tabla anterior permanezca visible hasta que llegue la nueva (sin parpadeo ni salto). La búsqueda por cliente se *debounce* (~300ms) con `useDebouncedValue` para no disparar una petición por pulsación. Al cambiar `status` o `search`, la página se reinicia a 1 (FR-014).
- **Justificación**: `keepPreviousData` es el patrón recomendado de TanStack Query para paginación/filtrado fluido (FR-017). El *debounce* evita ráfagas de red y parpadeos (US3 escenario 3). `query-client.ts` se ajusta con `retry` y `staleTime` razonables.
- **Alternativas consideradas**:
  - *Refetch en cada tecla sin debounce*: rechazado — ráfagas y parpadeo.
  - *Estado de servidor en `useState`/`useEffect` manual*: rechazado — reinventa cache/estados de carga que TanStack ya provee.

## D6 — Componentes shadcn/ui a incorporar y dependencias

- **Decisión**: Generar/incluir los componentes shadcn *new-york* faltantes: `table`, `input`, `select`, `badge`, `skeleton`, `sheet` (y `dropdown-menu` solo si se necesita). Añadir dependencias: `lucide-react` (iconos, declarado en `components.json`), `@radix-ui/react-select`, `@radix-ui/react-dialog` (base de `sheet`), `@radix-ui/react-slot` (composición `asChild`). Mantener el patrón existente (`cn()`, `class-variance-authority`, tokens de `index.css`).
- **Justificación**: Son las primitivas estándar que usan los componentes shadcn elegidos; ya hay `button` con CVA y `components.json` configurado. Reutilizar el sistema de tokens garantiza consistencia visual y dark mode *built-in* (FR-020). Radix aporta accesibilidad (roles/teclado) que ayuda a WCAG A (FR-019) y a React Doctor.
- **Alternativas consideradas**:
  - *Construir tabla/menú a mano*: rechazado — más superficie de bugs de accesibilidad y peor puntuación de React Doctor.
  - *Otra librería de componentes (MUI/AntD)*: rechazado — contradice el stack mandatado (shadcn/ui) y el presupuesto de bundle.

## D7 — 100/100 honesto en React Doctor

- **Decisión**: Tratar React Doctor como *gate* de la feature. Tras implementar, correr `npx react-doctor@latest --verbose` (scope full) y, en cambios, `--scope changed`; corregir por severidad (errores → warnings) siguiendo el playbook canónico. **Prohibido** alcanzar 100 mediante supresión artificial (sin nuevos `biome-ignore`, `eslint-disable`, `// react-doctor-ignore` ni desactivar reglas en `doctor.config`). Los `biome-ignore` ya existentes y justificados en `index.css` (reduce-motion) se conservan, no se añaden nuevos para inflar la puntuación.
- **Justificación**: FR-021/SC-006 exigen 100/100 **honesto**. El playbook de react.doctor es la fuente de verdad del flujo scan→triage→fix→validate. La honestidad se evidencia en el diff (sin supresiones nuevas) y en el reporte `--verbose`.
- **Alternativas consideradas**:
  - *Ignorar/silenciar reglas para llegar a 100*: rechazado explícitamente por la spec.
  - *No medir y asumir calidad*: rechazado — no verificable.

---

## Resumen de impacto en el código existente

- `App.tsx`: deja de renderizar `InvoiceList`/`InvoiceTransitionsTab` sueltos; pasa a `AppShell` + `InvoicesPage` (cargada *lazy*). `InvoiceTransitionsTab` queda fuera de la navegación (Configuración deshabilitada) hasta su feature.
- `features/invoices/components/InvoiceList.tsx`: se refactoriza a `InvoicesTable.tsx` usando la `table` de shadcn; se extrae `StatusBadge`. La acción "Pagar" existente se preserva.
- `features/invoices/types.ts`: se añade el tipo de respuesta paginada y se alinea `Invoice` con el DTO (incluye `lastStatusTransitionAt`, ya presente).
- `lib/query-client.ts`: se añaden defaults (`retry`, `staleTime`).
- Backend: ediciones acotadas en `ListInvoices.cs`, `ListInvoicesQueryValidator.cs`, `IInvoiceRepository.cs`, `MongoInvoiceRepository.cs`, `InvoiceDtos.cs` (+ tests).

Todas las incógnitas del Technical Context quedan resueltas; no hay marcadores **NEEDS CLARIFICATION** pendientes.
