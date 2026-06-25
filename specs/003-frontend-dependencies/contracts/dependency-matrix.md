# Contrato de Dependencias Frontend

**Feature**: `003-frontend-dependencies` | **Fecha**: 2026-06-24

Este contrato define el estado verificable que el proyecto `frontend/` DEBE satisfacer al completar la Fase 0.3. Cada cláusula es comprobable de forma automatizada o por inspección directa. Sustituye lenguaje vago por condiciones falsables (Principio III de la constitución).

## C1 — Dependencias declaradas en `package.json`

El proyecto DEBE declarar (presentes o añadidas):

```jsonc
{
  "dependencies": {
    "react": "^19",                       // FR-001
    "react-dom": "^19",                   // FR-001
    "@tanstack/react-query": "^5",        // FR-005
    "motion": "*",                        // FR-006 (última estable)
    "tailwindcss": "^4",                  // FR-004 (base shadcn)
    "@tailwindcss/vite": "^4",            // FR-004
    "clsx": "*",                          // FR-004
    "tailwind-merge": "*",                // FR-004
    "class-variance-authority": "*",      // FR-004
    "lucide-react": "*"                   // FR-004 (iconos)
  },
  "devDependencies": {
    "typescript": "*",                    // FR-002
    "vite": "^8",                         // FR-003
    "@vitejs/plugin-react": "*",          // FR-003
    "vitest": "^4",                       // FR-007
    "@testing-library/react": "*",        // FR-007
    "@testing-library/user-event": "*",   // FR-007
    "@testing-library/jest-dom": "*",     // FR-007 (matchers)
    "jsdom": "*",                         // FR-007
    "@biomejs/biome": "^2"                // FR-008
  }
}
```

> Las versiones con `*` se fijan a la última estable compatible al instalar; los pins exactos se ajustan solo si surge un conflicto (FR-009).

## C2 — Configuración requerida

| Archivo | Condición verificable | FR |
|---------|----------------------|----|
| `tsconfig.json` | `compilerOptions.strict === true` y `noImplicitAny === true` | FR-002 |
| `vite.config.ts` | Incluye plugin `tailwindcss()` y alias `@ → ./src` | FR-003, FR-004 |
| `components.json` | Existe; `tailwind.config === ""`; `cssVariables === true`; `css === "src/index.css"` | FR-004 |
| `tailwind.config.ts` | **NO existe** (Tailwind v4 CSS-first) | FR-004 |
| `src/index.css` | Contiene `@import "tailwindcss"`, bloque `@theme inline`, y `:root`/`.dark` a nivel raíz | FR-004 |
| `src/lib/utils.ts` | Exporta `cn()` basado en `clsx` + `twMerge` | FR-004 |
| `src/components/theme-provider.tsx` | Provee tema light/dark/system y aplica clase `.dark` | FR-004 |
| `src/main.tsx` | App envuelta en `QueryClientProvider` y `ThemeProvider` | FR-004, FR-005 |
| `vitest.config.ts` | `test.setupFiles` incluye `tests/setup.ts` | FR-007 |
| `tests/setup.ts` | Contiene `import '@testing-library/jest-dom'` | FR-007 |
| `biome.json` | Existe y `biome check .` se ejecuta sin fallo de herramienta | FR-008 |

## C3 — Comandos de verificación (deben terminar en éxito)

| Comando | Resultado esperado | FR / SC |
|---------|-------------------|---------|
| `npm install` | Sin conflictos de versión no resueltos | FR-009, SC-003 |
| `npm run build` | Build de producción sin errores | FR-011, SC-002 |
| `npm run dev` | Dev server arranca y sirve la app | FR-003, FR-011, SC-002 |
| `npm run test:run` (o `vitest run`) | ≥ 1 prueba descubierta y en verde | FR-007, SC-004 |
| `npm run lint` (`biome check .`) | Herramienta analiza el código sin fallo de ejecución | FR-008, SC-006 |

## C4 — Invariantes

- **INV-1**: La capa Domain del backend no se ve afectada por esta fase (sin acoplamiento cross-stack).
- **INV-2**: No se introduce webpack ni `postcss` como bundler de Tailwind (solo plugin Vite).
- **INV-3**: Dark mode disponible desde el primer commit de esta fase (no diferido).
- **INV-4**: Cero excepciones de `any` implícito habilitadas en `tsconfig.json` (SC-005).

## Criterio de aceptación del contrato

El contrato se cumple cuando **todas** las cláusulas C1–C4 son verdaderas y **todos** los comandos de C3 terminan en éxito, alcanzando SC-001 (100% de dependencias referenciadas/configuradas).
