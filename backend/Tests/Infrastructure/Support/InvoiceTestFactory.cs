using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Helper de datos para construir facturas de prueba en estados concretos (spec 007, T004).
/// </summary>
public static class InvoiceTestFactory
{
    /// <summary>
    /// Crea una <see cref="Invoice"/> válida para el cliente dado y, opcionalmente, la lleva
    /// a un estado específico vía <see cref="Invoice.UpdateStatus"/>.
    /// </summary>
    public static Invoice Create(
        string clientId,
        decimal amount = 100m,
        InvoiceStatus? status = null)
    {
        var invoice = new Invoice(clientId, amount);
        if (status.HasValue && status.Value != invoice.Status)
        {
            invoice.UpdateStatus(status.Value);
        }

        return invoice;
    }
}
