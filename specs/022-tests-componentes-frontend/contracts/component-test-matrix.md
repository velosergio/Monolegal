# Contrato — Matriz de pruebas de componentes

**Feature**: 022-tests-componentes-frontend | **Fecha**: 2026-06-29

Matriz trazable: cada componente → casos → tipo de aserción (R = render/estructura, S = snapshot) → requisito funcional cubierto. Las descripciones de los `it(...)` van en español.

## Insignias de estado

| Componente | Caso | Tipo | Aserción esperada | FR |
|------------|------|------|-------------------|----|
| `StatusBadge` | estado conocido (`pagado`) | R | muestra la etiqueta legible `Pagado` con clase de color del estado | FR-003, FR-005 |
| `StatusBadge` | estado desconocido (`loquesea`) | R | muestra el valor en bruto con estilo neutro (`bg-muted`/`text-muted-foreground`) | FR-005 |
| `StatusBadge` | snapshot por estado conocido | S | marcado estable del badge | FR-001, FR-002 |
| `ShipmentStatusBadge` | estado conocido (`sent`) | R | etiqueta legible + clase de color | FR-003, FR-005 |
| `ShipmentStatusBadge` | snapshot | S | marcado estable del badge | FR-001 |

## Tarjeta de métrica

| Componente | Caso | Tipo | Aserción esperada | FR |
|------------|------|------|-------------------|----|
| `StatCard` | con `label` y `value` | R | ambos visibles en el documento | FR-003 |
| `StatCard` | con `icon` opcional | R | el ícono es decorativo (`aria-hidden="true"`) | FR-005 |
| `StatCard` | snapshot (con y sin ícono) | S | marcado estable en ambas variantes | FR-001 |

## Estados vacíos

| Componente | Caso | Tipo | Aserción esperada | FR |
|------------|------|------|-------------------|----|
| `InvoicesEmptyState` | render | R | título "No se encontraron facturas" + texto de ayuda | FR-003 |
| `InvoicesEmptyState` | snapshot | S | marcado estable | FR-001 |
| `DashboardEmptyState` | render | R | título "No hay facturas todavía" + descripción | FR-003 |
| `DashboardEmptyState` | snapshot | S | marcado estable | FR-001 |
| `ShipmentsEmptyState` | `filtered=true` | R | título "No se encontraron envíos" | FR-005 |
| `ShipmentsEmptyState` | `filtered=false` | R | título "Aún no hay envíos" | FR-005 |
| `ShipmentsEmptyState` | snapshot (ambas ramas) | S | marcado estable por rama | FR-001, FR-002 |

## Esqueletos de carga

| Componente | Caso | Tipo | Aserción esperada | FR |
|------------|------|------|-------------------|----|
| `InvoicesTableSkeleton` | render | R | `aria-hidden="true"`; cabeceras de columna (ID, Cliente, Monto, Estado, Última Acción, Acciones) | FR-003, FR-005 |
| `InvoicesTableSkeleton` | snapshot (`rows={2}`) | S | marcado estable con nº de filas fijo | FR-001, FR-002 |
| `InvoiceDetailSkeleton` | render | R | `aria-hidden="true"`; 6 grupos de campos | FR-003 |
| `InvoiceDetailSkeleton` | snapshot | S | marcado estable | FR-001 |
| `DashboardSkeleton` | render | R | rol `status` con etiqueta "Cargando estadísticas" | FR-003, FR-005 |
| `DashboardSkeleton` | snapshot | S | marcado estable | FR-001 |
| `ShipmentsTableSkeleton` | snapshot (`rows={2}`) | S | marcado estable con nº de filas fijo | FR-001, FR-002 |

## Pie del sidebar

| Componente | Caso | Tipo | Aserción esperada | FR |
|------------|------|------|-------------------|----|
| `Footer` | expandido | R | muestra "Monolegal", versión y año (con fecha fija 2026) | FR-002, FR-005 |
| `Footer` | colapsado (`collapsed`) | R | muestra solo la versión `v0.1.0` | FR-005 |
| `Footer` | snapshot (ambas variantes, fecha fija) | S | marcado estable y determinista | FR-001, FR-002 |

## Inventario de criterios del roadmap (Spec 5.3) → pruebas

| Criterio roadmap | Respaldo |
|------------------|----------|
| Renderiza sin errores | Suite existente (48 archivos) + nuevos tests R de esta feature | 
| Interacciones simuladas (click, select) | Tests existentes (p. ej. `StatusFilter`, `ChangeStatusControl`, `InvoicesTable*`) |
| Async handlers (TanStack Query mocked) | Tests existentes (p. ej. `useTransitionInvoice`, `useInvoiceStats`, `invoiceMutations`) |
| Snapshot tests para UI crítica | **Nuevos** tests S de esta feature (badges, StatCard, estados vacíos, esqueletos, Footer) |

## Reglas de aceptación del contrato

- Todos los casos S son **deterministas**: sin fecha/aleatoriedad/animación variable (FR-002).
- Ningún caso usa `.skip`/`.only` (FR-007).
- No se modifica ningún componente de producción (FR-006).
- La suite completa (`vitest run`) pasa en verde incluyendo los nuevos snapshots (SC-003).
