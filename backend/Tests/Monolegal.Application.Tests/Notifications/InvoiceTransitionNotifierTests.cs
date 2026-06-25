using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Notifications;
using Backend.Tests.Infrastructure.Support;
using Backend.Tests.Monolegal.Application.Tests.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Notifications;

/// <summary>
/// Pruebas del orquestador <see cref="InvoiceTransitionNotifier"/> (spec 013): selección de
/// plantilla por estado (US1), registro de resultado y metadatos (US2) y logging estructurado (US3).
/// </summary>
[Trait("Category", "Application")]
public class InvoiceTransitionNotifierTests
{
    private static Invoice InvoiceAt(InvoiceStatus status, string clientId = "client_x")
    {
        var invoice = new Invoice(clientId, 250_000m);
        invoice.UpdateStatus(status);
        return invoice;
    }

    private static InvoiceTransitionNotifier CreateNotifier(
        FakeEmailService email,
        string? resolvedEmail = "cliente@correo.com",
        ILogger<InvoiceTransitionNotifier>? logger = null) =>
        new(email, new FakeClientEmailResolver(resolvedEmail),
            logger ?? NullLogger<InvoiceTransitionNotifier>.Instance);

    // ── US1: selección de plantilla por estado ───────────────────────────────────

    [Fact]
    public async Task Notify_SegundoRecordatorio_EnviaRecordatorio()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.SegundoRecordatorio);

        await CreateNotifier(email).NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        email.ReminderCalls.ShouldHaveSingleItem();
        email.PaymentConfirmationCalls.ShouldBeEmpty();
        email.DeactivationNoticeCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Notify_Pagado_EnviaConfirmacionDePago()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.Pagado);

        await CreateNotifier(email).NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        email.PaymentConfirmationCalls.ShouldHaveSingleItem();
        email.ReminderCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Notify_Desactivado_EnviaAvisoDeDesactivacion()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.Desactivado);

        await CreateNotifier(email).NotifyTransitionAsync(invoice, InvoiceStatus.SegundoRecordatorio);

        email.DeactivationNoticeCalls.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Notify_EstadoSinPlantilla_NoEnviaYRegistraSkipped()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.Pending);

        await CreateNotifier(email).NotifyTransitionAsync(invoice, InvoiceStatus.Draft);

        email.ReminderCalls.ShouldBeEmpty();
        email.PaymentConfirmationCalls.ShouldBeEmpty();
        email.DeactivationNoticeCalls.ShouldBeEmpty();
        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Skipped);
    }

    [Fact]
    public async Task Notify_SinCorreoDelCliente_NoEnviaYRegistraSkipped()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.SegundoRecordatorio);

        await CreateNotifier(email, resolvedEmail: null)
            .NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        email.ReminderCalls.ShouldBeEmpty();
        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Skipped);
        invoice.LastNotificationType.ShouldBe(NotificationType.Reminder);
    }

    // ── US2: registro de resultado y metadatos ───────────────────────────────────

    [Fact]
    public async Task Notify_RecordatorioExitoso_RegistraSentEIncrementaContadores()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.SegundoRecordatorio);

        await CreateNotifier(email).NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Sent);
        invoice.LastNotificationType.ShouldBe(NotificationType.Reminder);
        invoice.RemindersCount.ShouldBe(1);
        invoice.LastReminderSentAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Notify_ConfirmacionPagoExitosa_RegistraSentSinTocarContadores()
    {
        var email = new FakeEmailService();
        var invoice = InvoiceAt(InvoiceStatus.Pagado);

        await CreateNotifier(email).NotifyTransitionAsync(invoice, InvoiceStatus.SegundoRecordatorio);

        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Sent);
        invoice.RemindersCount.ShouldBe(0);
        invoice.LastReminderSentAt.ShouldBeNull();
    }

    [Fact]
    public async Task Notify_EnvioFallido_RegistraFailedSinTocarContadoresNiEstado()
    {
        var invoice = InvoiceAt(InvoiceStatus.SegundoRecordatorio);
        var notifier = new InvoiceTransitionNotifier(
            new ThrowingEmailService("SMTP caído"),
            new FakeClientEmailResolver("cliente@correo.com"),
            NullLogger<InvoiceTransitionNotifier>.Instance);

        await notifier.NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Failed);
        invoice.LastNotificationError.ShouldBe("SMTP caído");
        invoice.RemindersCount.ShouldBe(0);
        invoice.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio); // no se revierte
    }

    // ── US3: logging estructurado ─────────────────────────────────────────────────

    [Fact]
    public async Task Notify_EmiteLogEstructuradoConPropiedadesRequeridas()
    {
        var email = new FakeEmailService();
        var logger = new CapturingLogger<InvoiceTransitionNotifier>();
        var invoice = InvoiceAt(InvoiceStatus.SegundoRecordatorio);

        await CreateNotifier(email, logger: logger)
            .NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Properties.ShouldContainKey("InvoiceId");
        entry.Properties.ShouldContainKey("PreviousStatus");
        entry.Properties.ShouldContainKey("NewStatus");
        entry.Properties.ShouldContainKey("NotificationType");
        entry.Properties.ShouldContainKey("NotificationOutcome");
        entry.Properties["NotificationOutcome"]!.ToString().ShouldBe("Sent");
    }

    [Fact]
    public async Task Notify_EnvioFallido_EmiteLogDeError()
    {
        var logger = new CapturingLogger<InvoiceTransitionNotifier>();
        var invoice = InvoiceAt(InvoiceStatus.SegundoRecordatorio);
        var notifier = new InvoiceTransitionNotifier(
            new ThrowingEmailService(),
            new FakeClientEmailResolver("cliente@correo.com"),
            logger);

        await notifier.NotifyTransitionAsync(invoice, InvoiceStatus.PrimerRecordatorio);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Properties["NotificationOutcome"]!.ToString().ShouldBe("Failed");
    }

    // ── Logger de prueba que captura nivel y propiedades estructuradas ────────────

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, IReadOnlyDictionary<string, object?> Properties)> Entries { get; } = new();

        IDisposable? ILogger.BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var props = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object?>> kvps)
            {
                foreach (var kvp in kvps)
                    props[kvp.Key] = kvp.Value;
            }

            Entries.Add((logLevel, props));
        }
    }
}
