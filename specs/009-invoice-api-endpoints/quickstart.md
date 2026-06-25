# Quickstart — Validación de Endpoints API de Facturas

Guía para validar end-to-end los cuatro endpoints. No contiene implementación; consulta [contracts/](./contracts/) y [data-model.md](./data-model.md) para los detalles.

## Prerrequisitos

- .NET 10 SDK (10.0.301) instalado.
- MongoDB accesible (vía Docker Compose del proyecto) y `ASPNETCORE_ENVIRONMENT=Development` para activar el seeder de datos de prueba (spec 008), que crea facturas en estados mixtos.

## Ejecutar la suite de tests (Test-First)

```bash
cd backend
dotnet test
```

Resultado esperado: todas las suites en verde, incluyendo los nuevos tests de `Tests/Monolegal.Application.Tests/Endpoints` (listado, detalle, transición, estadísticas) y cobertura ≥85%.

## Levantar la API

```bash
cd backend/Api
dotnet run
```

La API queda disponible (con OpenAPI en `/openapi` en Development).

## Escenarios de validación manual

### 1. Listado con filtro y paginación (Spec 2.1)

```bash
curl "http://localhost:5000/api/invoices?status=primerrecordatorio&page=1&pageSize=10"
```
Esperado: `200`, `data` con facturas en `primerrecordatorio` ordenadas por `createdAt` desc, `total` = total filtrado, `pageSize` = 10.

Casos de error:
```bash
curl "http://localhost:5000/api/invoices?pageSize=51"   # → 400 (tope 50)
curl "http://localhost:5000/api/invoices?page=0"        # → 400
curl "http://localhost:5000/api/invoices?status=foo"    # → 400
```

### 2. Detalle (Spec 2.2)

```bash
curl "http://localhost:5000/api/invoices/{id-existente}"   # → 200 objeto completo
curl "http://localhost:5000/api/invoices/no-existe"        # → 404
```

### 3. Transición de estado (Spec 2.3)

```bash
curl -X POST "http://localhost:5000/api/invoices/transition/{id}" \
  -H "Content-Type: application/json" \
  -d '{ "newStatus": "segundorecordatorio" }'
```
Esperado: `200` con la factura actualizada si la transición está permitida; `400` si no lo está (la factura no cambia); `404` si el id no existe.

### 4. Estadísticas (Spec 2.4)

```bash
curl "http://localhost:5000/api/invoices/stats"
```
Esperado: `200` con `totalInvoices`, `byStatus`, `byClient`. Verificar la invariante `Σ(byStatus) == totalInvoices`.

## Criterios de aceptación (resumen)

- Filtro y paginación correctos con `total` independiente de la página.
- Detalle `200`/`404` (id inválido → `404`).
- Transición válida persiste y devuelve `200`; inválida → `400` sin cambios; inexistente → `404`.
- Estadísticas con invariante de suma cumplida.
- Todas las respuestas de error son controladas (sin `500`).
