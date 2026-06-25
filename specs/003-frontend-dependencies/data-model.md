# Modelo de Datos (Fase 1): Dependencias Frontend

**Feature**: `003-frontend-dependencies` | **Fecha**: 2026-06-24

> Esta fase no introduce entidades de dominio de negocio. El "modelo" relevante es la **matriz dependencia → propósito → ubicación → estado**, que actúa como fuente de verdad verificable de la configuración del frontend. Las entidades de negocio (Factura, Cliente, etc.) corresponden a fases funcionales posteriores.

## Entidad: Dependencia Frontend

Representa cada paquete o configuración requerida por la spec.

**Atributos**:

- **nombre**: identificador del paquete o configuración.
- **propósito**: capacidad que habilita (mapea a una Historia de Usuario / FR).
- **ubicación**: dónde se declara (`dependencies`, `devDependencies`, archivo de configuración o código en repo).
- **versión objetivo**: línea mayor esperada según la constitución.
- **estado**: `presente` (verificar) | `a añadir` | `a configurar`.

## Matriz dependencia → propósito

| Dependencia / Config | Propósito (HU / FR) | Ubicación | Versión objetivo | Estado |
|----------------------|---------------------|-----------|------------------|--------|
| `react`, `react-dom` | UI base (HU1 / FR-001) | `dependencies` | 19+ | presente |
| `typescript` + `tsconfig.json` strict | Tipado estricto sin `any` (HU1 / FR-002) | `devDependencies` + config | strict on | presente |
| `vite`, `@vitejs/plugin-react` | Build / dev server (HU2 / FR-003) | `devDependencies` + `vite.config.ts` | 8.x | presente |
| `@tailwindcss/vite`, `tailwindcss` | Base de estilos para shadcn (HU3 / FR-004) | `dependencies` + `vite.config.ts` | v4 | a añadir |
| `clsx`, `tailwind-merge`, `class-variance-authority` | Utilidad `cn()` y variantes (HU3 / FR-004) | `dependencies` | última estable | a añadir |
| `lucide-react` | Iconos para componentes shadcn (HU3 / FR-004) | `dependencies` | última estable | a añadir |
| `components.json` + `src/lib/utils.ts` + `src/components/ui/` | Inicialización shadcn/ui (HU3 / FR-004) | config + código en repo | shadcn latest | a configurar |
| `src/components/theme-provider.tsx` + clase `.dark` | Dark mode built-in (HU3 / FR-004) | código en repo + `src/index.css` | — | a configurar |
| `@tanstack/react-query` | Estado de servidor (HU4 / FR-005) | `dependencies` | v5 | a añadir |
| `motion` | Animaciones (HU5 / FR-006) | `dependencies` | última estable | a añadir |
| `vitest`, `@testing-library/react`, `@testing-library/user-event`, `jsdom` | Testing de componentes (HU6 / FR-007) | `devDependencies` + `vitest.config.ts` | vitest 4.x | presente |
| `@testing-library/jest-dom` + `tests/setup.ts` | Matchers de DOM (HU6 / FR-007) | `devDependencies` + config | última estable | a añadir |
| `@biomejs/biome` + `biome.json` | Linting / formateo (HU7 / FR-008) | `devDependencies` + config | 2.x | presente |

## Reglas de validación (derivadas de los requisitos)

- **RV-001** (FR-001/010): `react` y `react-dom` resuelven a versión mayor ≥ 19.
- **RV-002** (FR-002/SC-005): `tsconfig.json` tiene `strict: true` y `noImplicitAny: true`; no se permiten excepciones de `any` implícito.
- **RV-003** (FR-003): `vite` está instalado; `npm run dev` arranca y `npm run build` genera artefactos sin error.
- **RV-004** (FR-004): existe `components.json` con `"tailwind.config": ""`; existe `src/lib/utils.ts` con `cn()`; no existe `tailwind.config.ts`; `src/index.css` sigue el patrón de 4 pasos de Tailwind v4.
- **RV-005** (FR-004/V): el `ThemeProvider` envuelve la app y la clase `.dark` alterna el tema sin variantes `dark:` en tokens semánticos.
- **RV-006** (FR-005): `@tanstack/react-query` instalado; `QueryClientProvider` envuelve la app sin errores de tipos.
- **RV-007** (FR-006): `motion` instalado; un componente `motion.*` compila y renderiza.
- **RV-008** (FR-007/IV): `vitest run` descubre y ejecuta al menos una prueba en verde; los matchers de jest-dom están disponibles vía `setupFiles`.
- **RV-009** (FR-008/SC-006): `biome check .` se ejecuta sin fallo de herramienta.
- **RV-010** (FR-009/011): `npm install` resuelve sin conflictos de versión; el proyecto compila y el dev server arranca con cero errores.

## Transiciones de estado

Cada dependencia transita: `a añadir`/`a configurar` → **instalada/configurada** → **verificada**. La fase se considera completa cuando el 100% de las filas de la matriz están en estado **verificada** (SC-001).
