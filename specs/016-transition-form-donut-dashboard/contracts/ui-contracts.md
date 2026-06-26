# Contratos de UI — Phase 1

**Feature**: `016-transition-form-donut-dashboard` | **Date**: 2026-06-26

Esta feature no expone ni modifica contratos de API (reutiliza `GET /api/invoices/{id}`, `POST /api/invoices/transition/{id}` y `GET /api/invoices/stats` sin cambios). Los contratos relevantes son de **interfaz de usuario**: props, estados, eventos y comportamiento accesible de los componentes nuevos/editados.

---

## 1. Sistema de *toast*

### `ToastProvider`
- **Props**: `{ children: ReactNode }`.
- **Responsabilidad**: mantener la cola de `ToastMessage` y exponer `ToastApi` por contexto.
- **Montaje**: envuelve la app en `App.tsx` (por dentro o por fuera del router, pero por encima de las rutas que lo consumen).

### `useToast(): ToastApi`
- `success(message: string): void` — encola toast `success` (auto-cierre ~4s, descartable).
- `error(message: string): void` — encola toast `error` (persistente hasta descarte).
- `dismiss(id: string): void` — descarta un toast.
- **Contrato de error**: invocar `useToast` fuera de `ToastProvider` lanza un error de desarrollo claro.

### `ToastViewport`
- **Props**: ninguna (lee del contexto).
- **Montaje**: una sola instancia en `AppShell`.
- **Accesibilidad**:
  - Contenedor de éxitos: `role="status"` / `aria-live="polite"`.
  - Contenedor de errores: `role="alert"` / `aria-live="assertive"`.
  - Cada toast tiene botón de cierre con `aria-label="Cerrar notificación"`.
  - **No** roba el foco al aparecer.
- **Animación**: entrada/salida con Motion; con `prefers-reduced-motion` la transición es instantánea (`motionTransition(reduced)`), sin ocultar el contenido.
- **Posición/estilo**: fija (p. ej. esquina inferior derecha en escritorio, ancho completo seguro en móvil), consistente con el tema (dark mode incluido). No debe provocar desbordamiento horizontal (SC-009).

**Estados verificables**:
| Estado | Resultado esperado |
|--------|--------------------|
| `toast.success("…")` | Aparece toast en región `polite`; desaparece solo (~4s) o al cerrar. |
| `toast.error("…")` | Aparece toast en región `assertive`; permanece hasta cerrar. |
| Cierre manual | El toast desaparece al activar el botón de cierre (clic/teclado). |
| reduce-motion | Aparece/desaparece sin animación perceptible; sigue anunciándose. |

---

## 2. Formulario de transición manual (`ChangeStatusControl` editado)

- **Props** (sin cambios): `{ invoiceId: string; currentStatus: InvoiceStatus; allowedTransitions: InvoiceStatus[] }`.
- **Estructura**: `<form onSubmit={...}>` que contiene el `Select` de destinos y el botón "Cambiar Estado" (`type="submit"`).
- **Validación de cliente**:
  - Si `selected === ''` al enviar → `preventDefault`, se muestra mensaje de validación (`Selecciona un estado destino.`) asociado al `Select` vía `aria-describedby`, y **no** se llama a la mutación.
- **Envío**: con destino válido y no ocupado → `useTransitionInvoice().mutate({ id, newStatus })`.
- **Éxito**: `toast.success("Estado actualizado a «<etiqueta>».")`; limpiar selección y validación. El estado/historial/listado se actualizan por invalidación (sin recarga).
- **Error**: `toast.error(<motivo backend> || "No se pudo cambiar el estado.")`; **además** se conserva el mensaje de error inline persistente (`role="alert"`) ya existente.
- **Ocupado**: durante `isPending`, `Select` y botón deshabilitados; el botón muestra "Cambiando…"; envíos adicionales ignorados (anti doble-envío).
- **Estado terminal**: si `allowedTransitions.length === 0`, no se renderiza el formulario (mensaje informativo, ya existente).

**Escenarios verificables** (mapeo a spec):
| Escenario | Espec |
|-----------|-------|
| Enviar sin selección → mensaje, sin fetch | FR-003, SC-002, US1-1 |
| Enviar válido → estado ocupado, sin doble envío | FR-009, US1-2 |
| Éxito → toast éxito + estado/historial/listado actualizados | FR-005, FR-007, US1-3 |
| Error → toast error + inline persistente + sin cambio de estado | FR-006, FR-006a, FR-008, US1-4 |
| Terminal → formulario oculto/deshabilitado con motivo | FR-010, US1-5 |

---

## 3. Gráfico de **dona** por estado

### `DonutChart`
- **Props**: `{ data: ChartDatum[]; total: number; ariaLabel: string; centerLabel?: string }` (ver data-model §2).
- **Render**:
  - SVG `role="img"` con `aria-label={ariaLabel}` (resumen).
  - Pista (anillo de fondo) + un segmento por `datum` con `value > 0`, color tomado de `datum.color` (clase `stroke-*`).
  - Centro: número `total` (destacado, `tabular-nums`) + `centerLabel` (por defecto "Total").
  - **Leyenda** accesible: lista (`<ul>`) con un ítem por estado mostrando muestra de color, etiqueta y valor (y opcionalmente %). La leyenda es la fuente textual (no se depende del color).
- **Animación**: barrido de segmentos con Motion (`stroke-dashoffset`/`pathLength`), escalonado por índice; instantáneo con `prefers-reduced-motion`.
- **Casos límite**:
  | Caso | Resultado |
  |------|-----------|
  | `total === 0` / `data` vacío | Sin segmentos; centro muestra `0`; leyenda vacía o con ceros legibles; sin gráfico roto. |
  | Un único estado con valor | Un segmento que cubre el anillo completo. |
  | Estado desconocido | Segmento con color neutro; etiqueta con su valor en bruto. |

### `StatusDistributionChart` (editado)
- **Props** (sin cambios): `{ data: ChartDatum[] }` — ahora cada `datum` lleva `color` (clase por estado).
- **Comportamiento**: delega en `DonutChart` con `ariaLabel="Distribución de facturas por estado"`, `total` = suma de valores, `centerLabel="Total"`.

> `ClientDistributionChart` permanece como `BarChart` (sin cambios).

**Escenarios verificables**:
| Escenario | Espec |
|-----------|-------|
| Segmento por estado con color coherente con la insignia | FR-015, FR-016, US3-1 |
| Centro con total + etiqueta "Total" | FR-016a, US3-2 |
| Leyenda color↔estado↔valor accesible | FR-017, SC-007 |
| Animación de entrada / reduce-motion | FR-018, SC-008, US3-3 |
| Vacío (centro 0) y un solo estado (anillo completo) | FR-019, US3-4, US3-5 |

---

## 4. Ruteo y navegación

### `App.tsx`
- `<Route path="/" element={<DashboardPage/> (lazy, fallback DashboardSkeleton)} />`.
- **Sin** `<Route path="/dashboard">`.
- `<Route path="/facturas">` y `<Route path="/configuracion">` sin cambios.
- `<Route path="*" element={<Navigate to="/" replace />} />`.
- La app queda envuelta por `<ToastProvider>`.

### `navigation.ts`
- Ítem "Dashboard": `to: '/'` (antes `/dashboard`).

### `Sidebar` (`NavLink`)
- El `NavLink` cuya `to === '/'` usa `end` para que el resaltado activo aplique **solo** en la raíz (y no en `/facturas`, `/configuracion`).

**Escenarios verificables**:
| Escenario | Espec |
|-----------|-------|
| `"/"` renderiza el dashboard | FR-012, US2-1 |
| "Dashboard" activo en `"/"`, no en otras rutas | FR-013, US2-2 |
| `/dashboard` (eliminada) y rutas desconocidas → `"/"` | FR-012a, FR-014a, edge case |
| Facturas/Configuración siguen accesibles y la navegación es coherente | FR-014, US2-3 |
