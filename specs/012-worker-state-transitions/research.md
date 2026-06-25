# Research — Worker: Transiciones de Estado (Phase 0)

Decisiones técnicas que resuelven los puntos abiertos del Technical Context. El estado actual del código se inspeccionó directamente; muchas decisiones confirman lo ya implementado y otras definen las brechas a cerrar.

## D1 — Mecanismo de scheduling

- **Decisión**: Mantener `BackgroundService` (`InvoiceTransitionsWorker`) con bucle `while (!stoppingToken.IsCancellationRequested)` que ejecuta un ciclo y luego espera `Task.Delay(intervalo, stoppingToken)`.
- **Rationale**: Ya está implementado, es el patrón nativo de .NET para hosted services, se integra con el apagado ordenado del host y no requiere dependencias externas. La ejecución secuencial dentro de una instancia evita por construcción el solapamiento de ciclos (FR-010).
- **Alternativas consideradas**: Quartz.NET / Hangfire (overkill para un único job periódico, añade dependencia e infraestructura); `PeriodicTimer` (válido, pero el bucle actual con `Task.Delay` ya cubre el caso y permite "ejecutar al arranque").

## D2 — Configuración del intervalo (brecha FR-001/FR-002, SC-005)

- **Decisión**: Externalizar el intervalo a una clase de opciones `InvoiceTransitionsWorkerOptions { int IntervalMinutes = 60; bool RunOnStartup = true; }`, enlazada desde `IConfiguration` (sección `InvoiceTransitionsWorker`, sobreescribible por variable de entorno) e inyectada vía `IOptions<>`. Si no hay valor configurado, se usa el default (60 min) y se registra cuál se aplica.
- **Rationale**: Cumple "intervalo configurable sin recompilar" y "default razonable si no se especifica". El intervalo es configuración **operativa/infra**, distinta de los umbrales de días, que son configuración **de negocio administrable** (`SystemSettings`). Mantenerlos separados respeta SRP y la fuente de verdad de cada uno.
- **Alternativas consideradas**: Guardar el intervalo en `SystemSettings`/Mongo (mezcla config operativa con la de negocio y obliga a relectura por ciclo; innecesario); dejar la constante actual (incumple FR-001/FR-002).

## D3 — Aislamiento de errores por factura (brecha FR-007, SC-004)

- **Decisión**: Envolver el procesamiento de **cada factura** en su propio `try/catch` dentro del bucle. Un fallo registra el error con `InvoiceId` y continúa con la siguiente factura, incrementando un contador `errors`. El `try/catch` de ciclo completo se conserva como red de seguridad (fail-soft del ciclo).
- **Rationale**: El comportamiento actual envuelve todo el lote en un único `try/catch`, de modo que una excepción en una factura aborta el resto del ciclo. La spec exige que el lote continúe (SC-004 = 100%).
- **Alternativas consideradas**: Detener el ciclo ante el primer error (rechazado por la spec); reintentos por factura dentro del mismo ciclo (innecesario; el siguiente ciclo reintenta).

## D4 — No solapamiento y concurrencia multi-réplica (FR-010)

- **Decisión**: Para una instancia, el bucle secuencial garantiza no solapamiento. Para múltiples réplicas, apoyarse en la **idempotencia** de la transición: el servicio relee el estado y la condición de días antes de aplicar, y la persistencia es por documento; aplicar dos veces no produce un salto de estado inválido relevante. Un bloqueo distribuido (p. ej. lock en Mongo) se documenta como mejora futura fuera de alcance.
- **Rationale**: Equilibra simplicidad y la garantía de "no procesar las mismas facturas dos veces" a nivel de efecto de negocio. La constitución pide worker sin estado y escalable; el lock distribuido es optimización, no requisito de esta spec.
- **Alternativas consideradas**: Lock distribuido obligatorio (complejidad innecesaria para el volumen actual); singleton de réplica única (limita escalado horizontal).

## D5 — Cálculo del "tiempo transcurrido"

- **Decisión**: Usar `Invoice.LastStatusTransitionAt` como base del cálculo de elegibilidad (ya implementado en `InvoiceTransitionService.TryTransition`), con `now` inyectado para pruebas deterministas.
- **Rationale**: La transición depende del tiempo desde la última transición de estado, no desde la creación. `LastStatusTransitionAt` se inicializa con la creación, por lo que el primer salto (`Pending → PrimerRecordatorio`) también funciona. Inyectar `now` mantiene los tests deterministas.
- **Alternativas consideradas**: Usar `CreatedAt` (incorrecto para los saltos posteriores); usar `LastReminderSentAt` (ese campo pertenece al flujo de envío de correos, spec 3.3, no a la transición).

## D6 — Observabilidad del ciclo (brecha FR-008)

- **Decisión**: El log de fin de ciclo incluye `Timestamp`, `Evaluated`, `Transitioned`, `Errors` y `DurationMs`; por cada transición se registra `InvoiceId`, `PreviousStatus`, `NewStatus`. Serilog en formato estructurado.
- **Rationale**: El worker actual ya registra evaluadas/transicionadas/duración y el detalle por transición; solo falta exponer el contador de errores introducido en D3 para completar FR-008.
- **Alternativas consideradas**: Métricas/contadores (OpenTelemetry) — valiosas pero fuera del alcance; spec 3.4 cubre logging/monitoring más amplio.

## D7 — Estrategia de pruebas del worker (Test-First)

- **Decisión**: Probar el método `internal RunCycleAsync` directamente con repositorios en memoria (reutilizando los fakes de `InvoiceWorkerTests`/`Tests/.../Support`). Habilitar acceso con `[assembly: InternalsVisibleTo("Tests")]` en el ensamblado `Infrastructure`. Casos: error aislado por factura (el lote continúa), resumen estructurado con conteo de errores, repositorio vacío (cero cambios), respeto del intervalo configurado y ejecución al arranque.
- **Rationale**: Las pruebas actuales validan un handler simulado, no la clase real del worker; para cubrir las brechas (D2/D3/D6) hay que ejercitar `RunCycleAsync`. `InternalsVisibleTo` ya se usa en `Domain` para `Tests`, por lo que es un patrón aceptado en el proyecto.
- **Alternativas consideradas**: Hacer `RunCycleAsync` público (expone API innecesaria); extraer un servicio de aplicación "TransitionRunner" y probarlo (refactor mayor; se puede considerar a futuro pero excede el alcance de cierre de brechas).
