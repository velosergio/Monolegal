# Contrato — Interfaz Swagger UI

**Recurso**: `GET /swagger`

**Disponibilidad**: solo entorno `Development` (gate `IsDevelopment()`).

**Servido por**: `Swashbuckle.AspNetCore.SwaggerUI` (`UseSwaggerUI`), consumiendo `/openapi/v1.json`.

## Petición

```
GET /swagger
```

(El middleware puede redirigir a `/swagger/index.html`.)

## Respuesta

- **200 OK** (`text/html`): página HTML de Swagger UI que carga el documento desde `/openapi/v1.json`.
- **404 Not Found**: en entornos donde la documentación está deshabilitada (p. ej. Production).

## Comportamiento verificable

| Requisito | Comportamiento esperado |
|-----------|--------------------------|
| FR-001 | `/swagger` responde `200` con la página de documentación interactiva en Development. |
| FR-002 | La página lista todas las operaciones del documento (los 4 endpoints de facturas y demás registrados). |
| FR-003 / FR-004 | Cada operación muestra método, ruta, descripción y parámetros (ruta/consulta/cuerpo). |
| FR-005 | La sección de esquemas (Schemas) muestra los DTO de entrada/salida con sus campos. |
| FR-006 | Cada operación lista sus códigos de respuesta (`200`, `400`, `404`) con descripción. |
| FR-007 | "Try it out" habilita la edición de parámetros y la acción de ejecución. |
| FR-008 | Ejecutar una petición muestra el código de estado y el cuerpo reales de la API. |
| FR-011 | El botón "Authorize" permite introducir un Bearer token para "Try it out" sobre endpoints protegidos. |

## Configuración (referencia, ver `Program.cs`)

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // ya existente → /openapi/v1.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Monolegal API v1");
        // ruta por defecto de la UI: /swagger
    });
}
```

## Verificación manual de "Try it out" (FR-007/FR-008)

1. Iniciar el backend en entorno `Development`.
2. Abrir `https://localhost:<puerto>/swagger`.
3. Expandir `GET /api/invoices/stats`, pulsar **Try it out** → **Execute**.
4. Confirmar que se muestra `200` y un cuerpo con `totalInvoices`, `byStatus`, `byClient`.
