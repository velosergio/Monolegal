# Architecture Decision Records (ADR)

Registro de decisiones arquitectónicas no obvias de Monolegal. Cada ADR captura **contexto,
decisión, alternativas consideradas y consecuencias** de forma persistente, para dar trazabilidad y
evitar revisitar análisis ya cerrados (Principio VI de la [Constitución](../../.specify/memory/constitution.md)).

## Cómo crear un ADR

1. Copiar [`0000-plantilla.md`](./0000-plantilla.md) a `NNNN-titulo-kebab.md` con el siguiente número
   secuencial.
2. Rellenar las secciones; redactar en **español**.
3. Si la decisión reemplaza a otra, enlazar con `Reemplaza` / `Reemplazada por` y marcar el ADR previo
   como `Reemplazada`.
4. Añadir la fila correspondiente al índice de abajo.

## Estados

`Propuesta` → `Aceptada` → `Reemplazada` / `Obsoleta`.

## Índice

| ADR | Título | Estado | Fecha |
|-----|--------|--------|-------|
| [0000](./0000-plantilla.md) | Plantilla de ADR | — | — |
| [0001](./0001-verificacion-conexion-mongodb.md) | Verificación de conexión a MongoDB (fail-soft) y health check | Aceptada | 2026-06-24 |
| [0002](./0002-documentacion-openapi-generada.md) | Documentación de API generada desde un snapshot OpenAPI | Aceptada | 2026-06-30 |
| [0003](./0003-repositorios-singleton-mongodb.md) | Repositorios y cliente de MongoDB con ciclo de vida Singleton | Aceptada | 2026-06-30 |
| [0004](./0004-seleccion-proveedor-email-runtime.md) | Selección del proveedor de correo en runtime con factory y fallback NoOp | Aceptada | 2026-06-30 |
| [0005](./0005-migraciones-idempotentes-hostedservice.md) | Migraciones de datos idempotentes como IHostedService al arranque | Aceptada | 2026-06-30 |
| [0006](./0006-worker-backgroundservice-estado-mongodb.md) | Worker de transiciones como BackgroundService sin estado en memoria | Aceptada | 2026-06-30 |
