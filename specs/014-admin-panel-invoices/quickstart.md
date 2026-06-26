# Quickstart — Validación del Panel de Administración (Layout + Listado de Facturas)

**Feature**: `014-admin-panel-invoices`

Guía para validar end-to-end que la feature cumple la spec. Asume backend y MongoDB disponibles (con datos de seed) y el frontend en `frontend/`.

## Prerrequisitos

- Backend corriendo (API en `/api`) con datos de facturas (ver seeder de desarrollo, spec 008).
- Node + dependencias del frontend instaladas. Esta feature añade:

```bash
cd frontend
npm i lucide-react @radix-ui/react-select @radix-ui/react-dialog @radix-ui/react-slot
```

- Componentes shadcn requeridos presentes en `src/components/ui/` (`button`, `table`, `input`, `select`, `badge`, `skeleton`, `sheet`).

## Arranque

```bash
cd frontend
npm run dev          # Vite en http://localhost:5173 (proxy /api → backend)
```

## Escenarios de validación

### 1. Layout base (US1)

1. Abrir `/`. **Esperado**: navbar con logo Monolegal, navegación lateral y pie de página visibles.
2. Reducir el ancho a tamaño móvil. **Esperado**: la navegación lateral se colapsa; el botón de menú la abre/cierra con animación; sin desbordamiento horizontal.
3. Verificar ítems de navegación: **Facturas** activo; **Dashboard** y **Configuración** deshabilitados ("próximamente").
4. Con el tema del sistema en oscuro (o una elección previa almacenada), recargar. **Esperado**: el panel se renderiza correctamente en ese tema.

### 2. Listado con estados de carga (US2)

1. Entrar a Facturas con la red ralentizada (DevTools → Slow 3G). **Esperado**: aparecen **skeletons** con la forma de la tabla, no pantalla en blanco.
2. Al resolver: tabla con columnas **ID, Cliente, Monto, Estado, Última Acción**; monto en formato moneda; fecha legible; estado como badge de color; entrada animada suave.
3. Forzar respuesta vacía (filtro sin coincidencias). **Esperado**: estado vacío con mensaje claro; filtros visibles.
4. Forzar error (apagar backend o interceptar 500). **Esperado**: mensaje de error legible + botón "Reintentar"; el shell sigue usable.

### 3. Filtro, búsqueda y paginación (US3)

1. Elegir un estado en el filtro. **Esperado**: la tabla muestra solo ese estado; el total y la paginación se actualizan; la página vuelve a 1.
2. Elegir "Todos". **Esperado**: vuelven todos los estados.
3. Escribir el identificador de un cliente. **Esperado**: tras el *debounce*, la tabla muestra solo sus facturas (búsqueda **global**, no limitada a la página); sin parpadeo por pulsación.
4. Con > 10 coincidencias, navegar páginas. **Esperado**: máximo 10 por página; controles anterior/siguiente coherentes; indicación de página/total.
5. Estando en página 2, cambiar filtro o búsqueda. **Esperado**: la paginación se reinicia a la página 1.

### 4. Experiencia, accesibilidad y calidad (US4)

1. Activar "reducir movimiento" en el SO. **Esperado**: las animaciones se atenúan/omiten; el panel sigue usable.
2. Navegar **solo con teclado** por navegación, filtro, búsqueda, paginación y filas. **Esperado**: todo alcanzable y operable; foco siempre visible.
3. Comprobar contraste y responsive en anchos de móvil y escritorio.

## Comandos de calidad (gates)

```bash
cd frontend
npm run lint                 # Biome: sin errores
npm run build                # tsc -b + vite build: sin errores de tipos
npm run test:run             # Vitest: pruebas de shell, tabla, filtro, búsqueda, paginación, skeleton, vacío/error, teclado
npx react-doctor@latest --verbose   # Objetivo: 100/100 SIN supresiones nuevas (FR-021/SC-006)
```

Backend (extensión del parámetro de búsqueda):

```bash
cd backend
dotnet test                  # ListInvoicesTests (search + status + paginación) y repo Mongo de búsqueda en verde
```

## Criterios de aceptación (resumen)

- [ ] Layout responsive con navbar/sidebar/footer y dark mode correcto (SC-005).
- [ ] Skeletons en toda carga; nunca pantalla en blanco (SC-002).
- [ ] Máx. 10 ítems/página; filtro y **búsqueda global** coherentes con la paginación (SC-003).
- [ ] Panel 100% operable por teclado con foco visible (SC-004).
- [ ] React Doctor 100/100 **honesto** (SC-006); Biome y build sin errores.
- [ ] TTI < 2s y rendimiento > 90 en auditoría (SC-007).
- [ ] Con reduce-motion, ninguna animación impide completar tareas (SC-008).

Detalles de contrato en [`contracts/list-invoices-search.md`](./contracts/list-invoices-search.md) y [`contracts/ui-contracts.md`](./contracts/ui-contracts.md); modelo en [`data-model.md`](./data-model.md).
