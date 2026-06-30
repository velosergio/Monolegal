# Data Model — Test Runner Unificado (Spec 5.5)

Esta feature es de **tooling/orquestación**: no introduce persistencia ni entidades de dominio. El "modelo de datos" describe las estructuras conceptuales en memoria que maneja el orquestador durante una corrida.

## Entidad: SuiteDefinition (definición de suite)

Configuración declarativa de una suite a ejecutar. La lista de las cuatro suites se define en `scripts/test-all.mjs`.

| Campo | Tipo | Descripción | Reglas |
|---|---|---|---|
| `id` | string | Identificador estable de la suite | Uno de: `backend`, `worker`, `frontend`, `e2e` (FR-002) |
| `label` | string | Nombre legible para el reporte | No vacío |
| `command` | string | Ejecutable a lanzar | `dotnet` o `npm` |
| `args` | string[] | Argumentos del comando | p. ej. `["test"]`, `["run","test:run"]`, `["run","test:e2e"]` |
| `cwd` | string | Directorio de trabajo (relativo a la raíz) | `backend`, `worker`, `frontend` |

**Instancias fijas** (D3 de research):

| id | label | command | args | cwd |
|---|---|---|---|---|
| `backend` | Backend (xUnit) | `dotnet` | `test` | `backend` |
| `worker` | Worker (xUnit) | `dotnet` | `test` | `worker` |
| `frontend` | Frontend (Vitest) | `npm` | `run test:run` | `frontend` |
| `e2e` | E2E (Playwright) | `npm` | `run test:e2e` | `frontend` |

## Entidad: SuiteResult (resultado de suite)

Resultado de ejecutar una `SuiteDefinition`. Base del reporte consolidado.

| Campo | Tipo | Descripción | Reglas |
|---|---|---|---|
| `id` | string | Suite a la que corresponde | Referencia a `SuiteDefinition.id` |
| `status` | enum | Veredicto | `PASS` \| `FAIL` (sin tercer estado: no ejecutable ⇒ `FAIL`, FR-010) |
| `exitCode` | number \| null | Código de salida del proceso hijo | `0` ⇒ PASS; cualquier otro o `null` (no arrancó) ⇒ FAIL |
| `durationMs` | number | Duración de la suite en ms | ≥ 0; informativo para el resumen (D6) |
| `error` | string \| null | Mensaje si la suite no pudo ejecutarse | Presente solo cuando el spawn falla |

**Regla de derivación**: `status = (exitCode === 0) ? PASS : FAIL`. Un error de arranque fija `exitCode = null`, `status = FAIL`, `error = <motivo>`.

## Entidad: RunReport (reporte consolidado)

Agregado de todas las `SuiteResult` de una corrida + veredicto global. Es la salida final (D6) y determina el código de salida del proceso (D5).

| Campo | Tipo | Descripción | Reglas |
|---|---|---|---|
| `results` | SuiteResult[] | Resultado de cada suite ejecutada | Una entrada por suite seleccionada; por defecto las 4 |
| `allPassed` | boolean | `true` si todas las suites son PASS | `results.every(r => r.status === 'PASS')` |
| `totalDurationMs` | number | Duración total de la corrida | Suma de duraciones + sobrecarga |
| `aggregateExitCode` | number | Código de salida del orquestador | `allPassed ? 0 : 1` (FR-003, FR-006) |

**Invariante de coherencia (FR-006)**: `aggregateExitCode === 0` ⟺ todas las `SuiteResult.status === 'PASS'`. Si alguna es `FAIL`, `aggregateExitCode !== 0`.

## Máquina de estados de una corrida

```text
INICIO
  └─ para cada SuiteDefinition seleccionada (secuencial, D4):
        EJECUTANDO ──(exit 0)──────────────▶ SuiteResult: PASS
        EJECUTANDO ──(exit ≠ 0)────────────▶ SuiteResult: FAIL
        EJECUTANDO ──(spawn falla)─────────▶ SuiteResult: FAIL (error)
     (no se aborta la corrida; se continúa con la siguiente suite)
  └─ AGREGAR ▶ RunReport
        └─ imprimir resumen (PASS/FAIL por suite + global)
        └─ process.exit(aggregateExitCode)
```

## Reglas de validación (trazabilidad a requisitos)

- Se ejecutan **todas** las suites seleccionadas antes de agregar; ninguna se omite silenciosamente (FR-009 / SC-001).
- Selección por defecto = las cuatro suites (FR-001).
- `PASS` solo si `exitCode === 0`; cualquier otro caso es `FAIL` (FR-010).
- `aggregateExitCode` es 0 únicamente si todas PASS (FR-003, FR-004), y siempre coherente con el resumen (FR-006).
- El modelo no contiene estado persistente ni datos de producción (FR-011).
