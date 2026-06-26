using System;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Monolegal.Domain.Tests;

/// <summary>
/// Pruebas de la configuración de email embebida en <see cref="SystemSettings"/> (spec 017):
/// actualización del proveedor/emisor, gestión de plantillas y marca de auditoría.
/// </summary>
[Trait("Category", "Domain")]
public class SystemSettingsEmailTests
{
    [Fact]
    public void Defaults_ProveedorSmtp_SinPlantillasPersonalizadas()
    {
        var settings = new SystemSettings();

        settings.Email.ActiveProvider.ShouldBe(EmailProvider.Smtp);
        settings.EmailTemplates.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateEmailSettings_ReemplazaConfiguracionYActualizaFecha()
    {
        var settings = new SystemSettings();
        var before = settings.UpdatedAt;

        var nuevo = new EmailSettings
        {
            ActiveProvider = EmailProvider.Resend,
            FromAddress = "no-reply@monolegal.co",
            FromName = "Monolegal",
            Resend = new ResendSettings { FromDomain = "monolegal.co" },
        };

        settings.UpdateEmailSettings(nuevo);

        settings.Email.ActiveProvider.ShouldBe(EmailProvider.Resend);
        settings.Email.FromAddress.ShouldBe("no-reply@monolegal.co");
        settings.Email.Resend.FromDomain.ShouldBe("monolegal.co");
        settings.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void UpdateEmailSettings_Null_Lanza()
    {
        var settings = new SystemSettings();
        Should.Throw<ArgumentNullException>(() => settings.UpdateEmailSettings(null!));
    }

    [Fact]
    public void UpdateTemplate_CreaOActualizaLaPlantillaDelTipo()
    {
        var settings = new SystemSettings();

        settings.UpdateTemplate(NotificationType.Reminder, "Asunto {{factura.id}}", "Cuerpo {{factura.monto}}");

        settings.EmailTemplates.ShouldContainKey(NotificationType.Reminder);
        settings.EmailTemplates[NotificationType.Reminder].Subject.ShouldBe("Asunto {{factura.id}}");
        settings.EmailTemplates[NotificationType.Reminder].Body.ShouldBe("Cuerpo {{factura.monto}}");
    }

    [Fact]
    public void ResetTemplate_EliminaLaPersonalizacion()
    {
        var settings = new SystemSettings();
        settings.UpdateTemplate(NotificationType.PaymentConfirmation, "Asunto", "Cuerpo");

        settings.ResetTemplate(NotificationType.PaymentConfirmation);

        settings.EmailTemplates.ShouldNotContainKey(NotificationType.PaymentConfirmation);
    }
}
