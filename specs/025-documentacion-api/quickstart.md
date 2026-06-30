# Quickstart — Validación de la documentación (Spec 6.1 / 025)

Guía de validación para comprobar que los seis entregables existen, son correctos y que el acceso a Swagger desde el sidebar funciona. No incluye código de implementación (vive en `tasks.md` y la fase de implementación).

## Prerrequisitos

- Repo clonado; Node.js 22+, .NET 10 SDK, Docker (para levantar el stack).
- Para refrescar el snapshot OpenAPI: backend en Development sirviendo `/openapi/v1.json`.

## 1. Entregables de documentación escrita

```bash
ls docs/architecture.md docs/data-model.md docs/api-reference.md docs/setup.md docs/deployment.md docs/README.md
ls docs/postman/monolegal.postman_collection.json docs/postman/monolegal.postman_environment.json
```

**Esperado**: todos los archivos existen. Validar manualmente:
- `architecture.md`: describe las capas y componentes y contiene ≥ 1 diagrama Mermaid (FR-001).
- `data-model.md`: contiene un `erDiagram` con Client/Invoice/InvoiceItem/StatusChange (FR-002).
- `setup.md`: permite levantar el stack en local (FR-004).
- `deployment.md`: pasos de despliegue y exposición de Swagger (FR-005).

## 2. Setup verificado de extremo a extremo (FR-004, SC-001)

Siguiendo **solo** `docs/setup.md`:

```bash
docker-compose up -d --build
docker-compose ps           # 4 servicios "Up (healthy)"
curl http://localhost:5000/health
```

**Esperado**: una persona ajena al proyecto levanta el stack sin asistencia adicional.

## 3. Referencia de endpoints y Postman generados (FR-003, FR-006, FR-010)

```bash
# Regenerar desde el snapshot y verificar que no hay diff
node scripts/gen-api-docs.mjs
node scripts/gen-api-docs.mjs --verify   # exit 0 ⇒ sincronizado
```

**Esperado**: `docs/api-reference.md` lista el 100% de los endpoints (Clients, Invoices, Settings, Workers) con método/ruta/propósito/parámetros; `--verify` termina en `0`.

Importar en Postman `monolegal.postman_collection.json` + `monolegal.postman_environment.json`, fijar `baseUrl` y ejecutar:

```text
GET {{baseUrl}}/api/invoices
```

**Esperado**: importación sin errores y respuesta válida del endpoint de lectura (SC-005). El archivo de entorno no contiene credenciales reales (SC-006).

## 4. Acceso a Swagger desde el sidebar (FR-007, FR-008, FR-009)

```bash
cd frontend && npm run dev    # http://localhost:5173
```

- En el sidebar aparece el ítem **API (Swagger)** (AC-1).
- Al activarlo se abre Swagger UI en una pestaña nueva (`/swagger` vía proxy de dev) mostrando los endpoints (AC-2).
- Colapsando/expandiendo el sidebar el acceso sigue disponible (AC-4).
- Con `VITE_SWAGGER_URL` vacío, el ítem no se muestra (AC-5).

## 5. Pruebas automatizadas

```bash
cd frontend && npm run test:run    # incluye el test del ítem de Swagger en el sidebar
# Suite completa del proyecto (Spec 024):
npm run test:all
```

**Esperado**: el test del sidebar valida presencia, `href`, `target="_blank"` y `rel` seguro, y la ocultación con URL vacía. Las suites existentes siguen en verde.

## 6. Comprobaciones de invariantes

- Sin secretos en `docs/` ni en la colección/entorno Postman (SC-006).
- Sin cambios en endpoints, entidades ni lógica de negocio (SC-007) — `git diff` solo toca documentación, tooling de generación, configuración del frontend y el ítem de navegación.
- Toda la documentación en español (FR-011).
