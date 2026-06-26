using Backend.Application.Validation;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Validation;

/// <summary>
/// Pruebas de <see cref="UpdateEmailTemplateValidator"/> (spec 017, US2): no vacío y solo variables
/// del catálogo cerrado.
/// </summary>
[Trait("Category", "Application")]
public class UpdateEmailTemplateValidatorTests
{
    private static readonly UpdateEmailTemplateValidator Validator = new();

    [Fact]
    public void Valido_ConVariablesDelCatalogo_Pasa()
    {
        var input = new EmailTemplateInput(
            "Recordatorio {{factura.id}}",
            "Hola {{cliente.nombre}}, su factura {{factura.id}} por {{factura.monto}}.");

        Validator.Validate(input).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AsuntoVacio_Falla()
    {
        var input = new EmailTemplateInput("", "Cuerpo válido");
        Validator.Validate(input).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CuerpoVacio_Falla()
    {
        var input = new EmailTemplateInput("Asunto", "");
        Validator.Validate(input).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void VariableNoAdmitida_Falla()
    {
        var input = new EmailTemplateInput("Asunto {{factura.xyz}}", "Cuerpo {{factura.id}}");

        var result = Validator.Validate(input);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("factura.xyz"));
    }
}
