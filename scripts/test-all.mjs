#!/usr/bin/env node
// =============================================================================
// Orquestador unificado de pruebas — Spec 5.5 / feature 024-test-runner-unificado
//
// Punto de entrada único (`npm run test:all`) que ejecuta las CUATRO suites del
// proyecto y devuelve un código de salida agregado apto para CI:
//   - backend  → `dotnet test` en backend/
//   - worker   → `dotnet test` en worker/
//   - frontend → `npm run test:run` (vitest) en frontend/
//   - e2e      → `npm run test:e2e` (playwright) en frontend/
//
// Comportamiento (ver contracts/test-runner.md, research.md):
//   - Ejecuta TODAS las suites de forma secuencial; NO fail-fast (FR-009, D4).
//   - PASS solo si el proceso hijo termina con código 0; cualquier otro caso o
//     fallo de arranque ⇒ FAIL, nunca PASS (FR-010).
//   - Imprime un resumen consolidado PASS/FAIL por suite + veredicto global
//     (FR-005) y termina con exit 0 solo si todas pasan (FR-003/FR-004/FR-006).
//   - Multiplataforma Windows/Linux sin scripts por SO (FR-007).
//   - No interactivo: nunca solicita entrada del usuario (FR-008).
//
// Solo usa módulos integrados de Node (sin dependencias nuevas).
// =============================================================================

import { spawn } from 'node:child_process'
import process from 'node:process'

// --- Definición declarativa de las suites (data-model.md: SuiteDefinition) ---
// Añadir una suite futura = añadir una entrada aquí (Open/Closed, Principio II).
const SUITES = [
  { id: 'backend', label: 'Backend (xUnit)', command: 'dotnet', args: ['test'], cwd: 'backend' },
  { id: 'worker', label: 'Worker (xUnit)', command: 'dotnet', args: ['test'], cwd: 'worker' },
  { id: 'frontend', label: 'Frontend (Vitest)', command: 'npm', args: ['run', 'test:run'], cwd: 'frontend' },
  { id: 'e2e', label: 'E2E (Playwright)', command: 'npm', args: ['run', 'test:e2e'], cwd: 'frontend' },
]

// --- Selección opcional de suites (research D9; default = las cuatro) --------
// Uso: `node scripts/test-all.mjs backend worker`  o  `SUITES=backend,worker`.
// Sin argumentos ni variable ⇒ se ejecutan las cuatro (FR-001 / SC-001).
function selectSuites() {
  const fromArgs = process.argv.slice(2).map((s) => s.trim()).filter(Boolean)
  const fromEnv = (process.env.SUITES ?? '').split(',').map((s) => s.trim()).filter(Boolean)
  const requested = fromArgs.length ? fromArgs : fromEnv
  if (!requested.length) return SUITES

  const byId = new Map(SUITES.map((s) => [s.id, s]))
  const selected = []
  for (const id of requested) {
    const def = byId.get(id)
    if (!def) {
      console.error(`Suite desconocida: "${id}". Válidas: ${SUITES.map((s) => s.id).join(', ')}`)
      process.exit(2)
    }
    selected.push(def)
  }
  return selected
}

// --- Ejecutar una suite y devolver su SuiteResult (data-model.md) ------------
// Multiplataforma (FR-007): se ejecuta a través del shell (`/bin/sh -c` en POSIX,
// `cmd.exe /c` en Windows) porque los lanzadores `npm`/`npx` se exponen como
// scripts `.cmd` en Windows, que Node ya no permite ejecutar sin shell (EINVAL,
// endurecimiento CVE-2024-27980). Se pasa la línea de comando COMPLETA como una
// sola cadena (no un arreglo `args`) para evitar el aviso de deprecación DEP0190.
function runSuite(def) {
  return new Promise((resolve) => {
    const start = Date.now()
    let settled = false
    const finish = (result) => {
      if (settled) return
      settled = true
      resolve({ id: def.id, durationMs: Date.now() - start, ...result })
    }

    const commandLine = [def.command, ...def.args].join(' ')
    let child
    try {
      child = spawn(commandLine, {
        cwd: def.cwd,
        // stdin ignorado (no interactivo, FR-008) + stdout/stderr heredados para
        // streaming en vivo (FR-005/D6). Heredar stdin puede provocar que algunas
        // suites terminen antes de tiempo en ejecución no interactiva.
        stdio: ['ignore', 'inherit', 'inherit'],
        shell: true,
      })
    } catch (err) {
      // Fallo de arranque (binario ausente, etc.) ⇒ FAIL (FR-010).
      finish({ status: 'FAIL', exitCode: null, error: String(err?.message ?? err) })
      return
    }

    child.on('error', (err) => {
      finish({ status: 'FAIL', exitCode: null, error: String(err?.message ?? err) })
    })
    child.on('close', (code) => {
      const exitCode = code ?? null
      finish({ status: exitCode === 0 ? 'PASS' : 'FAIL', exitCode, error: null })
    })
  })
}

// --- Reporte consolidado (FR-005 / SC-004) -----------------------------------
function formatDuration(ms) {
  return `${(ms / 1000).toFixed(1)}s`
}

function printSummary(results) {
  console.log('\n════════ Resumen de pruebas ════════')
  for (const r of results) {
    const icon = r.status === 'PASS' ? '✔' : '✘'
    const extra = r.error ? `  (${r.error})` : ''
    console.log(`${icon} ${r.id.padEnd(10)} ${r.status.padEnd(4)}  (${formatDuration(r.durationMs)})${extra}`)
  }
  const failed = results.filter((r) => r.status === 'FAIL').length
  console.log('────────────────────────────────────')
  if (failed === 0) {
    console.log(`Resultado global: PASS (${results.length} de ${results.length} suites en verde)`)
  } else {
    console.log(`Resultado global: FAIL (${failed} de ${results.length} suites falló)`)
  }
}

// --- Flujo principal: ejecutar todas, agregar, salir -------------------------
async function main() {
  const suites = selectSuites()
  const results = []

  for (let i = 0; i < suites.length; i++) {
    const def = suites[i]
    console.log(`\n───── [${i + 1}/${suites.length}] ${def.label} ─────`)
    // Secuencial y SIN fail-fast: se continúa aunque una suite falle (FR-009/D4).
    results.push(await runSuite(def))
  }

  printSummary(results)

  // Código de salida agregado: 0 solo si TODAS pasan (FR-003/FR-004/FR-006).
  const allPassed = results.every((r) => r.status === 'PASS')
  process.exit(allPassed ? 0 : 1)
}

main()
