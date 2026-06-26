# Quickstart — Validación end-to-end

**Feature**: `016-transition-form-donut-dashboard` | **Date**: 2026-06-26

Guía para validar que la feature funciona de extremo a extremo. No incluye código de implementación; los detalles de tipos/props están en [data-model.md](./data-model.md) y [contracts/ui-contracts.md](./contracts/ui-contracts.md).

## Prerrequisitos

- Backend y MongoDB en marcha (p. ej. `docker compose up`) con datos de ejemplo (seed) que incluyan facturas en varios estados.
- Frontend en `frontend/`.

## Puesta en marcha (frontend)

```bash
cd frontend
npm install            # sin dependencias nuevas: no debe cambiar package.json
npm run dev            # http://localhost:5173 (proxy /api → backend)
```

## Comandos de calidad (deben pasar antes de dar por hecha la feature)

```bash
cd frontend
npm run test:run       # Vitest: formulario, toast, donut, ruteo, a11y
npm run lint           # Biome 100% compliant
npm run build          # tsc -b + vite build (TS strict, sin errores)
npm run doctor         # React Doctor 100/100 honesto (sin supresiones)
```

## Escenarios de validación manual

### A. Dashboard como pantalla de inicio (US2)
1. Abrir `http://localhost:5173/` → **se muestra el Dashboard** (no el listado de facturas).
2. En la navegación lateral, "Dashboard" aparece **resaltado** como activo.
3. Navegar a "Facturas" y volver: la navegación es coherente; "Dashboard" deja de estar activo en `/facturas` y vuelve a activarse en `/`.
4. Abrir directamente `http://localhost:5173/dashboard` (ruta eliminada) o una ruta inexistente → **redirige a `/`** (dashboard), sin error.

### B. Gráfico de dona por estado (US3)
1. En el dashboard con datos, la tarjeta "Facturas por estado" muestra un **donut** con un **segmento por estado**.
2. Cada segmento usa el **mismo color** que la insignia de ese estado en el listado/modal.
3. El **centro** muestra el **total** de facturas y la etiqueta "Total".
4. Existe una **leyenda** que asocia color ↔ estado ↔ valor (legible en español).
5. Al cargar, el donut **anima** su entrada; con "reducir movimiento" del SO activo, aparece sin animación.
6. Con la base de datos sin facturas: el centro muestra `0` y no hay gráfico roto. Con un solo estado presente: el anillo se ve completo.

### C. Formulario de transición manual con toast (US1)
1. Abrir el listado `/facturas`, activar una fila para abrir el modal de detalle de una factura con transiciones válidas.
2. En el formulario "Cambiar Estado", pulsar el botón **sin** seleccionar destino → aparece **mensaje de validación** y **no** se realiza ninguna petición (verificar en la pestaña Red).
3. Seleccionar un destino permitido y confirmar:
   - El control queda **ocupado** ("Cambiando…") y no admite doble envío.
   - Al éxito: aparece un **toast de éxito**; el **estado** mostrado, el **historial** del modal y la **fila** del listado reflejan el nuevo estado **sin recargar**.
4. Forzar un error (p. ej. detener el backend o provocar un rechazo): al confirmar aparece un **toast de error** y, además, un **mensaje inline persistente** en el formulario; el estado mostrado **no** cambia.
5. Abrir el modal de una factura en estado terminal (pagado/desactivado): el formulario de cambio **no** se muestra (o está deshabilitado) con una indicación del motivo.

### D. Accesibilidad y responsive (transversal)
1. Operar el formulario, los toasts y la navegación **solo con teclado** (Tab/Enter/Escape); el foco es siempre visible.
2. Los toasts se **anuncian** por lector de pantalla (región `aria-live`) y no roban el foco.
3. En anchos de móvil y escritorio: el dashboard, el donut, la leyenda y los toasts se ven **sin desbordamiento horizontal** ni solapamientos.

## Criterios de aceptación (resumen)

- Inicio `/` = dashboard; `/dashboard` y rutas desconocidas → `/` (FR-012, FR-012a, FR-014a).
- Donut por estado con colores coherentes, total al centro y leyenda accesible (FR-015–FR-019).
- Formulario con validación de cliente, toast de éxito/error e inline persistente en error, con actualización coherente sin recargar (FR-001–FR-011).
- React Doctor 100/100 honesto, Biome verde, build sin errores, sin dependencias de runtime nuevas (FR-021, SC-010).
