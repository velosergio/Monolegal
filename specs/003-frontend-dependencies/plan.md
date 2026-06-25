# Plan de Implementación: Dependencias Frontend

**Rama**: `003-frontend-dependencies` | **Fecha**: 2026-06-24 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de feature de `/specs/003-frontend-dependencies/spec.md`

## Resumen

Garantizar que el frontend React + Vite dispone de todas las dependencias requeridas por la constitución (React 19+, TypeScript strict, Vite, shadcn/ui, TanStack Query, Motion, Vitest + Testing Library, Biome), verificadas mediante compilación, arranque del servidor de desarrollo y ejecución de pruebas. El scaffolding de la Fase 0.1 ya instaló la base; esta fase **cierra las brechas detectadas** y formaliza la matriz dependencia→propósito como contrato verificable.

**Brechas detectadas en el estado actual** (auditoría de `frontend/package.json` y configs):

1. **shadcn/ui ausente**: No existe `components.json`, ni Tailwind CSS, ni utilidades base (`clsx`, `tailwind-merge`, `lucide-react`, `class-variance-authority`), ni alias de rutas `@/*`, ni proveedor de tema. La constitución exige componentes shadcn/ui **con dark mode built-in desde día uno**. Es la brecha más amplia (FR-004, HU3).
2. **TanStack Query ausente**: `@tanstack/react-query` no está referenciado (FR-005, HU4).
3. **Motion ausente**: el paquete `motion` no está referenciado (FR-006, HU5).
4. **Setup de matchers de Testing Library ausente** (menor): `vitest.config.ts` tiene `setupFiles: []` y falta `@testing-library/jest-dom` para aserciones de DOM legibles. Mejora recomendada para el ciclo Test-First (HU6).

**Ya presente y solo a verificar**: React 19+ (`react ^19.2.7`), TypeScript strict (`strict: true`, `noImplicitAny: true`), Vite (`^8.1.0`), Vitest + Testing Library (`vitest ^4.1.9`, `@testing-library/react`, `user-event`, `jsdom`), Biome (`@biomejs/biome ^2.5.1` con `biome.json`).

## Contexto Técnico

**Lenguaje/Versión**: TypeScript (`^6.0.3`) en modo estricto; runtime React 19 (`react ^19.2.7`, `react-dom ^19.2.7`).

**Dependencias Primarias** (objetivo de esta fase, por propósito):

- **UI base**: `react`, `react-dom` (19+) — presente, verificar.
- **Build/Dev**: `vite` (^8) + `@vitejs/plugin-react` — presente, verificar. Añadir `@tailwindcss/vite` para shadcn/ui v4.
- **Sistema de componentes**: shadcn/ui (CLI `init`) sobre **Tailwind CSS v4** (`tailwindcss`, `@tailwindcss/vite`) + utilidades base (`clsx`, `tailwind-merge`, `class-variance-authority`, `lucide-react`) + alias `@/*` + `components.json` (`"config": ""`) + dark mode vía `ThemeProvider`/clase `.dark`. **A añadir**.
- **Estado de servidor**: `@tanstack/react-query` (v5). **A añadir**.
- **Animaciones**: `motion` (sucesor de Framer Motion, API `motion/react`). **A añadir**.
- **Testing**: `vitest`, `@testing-library/react`, `@testing-library/user-event`, `jsdom` — presente. Añadir `@testing-library/jest-dom` + `setupFiles` (menor).
- **Calidad**: `@biomejs/biome` (`biome.json`) — presente, verificar.

**Almacenamiento**: N/A en frontend (el estado de servidor se consume vía TanStack Query desde la API backend; configuración del cliente HTTP es Fase 0.4+).

**Testing**: Vitest + Testing Library (jsdom).

**Plataforma Objetivo**: Navegadores modernos (build Vite); contenedor Docker para desarrollo/producción (VPS Linux). Bundle servido como estático.

**Tipo de Proyecto**: Aplicación web SPA frontend (carpeta `frontend/`), parte de un monorepo con backend separado.

**Objetivos de Performance**: N/A para esta fase (sin lógica de runtime nueva). La instalación + build de producción + arranque del dev server deben completarse sin errores. Los presupuestos de la constitución (TTI < 2s, Lighthouse > 90, main bundle < 50KB gzipped) se evalúan en fases de implementación de UI, no aquí.

**Restricciones**:

- TypeScript strict sin `any` (constitución, Principio V).
- Tailwind v4 **sin** `tailwind.config.ts` (configuración CSS-first vía `@theme inline`); `components.json` con `"config": ""`.
- Dark mode built-in desde el inicio (no retrofitted).
- Sin webpack (solo Vite); Biome 100% compliant.
- Organización por feature con límites claros de hooks/estado (se aplica al implementar, no en esta fase de dependencias).

**Escala/Alcance**: 1 proyecto frontend; 8 dependencias objetivo de la spec; 3 brechas estructurales (shadcn/ui, TanStack Query, Motion) + 1 mejora menor (matchers de Testing Library).

## Revisión de Constitución

*PUERTA: Debe pasar antes de investigación de Fase 0. Re-chequear después de diseño de Fase 1.*

### Alineación con Principios

✅ **I. Arquitectura Limpia (NO NEGOCIABLE)**: Esta fase solo provee dependencias. La organización por feature con límites de hooks/estado se respeta al implementar; las dependencias añadidas (TanStack Query para server state, Motion para presentación, shadcn/ui para UI) tienen responsabilidades separadas y no acoplan capas. **CUMPLE**.

➖ **II. Principios SOLID**: No aplica directamente a la instalación de dependencias; se evaluará al escribir componentes/hooks.

✅ **III. Desarrollo Dirigido por Especificaciones**: Esta fase deriva de la spec 0.3 en formato GIVEN/WHEN/THEN; toda la documentación en español. **CUMPLE**.

✅ **IV. Desarrollo Test-First (NO NEGOCIABLE)**: Vitest + Testing Library ya presentes; el plan añade `@testing-library/jest-dom` + `setupFiles` para habilitar aserciones de DOM legibles desde el inicio del ciclo Red-Green-Refactor. **CUMPLE** (refuerza el principio).

✅ **V. Frontend de Calidad Producción**: TypeScript strict ya activo; Biome presente; shadcn/ui con **dark mode built-in desde día uno** (no retrofitted) se establece en esta fase, cumpliendo la exigencia explícita de la constitución. **CUMPLE**.

✅ **VI. Código Observable y Mantenible**: shadcn/ui habilita patrones de error boundaries/degradación elegante en fases posteriores; TanStack Query centraliza el estado de servidor de forma observable. Esta fase deja las dependencias disponibles. **CUMPLE**.

✅ **Stack Tecnológico**: React 19+, Vite (sin webpack), TypeScript strict, shadcn/ui, TanStack Query, Motion, Vitest + Testing Library, Biome — todas contempladas. **CUMPLE**.

### Resultado de la Puerta

**✅ APROBADO** — Sin violaciones. La sección de Seguimiento de Complejidad no requiere justificaciones.

## Estructura del Proyecto

### Documentación (esta feature)

```text
specs/003-frontend-dependencies/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Salida Fase 0 (/speckit-plan)
├── data-model.md        # Salida Fase 1 — matriz dependencia→propósito (/speckit-plan)
├── quickstart.md        # Salida Fase 1 — guía de verificación (/speckit-plan)
├── contracts/           # Salida Fase 1 — contrato de dependencias (/speckit-plan)
│   └── dependency-matrix.md
└── tasks.md             # Salida Fase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Código Fuente (raíz del repositorio)

```text
frontend/
├── package.json                 # + shadcn deps, @tanstack/react-query, motion, jest-dom
├── components.json              # NUEVO — config shadcn/ui v4 ("config": "")
├── biome.json                   # presente — verificar
├── tsconfig.json                # + paths alias "@/*" (para shadcn)
├── vite.config.ts               # + plugin @tailwindcss/vite, alias "@"
├── vitest.config.ts             # + setupFiles (jest-dom)
├── index.html                   # presente
├── public/
├── tests/
│   ├── App.test.tsx             # presente
│   └── setup.ts                 # NUEVO — import '@testing-library/jest-dom'
└── src/
    ├── main.tsx                 # + envolver con QueryClientProvider + ThemeProvider
    ├── App.tsx
    ├── index.css                # + Tailwind v4 (@import, @theme inline, :root/.dark)
    ├── lib/
    │   └── utils.ts             # NUEVO — cn() (clsx + tailwind-merge)
    └── components/
        ├── ui/                  # NUEVO — componentes shadcn/ui añadidos vía CLI
        └── theme-provider.tsx   # NUEVO — dark mode (light/dark/system)
```

**Decisión de Estructura**: Se mantiene la estructura del proyecto `frontend/` establecida en la Fase 0.1. Esta fase modifica `package.json` (nuevas dependencias), añade configs de Tailwind v4/shadcn (`components.json`, alias en `tsconfig.json`/`vite.config.ts`, bloques en `src/index.css`), introduce `src/lib/utils.ts`, `src/components/theme-provider.tsx`, `src/components/ui/` y `tests/setup.ts`. No crea proyectos nuevos.

## Seguimiento de Complejidad

> Sin violaciones de la Revisión de Constitución. No se requieren justificaciones de complejidad.
