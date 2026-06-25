# Contrato: Interfaz del Repositorio de Facturas

**Relacionado con**: [plan.md](../plan.md) | [data-model.md](../data-model.md)

## `IInvoiceRepository`

Esta interfaz define el contrato que la capa de Infraestructura (`Infrastructure`) debe implementar para la persistencia de la entidad `Invoice`. La interfaz residirá en la capa de `Domain`, invirtiendo así la dependencia (Principio de Inversión de Dependencias - SOLID).

### Métodos Propuestos

```csharp
namespace Monolegal.Domain.Repositories;

public interface IInvoiceRepository
{
    /// <summary>
    /// Obtiene una factura por su identificador único.
    /// </summary>
    Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las facturas asociadas a un cliente específico.
    /// </summary>
    Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda una nueva factura en el repositorio.
    /// </summary>
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza una factura existente en el repositorio.
    /// </summary>
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
```

### Reglas para la Implementación (Capa Infrastructure)
- La implementación debe mapear la propiedad `string Id` de la entidad a un tipo `ObjectId` de MongoDB al insertar.
- Al recuperar datos desde MongoDB, debe mapear de vuelta el `ObjectId` a una representación `string`.
- Ninguno de estos métodos debe filtrar excepciones específicas de MongoDB hacia la capa de aplicación o dominio.
