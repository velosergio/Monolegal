using System.Linq;
using Backend.Application.Validation;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Validation;

/// <summary>
/// Pruebas de <see cref="UpdateEmailSettingsValidator"/> (spec 017, US1): remitente válido y
/// requeridos por proveedor activo (SMTP/Resend).
/// </summary>
[Trait("Category", "Application")]
public class UpdateEmailSettingsValidatorTests
{
    private static readonly UpdateEmailSettingsValidator Validator = new();

    [Fact]
    public void Smtp_Valido_Pasa()
    {
        var request = new EmailSettingsRequest(
            EmailProvider.Smtp, "no-reply@monolegal.co", "Monolegal",
            new SmtpSettingsRequest("smtp.example.com", 587, "apikey", true), null);

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Resend_Valido_Pasa()
    {
        var request = new EmailSettingsRequest(
            EmailProvider.Resend, "no-reply@monolegal.co", "Monolegal",
            null, new ResendSettingsRequest("mg.monolegal.co"));

        Validator.Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void FromAddressInvalido_Falla()
    {
        var request = new EmailSettingsRequest(
            EmailProvider.Smtp, "no-es-email", "Monolegal",
            new SmtpSettingsRequest("smtp.example.com"), null);

        var result = Validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.Contains("FromAddress"));
    }

    [Fact]
    public void Smtp_SinHost_Falla()
    {
        var request = new EmailSettingsRequest(
            EmailProvider.Smtp, "no-reply@monolegal.co", "Monolegal",
            new SmtpSettingsRequest(Host: null), null);

        var result = Validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "smtp.host");
    }

    [Fact]
    public void Smtp_PuertoFueraDeRango_Falla()
    {
        var request = new EmailSettingsRequest(
            EmailProvider.Smtp, "no-reply@monolegal.co", "Monolegal",
            new SmtpSettingsRequest("smtp.example.com", Port: 0), null);

        Validator.Validate(request).Errors.ShouldContain(e => e.PropertyName == "smtp.port");
    }

    [Fact]
    public void Resend_SinDominio_Falla()
    {
        var request = new EmailSettingsRequest(
            EmailProvider.Resend, "no-reply@monolegal.co", "Monolegal",
            null, new ResendSettingsRequest(FromDomain: null));

        var result = Validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "resend.fromDomain");
    }
}
