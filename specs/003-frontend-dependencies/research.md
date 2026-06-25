# Investigación (Fase 0): Dependencias Frontend

**Feature**: `003-frontend-dependencies` | **Fecha**: 2026-06-24

Esta fase no introduce incógnitas de negocio; las decisiones se centran en **cómo** integrar correctamente cada dependencia del stack de la constitución sobre el proyecto `frontend/` existente (Fase 0.1). No quedan marcadores NEEDS CLARIFICATION.

---

## Decisión 1 — Sistema de componentes: shadcn/ui sobre Tailwind CSS v4

- **Decisión**: Inicializar shadcn/ui con la CLI (`shadcn@latest init`) sobre **Tailwind CSS v4** usando el plugin `@tailwindcss/vite` (configuración CSS-first), **sin** `tailwind.config.ts`. Configurar `components.json` con `"tailwind.config": ""`, `cssVariables: true` y `css: "src/index.css"`.
- **Racional**:
  - La constitución exige componentes shadcn/ui y dark mode built-in desde día uno. shadcn/ui no es una dependencia npm tradicional: copia componentes al repo (`src/components/ui/`) y requiere Tailwind, `cn()` (clsx + tailwind-merge), alias `@/*` y variables CSS de tema.
  - Tailwind v4 elimina `tailwind.config.ts` y `postcss` a favor del plugin oficial de Vite y configuración CSS-first vía `@import "tailwindcss"` + `@theme inline`. Patrón validado en producción (skill `tailwind-v4-shadcn`).
  - Arquitectura de 4 pasos obligatoria: (1) variables en `:root`/`.dark` con `hsl()`; (2) mapeo en `@theme inline`; (3) estilos base en `@layer base` con `var(--x)` sin doble-wrap; (4) dark mode automático.
- **Alternativas consideradas**:
  - **Tailwind v3 + postcss**: rechazado — más configuración, deprecado para nuevos proyectos, contradice el patrón probado.
  - **Librerías de componentes con runtime (MUI, Chakra)**: rechazado — la constitución fija shadcn/ui explícitamente; además aumentan el bundle (presupuesto < 50KB main gzipped).
- **Dependencias resultantes**: `tailwindcss`, `@tailwindcss/vite`, `clsx`, `tailwind-merge`, `class-variance-authority`, `lucide-react` (iconos), más componentes copiados por la CLI.
- **Gotchas a evitar** (del skill `tailwind-v4-shadcn`): no instalar `tailwindcss-animate` ni `tw-animate-css` (deprecados/inexistentes en v4); no anidar `:root`/`.dark` dentro de `@layer base`; no doble-envolver `hsl(var(--x))`; eliminar cualquier `tailwind.config.ts`.

## Decisión 2 — Dark mode built-in (no retrofitted)

- **Decisión**: Implementar `ThemeProvider` propio (light/dark/system con persistencia en `localStorage`, clave `ml-ui-theme`) que aplica la clase `.dark` en `<html>`, y envolver la app en `src/main.tsx` desde esta fase.
- **Racional**: La constitución (Principio V) exige dark mode desde día uno, no añadido después. El patrón de clase `.dark` + variables CSS lo hace automático: los componentes usan tokens semánticos (`bg-background`, `text-foreground`) sin variantes `dark:`.
- **Alternativas consideradas**:
  - **`next-themes`**: rechazado — pensado para Next.js; el proyecto es Vite SPA puro.
  - **Posponer dark mode a una fase de UI**: rechazado — violaría la exigencia "built-in desde día uno".

## Decisión 3 — Estado de servidor: TanStack Query v5

- **Decisión**: Añadir `@tanstack/react-query` (v5) y dejar configurado un `QueryClient` + `QueryClientProvider` envolviendo la app en `src/main.tsx`.
- **Racional**: Constitución fija TanStack Query para server state. Dejar el proveedor listo permite que las features de datos (Fase 0.4+) solo declaren queries/mutaciones. v5 es la línea estable actual con tipado estricto compatible con TS strict.
- **Alternativas consideradas**:
  - **SWR / fetch manual + useState**: rechazado — la constitución fija TanStack Query; además no cubre caché/revalidación de forma consistente.
  - **Incluir Devtools en producción**: se evalúa como dependencia de desarrollo opcional (`@tanstack/react-query-devtools`) en fases de implementación, fuera del alcance mínimo de esta fase.

## Decisión 4 — Animaciones: Motion

- **Decisión**: Añadir el paquete `motion` (sucesor oficial de Framer Motion) y usar la API `motion/react`.
- **Racional**: Constitución fija "Motion para animaciones". El paquete `motion` es la evolución actual de `framer-motion` con el mismo modelo declarativo (`<motion.div>`) y soporte React 19. Prioridad P2 (no bloquea estructura).
- **Alternativas consideradas**:
  - **`framer-motion` (paquete antiguo)**: rechazado — `motion` es el nombre/paquete vigente recomendado.
  - **CSS transitions puras**: rechazado para el caso general — la constitución pide Motion; CSS se usará para micro-transiciones triviales cuando aplique.

## Decisión 5 — Matchers de Testing Library (mejora del setup existente)

- **Decisión**: Añadir `@testing-library/jest-dom` como dependencia de desarrollo y registrar `tests/setup.ts` (`import '@testing-library/jest-dom'`) en `vitest.config.ts` (`setupFiles`).
- **Racional**: El setup actual tiene `setupFiles: []`, por lo que faltan matchers legibles (`toBeInTheDocument`, `toHaveTextContent`). El ciclo Test-First (Principio IV) se beneficia de aserciones claras desde el inicio. Vitest + Testing Library + jsdom ya están presentes.
- **Alternativas consideradas**:
  - **No añadir jest-dom**: rechazado — degrada la legibilidad de aserciones de DOM y obliga a comprobaciones manuales más frágiles.

## Decisión 6 — Verificación de dependencias ya presentes

- **Decisión**: React 19+, TypeScript strict, Vite y Biome se **verifican** (no se reinstalan): confirmar versiones mayores, `strict: true`/`noImplicitAny: true` en `tsconfig.json`, arranque del dev server y `biome check`.
- **Racional**: La Fase 0.1 ya las instaló correctamente; reinstalar arriesga conflictos de versión. La spec (FR-001/002/003/008) exige que estén disponibles, lo cual ya se cumple; el plan solo lo formaliza como verificación.
- **Alternativas consideradas**:
  - **Forzar pins/upgrades**: rechazado para esta fase — los pins exactos de versión menor se deciden si surge un conflicto; no hay evidencia de conflicto actual.

---

## Resumen de paquetes a añadir

| Propósito | Paquete(s) | Tipo |
|-----------|-----------|------|
| Tailwind v4 (base shadcn) | `tailwindcss`, `@tailwindcss/vite` | dependencia / build |
| Utilidades shadcn | `clsx`, `tailwind-merge`, `class-variance-authority` | dependencia |
| Iconos | `lucide-react` | dependencia |
| Estado de servidor | `@tanstack/react-query` | dependencia |
| Animaciones | `motion` | dependencia |
| Matchers de test | `@testing-library/jest-dom` | desarrollo |
| Componentes shadcn | copiados por CLI a `src/components/ui/` | código en repo |

**Ya presentes (verificar, no reinstalar)**: `react`, `react-dom`, `typescript`, `vite`, `@vitejs/plugin-react`, `vitest`, `@testing-library/react`, `@testing-library/user-event`, `jsdom`, `@biomejs/biome`.
