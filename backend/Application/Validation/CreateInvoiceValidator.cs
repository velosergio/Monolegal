using System;
using System.Collections.Generic;
using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>Línea de detalle recibida para validación (spec 018).</summary>
public sealed record InvoiceItemInputModel(string? Description, decimal Quantity, decimal UnitPrice);

/// <summary>Cuerpo normalizado de creación de factura para validación (spec 018, RF-002).</summary>
public sealed record CreateInvoiceInput(string? ClientId, DateTime? DueDate, IReadOnlyList<InvoiceItemInputModel>? Items);

/// <summary>
/// Validador del alta de factura (spec 018, RF-001/RF-002). El monto no se valida: se deriva de los
/// items (RF-011). Exige cliente, fecha de vencimiento y al menos un item con descripción, cantidad
/// y precio unitario positivos.
/// </summary>
/// <remarks>
/// SOLID: SRP — única razón de cambio: las reglas de validación del alta de factura.
/// LSP — sustituye a <c>AbstractValidator&lt;CreateInvoiceInput&gt;</c> sin romper a sus consumidores.
/// </remarks>
public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceInput>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.DueDate)
            .NotNull().WithMessage("La fecha de vencimiento es obligatoria.");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("La factura debe tener al menos una línea de detalle.")
            .Must(items => items is { Count: > 0 })
            .WithMessage("La factura debe tener al menos una línea de detalle.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description)
                .NotEmpty().WithMessage("La descripción del item es obligatoria.");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");
            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("El precio unitario debe ser mayor que cero.");
        }).When(x => x.Items is not null);
    }
}
