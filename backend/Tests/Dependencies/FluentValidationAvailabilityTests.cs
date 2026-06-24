using FluentValidation;
using Shouldly;
using Xunit;

namespace Backend.Tests.Dependencies;

/// <summary>
/// US2 - Smoke test de disponibilidad de la librería de validación.
/// Verifica que FluentValidation resuelve desde la capa Application (referencia transitiva)
/// y que un AbstractValidator declarado compila y ejecuta reglas.
/// </summary>
public class FluentValidationAvailabilityTests
{
    private sealed record Entrada(string Nombre);

    private sealed class EntradaValidator : AbstractValidator<Entrada>
    {
        public EntradaValidator()
        {
            RuleFor(e => e.Nombre).NotEmpty();
        }
    }

    [Fact]
    public void Validator_DetectaEntradaInvalidaYValida()
    {
        var validator = new EntradaValidator();

        validator.Validate(new Entrada("")).IsValid.ShouldBeFalse();
        validator.Validate(new Entrada("Acme S.A.")).IsValid.ShouldBeTrue();
    }
}
