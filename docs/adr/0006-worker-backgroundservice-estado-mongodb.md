# ADR 0006 — Worker de transiciones como BackgroundService sin estado en memoria

**Estado**: Aceptada · **Fecha**: 2026-06-30 · **Feature**: 026 — Comentarios de Código y Documentación de Arquitectura

> ADR retroactivo: documenta una decisión estructural vigente (spec 012).

## Contexto

Las facturas avanzan por su ciclo de estado (Pending → PrimerRecordatorio → SegundoRecordatorio →
Desactivado) cuando vencen los plazos, disparando notificaciones por correo. Este trabajo debe
ejecutarse de forma periódica y autónoma, y el despliegue contempla **escalado horizontal** mediante
réplicas Docker del worker.

## Decisión

Implementar el procesamiento como un `BackgroundService` (`InvoiceTransitionsWorker`) que corre en un
intervalo configurable y **no mantiene estado en memoria**: en cada ciclo lee la configuración y las
facturas elegibles desde MongoDB (vía `ISystemSettingsRepository` e `IInvoiceRepository`), aplica las
transiciones y persiste el resultado. Todo el estado vive en MongoDB.

## Alternativas consideradas

- **Estado/planificación en memoria del proceso**: impediría el escalado horizontal y perdería trabajo
  ante reinicios. Descartada (contradice el Principio de Performance & Escalabilidad de la Constitución).
- **Un planificador externo (cron/Hangfire/Quartz)**: dependencia e infraestructura adicionales
  innecesarias para un intervalo simple. Descartada.

## Consecuencias

- **Positivas**: horizontalmente escalable; resistente a reinicios; lógica de transición compartida con
  la API; dependencias inyectadas como abstracciones (DIP), fácilmente testeable.
- **Negativas / costes**: con varias réplicas debe evitarse el doble procesamiento de la misma factura
  (idempotencia/condiciones de carrera a vigilar en la capa de datos).
