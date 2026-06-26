using System;
using System.Collections.Generic;
using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>Cuerpo normalizado de edición de factura para validación (spec 018, RF-003).</summary>
public sealed record UpdateInvoiceInput(string? ClientId, DateTime? DueDate, IReadOnlyList<InvoiceItemInputModel>? Items);

/// <summary>
/// Validador de la edición de factura (spec 018). Mismas reglas que el alta: cliente, vencimiento y
/// al menos un item válido. El bloqueo por estado terminal (RF-004a) se aplica en el dominio/endpoint,
/// no aquí, pues depende del estado persistido.
/// </summary>
public sealed class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceInput>
{
    public UpdateInvoiceValidator()
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
