using System;
using System.Threading.Tasks;
using Backend.Application.Notifications;
using Backend.Infrastructure.Email;
using Backend.Tests.Infrastructure.Support;
using Backend.Tests.Monolegal.Application.Tests.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests;

/// <summary>
/// Pruebas del servicio de herramientas de administración de envíos (spec 017, US4):
/// reenvío de fallidas y saneamiento de atascadas, con conteos y caso sin candidatos.
/// </summary>
[Trait("Category", "Application")]
public class EmailAdminServiceTests
{
    private static Invoice MakeInvoice(InvoiceStatus status, NotificationOutcome outcome)
    {
        var invoice = new Invoice("cli-1", 100m);
        if (status != InvoiceStatus.Pending)
            invoice.UpdateStatus(status);

        if (outcome != NotificationOutcome.None)
        {
            var type = status switch
            {
                InvoiceStatus.Pagado => NotificationType.PaymentConfirmation,
                InvoiceStatus.Desactivado => NotificationType.DeactivationNotice,
                _ => NotificationType.Reminder,
            };
            invoice.RecordNotificationResult(type, outcome, DateTime.UtcNow, outcome == NotificationOutcome.Failed ? "fallo previo" : null);
        }

        return invoice;
    }

    private static EmailAdminService BuildService(InMemoryInvoiceRepository repo, IEmailServiceSelector selector)
    {
        var notifier = new InvoiceTransitionNotifier(
            selector.Service,
            new FakeClientEmailResolver("cliente@correo.com"),
            NullLogger<InvoiceTransitionNotifier>.Instance);
        return new EmailAdminService(repo, notifier, NullLogger<EmailAdminService>.Instance);
    }

    private interface IEmailServiceSelector
    {
        Backend.Application.Abstractions.IEmailService Service { get; }
    }

    private sealed class WorkingEmail : IEmailServiceSelector
    {
        public Backend.Application.Abstractions.IEmailService Service { get; } = new FakeEmailService();
    }

    private sealed class FailingEmail : IEmailServiceSelector
    {
        public Backend.Application.Abstractions.IEmailService Service { get; } = new ThrowingEmailService();
    }

    [Fact]
    public async Task ResendFailed_ReenviaSoloFallidasYCuenta()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(MakeInvoice(InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed));
        await repo.AddAsync(MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Failed));
        await repo.AddAsync(MakeInvoice(InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Sent));

        var result = await BuildService(repo, new WorkingEmail()).ResendFailedAsync();

        result.Attempted.ShouldBe(2);
        result.Resent.ShouldBe(2);
        result.Failed.ShouldBe(0);
    }

    [Fact]
    public async Task ResendFailed_ProveedorCaido_CuentaFailed()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(MakeInvoice(InvoiceStatus.PrimerRecordatorio, NotificationOutcome.Failed));

        var result = await BuildService(repo, new FailingEmail()).ResendFailedAsync();

        result.Attempted.ShouldBe(1);
        result.Resent.ShouldBe(0);
        result.Failed.ShouldBe(1);
    }

    [Fact]
    public async Task ResendFailed_SinCandidatos_DevuelveCeros()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Sent));

        var result = await BuildService(repo, new WorkingEmail()).ResendFailedAsync();

        result.Attempted.ShouldBe(0);
        result.Resent.ShouldBe(0);
        result.Failed.ShouldBe(0);
    }

    [Fact]
    public async Task Sanitize_MarcaNoneEnEstadoNotificable()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(MakeInvoice(InvoiceStatus.PrimerRecordatorio, NotificationOutcome.None));
        await repo.AddAsync(MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.None));
        // Pending no es estado notificable: no se sanea.
        await repo.AddAsync(MakeInvoice(InvoiceStatus.Pending, NotificationOutcome.None));
        // Failed no entra en el conjunto None.
        await repo.AddAsync(MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Failed));

        var result = await BuildService(repo, new WorkingEmail()).SanitizeStuckAsync();

        result.Sanitized.ShouldBe(2);
        var failedNow = await repo.GetByNotificationOutcomeAsync(NotificationOutcome.Failed);
        // 2 saneadas + 1 que ya estaba Failed.
        failedNow.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Sanitize_SinCandidatos_DevuelveCero()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(MakeInvoice(InvoiceStatus.Pagado, NotificationOutcome.Sent));

        var result = await BuildService(repo, new WorkingEmail()).SanitizeStuckAsync();

        result.Sanitized.ShouldBe(0);
    }
}
