# Quickstart — Validación del Worker de Transiciones

Guía para validar de extremo a extremo que el worker evalúa y aplica las transiciones automáticas. Ver detalles de comportamiento en [contracts/invoice-transitions-worker.md](./contracts/invoice-transitions-worker.md) y de datos en [data-model.md](./data-model.md).

## Prerrequisitos

- .NET 10 SDK instalado.
- MongoDB accesible (variable `MONGODB_URI`), o ejecutar las pruebas unitarias/integración que usan repositorios en memoria (no requieren Mongo).

## 1. Validación rápida por pruebas (sin Mongo)

Ejecuta la suite que cubre el ciclo del worker y las reglas de transición:

```powershell
cd backend
dotnet test
```

Resultados esperados:

- Las pruebas del ciclo del worker (`InvoiceTransitionsWorkerCycleTests`) pasan: aislamiento de error por factura, resumen con conteo de errores, repositorio vacío e intervalo configurable.
- Las pruebas existentes (`InvoiceWorkerTests`, `InvoiceStatusTransitionsTests`) siguen en verde.

## 2. Validación manual vía endpoint de disparo

Con el API corriendo en Development y datos de prueba sembrados (spec 008):

```powershell
# Lanzar el API
cd backend/Api
dotnet run
```

Disparar un ciclo bajo demanda (no espera el intervalo):

```powershell
curl -X POST http://localhost:5000/api/workers/trigger-transitions
```

Respuesta esperada (ejemplo):

```json
{ "evaluated": 3, "transitioned": 2 }
```

Verifica en los logs (Serilog) que aparezca el resumen estructurado del ciclo con `Evaluated`, `Transitioned`, `Errors` y `DurationMs`, y una línea por cada transición con `InvoiceId`, estado anterior y nuevo.

## 3. Validación del intervalo configurable

Configura el intervalo por variable de entorno y observa el log de arranque del worker:

```powershell
$env:InvoiceTransitionsWorker__IntervalMinutes = "5"
cd backend/Api
dotnet run
```

Resultado esperado: el log de inicio del worker indica el intervalo efectivo (5 min). Sin la variable, el log indica el default (60 min).

## 4. Escenarios de aceptación cubiertos

| Escenario (spec.md) | Cómo validar |
|---------------------|--------------|
| US1 — facturas elegibles transicionan | Pruebas `InvoiceTransitionsWorkerCycleTests` + endpoint de disparo. |
| US1 — terminales no cambian | Pruebas con facturas `Pagado`/`Desactivado` → `Evaluated=0`. |
| US2 — intervalo configurable | Paso 3 (variable de entorno) + log de arranque. |
| US3 — trazabilidad de ejecución | Inspección de logs estructurados (resumen + por transición). |
| Edge — error por factura aislado | Prueba con repositorio que lanza en una factura; el lote continúa y `Errors=1`. |
| Edge — sin candidatos | Prueba con repositorio vacío → ciclo válido con ceros. |
```
