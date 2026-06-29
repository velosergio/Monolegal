# Quickstart — Validar los tests de componentes (Spec 5.3)

**Feature**: 022-tests-componentes-frontend | **Fecha**: 2026-06-29

Guía para ejecutar y validar que la Spec 5.3 queda cubierta (render, interacciones, async y snapshots).

## Prerrequisitos

- Node 22+ y dependencias instaladas (`npm install` dentro de `frontend/`).
- No requiere backend, MongoDB ni red: las pruebas corren en jsdom.

## Comandos

Desde `frontend/`:

```bash
# Ejecutar toda la suite una vez (modo CI)
npm run test:run

# Ejecutar solo las pruebas de snapshot nuevas
npm run test:run -- tests/components/snapshots

# Primera vez: generar los snapshots (se crean en __snapshots__/)
npm run test:run -- tests/components/snapshots

# Si un cambio de UI es intencional, actualizar los snapshots de forma consciente
npm run test:run -- -u tests/components/snapshots

# Lint/format de los archivos nuevos
npm run lint
npm run format
```

> **Nunca** usar `-u` (`--update`) en CI. La actualización de snapshots se revisa en el PR.

## Escenarios de validación

1. **Snapshots se generan (US1)**: ejecutar la suite por primera vez → aparecen archivos en
   `frontend/tests/components/snapshots/__snapshots__/`. Segunda corrida sin cambios → **PASS** sin diferencias.
2. **Regresión detectada (US1)**: modificar temporalmente el marcado de un componente snapshoteado (p. ej. cambiar un texto en `StatusBadge`) → la prueba de snapshot **FALLA** mostrando el diff. Revertir el cambio → vuelve a **PASS**.
3. **Determinismo del `Footer` (US1)**: ejecutar la suite dos veces → el snapshot del `Footer` no cambia (la fecha está fija en la prueba con `vi.setSystemTime`).
4. **Render de componentes antes sin prueba (US2)**: ejecutar `tests/components/snapshots` y los nuevos tests de render → cada componente presentacional crítico renderiza sin errores y expone su contenido/estructura.
5. **Variantes por prop (US2)**: `ShipmentsEmptyState` con `filtered=true` y `filtered=false` muestran títulos distintos; `Footer` colapsado muestra solo la versión; `StatCard` con/sin ícono.
6. **Verificación consolidada (US3)**: `npm run test:run` → **todos** los archivos pasan, incluidos los nuevos. Contrastar con el inventario de
   [contracts/component-test-matrix.md](./contracts/component-test-matrix.md): cada criterio del roadmap tiene respaldo.

## Resultado esperado

- Suite completa en verde (todos los archivos, todos los casos).
- Carpeta `__snapshots__/` versionada con los snapshots de la UI crítica.
- Cero pruebas omitidas (`.skip`/`.only`); cero cambios en código de producción.
- Dos corridas consecutivas sin diferencias de snapshot (sin intermitencias).
