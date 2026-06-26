# Plan de Implementación: Panel de Administración — Detalle de Factura (Modal) y Dashboard de Estadísticas

**Branch**: `015-admin-panel-detail-dashboard` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/015-admin-panel-detail-dashboard/spec.md`

## Summary

Añadir al panel de administración (sobre el listado de la spec 014) dos capacidades:

1. **Modal de detalle de factura** (roadmap 4.3): al activar una fila se abre un diálogo que muestra **todos los campos** de la factura, su **historial completo de cambios de estado** (línea de tiempo), y un control para **cambiar el estado** que solo ofrece las transiciones válidas y ejecuta el cambio dentro del propio modal. Todo se alimenta con un único `GET /api/invoices/{id}` y se mantiene fresco vía TanStack Query.
2. **Dashboard de estadísticas** (roadmap 4.4): una sección `/dashboard` con tarjetas (total, por estado, por cliente), **gráficos animados con Motion** e indicación del **último refresh**.

**Enfoque técnico**: El frontend ya está montado (React 19 + Vite 8 + Tailwind v4 + shadcn/ui *new-york*, TanStack Query, Motion, react-router v7, Biome, Vitest). El dashboard es **frontend-only** (el endpoint `GET /api/invoices/stats` ya devuelve `total/byStatus/byClient`; el "último refresh" sale de `dataUpdatedAt`). El modal requiere **dos extensiones acotadas de backend**, ambas confinadas por capas según Arquitectura Limpia:

- **Historial de transiciones (audit log)**: hoy la entidad `Invoice` solo guarda `LastStatusTransitionAt`. Se añade una lista **embebida** `StatusHistory` (value object `StatusChange { from, to, at, source }`) que se *appendea* en el único punto de mutación de estado (`Invoice.UpdateStatus`). Como **ambas** rutas de cambio (endpoint manual `TransitionInvoice` y `InvoiceTransitionsWorker`) persisten con `UpdateAsync` (reemplazo completo del documento) y el `Invoice` se mapea como POCO directo a Mongo, el historial se persiste sin tocar la capa de Infrastructure. El origen (`Automatic`/`Manual`) se propaga desde el call site a través de `InvoiceTransitionService`.
- **Destinos válidos por factura**: la matriz de transiciones vive en `InvoiceTransitionService`. Se añade un método puro `GetAllowedTransitions(status)` y se expone `allowedTransitions` en `InvoiceDetailDto`, evitando duplicar la matriz en el frontend (Constitución I/II). Un solo `GET /api/invoices/{id}` entrega campos + historial + destinos válidos.

Brechas detectadas en research y **eliminación de legacy** (clarificación 2026-06-26, "no dejar nada en legacy", research D9): (a) el método muerto `IInvoiceRepository.UpdateStatusAsync` (`$set` parcial que podría saltarse el historial) **se elimina por completo** (interfaz, impl, fakes y tests); ninguna ruta de producción lo usa. (b) Las facturas existentes sin `StatusHistory` reciben su evento de creación mediante una **migración única e idempotente** (la UI mantiene la derivación desde `CreatedAt` como respaldo). (c) Los estados legacy `Draft/Overdue/Cancelled` **se retiran de `InvoiceStatus`**; la misma migración **remapea** primero los documentos que los tengan a un estado activo válido y el constructor de `Invoice` inicia en `Pending` (FR-029, FR-030, FR-031).

## Technical Context

**Language/Version**: TypeScript 6 (strict) sobre React 19 + Vite 8 (frontend). C# / .NET 10 (extensión acotada del backend para historial y destinos válidos).

**Primary Dependencies**:
- Existentes (frontend): `react@19`, `react-dom@19`, `@tanstack/react-query@5`, `motion@12`, `react-router-dom@7`, `tailwindcss@4` (`@tailwindcss/vite`), `@radix-ui/react-dialog` (ya presente), `@radix-ui/react-select` (ya presente), `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`. Biome, Vitest + Testing Library.
- **Nuevas (frontend)**: ninguna dependencia de runtime nueva. Componentes shadcn a generar localmente: `dialog` (modal, sobre el `@radix-ui/react-dialog` ya instalado) y, si se requiere, `card`/`separator`/`tooltip` (primitivas ligeras). Los **gráficos se construyen in-house con SVG + Motion** (sin librería de charting), satisfaciendo "gráficos motion animados" sin penalizar el bundle.
- Backend: sin dependencias nuevas (MongoDB.Driver + FluentValidation ya en uso).

**Storage**: MongoDB. Se añade el campo embebido `StatusHistory` (lista de `StatusChange`) al documento `Invoices`; se serializa automáticamente vía el POCO `Invoice`. **Sin nueva colección ni índice**. La serialización del enum `StatusChangeSource` se alinea con la convención existente (string). El dashboard no toca el almacenamiento (reutiliza los agregados `CountByStatusAsync`/`CountByClientAsync`).

**Testing**:
- Frontend (Vitest + Testing Library): modal (apertura desde fila, campos, cierre por botón/escape/overlay y retorno de foco), timeline de historial (orden, evento de creación, vacío), control de cambio de estado (destinos válidos, oculto/deshabilitado en estado terminal, éxito → refresco de estado+historial+listado, error 400), dashboard (skeletons, tarjetas, gráficos animados, último refresh, vacío, error), navegación al dashboard, accesibilidad de teclado, `prefers-reduced-motion`.
- Backend (xUnit + Shouldly): dominio — `UpdateStatus` registra `StatusChange` con `from/to/at/source`; `GetAllowedTransitions` para cada estado (incluida la inclusión de `Pagado` y el conjunto vacío en estados terminales). Api — `GetInvoiceById` y `TransitionInvoice` devuelven `statusHistory` y `allowedTransitions` correctos. Infrastructure — round-trip Mongo del historial embebido (persistencia y lectura) y que la transición vía `UpdateAsync` conserva el historial acumulado.

**Target Platform**: SPA servida por Vite/estáticos detrás del backend (proxy `/api`). Navegadores modernos; responsive móvil/escritorio. Modo claro/oscuro por clase `.dark` (`ThemeProvider`).

**Project Type**: Web (frontend SPA + servicio backend por capas Domain/Application/Infrastructure/Api). Esta feature toca principalmente `frontend/src` (modal y dashboard) y, de forma acotada, `backend` (Domain + Api) para el historial y los destinos válidos.

**Performance Goals**: TTI < 2s y Lighthouse > 90 (Constitución V). Bundle principal < 50KB gzip → la ruta `/dashboard` y el modal se cargan *lazy*; los gráficos in-house (SVG + Motion ya importado) evitan una dependencia de charting pesada. Un único fetch por modal (detalle con historial+destinos) minimiza *round-trips*. Mutación de cambio de estado con invalidación dirigida (sin recargar la página).

**Constraints**:
- TypeScript strict sin `any`; Biome 100% *compliant*; **React Doctor 100/100 honesto** (sin suprimir avisos — FR-028/SC-010).
- Accesibilidad WCAG A: foco visible, operable por teclado, *focus trap* del modal con Radix dialog y retorno de foco a la fila de origen; animaciones respetan `prefers-reduced-motion` (regla base en `index.css` + `useReducedMotion`).
- Dark mode *built-in*. Documentación en español (Constitución III).
- La extensión del backend respeta la dirección de dependencias: el value object y la matriz de destinos viven en Domain; la exposición en Api; la serialización en Infrastructure. La lógica de validez **no** se replica en el frontend.

**Scale/Scope**: Decenas–miles de facturas; historial por factura del orden de unidades de eventos. Alcance: roadmap 4.3 + 4.4 (incluida la ejecución del cambio de estado, que absorbe lo necesario de 4.5). Fuera de alcance: cualquier alcance adicional de 4.5 no requerido por el cambio desde el modal.

### Unknowns resueltos (ver research.md)

| Tema | Estado |
|------|--------|
| Persistencia del historial: embebido vs. colección aparte; rutas de escritura; captura de origen; backfill | Resuelto → lista embebida en el agregado, *append* en `UpdateStatus(newStatus, source)`; ambas rutas usan `UpdateAsync`; histórico previo sembrado por migración de backfill (D1) |
| Eliminación de legacy (código + datos): método muerto, estados legacy del enum, backfill | Resuelto → eliminar `UpdateStatusAsync`; retirar `Draft/Overdue/Cancelled` del enum; migración única e idempotente (remapeo + backfill); constructor inicia en `Pending` (D9) |
| Exposición de destinos válidos: endpoint dedicado vs. campo en el DTO de detalle | Resuelto → `GetAllowedTransitions` en el dominio + `allowedTransitions` en `InvoiceDetailDto` (un solo fetch) (D2) |
| Estrategia de gráficos: librería de charting vs. SVG + Motion in-house | Resuelto → gráficos in-house con SVG + Motion (sin dep nueva, respeta reduce-motion y presupuesto de bundle) (D3) |
| Mecanismo de apertura/selección del modal y gestión de foco | Resuelto → estado de selección en *search param* `?factura=<id>` + shadcn `dialog` (Radix) con *focus trap* y retorno de foco (D4) |
| Coherencia tras cambio de estado (modal + listado + dashboard) | Resuelto → `useMutation` con invalidación dirigida de `['invoice',id]`, `['invoices']` y `['invoice-stats']` (D5) |
| Dashboard "por cliente" con muchos clientes | Resuelto → top-N clientes + agrupación "Otros" en gráfico/tabla legible (D6) |
| "Último refresh" del dashboard | Resuelto → `dataUpdatedAt` de TanStack Query, formateado en español (D7) |
| 100/100 honesto en React Doctor | Resuelto → playbook react-doctor; sin supresiones nuevas (D8) |

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | Frontend por feature: `features/invoices` (modal de detalle, historial, cambio de estado) y nuevo `features/dashboard`. Backend: el historial (`StatusChange`, value object) y la matriz de destinos (`GetAllowedTransitions`) viven en **Domain**; la exposición en **Api** (DTO); la serialización en **Infrastructure** (POCO Mongo). Un cambio de almacenamiento no se propaga a la UI; la validez de transición no sale del dominio. | ✅ PASS |
| II. SOLID | SRP: `InvoiceDetailModal` compone; `StatusHistoryTimeline`, `ChangeStatusControl`, `DashboardPage`, `StatCard`, gráficos — cada uno con una responsabilidad. OCP/DIP: la UI depende de la forma del DTO (`InvoiceDetail`, `InvoiceStats`), no del fetch concreto; `GetAllowedTransitions` extiende la matriz sin que la UI la conozca. | ✅ PASS |
| III. SDD (specs en español) | Spec 015 escrita, clarificada y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Se escriben primero las pruebas: dominio (registro de historial, destinos válidos), Api (forma del DTO), frontend (modal, timeline, cambio de estado, dashboard, a11y) antes de implementar. | ✅ PASS |
| V. Frontend Producción | TS strict, Biome, **React Doctor 100/100 honesto**, WCAG A (focus trap/retorno de foco del modal), dark mode *built-in*, TTI<2s / Lighthouse>90, responsive real, `prefers-reduced-motion`. | ✅ PASS (verificado al final con React Doctor) |
| VI. Observable y Mantenible | `ErrorBoundary` ya existente envuelve las rutas; estados de error legibles con reintento (modal y dashboard); logging estructurado backend en los endpoints/servicio. Sin `console.*` en producción. | ✅ PASS |
| Stack tecnológico | shadcn/ui (`dialog` sobre Radix ya instalado) + Motion (gráficos) + TanStack Query (`useQuery`/`useMutation`) + react-router (ruta `/dashboard`). Sin dependencias de runtime nuevas. Backend sin dependencias nuevas. | ✅ PASS |
| Seguridad | Sin secretos en frontend; acceso del panel protegido por capa previa (rol Admin). El cambio de estado reusa el endpoint validado (FluentValidation + matriz de dominio); el frontend nunca decide la validez por su cuenta. | ✅ PASS |
| Performance & Escalabilidad | Ruta `/dashboard` y modal *lazy*; gráficos in-house (sin dep pesada); un fetch por modal; invalidación dirigida tras mutación; agregados de stats ya existentes (sin queries sin límite). | ✅ PASS |

**Resultado del gate**: PASS. Ningún principio NO NEGOCIABLE se incumple. Las dos extensiones de backend (historial embebido y destinos válidos) respetan la dirección de dependencias de la Arquitectura Limpia y mantienen el dominio como única fuente de verdad de la validez de transición.

## Project Structure

### Documentation (this feature)

```text
specs/015-admin-panel-detail-dashboard/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas (historial, destinos válidos, gráficos, modal, cache)
├── data-model.md        # Phase 1 — entidades/DTOs (StatusChange, InvoiceDetail extendido, InvoiceStats) + tipos frontend
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/
│   ├── invoice-detail.md         # GET /api/invoices/{id} extendido (statusHistory + allowedTransitions)
│   ├── transition-invoice.md     # POST /api/invoices/transition/{id} (respuesta extendida) — reuso
│   ├── invoice-stats.md          # GET /api/invoices/stats (reuso, sin cambios de contrato)
│   └── ui-contracts.md           # Contratos de componentes/página (props, estados, a11y) del modal y el dashboard
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   ├── ui/
│   │   │   ├── dialog.tsx                    # (NUEVO) shadcn dialog (sobre @radix-ui/react-dialog ya instalado)
│   │   │   ├── card.tsx                      # (NUEVO) tarjeta para stats del dashboard
│   │   │   ├── separator.tsx                 # (NUEVO, si se requiere) separadores del modal
│   │   │   ├── select.tsx / badge.tsx / skeleton.tsx / button.tsx  # (EXISTEN)
│   │   ├── layout/
│   │   │   └── navigation.ts                 # (EDITADO) Dashboard → disabled: false
│   │   └── feedback/ErrorBoundary.tsx        # (EXISTE)
│   ├── features/invoices/
│   │   ├── api/
│   │   │   ├── getInvoiceDetail.ts           # (NUEVO) GET /api/invoices/{id}
│   │   │   ├── useInvoiceDetail.ts           # (NUEVO) useQuery ['invoice', id]
│   │   │   ├── transitionInvoice.ts          # (NUEVO) POST /api/invoices/transition/{id}
│   │   │   └── useTransitionInvoice.ts       # (NUEVO) useMutation + invalidación dirigida
│   │   ├── components/
│   │   │   ├── InvoiceDetailModal.tsx        # (NUEVO) diálogo: orquesta detalle + historial + cambio de estado
│   │   │   ├── InvoiceDetailFields.tsx       # (NUEVO) todos los campos de la factura
│   │   │   ├── StatusHistoryTimeline.tsx     # (NUEVO) línea de tiempo del historial
│   │   │   ├── ChangeStatusControl.tsx       # (NUEVO) select de destinos válidos + confirmar
│   │   │   ├── InvoiceDetailSkeleton.tsx     # (NUEVO) skeleton del modal
│   │   │   ├── InvoicesTable.tsx             # (EDITADO) fila activable que abre el modal
│   │   │   └── InvoicesPage.tsx              # (EDITADO) monta el modal a partir del search param
│   │   ├── hooks/
│   │   │   └── useSelectedInvoice.ts         # (NUEVO) selección por ?factura=<id> (abrir/cerrar modal)
│   │   ├── types.ts                          # (EDITADO) InvoiceDetail, StatusChange, allowedTransitions
│   │   └── utils.ts                          # (EDITADO) formateadores (fecha/hora, origen de cambio)
│   ├── features/dashboard/
│   │   ├── api/
│   │   │   ├── getInvoiceStats.ts            # (NUEVO) GET /api/invoices/stats
│   │   │   └── useInvoiceStats.ts            # (NUEVO) useQuery ['invoice-stats']
│   │   ├── components/
│   │   │   ├── DashboardPage.tsx             # (NUEVO) orquesta tarjetas + gráficos + último refresh
│   │   │   ├── StatCard.tsx                  # (NUEVO) tarjeta de métrica
│   │   │   ├── StatusDistributionChart.tsx   # (NUEVO) gráfico por estado (SVG + Motion)
│   │   │   ├── ClientDistributionChart.tsx   # (NUEVO) gráfico por cliente (top-N + "Otros")
│   │   │   ├── LastRefreshIndicator.tsx      # (NUEVO) "último refresh" (dataUpdatedAt)
│   │   │   ├── DashboardSkeleton.tsx         # (NUEVO) skeleton de tarjetas/gráficos
│   │   │   └── DashboardEmptyState.tsx       # (NUEVO) sin datos
│   │   └── types.ts                          # (NUEVO) InvoiceStats
│   ├── lib/
│   │   ├── motion.ts                         # (EDITADO si hace falta) variantes para modal/gráficos
│   │   └── query-client.ts                   # (EXISTE)
│   ├── App.tsx                               # (EDITADO) añade <Route path="/dashboard"> (lazy)
│   └── index.css                             # (EXISTE)
└── tests/                                    # Vitest + Testing Library (co-ubicados)

backend/   (extensión acotada + eliminación de legacy — historial, destinos válidos y limpieza)
├── Domain/
│   ├── Enums/StatusChangeSource.cs           # (NUEVO) Automatic | Manual
│   ├── Enums/InvoiceStatus.cs                # (EDITADO) retirar Draft/Overdue/Cancelled (legacy, FR-031)
│   ├── Entities/StatusChange.cs              # (NUEVO) value object { From, To, At, Source }
│   ├── Entities/Invoice.cs                   # (EDITADO) StatusHistory + UpdateStatus(newStatus, source) append; constructor inicia en Pending
│   ├── Repositories/IInvoiceRepository.cs    # (EDITADO) eliminar UpdateStatusAsync (código muerto, FR-029)
│   └── Services/InvoiceTransitionService.cs  # (EDITADO) GetAllowedTransitions(status) + propaga el origen
├── Api/Endpoints/Invoices/
│   ├── InvoiceDtos.cs                        # (EDITADO) InvoiceDetailDto + StatusHistory + AllowedTransitions; StatusChangeDto
│   ├── GetInvoiceById.cs                     # (EDITADO) inyecta el servicio y mapea historial + destinos
│   └── TransitionInvoice.cs                  # (sin cambios de firma; su respuesta hereda los nuevos campos)
├── Infrastructure/
│   ├── Repositories/MongoInvoiceRepository.cs    # (EDITADO) eliminar UpdateStatusAsync; serialización del nuevo campo embebido
│   └── Hosting/StatusHistoryBackfillMigration.cs # (NUEVO) migración única e idempotente: remapeo de estados legacy + backfill de historial (FR-030/FR-031)
└── Tests/                                    # (NUEVO/EDITADO) dominio (historial, destinos, inicio en Pending), Api (DTO), Infra (round-trip, migración); ELIMINAR tests de UpdateStatusAsync (InvoiceRepositoryContractTests, MongoInvoiceRepositoryStatusUpdateTests) y fakes asociados
```

**Structure Decision**: Se mantiene la SPA por feature ya establecida. El modal y su lógica de datos viven en `features/invoices` (extienden el listado existente); el dashboard estrena `features/dashboard` con su propia `api`/`components`/`types`. La ruta `/dashboard` se añade al router existente (react-router v7, *lazy*) y la entrada de navegación se habilita. En backend, el historial y la matriz de destinos se confinan en Domain y se exponen vía Api, preservando la Arquitectura Limpia; Infrastructure solo serializa el nuevo campo embebido (sin migración de esquema explícita: las facturas previas se leen con historial vacío).

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. La feature es mayoritariamente de frontend (modal + dashboard). Las extensiones de backend son acotadas y se confinan por capas: el historial como lista embebida en el agregado `Invoice` y los destinos válidos como método puro del servicio de dominio expuesto en el DTO de detalle. No se introducen dependencias de runtime nuevas (los gráficos se construyen con SVG + Motion ya disponible).
>
> **Eliminación de legacy** (clarificación 2026-06-26, "no dejar nada en legacy"): se añade una migración única e idempotente (backfill de historial + remapeo de estados legacy) y se elimina código muerto (`UpdateStatusAsync`) y valores legacy del enum (`Draft/Overdue/Cancelled`). No es complejidad adicional injustificada sino **reducción de deuda**: simplifica el modelo de estados (un único conjunto activo), elimina una vía que podría evadir el historial (refuerza la invariante de FR-029) y garantiza que no queden datos en estados no soportados. Riesgo gestionado por el orden de la migración (remapear antes de retirar valores del enum) y la idempotencia; el mapeo de negocio de estados legacy queda marcado para confirmación en planificación. Considerar el impacto en specs previas (007/008) que asuman `Draft` como estado inicial.
