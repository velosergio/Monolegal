using Backend.Application.Validation;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Validation;

/// <summary>Pruebas de <see cref="SendTestEmailValidator"/> (spec 017, US3).</summary>
[Trait("Category", "Application")]
public class SendTestEmailValidatorTests
{
    private static readonly SendTestEmailValidator Validator = new();

    [Fact]
    public void Valido_Pasa()
        => Validator.Validate(new SendTestEmailInput("prueba@dominio.com", "reminder")).IsValid.ShouldBeTrue();

    [Fact]
    public void CorreoInvalido_Falla()
        => Validator.Validate(new SendTestEmailInput("no-es-correo", "reminder")).IsValid.ShouldBeFalse();

    [Fact]
    public void TipoInvalido_Falla()
        => Validator.Validate(new SendTestEmailInput("prueba@dominio.com", "foo")).IsValid.ShouldBeFalse();
}
