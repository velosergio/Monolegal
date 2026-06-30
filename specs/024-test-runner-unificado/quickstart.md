# Quickstart — Ejecutar y validar el Test Runner Unificado (Spec 5.5)

Guía para correr y verificar el comando único que ejecuta las cuatro suites (backend, worker, frontend, E2E) con reporte consolidado y código de salida agregado. No incluye el código del orquestador (eso vive en `tasks.md` y la implementación); aquí están los pasos para ejecutar y validar.

Contrato completo del comando: [contracts/test-runner.md](./contracts/test-runner.md).

## Prerrequisitos

- **Node 22+** (`.node-version`) y dependencias del frontend instaladas: en `frontend/`, `npm ci`.
- **.NET 10 SDK** (`global.json`) para backend y worker.
- **MongoDB** accesible (backend-integración y E2E).
- **Backend ASP.NET** en `http://localhost:5155` (Development) para la suite E2E; Playwright levanta el frontend solo.
- Navegadores de Playwright instalados (una vez): en `frontend/`, `npx playwright install`.

## Ejecución básica

```bash
# Desde la raíz del repositorio
npm run test:all
```

Esto ejecuta, en orden, las cuatro suites y al final imprime el resumen consolidado. El comando termina con código `0` si todas pasan y distinto de `0` si alguna falla.

```bash
# Comprobar el código de salida agregado (Linux/macOS bash)
npm run test:all; echo "exit=$?"
```

```powershell
# Comprobar el código de salida agregado (Windows PowerShell)
npm run test:all; "exit=$LASTEXITCODE"
```

## Ejecución de un subconjunto (opcional, depuración local)

```bash
# Solo las suites .NET
node scripts/test-all.mjs backend worker
# o vía variable de entorno
SUITES=backend,worker npm run test:all
```

> Sin argumentos siempre corre las **cuatro** suites (que es lo que ejecuta CI).

## Escenarios de validación

Estos escenarios prueban la feature de extremo a extremo. Mapean a los criterios de éxito de la spec.

### V1 — Todas en verde ⇒ exit 0 (SC-002)

1. Entorno sano (Mongo + backend levantados; deps instaladas) y las cuatro suites pasando.
2. Ejecutar `npm run test:all`.
3. **Esperado**: el resumen muestra `PASS` en backend, worker, frontend y e2e; "Resultado global: PASS"; **exit code 0**.

### V2 — Una suite falla ⇒ exit ≠ 0 y FAIL puntual (SC-003, SC-004)

1. Introducir temporalmente un fallo en una sola suite (p. ej. una aserción falsa en una prueba de frontend). **No** commitear este cambio.
2. Ejecutar `npm run test:all`.
3. **Esperado**: el resumen marca esa suite como `FAIL` y las demás como `PASS`; "Resultado global: FAIL"; **exit code distinto de 0**.
4. Revertir el cambio temporal.

### V3 — Se ejecutan todas, no fail-fast (FR-009)

1. Con una suite fallando (V2), observar que las suites posteriores **también se ejecutan** y aparecen en el resumen.
2. **Esperado**: las cuatro suites figuran en el resumen con su veredicto; ninguna queda sin ejecutar por el fallo previo.

### V4 — Suite no ejecutable cuenta como FAIL (FR-010)

1. Simular una precondición ausente (p. ej. detener MongoDB antes de correr, o renombrar temporalmente una herramienta).
2. Ejecutar `npm run test:all`.
3. **Esperado**: la suite afectada aparece como `FAIL` (no como PASS ni cuelga); "Resultado global: FAIL"; **exit code distinto de 0**. Restaurar el entorno.

### V5 — Paridad multiplataforma (SC-005)

1. Ejecutar `npm run test:all` en Windows y en Linux sobre el mismo estado del repo y entorno equivalente.
2. **Esperado**: en ambos SO se ejecutan las mismas cuatro suites, el formato del resumen y la semántica del código de salida son equivalentes.

### V6 — No interactivo / apto para CI (SC-006)

1. Ejecutar `npm run test:all` en un contexto sin TTY (p. ej. redirigiendo la salida o en el runner de CI).
2. **Esperado**: la corrida completa sin solicitar entrada del usuario y termina por sí sola con un código de salida.

## Validación en CI (opcional)

El workflow `.github/workflows/test-all.yml` debe: configurar .NET y Node, instalar dependencias del frontend y navegadores de Playwright, levantar el servicio Mongo y el backend (Development), y ejecutar `npm run test:all`. El job falla si el comando retorna un código distinto de cero — materializando el CI Gate del Principio IV.

## Resultado esperado (resumen)

- Un único comando ejecuta backend + worker + frontend + E2E.
- Resumen final con `PASS`/`FAIL` por suite y veredicto global, coherente con el código de salida.
- `exit 0` solo si las cuatro suites pasan; distinto de cero en cualquier otro caso.
- Sin cambios en pruebas existentes ni en código de producción.
