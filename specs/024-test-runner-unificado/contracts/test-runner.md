# Contrato — Orquestador de pruebas unificado (`test:all`)

Interfaz de línea de comandos (CLI) que expone esta feature. Es el "contrato externo" que consumen las personas del equipo y la pipeline de CI. No es una API HTTP; es el comportamiento observable del comando único.

## Invocación

```bash
# Desde la raíz del repositorio
npm run test:all
# equivalente directo:
node scripts/test-all.mjs
```

- **Multiplataforma**: el mismo comando funciona en Windows y Linux (FR-007). No hay variante por SO.
- **No interactivo**: no solicita entrada del usuario; nunca queda a la espera de stdin (FR-008 / SC-006).

### Argumentos / variables (opcionales, no requeridos por la spec)

| Forma | Efecto | Default |
|---|---|---|
| `node scripts/test-all.mjs <id...>` | Ejecuta solo las suites indicadas (`backend`, `worker`, `frontend`, `e2e`) | Sin args ⇒ las **cuatro** |
| `SUITES=backend,worker` (env) | Igual que lo anterior vía variable de entorno | Sin var ⇒ las **cuatro** |

> El comportamiento por defecto (sin argumentos) ejecuta **siempre las cuatro suites** (FR-001 / SC-001). El filtrado es una conveniencia para depuración local y no afecta al gate de CI, que corre el comando sin argumentos.

## Suites ejecutadas (orden secuencial)

| Orden | id | Comando efectivo | Directorio |
|---|---|---|---|
| 1 | `backend` | `dotnet test` | `backend/` |
| 2 | `worker` | `dotnet test` | `worker/` |
| 3 | `frontend` | `npm run test:run` (`vitest run`) | `frontend/` |
| 4 | `e2e` | `npm run test:e2e` (`playwright test`) | `frontend/` |

Se ejecutan **todas**, aunque alguna falle (no fail-fast por defecto, D4). Ninguna se omite silenciosamente (FR-009).

## Salida estándar (formato del reporte)

1. **Durante la ejecución**: por cada suite, un encabezado y el streaming en vivo de su salida (stdio heredado), de modo que los fallos conserven su detalle.

   ```text
   ───── [1/4] Backend (xUnit) ─────
   <salida de dotnet test ...>
   ```

2. **Al final**: bloque resumen consolidado con una línea por suite y el veredicto global (FR-005 / SC-004).

   ```text
   ════════ Resumen de pruebas ════════
   ✔ backend    PASS   (12.3s)
   ✔ worker     PASS   (4.1s)
   ✘ frontend   FAIL   (8.7s)
   ✔ e2e        PASS   (33.5s)
   ────────────────────────────────────
   Resultado global: FAIL (1 de 4 suites falló)
   ```

> El texto exacto (iconos, idioma de etiquetas) puede ajustarse en implementación; el contrato exige: **una línea por suite con PASS/FAIL** y **una línea de veredicto global**, en español (Principio III).

## Código de salida (contrato de CI)

| Condición | Exit code |
|---|---|
| Las cuatro suites seleccionadas terminan en PASS | `0` |
| Una o más suites terminan en FAIL | distinto de `0` (`1`) |
| Una suite no puede ejecutarse (binario ausente, fallo de spawn) | esa suite ⇒ FAIL ⇒ exit distinto de `0` (FR-010) |

**Invariante (FR-006)**: `exit 0` ⟺ todas las suites del resumen son PASS. Coherencia total entre el resumen impreso y el código de salida.

## Precondiciones (responsabilidad del entorno, no del orquestador)

El orquestador **no** levanta infraestructura (D7). Para que las suites que dependen de servicios pasen, el entorno debe proveer:

- **MongoDB** accesible (backend-integración y E2E).
- **Backend ASP.NET** en `http://localhost:5155` (Development) para E2E; Playwright levanta el frontend automáticamente vía su `webServer`.
- **Toolchains**: .NET SDK 10 (`global.json`) y Node 22+ (`.node-version`); dependencias del frontend instaladas (`npm ci`) y navegadores de Playwright (`npx playwright install`).

Si una precondición no se cumple, la suite afectada termina en **FAIL** (no en PASS ni en cuelgue), reflejándose en el resumen y en el exit code.

## No-objetivos (alcance)

- No añade, elimina ni modifica pruebas existentes ni código de producción (FR-011 / SC-007).
- No arranca Docker/Mongo/backend por sí mismo (eso es del entorno o del workflow CI).
- No paraleliza suites por defecto (evita contención sobre la base de datos compartida).
