using System;

namespace Monolegal.Domain.Entities;

/// <summary>
/// Línea de detalle de una factura (spec 018). Value object inmutable embebido en
/// <see cref="Invoice"/>: describe un concepto cobrado mediante descripción, cantidad y
/// precio unitario. El <see cref="Subtotal"/> siempre se deriva (cantidad × precio unitario)
/// y nunca se ingresa de forma independiente; la suma de subtotales determina el monto total
/// de la factura (RF-011, research D1/D2).
/// </summary>
public sealed class InvoiceItem
{
    public string Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    /// <summary>Subtotal derivado de la línea: <c>Quantity × UnitPrice</c>.</summary>
    public decimal Subtotal => Quantity * UnitPrice;

    public InvoiceItem(string description, decimal quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción del item es obligatoria.", nameof(description));

        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor que cero.", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("El precio unitario debe ser mayor que cero.", nameof(unitPrice));

        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
