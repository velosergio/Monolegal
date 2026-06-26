# Plan de Implementación: Panel de Administración — Layout Base y Listado de Facturas

**Branch**: `014-admin-panel-invoices` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/014-admin-panel-invoices/spec.md`

## Summary

Construir la primera versión navegable del panel de administración: un **layout base** (navbar con logo, navegación lateral, pie de página, responsive y modo claro/oscuro) y la **página de listado de facturas** (tabla ID/Cliente/Monto/Estado/Última Acción, filtro por estado, búsqueda global por cliente, paginación de 10/página y skeleton loaders), con una capa transversal de **experiencia pulida** (componentes shadcn/ui consistentes, animaciones Motion que respetan "reducir movimiento", transiciones suaves, accesibilidad WCAG A) y un objetivo de calidad medible: **100/100 honesto en React Doctor**.

Enfoque técnico: el frontend ya está inicializado (React 19 + Vite + Tailwind v4 CSS-first, shadcn/ui estilo *new-york* con `components.json`, alias `@/`, Biome, Vitest + Testing Library, Motion y TanStack Query). Esta feature aporta cuatro bloques:

1. **Shell del panel** (`AppShell`: navbar + sidebar + footer + contenedor responsive) usando primitivas shadcn (`sheet` para el menú móvil, `button`), `lucide-react` para iconos, y Motion para las transiciones de apertura/cierre del menú. La navegación muestra **Facturas** activa y **Dashboard/Configuración deshabilitadas** ("próximamente").
2. **Página de Facturas** (`InvoicesPage`) que orquesta datos vía TanStack Query: estado de URL/vista (estado + búsqueda + página) → `useInvoices` → tabla (`table`), filtro (`select`), búsqueda (`input` con *debounce*), paginación, `badge` de estado, `skeleton` de carga, estados vacío/error con reintento.
3. **Capa de datos** (`features/invoices/api`): `getInvoices` que llama a `GET /api/invoices?status=&search=&page=&pageSize=` y `useInvoices` (TanStack Query con `keepPreviousData` para paginación sin parpadeo).
4. **Extensión acotada del backend**: añadir el parámetro **`search`** (búsqueda por `clientId`, case-insensitive) al endpoint de listado, propagado por `ListInvoicesQuery` → `IInvoiceRepository.GetPagedAsync` → filtro Mongo combinado con el estado; y añadir **`LastStatusTransitionAt`** al `InvoiceListItemDto` para alimentar la columna "Última Acción".

Brecha detectada en research: el `InvoiceListItemDto` actual devuelve `CreatedAt`, pero la columna "Última Acción" y el tipo `Invoice` del frontend esperan `lastStatusTransitionAt`. Se resuelve añadiendo el campo al DTO (sin colección ni mapeo nuevo).

## Technical Context

**Language/Version**: TypeScript 6 (strict) sobre React 19 + Vite 8 (frontend). C# / .NET 10 (extensión acotada del backend para el parámetro de búsqueda).

**Primary Dependencies**:
- Existentes (frontend): `react@19`, `@tanstack/react-query@5`, `motion@12`, `tailwindcss@4` (`@tailwindcss/vite`), `class-variance-authority`, `clsx`, `tailwind-merge`. shadcn/ui *new-york* (`components.json`), Biome, Vitest + Testing Library.
- **Nuevas (frontend)**: `lucide-react` (iconos; `iconLibrary: lucide` en `components.json`), `@radix-ui/react-select` (filtro de estado), `@radix-ui/react-dialog` (menú lateral móvil vía `sheet`), `@radix-ui/react-slot` (composición de `button`/`asChild` en componentes shadcn). Componentes shadcn a generar: `table`, `input`, `select`, `badge`, `skeleton`, `sheet`, `dropdown-menu` (si se requiere) — además del `button` existente.
- Backend: sin dependencias nuevas (MongoDB.Driver + FluentValidation ya en uso).

**Storage**: MongoDB (sin cambios de esquema). La búsqueda por cliente es un filtro adicional (regex case-insensitive sobre `ClientId`) combinado con el filtro de estado existente en `GetPagedAsync`. La columna "Última Acción" usa `LastStatusTransitionAt`, ya persistido en el documento.

**Testing**: Vitest + Testing Library (frontend): pruebas de componentes para shell (navbar/sidebar/footer/responsive), tabla, filtro, búsqueda (con *debounce*), paginación, skeleton, estados vacío/error, accesibilidad de teclado. xUnit + Shouldly (backend): `ListInvoicesTests` para el nuevo parámetro `search` (combinación con status/paginación, normalización, validación) y pruebas de repositorio Mongo del filtro de búsqueda.

**Target Platform**: SPA servida por Vite/estáticos detrás del backend (proxy `/api`). Navegadores modernos; responsive móvil/escritorio. Modo claro/oscuro por clase `.dark` (gestionado por `ThemeProvider`).

**Project Type**: Web (frontend SPA + servicio backend por capas). Esta feature toca principalmente `frontend/src` y, de forma acotada, `backend` (Api/Application/Domain/Infrastructure) para el parámetro de búsqueda y el campo del DTO.

**Performance Goals**: TTI < 2s y rendimiento Lighthouse > 90 (Constitución V). Bundle principal < 50KB gzip (Constitución Performance) → *code splitting* por sección (la página de Facturas se carga *lazy*), tree-shaking de Radix/lucide y Motion importado selectivamente. Paginación server-side (sin traer listados sin límite). Listas de transición suaves sin *layout shift* perceptible.

**Constraints**:
- TypeScript strict sin `any`; Biome 100% *compliant*; **React Doctor 100/100 honesto** (sin suprimir avisos para inflar — FR-021/SC-006).
- Accesibilidad WCAG A: foco visible, operable por teclado, roles/labels correctos; animaciones respetan `prefers-reduced-motion` (ya hay regla base en `index.css`).
- Dark mode *built-in* (no retrofit). Documentación en español (Constitución III).
- La extensión del backend mantiene Arquitectura Limpia: el filtro concreto vive en Infrastructure (Mongo), el contrato en Domain (firma del repositorio), la validación/normalización en Application, el parámetro HTTP en Api.

**Scale/Scope**: Decenas–miles de facturas (paginación obligatoria). Alcance acotado: roadmap 4.1 + 4.2. Fuera de alcance: detalle en modal (4.3), dashboard (4.4), transición manual desde UI (4.5) y el control visible de tema (diferido a Configuración).

### Unknowns resueltos (ver research.md)

| Tema | Estado |
|------|--------|
| ¿Enrutamiento multi-ruta o sección única? | Resuelto → sin router; shell con sección Facturas activa, resto deshabilitado (D1) |
| Búsqueda global por cliente sin tocar contrato actual | Resuelto → extender endpoint con `search` server-side (D2) |
| Desfase DTO (`CreatedAt`) vs. columna "Última Acción" | Resuelto → añadir `LastStatusTransitionAt` al `InvoiceListItemDto` (D3) |
| Estrategia de animación (Motion) + reduce-motion | Resuelto → variantes Motion + hook `useReducedMotion` (D4) |
| Filtro/búsqueda/paginación sin parpadeo | Resuelto → TanStack Query con `placeholderData: keepPreviousData` + *debounce* de búsqueda (D5) |
| Componentes shadcn a incorporar y deps Radix/lucide | Resuelto → table/input/select/badge/skeleton/sheet + lucide-react (D6) |
| Cómo lograr 100/100 honesto en React Doctor | Resuelto → playbook react-doctor; sin `biome-ignore`/supresiones nuevas (D7) |

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | Frontend organizado por feature (`features/invoices`, componentes de layout en `components/layout`), límites claros de hooks/estado. La extensión backend confina el filtro concreto a Infrastructure; contrato en Domain; validación en Application; parámetro en Api. Un cambio de almacenamiento no se propaga a la UI. | ✅ PASS |
| II. SOLID | SRP: `AppShell` solo compone layout; `useInvoices` solo obtiene datos; `InvoicesTable`/`StatusFilter`/`ClientSearch`/`Pagination` con responsabilidad única. OCP/DIP: la UI depende de la forma del DTO (tipo `Invoice`), no de la implementación de fetch. | ✅ PASS |
| III. SDD (specs en español) | Spec 014 escrita, clarificada y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Se escriben primero pruebas Vitest (shell, tabla, filtro, búsqueda con debounce, paginación, skeleton, vacío/error, teclado) y xUnit del parámetro `search` antes de implementar. | ✅ PASS |
| V. Frontend Producción | TS strict (config existente), Biome recomendado, **React Doctor 100/100 honesto**, WCAG A, dark mode *built-in*, TTI<2s / Lighthouse>90, responsive real. | ✅ PASS (objetivo verificado al final con React Doctor) |
| VI. Observable y Mantenible | `ErrorBoundary` para degradación elegante; estados de error legibles con reintento; sin `console.*` en producción (solo dev). DI implícita por props/hooks. | ✅ PASS |
| Stack tecnológico | shadcn/ui + Motion + TanStack Query + Vite ya mandatados; nuevas deps (lucide-react, primitivas Radix de select/dialog/slot) son las dependencias estándar de los componentes shadcn. Sin webpack, sin estado global innecesario. | ✅ PASS |
| Seguridad | Sin secretos en frontend; acceso del panel protegido por capa previa (rol Admin) fuera de alcance. La búsqueda usa parámetro validado/normalizado server-side (sin inyección: filtro tipado del driver Mongo). | ✅ PASS |
| Performance & Escalabilidad | Paginación server-side (sin queries sin límite); *code splitting* por sección; `keepPreviousData` evita parpadeo; bundle principal objetivo < 50KB gzip. | ✅ PASS |

**Resultado del gate**: PASS. Ningún principio NO NEGOCIABLE se incumple. La única dependencia transversal (parámetro `search` en backend) respeta la dirección de dependencias de la Arquitectura Limpia.

## Project Structure

### Documentation (this feature)

```text
specs/014-admin-panel-invoices/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas (routing, búsqueda, shadcn, motion, calidad)
├── data-model.md        # Phase 1 — modelo de vista + extensión del DTO/repositorio
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/
│   ├── list-invoices-search.md   # Contrato del endpoint de listado extendido (status + search + paginación)
│   └── ui-contracts.md           # Contratos de componentes/página del panel (props, estados, a11y)
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   ├── ui/                              # Primitivas shadcn/ui
│   │   │   ├── button.tsx                   # (EXISTE)
│   │   │   ├── table.tsx                    # (NUEVO) shadcn table
│   │   │   ├── input.tsx                    # (NUEVO) shadcn input
│   │   │   ├── select.tsx                   # (NUEVO) shadcn select (Radix)
│   │   │   ├── badge.tsx                    # (NUEVO) shadcn badge (estado)
│   │   │   ├── skeleton.tsx                 # (NUEVO) shadcn skeleton
│   │   │   └── sheet.tsx                    # (NUEVO) shadcn sheet (menú móvil, Radix dialog)
│   │   ├── layout/
│   │   │   ├── AppShell.tsx                 # (NUEVO) compone navbar + sidebar + footer + main
│   │   │   ├── Navbar.tsx                   # (NUEVO) logo Monolegal + disparador de menú móvil
│   │   │   ├── Sidebar.tsx                  # (NUEVO) navegación (Facturas activa; Dashboard/Config deshabilitados)
│   │   │   └── Footer.tsx                   # (NUEVO) info de la app (nombre/versión/año)
│   │   ├── feedback/
│   │   │   └── ErrorBoundary.tsx            # (NUEVO) degradación elegante (Constitución VI)
│   │   └── theme-provider.tsx               # (EXISTE) dark mode
│   ├── features/invoices/
│   │   ├── api/
│   │   │   ├── getInvoices.ts               # (NUEVO) fetch GET /api/invoices?status&search&page&pageSize
│   │   │   ├── useInvoices.ts               # (NUEVO) useQuery con keepPreviousData
│   │   │   └── payInvoice.ts                # (EXISTE)
│   │   ├── components/
│   │   │   ├── InvoicesPage.tsx             # (NUEVO) orquesta estado de vista + datos + animación
│   │   │   ├── InvoicesTable.tsx            # (REFACTOR de InvoiceList) tabla shadcn + columnas
│   │   │   ├── InvoicesTableSkeleton.tsx    # (NUEVO) skeleton con forma de la tabla
│   │   │   ├── StatusFilter.tsx             # (NUEVO) select de estado (+ "Todos")
│   │   │   ├── ClientSearch.tsx             # (NUEVO) input de búsqueda con debounce
│   │   │   ├── InvoicesPagination.tsx       # (NUEVO) controles de paginación
│   │   │   ├── StatusBadge.tsx              # (EXTRAÍDO) badge de color por estado
│   │   │   └── InvoicesEmptyState.tsx       # (NUEVO) estado vacío / sin coincidencias
│   │   ├── hooks/
│   │   │   └── useInvoicesViewState.ts      # (NUEVO) estado de vista: status + search + page (reset en cambios)
│   │   └── types.ts                         # (EDITADO) Invoice + paginación; alineado con DTO
│   ├── hooks/
│   │   ├── use-theme.ts                     # (NUEVO) hook para consumir ThemeProvider
│   │   └── use-debounced-value.ts           # (NUEVO) debounce para la búsqueda
│   ├── lib/
│   │   ├── utils.ts                         # (EXISTE) cn()
│   │   └── query-client.ts                  # (EDITADO) defaults: retry/staleTime
│   ├── App.tsx                              # (REFACTOR) renderiza AppShell + InvoicesPage (lazy)
│   ├── main.tsx                             # (EXISTE) providers
│   └── index.css                            # (EXISTE) tokens shadcn + reduce-motion
└── tests/                                   # Vitest + Testing Library (co-ubicados o en tests/)

backend/   (extensión acotada — habilita FR-012 búsqueda global y "Última Acción")
├── Domain/Repositories/IInvoiceRepository.cs        # (EDITADO) GetPagedAsync(... string? clientSearch ...)
├── Application/Validation/ListInvoicesQueryValidator.cs # (EDITADO) ListInvoicesQuery + 'search' (trim/normalización)
├── Infrastructure/Repositories/MongoInvoiceRepository.cs # (EDITADO) filtro regex case-insensitive ClientId + status
├── Api/Endpoints/Invoices/ListInvoices.cs           # (EDITADO) acepta y propaga 'search'
├── Api/Endpoints/Invoices/InvoiceDtos.cs            # (EDITADO) InvoiceListItemDto + LastStatusTransitionAt
└── Tests/                                            # (EDITADO/NUEVO) ListInvoicesTests + repo Mongo search
```

**Structure Decision**: SPA por feature ya establecida en `frontend/src` (alias `@/`, shadcn/ui *new-york*). El layout vive en `components/layout` (reutilizable por futuras secciones), las primitivas en `components/ui`, y la lógica de facturas en `features/invoices` con separación clara `api`/`components`/`hooks`. No se introduce router: con una sola sección funcional (Facturas) el shell renderiza la página directamente y marca el resto como "próximamente"; la estructura permite incorporar enrutamiento *lazy* por ruta sin reescritura cuando lleguen 4.3–4.5. La extensión del backend se confina por capas para preservar la Arquitectura Limpia.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. La feature es principalmente de frontend; la única extensión backend (parámetro `search` + campo `LastStatusTransitionAt` en el DTO) es mínima, se confina por capas y habilita un requisito explícito (búsqueda global, FR-012) y la semántica correcta de la columna "Última Acción". Las dependencias nuevas son las primitivas estándar que requieren los componentes shadcn/ui (Radix select/dialog/slot) y la librería de iconos declarada en `components.json` (lucide-react).
