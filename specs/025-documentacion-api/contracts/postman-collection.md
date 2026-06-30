# Contrato — Colección y entorno de Postman

## Colección — `docs/postman/monolegal.postman_collection.json`

- **Formato**: Postman Collection v2.1.0.
- **Origen**: generada por `scripts/gen-api-docs.mjs` desde `docs/openapi.json`.
- **Organización**: carpetas por recurso/tag (Clients, Invoices, Settings, Workers); una request por operación con nombre legible.
- **Requests**: cada una incluye método, URL basada en `{{baseUrl}}` + ruta (con `:param` para parámetros de ruta), cabeceras necesarias (`Content-Type: application/json` cuando hay cuerpo) y, cuando aplica, un cuerpo de ejemplo derivado del esquema OpenAPI.
- **Autenticación**: a nivel de colección, tipo Bearer con `{{token}}` (coherente con el esquema de seguridad Bearer/JWT declarado en OpenAPI).
- **Variables**: usa `{{baseUrl}}` y `{{token}}`; **sin** valores reales ni secretos embebidos (FR-006, FR-009, SC-006).

## Entorno — `docs/postman/monolegal.postman_environment.json`

Plantilla de entorno importable con placeholders:

| Variable | Valor por defecto | Descripción |
|----------|-------------------|-------------|
| `baseUrl` | `http://localhost:5155` | Base del backend (dev local). En Docker/prod se ajusta al host correspondiente |
| `token` | *(vacío)* | JWT del administrador; se completa por el usuario, nunca se versiona con valor real |

## Criterios de aceptación

- La colección se importa en Postman sin errores (SC-005).
- Ejecutar una request de lectura (p. ej. `GET {{baseUrl}}/api/invoices`) contra una instancia local configurada devuelve una respuesta válida (SC-005).
- El archivo de entorno no contiene credenciales reales (SC-006).
- La colección refleja el 100% de los endpoints del snapshot (coherente con `api-reference.md`).
