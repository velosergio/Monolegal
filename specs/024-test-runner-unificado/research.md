# Research — Test Runner Unificado (Spec 5.5)

Consolidación de decisiones técnicas para el orquestador único de pruebas. No quedan marcadores NEEDS CLARIFICATION.

## D1 — Mecanismo de orquestación: script Node.js en la raíz

- **Decisión**: Un `package.json` en la raíz del repo con el script `test:all` que ejecuta `node scripts/test-all.mjs`. El orquestador se escribe en Node.js usando solo módulos integrados (`node:child_process`, `node:process`).
- **Rationale**: Node ya es prerrequisito del frontend (`.node-version`, usado en `frontend.yml`), por lo que está disponible en local (Windows) y CI (Linux) sin instalar nada nuevo. Un único script JavaScript ofrece la **misma semántica multiplataforma** (FR-007), control total sobre el reporte consolidado (FR-005) y sobre el código de salida agregado (FR-003), y permite tratar una suite no ejecutable como FAIL (FR-010). Cumple "el mismo comando conceptual" en ambos SO sin duplicar lógica.
- **Alternativas consideradas**:
  - *Par de scripts `test-all.ps1` + `test-all.sh`*: rechazado — duplica lógica y la paridad Windows/Linux es frágil de mantener.
  - *`npm-run-all` / `concurrently`*: rechazado — añade dependencia y su reporte agregado y control de exit-code por suite es menos explícito que un script propio.
  - *Target de MSBuild / `dotnet`*: rechazado — natural para .NET pero forzado para orquestar `npm`/`playwright`; Node es el denominador común más limpio.
  - *Makefile / Taskfile*: rechazado — `make` no está garantizado en Windows; añade herramienta externa.

## D2 — Ubicación del punto de entrada: `package.json` raíz nuevo

- **Decisión**: Crear `package.json` en la raíz con `"private": true`, sin dependencias, exponiendo `"test:all": "node scripts/test-all.mjs"` (y opcionalmente alias `"test": "node scripts/test-all.mjs"`). El orquestador vive en `scripts/test-all.mjs`.
- **Rationale**: La raíz es el único nivel que abarca `backend/`, `worker/` y `frontend/`. Coincide con el ejemplo del roadmap (`npm run test:all`). Mantenerlo mínimo y `private` evita interferir con `frontend/package.json` y con la publicación.
- **Alternativas consideradas**: Colocar el script dentro de `frontend/package.json` — rechazado: confunde el alcance (las suites .NET no son del frontend) y obliga a ejecutar desde `frontend/`.

## D3 — Definición de las cuatro suites y sus comandos

- **Decisión**: El orquestador define una lista declarativa de suites, cada una con `{ nombre, comando, args, cwd }`:
  | Suite | Comando | cwd |
  |---|---|---|
  | backend | `dotnet test` | `backend/` |
  | worker | `dotnet test` | `worker/` |
  | frontend | `npm run test:run` | `frontend/` |
  | e2e | `npm run test:e2e` | `frontend/` |
- **Rationale**: Replica exactamente cómo cada suite se ejecuta hoy en sus workflows CI (`backend.yml`, `worker.yml`, `frontend.yml`) y en `frontend/package.json` (`test:run` → `vitest run`, `test:e2e` → `playwright test`). Backend y worker son proyectos .NET independientes y se invocan por separado (FR-002, FR-009) para dar visibilidad de cuál componente falla. Añadir una suite futura = añadir una entrada (Open/Closed).
- **Alternativas consideradas**: Una sola invocación `dotnet test` sobre una solución global que agrupe backend+worker — rechazado: no existe `.sln` raíz, y se perdería el veredicto separado por componente que pide la spec.

## D4 — Estrategia de ejecución: ejecutar todas, no fail-fast

- **Decisión**: Ejecutar las cuatro suites **secuencialmente** y **siempre todas**, aunque una falle; agregar resultados al final. El veredicto global es FAIL si alguna suite falla.
- **Rationale**: La spec (Assumptions + FR-005/FR-009) exige un reporte consolidado con el estado real de las cuatro suites; un modo fail-fast ocultaría el estado de las posteriores. La ejecución secuencial evita contención sobre recursos compartidos (la base de datos única que usan integración/E2E, ver `playwright.config.ts`: `workers: 1`).
- **Alternativas consideradas**:
  - *Fail-fast (parar en la primera que falla)*: rechazado como predeterminado; se ofrece como opción futura vía variable de entorno si CI lo prefiere.
  - *Ejecución en paralelo*: rechazado — riesgo de interferencia sobre Mongo compartido entre backend-integración y E2E; complica el log consolidado.

## D5 — Código de salida agregado y mapeo de resultados

- **Decisión**: Cada suite se considera PASS si su proceso termina con código 0; FAIL en cualquier otro caso, incluido fallo de arranque (binario ausente, error de spawn) → se captura la excepción y se marca FAIL. El proceso del orquestador termina con `0` si **todas** PASS, `1` si **alguna** FAIL.
- **Rationale**: Cumple FR-003, FR-004, FR-006 y FR-010 (no ejecutable = FAIL, nunca PASS). Semántica binaria simple y predecible para CI.
- **Alternativas consideradas**: Propagar el código de la primera suite fallida — rechazado: menos predecible; basta con 0/1 para el gate.

## D6 — Formato del reporte consolidado

- **Decisión**: Streaming en vivo de la salida de cada suite (heredando stdio del hijo) precedido por un encabezado por suite, y al final un bloque resumen con una línea por suite (`PASS`/`FAIL`), la duración por suite y el veredicto global.
- **Rationale**: FR-005/SC-004. El streaming preserva el detalle de fallos para diagnóstico (Principio VI, observabilidad) y el resumen final da la vista de un vistazo. Texto plano legible en cualquier terminal y en los logs de CI.
- **Alternativas consideradas**: Capturar y reimprimir al final — rechazado: oculta el progreso en CI y consume memoria; JSON-only — rechazado: menos legible para humanos (podría añadirse como salida opcional futura).

## D7 — Precondiciones de las suites (especialmente E2E)

- **Decisión**: El orquestador **no** levanta infraestructura; asume las preconditions documentadas: MongoDB accesible para backend-integración y E2E, y el backend ASP.NET en `:5155` para E2E (Playwright levanta el frontend vía su `webServer`). Una precondición no satisfecha se traduce en FAIL de la suite afectada (no en PASS ni en cuelgue).
- **Rationale**: Mantiene el orquestador con responsabilidad única (FR-011, Principio II) y evita acoplarlo a Docker/Mongo. Coincide con la asunción de la spec 023 ("backend levantado y sano como precondición"). La preparación del entorno se documenta en quickstart y se materializa en el workflow CI.
- **Alternativas consideradas**: Que el orquestador arranque Mongo/backend/`docker compose` — rechazado: amplía el alcance, lo vuelve frágil y mezcla responsabilidades. Se deja como tarea del entorno (local o `test-all.yml`).

## D8 — Integración en CI

- **Decisión**: Añadir (opcionalmente) `.github/workflows/test-all.yml` que: configura .NET (`global-json-file`) y Node (`.node-version`), instala dependencias del frontend (`npm ci`) y navegadores Playwright (`npx playwright install --with-deps`), levanta el servicio Mongo y el backend en Development, y ejecuta `npm run test:all`. Los workflows por componente existentes pueden conservarse (feedback rápido por path) o consolidarse más adelante.
- **Rationale**: Demuestra el CI Gate del Principio IV de extremo a extremo con un solo comando. Reutiliza los patrones ya probados en `backend.yml`/`frontend.yml` (servicio Mongo, setup de toolchains).
- **Alternativas consideradas**: Reemplazar de inmediato los workflows por componente — pospuesto: fuera del alcance mínimo; los actuales dan feedback granular por path y no estorban.

## D9 — Selección de suites (opcional, no requerido)

- **Decisión**: Permitir, de forma opcional y aditiva, filtrar suites por argumentos/variable de entorno (p. ej. `node scripts/test-all.mjs backend worker` o `SUITES=backend,worker`) sin alterar el comportamiento por defecto (todas). No es un requisito de la spec; se documenta como capacidad de conveniencia para depuración local.
- **Rationale**: Útil para iterar localmente sobre una suite sin perder el comando único; no afecta el gate de CI (que corre todas). Mantiene el default = las cuatro suites (FR-001/SC-001).
- **Alternativas consideradas**: No incluir filtrado — válido; se deja como mejora opcional de bajo coste, claramente separada del flujo por defecto.
