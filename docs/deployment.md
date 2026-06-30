# Guía de despliegue — Monolegal

Despliegue del stack a un entorno productivo (VPS con Docker). El objetivo es paridad con el
entorno local pero con configuración endurecida: secretos solo por entorno, Swagger restringido y
MongoDB gestionado.

## Requisitos

- Host con Docker y Docker Compose (VPS Linux recomendado).
- Imágenes construidas con el `Dockerfile` multi-stage del repo (tamaño final < 500 MB,
  Principio de infraestructura de la Constitución).
- MongoDB de producción (clúster gestionado o contenedor con volumen persistente).

## Construcción y publicación de imágenes

```bash
# Construir la imagen multi-stage
docker build -t monolegal:latest .

# Etiquetar y publicar a tu registry
docker tag monolegal:latest your-registry/monolegal:latest
docker push your-registry/monolegal:latest
```

## Despliegue con Docker Compose

```bash
# En el VPS, con las variables de producción en el entorno o en un .env seguro
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

Servicios desplegados: `frontend`, `backend`, `worker`, `mongo` (o MongoDB externo).

## Variables de entorno de producción

Las credenciales se inyectan como variables de entorno / secretos del orquestador, **nunca**
embebidas en imágenes ni en la base de datos.

| Variable | Valor (producción) | Notas |
|----------|--------------------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Deshabilita Swagger por defecto |
| `MONGODB_URI` | `mongodb+srv://user:pass@cluster/monolegal_prod` | Secreto. Pooling configurado |
| `VITE_API_PROXY_TARGET` | URL interna del backend | Para el proxy `/api` del frontend |
| `VITE_SWAGGER_URL` | *(vacío)* | Vacío ⇒ el botón de Swagger se oculta (ver abajo) |
| `LOG_LEVEL` | `Information` | Nivel de Serilog |
| `Email__*`, `Email__Resend__ApiKey` | — | Secretos del proveedor de correo |

## Exposición de Swagger en producción

Por decisión de diseño (spec 010, D3), la documentación interactiva **solo se habilita en
`Development`**: en `Production` el backend no mapea `/openapi/v1.json` ni `/swagger`.

Implicaciones para el despliegue:

- **Recomendado (por defecto)**: dejar Swagger deshabilitado y `VITE_SWAGGER_URL` **vacío**. El
  frontend oculta automáticamente el botón "API (Swagger)" del sidebar, evitando un enlace roto.
  La documentación de la API queda disponible de forma estática en
  [`docs/api-reference.md`](./api-reference.md) y en la [colección de Postman](./postman/).
- **Si necesitas Swagger en producción**: habilítalo de forma controlada (p. ej. extender el bloque
  `if (app.Environment.IsDevelopment())` de `backend/Api/Program.cs` a un entorno protegido, o
  exponer `/swagger` solo tras autenticación/red interna) y configura `VITE_SWAGGER_URL` con la URL
  pública correspondiente. Ten en cuenta el riesgo de exponer la superficie de la API públicamente.

> El reverse proxy de producción solo necesita enrutar `/` (frontend) y `/api` (backend). Enrutar
> `/swagger` y `/openapi` es opcional y solo aplica si decides exponer la documentación interactiva.

## Verificación post-despliegue

```bash
curl https://<host>/health            # 200 Healthy (ping real a MongoDB)
curl https://<host>/api/invoices      # responde con la lista paginada
```

- Comprueba los logs estructurados (Serilog) del backend y worker.
- Confirma que el worker procesa transiciones (revisa `LastStatusTransitionAt` de las facturas).

## Mantenimiento de la documentación de la API

Cuando cambian los endpoints, refresca el snapshot y regenera los artefactos (ver
[ADR 0002](./adr/0002-documentacion-openapi-generada.md)):

```bash
# con el backend en Development sirviendo /openapi/v1.json
curl http://localhost:5155/openapi/v1.json -o docs/openapi.json
npm run docs:api            # regenera api-reference.md y la colección Postman
npm run docs:api:verify     # verificación usada en CI (falla si hay desincronización)
```
