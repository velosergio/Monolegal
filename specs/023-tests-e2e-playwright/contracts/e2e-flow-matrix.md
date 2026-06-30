# Contrato — Matriz de flujos E2E

Contrato de la suite end-to-end: para cada flujo crítico del roadmap (Spec 5.4) se define el caso, el archivo donde vive, los localizadores accesibles que ancla, las aserciones esperadas y los requisitos/criterios que cubre. Es la fuente de trazabilidad entre roadmap → spec → pruebas.

> Localizadores estables disponibles (no requieren tocar producción):
> - Filtro lista: `getByRole('combobox', { name: 'Filtrar por estado' })` (o `getByLabel('Filtrar por estado')`)
> - Detalle factura: botón `getByRole('button', { name: /Ver detalle de la factura de/ })`
> - Select transición: `getByRole('combobox', { name: 'Nuevo estado' })` / `getByLabel('Nuevo estado')`
> - Botón transición: `getByRole('button', { name: 'Cambiar Estado' })`
> - Dashboard: `getByText('Total de facturas')`, `getByLabel('Distribución de facturas por estado')`
> - Etiquetas de estado visibles: `Pendiente`, `1er Recordatorio`, `2do Recordatorio`, `Pagado`, `Desactivado`

## Flujo 1 — Abrir lista de facturas y filtrar por estado

**Archivo**: `frontend/e2e/invoices-list-filter.spec.ts` — **Cubre**: US1, FR-002, FR-003, SC-001

| # | Caso | Pasos | Aserción esperada |
|---|---|---|---|
| 1.1 | Carga de la lista | Navegar a `/facturas` | Se ve el título "Facturas" y la tabla con ≥1 fila de factura; sin mensaje de error de carga |
| 1.2 | Filtrar por estado concreto | Abrir filtro "Filtrar por estado" → elegir "1er Recordatorio" | Todas las filas visibles muestran badge "1er Recordatorio"; el conteo coincide con lo esperado para ese estado |
| 1.3 | Volver a todos los estados | Con filtro aplicado → elegir "Todos los estados" | Se muestran de nuevo facturas de múltiples estados (≥ las del paso 1.1) |
| 1.4 | Estado sin facturas (estado vacío) | Filtrar por un estado sin facturas (según seed/estado actual) | Se muestra el estado vacío del listado sin error |

**Precondición**: datos sembrados (estado conocido). Solo lectura → no requiere reset dedicado.

## Flujo 2 — Transición manual de estado

**Archivo**: `frontend/e2e/manual-transition.spec.ts` — **Cubre**: US2, FR-004, FR-005, SC-006

| # | Caso | Pasos | Aserción esperada |
|---|---|---|---|
| 2.1 | Destinos permitidos visibles | Abrir detalle de una factura `Pendiente` → abrir select "Nuevo estado" | Las opciones son exactamente los destinos permitidos del backend para `Pendiente` (1er Recordatorio, Pagado) |
| 2.2 | Aplicar transición no terminal | Detalle de factura `Pendiente` → seleccionar "1er Recordatorio" → "Cambiar Estado" | Toast de confirmación "Estado actualizado a «1er Recordatorio»."; el detalle/lista muestran el nuevo estado |
| 2.3 | Estado terminal sin control | Abrir detalle de la factura `Pagado` | No hay control de transición; se comunica que la factura no admite cambios de estado |
| 2.4 | Persistencia en la lista | Tras 2.2, cerrar modal y volver a la lista | La factura figura con el estado actualizado de forma persistente |

**Precondición**: `resetData()` antes del bloque (datos conocidos). Spec serializada (muta estado).

## Flujo 3 — Dashboard actualizado tras la transición

**Archivo**: `frontend/e2e/dashboard-updated.spec.ts` — **Cubre**: US3, FR-006, SC-006

| # | Caso | Pasos | Aserción esperada |
|---|---|---|---|
| 3.1 | Reflejo del cambio | Leer distribución en `/` → realizar transición `Pendiente→1er Recordatorio` → volver a `/` | El conteo de "Pendiente" baja en 1 y el de "1er Recordatorio" sube en 1 (comparación por **delta**, no absoluta) |
| 3.2 | Métrica total coherente | Comparar "Total de facturas" antes/después | El total permanece igual (la transición no crea/elimina facturas) |
| 3.3 | Estado vacío | Resetear a BD sin facturas (si aplica el escenario) → abrir `/` | Se muestra el estado vacío del dashboard sin error |

**Precondición**: `resetData()` antes del bloque; spec serializada (muta y lee agregados).

## Requisitos de ejecución (contrato de la suite)

- **Comando**: `npm run test:e2e` (desde `frontend/`) → `playwright test` sobre `frontend/e2e/`.
- **Código de salida**: distinto de cero si cualquier prueba falla (FR-009, SC-003).
- **Sin omisiones**: prohibido `.skip`/`.only` en commits (FR-009, Principio IV).
- **Determinismo**: dos corridas consecutivas en verde sin flakiness (SC-002); aserciones por delta/contenido estable (D6).
- **Independencia de orden**: cualquier subconjunto corre aislado partiendo de reset+seed (SC-004).
- **Sin tocar producción**: la suite no modifica `frontend/src/**` ni el backend (FR-010, SC-005).

## Trazabilidad roadmap → pruebas

| Criterio roadmap (Spec 5.4) | Flujo/caso | Requisito |
|---|---|---|
| Abrir lista de facturas | 1.1 | FR-002 |
| Filtrar por estado | 1.2, 1.3, 1.4 | FR-003 |
| Hacer transición manual | 2.1, 2.2, 2.4 (+ 2.3 terminal) | FR-004, FR-005 |
| Ver dashboard actualizado | 3.1, 3.2 | FR-006 |
