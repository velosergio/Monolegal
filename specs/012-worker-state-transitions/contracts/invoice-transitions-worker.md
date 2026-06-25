# Contrato — Worker de Transiciones de Estado

Este worker es un componente interno (no expone una API pública propia), pero su **comportamiento observable** constituye un contrato verificable. Adicionalmente, existe un endpoint manual que ejecuta la misma lógica de ciclo bajo demanda.

## 1. Contrato de comportamiento del ciclo (`InvoiceTransitionsWorker.RunCycleAsync`)

**Precondición**: Repositorios de facturas y settings disponibles; `now` (reloj) provisto.

**Pasos del ciclo**:

1. Cargar `InvoiceTransitionsConfig` desde `ISystemSettingsRepository.GetSettingsAsync()`.
2. Obtener candidatos vía `IInvoiceRepository.GetTransitionableAsync()` (solo `Pending`, `PrimerRecordatorio`, `SegundoRecordatorio`).
3. Por cada candidato: evaluar `InvoiceTransitionService.TryApplyTransition(invoice, config, now)`.
   - Si devuelve `true`: persistir con `IInvoiceRepository.UpdateAsync(invoice)` y registrar la transición.
   - Si lanza excepción: registrar el error con `InvoiceId`, incrementar `Errors` y **continuar** con el siguiente candidato.
4. Emitir resumen estructurado del ciclo.

**Postcondiciones / invariantes**:

| ID | Invariante |
|----|-----------|
| C-01 | Toda factura elegible (días cumplidos) queda en su estado siguiente tras el ciclo. (FR-003/FR-004/FR-005, SC-001) |
| C-02 | Ninguna factura en `Pagado`/`Desactivado` (ni estados legacy) es modificada. (FR-006, SC-002) |
| C-03 | Un error al procesar una factura no impide procesar el resto del lote. (FR-007, SC-004) |
| C-04 | El resumen del ciclo incluye `Timestamp`, `Evaluated`, `Transitioned`, `Errors`, `DurationMs`. (FR-008) |
| C-05 | Por cada transición aplicada se registra `InvoiceId`, estado anterior y nuevo. (FR-009) |
| C-06 | Un repositorio sin candidatos produce un ciclo válido con `Evaluated=0`, `Transitioned=0`, `Errors=0`. (Edge case) |
| C-07 | Ante `CancellationToken` señalado, el ciclo/bucle termina de forma ordenada sin dejar estado inconsistente. (FR-011) |

## 2. Contrato de configuración (`InvoiceTransitionsWorkerOptions`)

Sección de configuración `InvoiceTransitionsWorker` (enlazable por `appsettings` o variable de entorno):

| Clave | Tipo | Default | Regla |
|-------|------|---------|-------|
| `InvoiceTransitionsWorker:IntervalMinutes` | `int` | 60 | > 0; valor inválido o ausente → default + log. (FR-001/FR-002) |
| `InvoiceTransitionsWorker:RunOnStartup` | `bool` | true | Ejecuta el primer ciclo al arrancar. |

Variable de entorno equivalente (formato .NET): `InvoiceTransitionsWorker__IntervalMinutes`.

**Invariante C-08**: cambiar `IntervalMinutes` modifica la frecuencia efectiva sin cambios de código (SC-005).

## 3. Endpoint manual de disparo (existente)

`POST /api/workers/trigger-transitions`

Ejecuta un ciclo bajo demanda (útil para pruebas E2E sin esperar el intervalo).

**Request**: sin cuerpo.

**Response 200 OK**:

```json
{
  "evaluated": 3,
  "transitioned": 2
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `evaluated` | `int` | Facturas candidatas evaluadas en el ciclo. |
| `transitioned` | `int` | Facturas que cambiaron de estado. |

> Nota: el endpoint y el worker deben compartir la misma lógica de ciclo. Como mejora opcional (no obligatoria en esta spec) puede extraerse a un método/servicio común para evitar duplicación; hoy el endpoint replica el flujo y debe incluir el conteo de errores para mantenerse consistente con C-04.
