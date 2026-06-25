# Research: invoice-status-transitions

## Decisión de Almacenamiento de Configuración

**Decision**: Almacenar los tiempos de transición en una colección `Settings` en MongoDB, con un documento único de configuración general (o por Tenant si aplica).

**Rationale**: Al ser un sistema con Minimal APIs y persistencia en MongoDB, mantener una colección `Settings` permite fácil extensión en el futuro sin modificar la entidad `Invoice`. La vista de configuración consultará y mutará este documento de manera atómica sin reiniciar contenedores.

**Alternatives considered**: 
- Almacenar configuración en `appsettings.json`. Rechazado porque la UI del frontend (vista de configuración) debe poder mutar este valor en tiempo real.
- Hardcodear en dominio. Rechazado por los requerimientos del feature de permitir a los administradores configurarlo.

## Mecanismo de Evaluación de Tiempos (Worker vs Cron)

**Decision**: Implementar un proceso en background en el Worker container.

**Rationale**: La constitución establece "Worker: Horizontalmente escalable vía Docker replicas; sin estado en-memoria". Un worker que se ejecute periódicamente consultará las facturas vencidas basándose en la configuración de `Settings` y emitirá comandos para actualizar sus estados usando las reglas de dominio.

**Alternatives considered**:
- Disparar la transición de manera perezosa al consultar la factura. Rechazado porque podría retrasar el disparo de eventos secundarios (ej. envío de correos).
