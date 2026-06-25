using System;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Backend.Application.Notifications;
using Backend.Application.Validation;
using Backend.Tests.Infrastructure.Support;
using Backend.Tests.Monolegal.Application.Tests.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Tests del endpoint POST /api/invoices/transition/{id} (US3, spec 009).
/// Replica el flujo del handler (validación → 404 → transición → 400/200) sobre componentes reales.
/// </summary>
[Trait("Category", "Application")]
public class TransitionInvoiceTests
{
    private enum Outcome { Ok, NotFound, BadRequest }

    /// <summary>Replica la orquestación del endpoint TransitionInvoice.</summary>
    private static async Task<Outcome> TransitionAsync(IInvoiceRepository repo, string id, string? newStatus)
    {
        var validator = new TransitionInvoiceRequestValidator(InvoiceStatusApi.IsValid);
        var validation = await validator.ValidateAsync(new TransitionInvoiceInput(newStatus));
        if (!validation.IsValid)
            return Outcome.BadRequest;

        InvoiceStatusApi.TryParse(newStatus, out var parsed);

        var invoice = await repo.GetByIdAsync(id);
        if (invoice is null)
            return Outcome.NotFound;

        var service = new InvoiceTransitionService();
        try
        {
            service.ApplyManualTransition(invoice, parsed);
        }
        catch (InvalidOperationException)
        {
            return Outcome.BadRequest;
        }

        await repo.UpdateAsync(invoice);
        return Outcome.Ok;
    }

    [Fact]
    public async Task AllowedTransition_PersistsAndReturnsOk()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.PrimerRecordatorio);
        await repo.AddAsync(invoice);

        var outcome = await TransitionAsync(repo, invoice.Id, "segundorecordatorio");

        outcome.ShouldBe(Outcome.Ok);
        (await repo.GetByIdAsync(invoice.Id))!.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
    }

    [Fact]
    public async Task DisallowedTransition_ReturnsBadRequestAndNoChange()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending);
        await repo.AddAsync(invoice);

        var outcome = await TransitionAsync(repo, invoice.Id, "desactivado");

        outcome.ShouldBe(Outcome.BadRequest);
        (await repo.GetByIdAsync(invoice.Id))!.Status.ShouldBe(InvoiceStatus.Pending);
    }

    [Fact]
    public async Task NonExistentInvoice_ReturnsNotFound()
    {
        var repo = new InMemoryInvoiceRepository();

        var outcome = await TransitionAsync(repo, "no-existe", "pagado");

        outcome.ShouldBe(Outcome.NotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foo")]
    public async Task InvalidNewStatus_ReturnsBadRequest(string? newStatus)
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.Pending);
        await repo.AddAsync(invoice);

        var outcome = await TransitionAsync(repo, invoice.Id, newStatus);

        outcome.ShouldBe(Outcome.BadRequest);
    }

    // ── spec 013 [US2]: la transición manual notifica y registra el resultado ──────

    /// <summary>Replica el flujo del endpoint incluyendo la notificación (T024/T031).</summary>
    private static async Task<Outcome> TransitionWithNotificationAsync(
        IInvoiceRepository repo, IInvoiceTransitionNotifier notifier, string id, string? newStatus)
    {
        var validator = new TransitionInvoiceRequestValidator(InvoiceStatusApi.IsValid);
        var validation = await validator.ValidateAsync(new TransitionInvoiceInput(newStatus));
        if (!validation.IsValid)
            return Outcome.BadRequest;

        InvoiceStatusApi.TryParse(newStatus, out var parsed);

        var invoice = await repo.GetByIdAsync(id);
        if (invoice is null)
            return Outcome.NotFound;

        var previousStatus = invoice.Status;
        try
        {
            new InvoiceTransitionService().ApplyManualTransition(invoice, parsed);
        }
        catch (InvalidOperationException)
        {
            return Outcome.BadRequest;
        }

        await notifier.NotifyTransitionAsync(invoice, previousStatus);
        await repo.UpdateAsync(invoice);
        return Outcome.Ok;
    }

    [Fact]
    public async Task Transition_ToReminder_RecordsReminderResult()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.PrimerRecordatorio);
        await repo.AddAsync(invoice);
        var notifier = new InvoiceTransitionNotifier(
            new FakeEmailService(), new FakeClientEmailResolver(), NullLogger<InvoiceTransitionNotifier>.Instance);

        var outcome = await TransitionWithNotificationAsync(repo, notifier, invoice.Id, "segundorecordatorio");

        outcome.ShouldBe(Outcome.Ok);
        var stored = await repo.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
        stored.LastNotificationType.ShouldBe(NotificationType.Reminder);
        stored.LastNotificationOutcome.ShouldBe(NotificationOutcome.Sent);
    }

    [Fact]
    public async Task Transition_WhenEmailFails_StillPersistsTransition()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = InvoiceTestFactory.Create("c1", 100m, InvoiceStatus.SegundoRecordatorio);
        await repo.AddAsync(invoice);
        var notifier = new InvoiceTransitionNotifier(
            new ThrowingEmailService(), new FakeClientEmailResolver("cliente@correo.com"),
            NullLogger<InvoiceTransitionNotifier>.Instance);

        var outcome = await TransitionWithNotificationAsync(repo, notifier, invoice.Id, "desactivado");

        outcome.ShouldBe(Outcome.Ok);
        var stored = await repo.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.Desactivado); // no se revierte
        stored.LastNotificationOutcome.ShouldBe(NotificationOutcome.Failed);
    }
}
