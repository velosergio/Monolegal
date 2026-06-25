# Contrato: IInvoiceRepository

**Feature**: 007-invoice-mongo-repository
**Capa**: Domain (`Monolegal.Domain.Repositories`)
**Implementación**: Infrastructure (`Monolegal.Infrastructure.Repositories.MongoInvoiceRepository`)

Abstracción de acceso a datos de facturas. El dominio depende de esta interfaz; la implementación concreta vive en Infrastructure (Principio I — Arquitectura Limpia).

## Firma

```csharp
public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string id, InvoiceStatus newStatus, CancellationToken cancellationToken = default);
}
```

> Nota: el input de la spec nombra `InsertAsync(invoice)`. En el contrato esto se materializa como `AddAsync` (semántica de inserción de un documento nuevo). Ver `research.md` D1.

## Comportamiento esperado (contrato verificable)

### `GetByStatusAsync(status)` — FR-001, FR-008

| Given | When | Then |
|-------|------|------|
| Facturas en estados mixtos | se consulta un estado | devuelve solo las facturas en ese estado |
| Ninguna factura en el estado | se consulta ese estado | devuelve colección vacía (no error) |
| Varias facturas en el estado | se consulta ese estado | devuelve todas ellas |

### `GetByClientIdAsync(clientId)` — FR-002, FR-008

| Given | When | Then |
|-------|------|------|
| Facturas de varios clientes | se consulta un `clientId` | devuelve solo las de ese cliente |
| Cliente sin facturas | se consulta ese `clientId` | devuelve colección vacía |

### `AddAsync(invoice)` (Insert) — FR-004, SC-007

| Given | When | Then |
|-------|------|------|
| Factura válida nueva | se persiste | queda recuperable por `Id`, por estado y por cliente |

### `UpdateStatusAsync(id, newStatus)` — FR-003, FR-007, FR-009

| Given | When | Then |
|-------|------|------|
| Factura existente | se cambia su estado | refleja el nuevo estado y `LastStatusTransitionAt`/`UpdatedAt` actualizados |
| Factura existente | se cambia su estado | otros campos (Amount, RemindersCount) y otras facturas no se alteran |
| `id` inexistente | se intenta cambiar estado | no-op: 0 documentos modificados, sin excepción |

## Invariantes de infraestructura

- Las consultas por `Status` y `ClientId` se apoyan en índices `Status_asc` y `ClientId_asc` (FR-005, FR-006).
- `UpdateStatusAsync` actualiza únicamente `Status`, `UpdatedAt`, `LastStatusTransitionAt` (no reescribe el documento).
- Los índices se crean de forma idempotente al arranque (FR-010).
