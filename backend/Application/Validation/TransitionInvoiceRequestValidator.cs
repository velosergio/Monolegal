using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>
/// Datos normalizados de la petición de transición manual de estado.
/// </summary>
public sealed record TransitionInvoiceInput(string? NewStatus);

/// <summary>
/// Validador del cuerpo de POST /api/invoices/transition/{id} (spec 009, FR-017/FR-018).
/// Regla: newStatus requerido y perteneciente al conjunto de estados válidos.
/// El predicado de validez se inyecta para no acoplar Application a la capa Api.
/// </summary>
public sealed class TransitionInvoiceRequestValidator : AbstractValidator<TransitionInvoiceInput>
{
    public TransitionInvoiceRequestValidator(System.Func<string, bool> isValidStatus)
    {
        RuleFor(x => x.NewStatus)
            .NotEmpty()
            .WithMessage("El campo 'newStatus' es obligatorio.")
            .DependentRules(() =>
            {
                RuleFor(x => x.NewStatus!)
                    .Must(isValidStatus)
                    .WithMessage("El campo 'newStatus' no corresponde a un estado válido.");
            });
    }
}
