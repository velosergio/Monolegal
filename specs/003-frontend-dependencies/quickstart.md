# Quickstart (Fase 1): Verificación de Dependencias Frontend

**Feature**: `003-frontend-dependencies` | **Fecha**: 2026-06-24

Guía ejecutable para validar que la Fase 0.3 está completa. Las instrucciones de implementación detalladas (componentes, código de providers) corresponden a `tasks.md` / fase de implementación; aquí solo se documenta **cómo verificar** el resultado contra el [contrato](contracts/dependency-matrix.md) y el [modelo de datos](data-model.md).

## Prerrequisitos

- Proyecto `frontend/` de la Fase 0.1 presente.
- Node.js y npm instalados; conectividad con el registro de paquetes.
- Directorio de trabajo: `frontend/`.

## Paso 1 — Instalar dependencias

```bash
cd frontend
npm install
```

**Esperado**: instalación sin conflictos de versión no resueltos (FR-009, SC-003).

## Paso 2 — Verificar dependencias ya presentes

```bash
npm ls react react-dom vite typescript vitest @biomejs/biome
```

**Esperado**: `react`/`react-dom` en 19.x, `vite` 8.x, `vitest` 4.x, `typescript` presente, biome 2.x.

Verificar TypeScript strict (debe mostrar `true` en ambos):

```bash
node -e "const t=require('./tsconfig.json');console.log('strict:',t.compilerOptions.strict,'noImplicitAny:',t.compilerOptions.noImplicitAny)"
```

**Esperado**: `strict: true noImplicitAny: true` (FR-002, SC-005).

## Paso 3 — Verificar dependencias añadidas

```bash
npm ls @tanstack/react-query motion tailwindcss @tailwindcss/vite clsx tailwind-merge class-variance-authority lucide-react @testing-library/jest-dom
```

**Esperado**: todas resueltas sin `(empty)` ni `UNMET DEPENDENCY` (FR-004, FR-005, FR-006, FR-007).

## Paso 4 — Verificar configuración de shadcn/ui + Tailwind v4

```bash
# components.json existe con config vacía (v4)
test -f components.json && grep -q '"config": ""' components.json && echo "components.json OK"
# NO debe existir tailwind.config.ts (v4 CSS-first)
test ! -f tailwind.config.ts && echo "sin tailwind.config.ts OK"
# utilidad cn() presente
test -f src/lib/utils.ts && echo "utils.ts OK"
# dark mode presente
test -f src/components/theme-provider.tsx && echo "theme-provider OK"
```

**Esperado**: las cuatro líneas `OK` (FR-004, INV-2, INV-3 del contrato).

## Paso 5 — Compilar y arrancar

```bash
npm run build      # tsc -b && vite build
```

**Esperado**: build de producción sin errores (FR-011, SC-002).

```bash
npm run dev        # arrancar dev server (Ctrl+C para salir)
```

**Esperado**: servidor sirviendo en `http://localhost:5173` sin errores de dependencias (FR-003).

## Paso 6 — Ejecutar pruebas

```bash
npm run test:run   # vitest run
```

**Esperado**: ≥ 1 prueba descubierta y en verde; matchers de jest-dom disponibles (FR-007, SC-004).

## Paso 7 — Verificar calidad (Biome)

```bash
npm run lint       # biome check .
```

**Esperado**: la herramienta analiza el código sin fallo de ejecución (FR-008, SC-006).

## Criterio de éxito global

La fase está completa cuando los Pasos 1–7 terminan sin error, satisfaciendo SC-001 (100% de las dependencias objetivo referenciadas/configuradas) y todas las cláusulas del [contrato de dependencias](contracts/dependency-matrix.md).

## Mapeo de verificación → Historias de Usuario

| Paso | Historias / FR cubiertos |
|------|--------------------------|
| 2 | HU1 (React, TS strict), HU2 (Vite), HU7 (Biome) |
| 3 | HU3 (shadcn), HU4 (TanStack Query), HU5 (Motion), HU6 (jest-dom) |
| 4 | HU3 (shadcn/ui + dark mode) |
| 5 | HU1, HU2 (compilación/build) |
| 6 | HU6 (Vitest + Testing Library) |
| 7 | HU7 (Biome) |
