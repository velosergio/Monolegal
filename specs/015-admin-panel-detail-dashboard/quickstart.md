# Quickstart — Validación de Detalle de Factura (Modal) y Dashboard (spec 015)

Guía de validación end-to-end. Asume el monorepo ya configurado (backend .NET 10 + MongoDB, frontend Vite). Referencias: [plan.md](./plan.md), [data-model.md](./data-model.md), [contracts/](./contracts/).

## Prerrequisitos

- MongoDB en ejecución y datos de desarrollo sembrados (seeder de la spec 008).
- Backend levantado con los endpoints de facturas (`/api/invoices`, `/api/invoices/{id}`, `/api/invoices/transition/{id}`, `/api/invoices/stats`).
- Frontend con dependencias instaladas.

## Comandos

```bash
# Backend (desde backend/)
dotnet build
dotnet test                 # dominio + Api + Infrastructure (incluye nuevas pruebas de historial/destinos)
dotnet run --project Api    # levanta la API

# Frontend (desde frontend/)
npm install
npm run test                # Vitest + Testing Library (modal, dashboard)
npm run dev                 # levanta el panel
```

## Escenarios de validación

### A. Modal de detalle (US1) — FR-001..FR-006

1. Abrir el panel en `/facturas`.
2. Hacer clic en una fila → se abre el modal; la URL refleja `?factura=<id>`.
3. **Esperado**: durante la carga se ven skeletons; al resolver, se muestran todos los campos (id abreviado, cliente, monto como moneda, estado como etiqueta, fechas en español, nº de recordatorios, último recordatorio, última transición).
4. Cerrar con botón, `Escape` y clic fuera → el modal se cierra, la URL pierde `?factura`, el foco vuelve a la fila.
5. Forzar un fallo de red del detalle → mensaje de error + reintento, sin romper el listado.

### B. Historial de cambios de estado (US2) — FR-007..FR-011

1. Abrir el modal de una factura con varias transiciones.
2. **Esperado**: línea de tiempo con cada cambio (`from → to`, fecha/hora, origen automático/manual), ordenada de forma clara.
3. Abrir el modal de una factura **previa a la feature** (sin historial) → se muestra el evento de creación derivado de `createdAt` (no una sección vacía).

### C. Cambio de estado desde el modal (US3) — FR-012..FR-017

1. Abrir una factura en `pending` → el control ofrece solo `1er Recordatorio` y `Pagado` (de `allowedTransitions`).
2. Elegir `1er Recordatorio` y confirmar → estado actualizado, nuevo evento `manual` en el historial, y el listado de fondo coherente; sin recargar la página.
3. Abrir una factura `pagado` (terminal) → el botón "Cambiar Estado" está oculto/deshabilitado con indicación.
4. Simular un `400` (transición no permitida) → mensaje de error, estado mostrado sin cambios, detalle refrescado.

### D. Dashboard (US4) — FR-018..FR-023

1. Habilitar "Dashboard" en la navegación y entrar a `/dashboard`.
2. **Esperado**: skeletons durante la carga; luego tarjetas (total, por estado, por cliente), gráficos animados (Motion) y un indicador de "último refresh".
3. Verificar el desglose "por cliente" con muchos clientes → top-N + "Otros", legible y sin desbordar.
4. Con base de datos vacía → ceros legibles + estado vacío en gráficos.
5. Forzar fallo de `/stats` → mensaje de error + reintento.

### E. Navegación al dashboard (US5)

1. Desde `/facturas`, abrir la navegación lateral → "Dashboard" habilitado.
2. Seleccionarlo → navega a `/dashboard`, sección activa resaltada; volver a "Facturas" funciona en escritorio y móvil.

### F. Accesibilidad, animación y calidad (transversal) — FR-024..FR-028

1. Navegar solo con teclado: abrir modal, recorrer campos/historial/control de estado, cerrar; navegar al dashboard. Foco siempre visible; foco atrapado dentro del modal y devuelto al cerrar.
2. Activar "reducir movimiento" en el sistema → animaciones de modal y gráficos se atenúan/desactivan sin impedir el uso.
3. Ejecutar la inspección de React del proyecto (React Doctor) → **100/100 honesto** sin avisos suprimidos.
4. Verificar TTI < 2s y rendimiento > 90 en auditoría estándar; sin desbordamiento horizontal en móvil/escritorio.

## Criterios de aceptación (resumen)

- [ ] El modal muestra todos los campos, el historial y el control de cambio de estado a partir de un único `GET /api/invoices/{id}`.
- [ ] El cambio de estado solo ofrece destinos válidos del backend y, al aplicarlo, sincroniza estado + historial + listado sin recargar.
- [ ] El dashboard muestra tarjetas, gráficos animados y último refresh, con estados de carga/vacío/error claros.
- [ ] Accesible por teclado, respeta reduce-motion, y React Doctor reporta 100/100 honesto.
