# Contrato — Generador de documentación de API (`scripts/gen-api-docs.mjs`)

Script Node sin dependencias que transforma el snapshot OpenAPI en la referencia de endpoints y la colección de Postman, de forma reproducible.

## Invocación

```bash
node scripts/gen-api-docs.mjs            # genera/actualiza los artefactos
node scripts/gen-api-docs.mjs --verify   # verifica: falla (exit ≠ 0) si la salida difiere de lo versionado
```

(Opcionalmente expuesto como script npm en el `package.json` raíz, p. ej. `npm run docs:api` y `npm run docs:api:verify`.)

## Entradas

- `docs/openapi.json` — snapshot del documento OpenAPI del backend (obtenido de `/openapi/v1.json`). Es la **única** fuente de verdad del generador.

## Salidas

- `docs/api-reference.md` — referencia en Markdown: por cada operación, método + ruta + resumen/propósito + parámetros (ruta/consulta/cuerpo) + códigos de respuesta; agrupada por tag/recurso (Clients, Invoices, Settings, Workers).
- `docs/postman/monolegal.postman_collection.json` — colección Postman v2.1 (ver `postman-collection.md`).

## Comportamiento

- **Determinista**: misma entrada ⇒ misma salida byte a byte (orden estable de rutas/operaciones, sin timestamps).
- **Cobertura total**: incluye el 100% de las operaciones presentes en `docs/openapi.json` (SC-003).
- **No interactivo**: no solicita entrada; apto para CI.
- **`--verify`**: regenera a memoria/temporal y compara con los archivos versionados; exit `0` si coinciden, `≠ 0` si difieren (para detectar documentación desincronizada en CI, FR-010).
- **Sin secretos**: nunca embebe credenciales; los valores sensibles se representan como variables Postman.

## Refresco del snapshot (fuera del generador)

Cuando cambian los endpoints, se actualiza el snapshot antes de regenerar:

```bash
# con el backend en Development sirviendo /openapi/v1.json
curl http://localhost:5155/openapi/v1.json -o docs/openapi.json
node scripts/gen-api-docs.mjs
```

## No objetivos

- No levanta el backend ni depende de un servicio en ejecución para generar (solo para refrescar el snapshot).
- No modifica endpoints ni esquemas; solo lee el documento OpenAPI.
