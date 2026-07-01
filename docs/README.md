# Documentación de Monolegal

Índice de la documentación del proyecto. Toda la documentación está en español
(Principio III de la [Constitución](../.specify/memory/constitution.md)).

## Entregables

| Documento | Contenido |
|-----------|-----------|
| [architecture.md](./architecture.md) | Panorama de arquitectura: capas del backend, componentes (backend, worker, frontend, MongoDB) y diagramas |
| [dependency-injection.md](./dependency-injection.md) | Mapa de Inyección de Dependencias: abstracción → implementación → ciclo de vida |
| [data-model.md](./data-model.md) | Modelo de datos: ERD, entidades, enums, colecciones y ciclo de estados |
| [api-reference.md](./api-reference.md) | Referencia de endpoints (**generada** desde `openapi.json`) |
| [setup.md](./setup.md) | Configuración local (Docker o servicios en local) y variables de entorno |
| [guia-despliegue-prueba-tecnica.md](./guia-despliegue-prueba-tecnica.md) | Despliegue y pruebas para evaluación de la prueba técnica |
| [deployment.md](./deployment.md) | Guía de despliegue a producción y exposición de Swagger |
| [postman/](./postman/) | Colección de Postman (**generada**) y plantilla de entorno |

## Documentación interactiva (Swagger UI)

Con el backend en ejecución (Development), Swagger UI está disponible en `/swagger`
(p. ej. http://localhost:5000/swagger con Docker, o http://localhost:5155/swagger en local). El
panel de administración incluye un acceso **API (Swagger)** en el sidebar. El documento OpenAPI se
sirve en `/openapi/v1.json`. En producción está deshabilitado por defecto (ver
[deployment.md](./deployment.md)).

## Decisiones de arquitectura (ADR)

Índice completo y plantilla en [`adr/README.md`](./adr/README.md).

- [0001 — Verificación de conexión a MongoDB](./adr/0001-verificacion-conexion-mongodb.md)
- [0002 — Documentación de API generada desde snapshot OpenAPI](./adr/0002-documentacion-openapi-generada.md)
- [0003 — Repositorios y cliente de MongoDB con ciclo de vida Singleton](./adr/0003-repositorios-singleton-mongodb.md)
- [0004 — Selección del proveedor de correo en runtime (factory + NoOp)](./adr/0004-seleccion-proveedor-email-runtime.md)
- [0005 — Migraciones idempotentes como IHostedService](./adr/0005-migraciones-idempotentes-hostedservice.md)
- [0006 — Worker BackgroundService sin estado en memoria](./adr/0006-worker-backgroundservice-estado-mongodb.md)

## Regenerar la documentación de la API

`api-reference.md` y la colección de Postman son **archivos generados** desde `docs/openapi.json`.
No se editan a mano:

```bash
# Refrescar el snapshot cuando cambian endpoints (backend en Development)
curl http://localhost:5155/openapi/v1.json -o docs/openapi.json

# Regenerar artefactos y verificar sincronización
npm run docs:api
npm run docs:api:verify
```

## Más documentación

- [README del proyecto](../README.md) — quick start, stack y troubleshooting
- [Constitución](../.specify/memory/constitution.md) — principios de desarrollo
- [Roadmap](../roadmap.md) y [especificaciones](../specs/) — diseño dirigido por specs
