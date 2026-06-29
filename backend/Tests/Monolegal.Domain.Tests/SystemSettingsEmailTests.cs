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

    // ── C10.2: resetear un tipo sin personalización no toca la marca de auditoría ──
    [Fact]
    public void ResetTemplate_TipoInexistente_NoCambiaUpdatedAt()
    {
        var settings = new SystemSettings();
        var before = settings.UpdatedAt;

        settings.ResetTemplate(NotificationType.Reminder); // no había plantilla para este tipo

        settings.UpdatedAt.ShouldBe(before);
        settings.EmailTemplates.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateTransitions_ReemplazaConfiguracionYActualizaFecha()
    {
        var settings = new SystemSettings();
        var before = settings.UpdatedAt;

        var config = new InvoiceTransitionsConfig
        {
            PendingToFirstReminderDays = 5,
            FirstToSecondReminderDays = 6,
            SecondToDeactivatedDays = 7,
        };

        settings.UpdateTransitions(config);

        settings.InvoiceTransitions.PendingToFirstReminderDays.ShouldBe(5);
        settings.InvoiceTransitions.FirstToSecondReminderDays.ShouldBe(6);
        settings.InvoiceTransitions.SecondToDeactivatedDays.ShouldBe(7);
        settings.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    // ── C10.4: parámetros no secretos de SMTP/Resend se conservan ──
    [Fact]
    public void SmtpSettings_ConservaPropiedadesAsignadas()
    {
        var smtp = new SmtpSettings
        {
            Host = "smtp.monolegal.co",
            Port = 2525,
            Username = "buzon",
            UseStartTls = false,
        };

        smtp.Host.ShouldBe("smtp.monolegal.co");
        smtp.Port.ShouldBe(2525);
        smtp.Username.ShouldBe("buzon");
        smtp.UseStartTls.ShouldBeFalse();
    }

    [Fact]
    public void SmtpSettings_Defaults_PuertoSeguroYStartTls()
    {
        var smtp = new SmtpSettings();

        smtp.Port.ShouldBe(587);
        smtp.UseStartTls.ShouldBeTrue();
        smtp.Host.ShouldBeNull();
    }
}
