using FluentValidation;

namespace Backend.Application.Validation;

/// <summary>Cuerpo normalizado para crear/editar un cliente (spec 018, RF-015).</summary>
public sealed record ClientInput(string? Name, string? Email, string? Phone, string? Address);

/// <summary>
/// Validador del alta de cliente (spec 018, RF-015): nombre obligatorio y email con formato válido.
/// La unicidad del email (RF-015a) se verifica en el endpoint contra el repositorio.
/// </summary>
public sealed class CreateClientValidator : AbstractValidator<ClientInput>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email debe tener un formato válido.");
    }
}

/// <summary>
/// Validador de la edición de cliente (spec 018, RF-016). Mismas reglas de campo que el alta; la
/// unicidad del email excluyendo al propio cliente se verifica en el endpoint.
/// </summary>
public sealed class UpdateClientValidator : AbstractValidator<ClientInput>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email debe tener un formato válido.");
    }
}
