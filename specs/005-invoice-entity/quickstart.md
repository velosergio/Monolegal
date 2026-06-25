# Quickstart: Validación de la Entidad Invoice

**Relacionado con**: [plan.md](./plan.md) | [data-model.md](./data-model.md)

Esta guía documenta cómo validar que la entidad `Invoice` ha sido implementada correctamente a nivel de dominio, mediante la ejecución de sus pruebas unitarias.

## Prerrequisitos

- .NET 10 SDK instalado.
- Acceso a la solución `Monolegal` y los proyectos `Monolegal.Domain` y `Monolegal.Domain.Tests`.

## Pasos de Validación

### 1. Ejecutar las pruebas unitarias

El diseño estricto de la entidad de dominio asegura que no requiere bases de datos corriendo ni infraestructura. Puedes ejecutar las pruebas de dominio directamente:

```bash
cd backend/tests/Monolegal.Domain.Tests
dotnet test --filter "FullyQualifiedName~InvoiceTests"
```

### 2. Resultados Esperados

Todas las pruebas deben pasar exitosamente. La suite de pruebas debe confirmar lo siguiente:
- **Validación del Constructor**: Intentar inicializar un `Invoice` con `Amount` = 0 arroja una excepción.
- **Asignación de Estado Inicial**: Al crear la entidad, su `Status` es `Draft` o `Pending`, y `RemindersCount` es 0.
- **Fechas de Auditoría**: `CreatedAt` se asigna al instanciar. 
- **Invariantes de Mutación**: Invocar `RecordReminderSent()` incrementa `RemindersCount`, establece `LastReminderSentAt` y actualiza `UpdatedAt`.

## Validación Manual (Opcional - REPL / F# Interactive o aplicación de consola temporal)

Si deseas probar la entidad de manera interactiva sin las pruebas predefinidas:

1. Crea un proyecto de consola de prueba y referencia a `Monolegal.Domain`:
   ```bash
   dotnet new console -n TestDomain
   dotnet add reference ../backend/src/Monolegal.Domain/Monolegal.Domain.csproj
   ```

2. Ejecuta el siguiente flujo:
   ```csharp
   using Monolegal.Domain.Entities;

   // 1. Creación válida
   var invoice = new Invoice(clientId: "client_123", amount: 1500.50m);
   Console.WriteLine($"Factura creada. Estado: {invoice.Status}, Monto: {invoice.Amount}");
   // Esperado: Estado: Draft/Pending, Monto: 1500.50

   // 2. Modificación y validación de invariantes
   invoice.RecordReminderSent();
   Console.WriteLine($"Recordatorios: {invoice.RemindersCount}, UpdatedAt: {invoice.UpdatedAt}");
   // Esperado: Recordatorios: 1, UpdatedAt debe reflejar la hora actual (distinta de CreatedAt si hay pausa).
   ```
