# Research — Detalle de Factura (Modal) y Dashboard de Estadísticas (spec 015)

Decisiones técnicas que resuelven los *unknowns* del plan. Cada entrada: **Decisión**, **Justificación**, **Alternativas consideradas**.

---

## D1 — Persistencia del historial de cambios de estado

**Decisión**: Persistir el historial como una **lista embebida** `StatusHistory` dentro del agregado `Invoice` (value object `StatusChange { From, To, At, Source }`). El *append* ocurre en el único punto de mutación de estado del dominio, `Invoice.UpdateStatus(newStatus, source)`. El origen (`Automatic`/`Manual`) se propaga desde el call site:
- `InvoiceTransitionService.TryApplyTransition` (worker) → `source = Automatic`.
- `InvoiceTransitionService.ApplyManualTransition` / `ApplyPayment` (endpoint manual y pago) → `source = Manual`.

Las facturas existentes reciben su historial mediante una **migración de backfill única e idempotente** que siembra un evento de creación (estado inicial en `CreatedAt`); el modal mantiene además una derivación del evento de creación como respaldo defensivo si encontrara historial vacío (FR-010, FR-030). Ver D9 para la eliminación de legacy asociada.

**Justificación**:
- El `Invoice` se mapea como **POCO directo** a la colección `Invoices` (`database.GetCollection<Invoice>("Invoices")`); una lista embebida se serializa a BSON automáticamente, **sin nueva colección ni índice ni mapeo manual**.
- **Ambas** rutas de transición ya persisten con `IInvoiceRepository.UpdateAsync` (reemplazo completo del documento): el worker (`InvoiceTransitionsWorker.RunCycleAsync`) y el endpoint (`TransitionInvoice`) y el pago (`PayInvoice`). Por tanto el historial acumulado se escribe íntegro en cada cambio, sin tocar Infrastructure.
- El historial es parte del estado del agregado factura → pertenece al agregado (DDD), no a una entidad satélite.

**Alternativas consideradas**:
- **Colección separada de eventos** (`InvoiceStatusEvents`): mayor flexibilidad de consulta y volumen, pero introduce una nueva colección, índices, un repositorio nuevo y escrituras transversales; sobredimensionado para un historial de unidades de eventos por factura consultado solo en el modal.
- **Mantener solo `LastStatusTransitionAt`**: descartado por la clarificación (audit log completo).
- **Append vía `IInvoiceRepository.UpdateStatusAsync` (`$push`)**: este método legacy hace un `$set` parcial y **no** lo usa ninguna ruta de transición actual (worker/endpoint usan `UpdateAsync`). Por la directriz "no dejar nada en legacy" (clarificación 2026-06-26) se **elimina por completo** en lugar de documentarlo como invariante (ver D9).

---

## D2 — Exposición de los destinos de transición válidos

**Decisión**: Añadir al dominio un método puro `InvoiceTransitionService.GetAllowedTransitions(InvoiceStatus current) → IReadOnlyCollection<InvoiceStatus>` derivado de la misma matriz que ya valida `ApplyManualTransition`/`ApplyPayment`, y exponer el resultado como `allowedTransitions` (array de strings de estado) en `InvoiceDetailDto`. El modal consume ese array; **no** replica la matriz.

Matriz resultante (incluyendo `Pagado`):
- `Pending → { PrimerRecordatorio, Pagado }`
- `PrimerRecordatorio → { SegundoRecordatorio, Pagado }`
- `SegundoRecordatorio → { Desactivado, Pagado }`
- `Desactivado → { Pagado }`
- `Pagado → { }` (terminal)

**Justificación**:
- Única fuente de verdad en el dominio (Constitución I/II): el frontend no decide la validez. Evita divergencia entre matriz de backend y lógica de UI.
- Embeber `allowedTransitions` en el DTO de detalle hace que **un solo `GET /api/invoices/{id}`** alimente todo el modal (campos + historial + validez), minimizando *round-trips* (Performance).
- `GetAllowedTransitions` se reutiliza internamente para mantener `ApplyManualTransition` y la lista expuesta consistentes (DRY).

**Alternativas consideradas**:
- **Endpoint dedicado** `GET /api/invoices/{id}/allowed-transitions`: más granular pero añade un *round-trip* y una superficie de API extra para un dato que el modal siempre necesita junto al detalle.
- **Replicar la matriz en el frontend**: descartado por la clarificación y por acoplar lógica de dominio a la UI (riesgo de divergencia).

---

## D3 — Estrategia de gráficos del dashboard

**Decisión**: Construir los gráficos **in-house con SVG + Motion** (barras/donut animados), sin añadir una librería de charting. Se reutiliza `motion/react` (ya dependencia) y las variantes de `lib/motion.ts`, respetando `prefers-reduced-motion` vía `useReducedMotion`.

**Justificación**:
- "Gráficos (motion animados)" del roadmap se satisface literalmente con animaciones Motion controladas.
- Respeta el presupuesto de bundle (< 50KB gzip principal; Constitución Performance): evita una dependencia pesada (p. ej. recharts/d3) para dos gráficos sencillos (distribución por estado y por cliente).
- Control total de accesibilidad (roles/labels, `prefers-reduced-motion`) y del tema claro/oscuro (tokens shadcn/CSS variables).

**Alternativas consideradas**:
- **shadcn `chart` (recharts)**: integración estándar y rica, pero suma dependencia y peso para necesidades simples; la animación de recharts es menos alineada con Motion. Reservable si los gráficos crecen en complejidad.
- **Otra librería (visx, chart.js)**: mismo trade-off de peso/complejidad sin beneficio para este alcance.

---

## D4 — Apertura/selección del modal y gestión de foco

**Decisión**: La factura seleccionada se refleja en un **search param** `?factura=<id>` (hook `useSelectedInvoice` sobre `useSearchParams` de react-router v7). El modal es el componente shadcn `dialog` (Radix `@radix-ui/react-dialog`, ya instalado), que aporta *focus trap*, cierre por `Escape`/overlay y `aria` correctos; al cerrarse se limpia el param y el foco retorna a la fila de origen.

**Justificación**:
- Deep-linking y botón "atrás" del navegador funcionan (compartir/abrir una factura concreta); coherente con el ruteo ya adoptado en la spec 014/015.
- Radix dialog cumple WCAG A *out of the box* (focus trap + retorno de foco), reduciendo riesgo de regresiones de accesibilidad y ayudando al 100/100 honesto de React Doctor.
- Mantener la selección fuera del estado local de la tabla evita *prop drilling* y desacopla fila ↔ modal.

**Alternativas consideradas**:
- **Estado local (`useState`) en `InvoicesPage`**: simple, pero sin deep-linking ni navegación por historial; foco igualmente gestionable.
- **Ruta anidada `/facturas/:id`**: válida, pero el modal es un *overlay* del listado; un search param expresa mejor "listado + selección" sin desmontar la página.

---

## D5 — Coherencia de datos tras el cambio de estado

**Decisión**: El cambio de estado usa `useMutation` (`useTransitionInvoice`) que, en `onSuccess`, **invalida de forma dirigida**: `['invoice', id]` (refresca el modal: estado + historial), `['invoices']` (el listado de fondo) y `['invoice-stats']` (el dashboard). Durante la mutación los controles quedan en estado ocupado (evita doble envío). Un 400/ën error muestra mensaje legible y refresca el detalle para reflejar la realidad del backend (FR-016).

**Justificación**:
- Satisface FR-015/SC-004 (estado, historial y listado coherentes sin recargar la página) y mantiene el dashboard alineado si está montado o al revisitarlo.
- La respuesta del endpoint de transición ya devuelve `InvoiceDetailDto` (con historial+destinos), de modo que el modal puede actualizarse incluso antes de que resuelva la invalidación (set inmediato del cache opcional).
- Invalidación dirigida (no global) evita refetches innecesarios y parpadeos.

**Alternativas consideradas**:
- **Invalidar todo el cache**: simple pero costoso e impreciso.
- **Actualización optimista**: innecesaria aquí; el cambio es puntual y la respuesta del endpoint ya trae el estado final.

---

## D6 — Distribución "por cliente" con muchos clientes

**Decisión**: En el gráfico/tabla "por cliente" mostrar los **top-N** clientes por cantidad de facturas (p. ej. N=5–8) y agrupar el resto en "**Otros**". Las tarjetas muestran el total de clientes distintos. La cifra exacta por cliente queda legible en *tooltip*/lista.

**Justificación**:
- `byClient` puede tener cardinalidad alta; renderizar todos rompe el layout y la legibilidad (Edge Case de la spec).
- Top-N + "Otros" comunica la concentración de cartera sin desbordar; mantiene el rendimiento de render.

**Alternativas consideradas**:
- **Renderizar todos los clientes**: ilegible y costoso con cardinalidad alta.
- **Paginar el desglose**: sobredimensionado para una vista de resumen.

---

## D7 — Indicador de "último refresh"

**Decisión**: Derivar el "último refresh" del `dataUpdatedAt` que expone `useQuery` para `['invoice-stats']`, formateado en español (fecha/hora local legible y/o "hace N min"). El dashboard carga al montar/navegar y ofrece un **botón manual** de "actualizar" (`refetch`); **sin auto-refresco periódico/polling** (clarificación 2026-06-26, FR-021a).

**Justificación**:
- `dataUpdatedAt` es la marca real de cuándo se obtuvieron los datos mostrados (no un reloj de pared arbitrario), exacta y sin estado extra.
- Coherente con el uso de TanStack Query en el resto del panel.

**Alternativas consideradas**:
- **Timestamp manual en estado**: redundante y propenso a desincronizarse del dato realmente mostrado.
- **Campo de servidor "generatedAt"**: requeriría cambiar el contrato de stats sin beneficio frente a `dataUpdatedAt`.

---

## D8 — 100/100 honesto en React Doctor

**Decisión**: Aplicar el *playbook* de react-doctor del proyecto (mismo estándar que la spec 014): sin `biome-ignore` ni supresiones nuevas; componentes con responsabilidad única, *hooks* con dependencias correctas, accesibilidad (roles/labels/foco) y *lazy loading* por ruta. Verificación al final de la implementación.

**Justificación**: FR-028/SC-010 exigen 100/100 honesto; reutilizar el playbook ya validado en 014 reduce riesgo y esfuerzo.

**Alternativas consideradas**: Ninguna — es un requisito no negociable de la Constitución (V).

---

## D9 — Eliminación de legacy (código y datos) *(clarificación 2026-06-26)*

**Decisión**: Bajo la directriz "no dejar nada en legacy":

1. **Código muerto**: eliminar `IInvoiceRepository.UpdateStatusAsync` (interfaz), su implementación en `MongoInvoiceRepository`, los fakes (`InMemoryInvoiceRepository`, `ThrowingInvoiceRepository`, fakes de tests de aplicación) y los tests de contrato/integración que lo cubren (`InvoiceRepositoryContractTests` y `MongoInvoiceRepositoryStatusUpdateTests`, secciones de `UpdateStatusAsync`). El cambio de estado queda con una única vía (`UpdateStatus` del agregado + `UpdateAsync`), siempre historiada.
2. **Backfill de datos**: migración única e idempotente (seeder/hosted service de arranque o script administrativo) que, para cada factura sin `StatusHistory`, *appendea* un evento de creación.
3. **Estados legacy del enum**: retirar `Draft/Overdue/Cancelled` de `InvoiceStatus`. La **misma migración** remapea primero los documentos en esos estados a un estado activo válido (mapeo **firme**, clarificación 2026-06-26: Borrador→Pending, Vencida→Pending, Cancelada→Desactivado). El constructor de `Invoice` inicia en `Pending` (no `Draft`).

**Justificación**:
- Elimina toda vía que pudiera saltarse el historial (defensa de la invariante de FR-029) y todo estado no soportado en datos (FR-031), evitando deuda y comportamientos ambiguos.
- El orden importa: **remapear datos antes** de retirar los valores del enum evita romper la deserialización de documentos existentes (riesgo principal de quitar miembros del enum).
- La idempotencia permite reejecutar la migración sin duplicar eventos ni revertir estados.

**Alternativas consideradas**:
- **Conservar `UpdateStatusAsync` documentado** y **no migrar** (derivar en UI): descartado por la directriz; deja código y datos legacy.
- **Quitar los valores del enum sin remapear datos**: descartado por el riesgo de deserialización de documentos en estados retirados.
- **Mantener los estados legacy y solo manejarlos con etiqueta neutra**: descartado por la directriz (deja legacy en el sistema).

**Riesgos / mitigaciones**:
- *Pérdida de transiciones pasadas*: irreemplazable (no se registraban); se documenta que el historial es completo a partir de la feature. El backfill solo siembra el evento de creación.
- *Mapeo de negocio de estados legacy*: fijado (Borrador→Pending, Vencida→Pending, Cancelada→Desactivado); el requisito firme adicional es "ningún estado no soportado tras la migración".
- *Otras specs que asuman `Draft` inicial*: revisar seeders/tests de specs previas (p. ej. 007/008) que esperen `Draft`; ajustarlos al iniciar en `Pending`.

## Resumen de impacto

- **Backend (acotado)**: `StatusChangeSource` (enum), `StatusChange` (value object), `Invoice.StatusHistory` + `UpdateStatus(newStatus, source)`, constructor inicia en `Pending`, `InvoiceTransitionService.GetAllowedTransitions`, `InvoiceDetailDto` (+`statusHistory`, +`allowedTransitions`), `GetInvoiceById` (inyecta el servicio). **Limpieza de legacy (D9)**: eliminar `UpdateStatusAsync` (interfaz/impl/fakes/tests), retirar `Draft/Overdue/Cancelled` del enum y añadir una **migración única e idempotente** (backfill de historial + remapeo de estados legacy). Sin colección/índice nuevos; sin dependencias nuevas.
- **Frontend (principal)**: `features/invoices` (modal de detalle, historial, cambio de estado, selección por search param, mutación con invalidación) y nuevo `features/dashboard` (tarjetas, gráficos SVG+Motion, último refresh). Ruta `/dashboard` *lazy* y navegación habilitada. `dialog`/`card` de shadcn locales. Sin dependencias de runtime nuevas.
