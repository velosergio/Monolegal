# Quickstart — Validación de la Documentación Swagger/OpenAPI (spec 010)

Guía para validar end-to-end que la documentación interactiva funciona. No contiene código de implementación; referencia los contratos en `contracts/` y el modelo en `data-model.md`.

## Prerrequisitos

- SDK .NET 10 instalado.
- Paquete `Swashbuckle.AspNetCore.SwaggerUI` (10.1.7) referenciado en `backend/Api/Api.csproj`.
- Endpoints de facturas de la spec 009 implementados (ya presentes en `backend/Api/Endpoints/Invoices`).
- Entorno de ejecución = `Development` (la documentación está restringida a no-producción, ver research.md D3).

## 1. Levantar el backend

```powershell
cd backend/Api
dotnet run
```

Asegúrate de usar un perfil con `ASPNETCORE_ENVIRONMENT=Development`. El perfil `https` de `launchSettings.json` abre el navegador en `/swagger` automáticamente (`launchUrl: "swagger"`).

## 2. Validar el documento OpenAPI (FR-002, FR-005, FR-009)

```powershell
curl -k https://localhost:<puerto>/openapi/v1.json
```

**Esperado**: `200` con un documento OpenAPI 3.x que incluye las rutas `/api/invoices`, `/api/invoices/{id}`, `/api/invoices/transition/{id}`, `/api/invoices/stats`, los esquemas de los DTO y el `securityScheme` Bearer. Ver `contracts/openapi-document.md`.

## 3. Validar la interfaz Swagger UI (FR-001..FR-006)

Abrir en el navegador:

```
https://localhost:<puerto>/swagger
```

**Esperado**:
- La página carga sin errores y lista todas las operaciones (FR-001/FR-002).
- Cada operación muestra método, ruta, descripción y parámetros (FR-003/FR-004).
- La sección **Schemas** muestra `InvoiceListItemDto`, `InvoiceDetailDto`, `TransitionRequest`, `InvoiceStatsDto` y la respuesta paginada (FR-005).
- Cada operación lista sus códigos de estado (`200`/`400`/`404`) (FR-006).

## 4. Validar "Try it out" (FR-007, FR-008)

1. Expandir `GET /api/invoices/stats`.
2. Pulsar **Try it out** → **Execute**.
3. **Esperado**: la página muestra el código de estado `200` y un cuerpo real con `totalInvoices`, `byStatus`, `byClient`, además de la URL invocada.
4. (Opcional, endpoints protegidos) Pulsar **Authorize**, introducir un Bearer token y repetir "Try it out" (FR-011).

## 5. Validar restricción por entorno (research.md D3)

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet run
```

**Esperado**: `GET /swagger` y `GET /openapi/v1.json` devuelven `404` (documentación no expuesta en producción).

## 6. Pruebas automatizadas

```powershell
cd backend
dotnet test
```

**Esperado**: pasan las pruebas de integración de documentación (`OpenApiDocumentTests`, `SwaggerUiTests`) que verifican la presencia del documento, las operaciones/esquemas esperados y la respuesta de la UI en Development.

## Criterios de aceptación cubiertos

| Paso | Requisitos / Success Criteria |
|------|-------------------------------|
| 2 | FR-002, FR-005, FR-009, FR-010 · SC-001, SC-003, SC-006 |
| 3 | FR-001..FR-006 · SC-001, SC-002, SC-003 |
| 4 | FR-007, FR-008, FR-011 · SC-004 |
| 5 | research.md D3 (Assumption de exposición en producción) |
