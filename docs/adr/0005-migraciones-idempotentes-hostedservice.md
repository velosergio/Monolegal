# ADR 0005 — Migraciones de datos idempotentes como IHostedService al arranque

**Estado**: Aceptada · **Fecha**: 2026-06-30 · **Feature**: 026 — Comentarios de Código y Documentación de Arquitectura

> ADR retroactivo: documenta una decisión vigente introducida en las specs 015 y 018.

## Contexto

Algunos cambios de modelo requieren transformar datos existentes en MongoDB: el *backfill* del
historial de estados (spec 015) y el de items/vencimiento de facturas previas (spec 018). MongoDB no
tiene un sistema de migraciones versionadas como las bases relacionales, y el despliegue se hace con
réplicas (worker escalable), por lo que una migración podría ejecutarse más de una vez.

## Decisión

Implementar cada migración como un `IHostedService` registrado en `AddInfrastructure`
(`StatusHistoryBackfillMigration`, `InvoiceItemsBackfillMigration`) que se ejecuta al arranque y es
**idempotente**: detecta si el dato ya está migrado y no repite el trabajo, de modo que reejecutarla
(o ejecutarla en varias réplicas) es seguro.

## Alternativas consideradas

- **Scripts de migración manuales fuera de la app**: requieren un paso operativo aparte, fácil de
  olvidar, y no garantizan que el esquema en runtime coincida con el de los datos. Descartada.
- **Una librería de migraciones versionadas para MongoDB**: dependencia adicional desproporcionada para
  el número de migraciones del proyecto. Descartada.

## Consecuencias

- **Positivas**: la migración viaja con el despliegue; segura ante reinicios y réplicas; sin pasos
  manuales; observable vía logging estructurado.
- **Negativas / costes**: cada migración debe escribirse cuidando la idempotencia; el coste de la
  comprobación se paga en cada arranque (mitigado porque la comprobación es barata cuando ya está hecha).
