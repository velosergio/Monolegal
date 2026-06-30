# Research — Tests E2E con Playwright (Spec 5.4)

Decisiones técnicas de la Fase 0. Cada entrada documenta la decisión, su justificación y las alternativas descartadas. Todas las "NEEDS CLARIFICATION" del Technical Context quedan resueltas aquí.

## D1 — Runner E2E: Playwright Test

- **Decisión**: Usar `@playwright/test` (runner oficial) en lugar de la librería `playwright` con otro runner, o de Cypress.
- **Justificación**: La constitución fija **Playwright** como herramienta E2E del stack. `@playwright/test` trae runner, aserciones (`expect` con auto-retry), fixtures, paralelismo, trazas/screenshots/vídeo on-failure y `webServer` integrado, todo lo necesario sin pegamento adicional. El auto-waiting de los localizadores reduce el flakiness (Principio IV / SC-002).
- **Alternativas descartadas**: Cypress (no es el estándar del proyecto; modelo de ejecución distinto); Selenium/WebdriverIO (más verboso, peor manejo de esperas); usar `playwright` "a pelo" con Vitest (duplicaría infra y perdería fixtures/trazas).

## D2 — Orquestación de servidores

- **Decisión**: `playwright.config.ts` levanta el **frontend** mediante `webServer` (Vite `preview` sobre `npm run build`, con fallback a `dev`), apuntando `baseURL` a `http://localhost:5173`. El **backend** se asume levantado en `http://localhost:5155` (perfil http, `ASPNETCORE_ENVIRONMENT=Development`); el frontend lo alcanza por el proxy `/api` ya configurado en `vite.config.ts`.
- **Justificación**: Reutiliza la topología real de desarrollo (puerto 5173 + proxy a 5155) sin inventar configuración nueva. Mantener el backend como proceso externo evita acoplar el runner de frontend a `dotnet` y permite, en CI, levantar backend+Mongo con Docker Compose antes de Playwright. `webServer` con `reuseExistingServer` acelera el ciclo local.
- **Alternativas descartadas**: Levantar backend también desde `webServer` con `dotnet run` (acopla y complica el control de readiness/Mongo); montar todo en un único contenedor de pruebas (mayor complejidad inicial, se puede añadir luego en la Spec 5.5 — Test Runner Unificado).
- **Pendiente operativo (no bloqueante)**: La orquestación backend+Mongo en CI se documenta en `quickstart.md`; su automatización completa pertenece a la Spec 5.5.

## D3 — Estado de datos determinista (reset + seed)

- **Decisión**: Antes de la corrida (y/o por bloque de pruebas que muta estado), restablecer la BD con `POST /api/settings/maintenance/flush-database`, que vacía, reconstruye índices y re-ejecuta el sembrador idempotente (3 clientes, 8 facturas con estados variados).
- **Justificación**: Da un punto de partida **conocido y reproducible** (FR-007, SC-004) sin que las pruebas dependan de datos residuales ni del orden. El seeder ya garantiza al menos una factura en `PrimerRecordatorio` y otra en `SegundoRecordatorio`, además de `Pending`, `Pagado` y `Desactivado`, cubriendo estados terminales y no terminales.
- **Alternativas descartadas**: Crear datos vía API en cada prueba (más frágil y lento; duplica lógica de dominio); manipular MongoDB directamente desde la prueba (acopla a infraestructura, viola caja negra); depender solo del seeder de arranque (no limpia mutaciones de pruebas previas).
- **Riesgo y mitigación**: El endpoint de flush es destructivo ("zona de peligro"). Mitigación: usarlo **solo** contra entorno de pruebas/Development; documentarlo explícitamente; las pruebas que mutan estado se serializan o resetean en su propio `beforeEach`/fixture para no interferir entre workers paralelos.

## D4 — Aislamiento entre pruebas y paralelismo

- **Decisión**: Las pruebas que **mutan** estado (transición manual) se ejecutan de forma serializada o con reset previo dedicado; las de **solo lectura** (listar/filtrar) pueden compartir el estado sembrado. Configurar `fullyParallel` con criterio: por defecto serial para specs que escriben, hasta validar independencia.
- **Justificación**: El backend comparte una única base de datos; ejecutar mutaciones en paralelo sin aislamiento por colección/cliente provocaría flakiness. Empezar conservador (serial donde haya escritura) cumple SC-002/SC-004 y se puede optimizar luego.
- **Alternativas descartadas**: Paralelismo total desde el inicio (riesgo alto de flakiness con BD compartida); base de datos por worker (requiere parametrizar el backend, fuera de alcance de esta feature).

## D5 — Estrategia de localizadores (anti-flaky, accesible)

- **Decisión**: Localizar por **rol y etiqueta accesible** y por **texto visible** estable, usando los `aria-label` ya presentes: `Filtrar por estado`, `Nuevo estado`, `Ver detalle de la factura de {cliente}`, y textos de botón `Nueva factura`, `Pagar`, `Cambiar Estado`. Las etiquetas de estado visibles son: Pendiente, 1er Recordatorio, 2do Recordatorio, Pagado, Desactivado.
- **Justificación**: Cumple FR-008 (evitar selectores frágiles) y refuerza accesibilidad (Principio V). El proyecto ya expone estos contratos accesibles, por lo que no requiere tocar producción.
- **Alternativas descartadas**: Selectores por clase CSS/estructura DOM (frágiles ante refactors de marcado/Tailwind); añadir `data-testid` al código de producción (innecesario; violaría "no tocar producción" salvo que falte un anclaje, lo cual no ocurre aquí).

## D6 — Determinismo frente a fuentes de indeterminismo

- **Decisión**: Anclar aserciones a contenido estable (estados, etiquetas, conteos relativos) y **no** a fechas, "último refresco" ni detalles de animación. Para conteos del dashboard, comparar el **delta** esperado (estado origen −1, destino +1) respecto a una lectura previa, en vez de valores absolutos hardcodeados.
- **Justificación**: El dashboard muestra indicadores de tiempo y los gráficos usan animación (Motion); fijar valores absolutos o esperar marcas de tiempo introduciría flakiness (SC-002). El delta es robusto frente a evoluciones del seed.
- **Alternativas descartadas**: Hardcodear conteos exactos del seed (frágil si cambia el seed); deshabilitar animaciones tocando producción (innecesario; las aserciones de contenido no dependen de la animación).

## D7 — Coexistencia con Vitest

- **Decisión**: Playwright corre solo `frontend/e2e/`; Vitest sigue sobre `frontend/tests/` y `frontend/src/**`. Se excluye `e2e/` de la config de Vitest si fuera necesario para que no intente ejecutar specs de Playwright.
- **Justificación**: Evita que un runner intente ejecutar las pruebas del otro (los `*.spec.ts` de Playwright usan `@playwright/test`, incompatibles con el entorno jsdom de Vitest). Mantiene fronteras claras (Principio I).
- **Alternativas descartadas**: Unificar ambos en un runner (no soportado; Playwright y Vitest tienen entornos de ejecución distintos).

## D8 — Artefactos de diagnóstico

- **Decisión**: Activar `trace: 'on-first-retry'`, `screenshot: 'only-on-failure'` y `video: 'retain-on-failure'` en `playwright.config.ts`; reporter `html` (y `list` en consola).
- **Justificación**: Facilita diagnóstico en CI sin penalizar el camino feliz (Principio VI). No genera ruido cuando todo pasa.
- **Alternativas descartadas**: Trazas/vídeo siempre (coste de almacenamiento/tiempo innecesario); sin artefactos (dificulta depurar fallos en CI).

## Resumen de versiones/topología

- Frontend dev/preview: `http://localhost:5173` (Vite 8), proxy `/api` → backend.
- Backend Development: `http://localhost:5155` (ASP.NET Core 10), siembra idempotente al arrancar, endpoint de flush disponible.
- Estados de factura (API → etiqueta visible): `pending`→Pendiente, `primerrecordatorio`→1er Recordatorio, `segundorecordatorio`→2do Recordatorio, `pagado`→Pagado, `desactivado`→Desactivado.
- Transiciones permitidas (fuente de verdad: backend): Pending→{PrimerRecordatorio, Pagado}; PrimerRecordatorio→{SegundoRecordatorio, Pagado}; SegundoRecordatorio→{Desactivado, Pagado}; Desactivado→{Pagado}; Pagado→{} (terminal).
