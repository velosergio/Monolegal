using System;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Backend.Application.Notifications;
using Backend.Tests.Infrastructure.Support;
using Backend.Tests.Monolegal.Application.Tests.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Integration tests para el handler del endpoint POST /api/invoices/{id}/pay (US2).
/// Ejercitan la capa de aplicación end-to-end (InvoiceTransitionService + repositorios)
/// sin depender de MongoDB ni de la capa HTTP, modelando exactamente el flujo que
/// ejecutará el endpoint PayInvoice (T017).
///
/// Validates: FR-004 | US2 (spec.md)
/// </summary>
[Trait("Category", "Application")]
public class PayInvoiceTests
{
    // Usa el fake compartido Backend.Tests.Infrastructure.Support.InMemoryInvoiceRepository.

    // ─────────────────────────────────────────────────────────────────────────
    // Handler (replica la lógica que tendrá el endpoint T017)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resultado del handler de pago.
    /// </summary>
    private record PayResult(bool Success, string? Error = null);

    /// <summary>
    /// Simula el handler del endpoint POST /api/invoices/{id}/pay.
    /// Busca la factura por id, aplica el pago mediante InvoiceTransitionService
    /// y persiste el cambio. Devuelve un PayResult indicando éxito o el motivo de fallo.
    /// </summary>
    private static async Task<PayResult> PayInvoiceAsync(
        IInvoiceRepository invoiceRepo,
        string invoiceId)
    {
        var invoice = await invoiceRepo.GetByIdAsync(invoiceId);

        if (invoice is null)
            return new PayResult(false, "NotFound");

        var service = new InvoiceTransitionService();

        try
        {
            service.ApplyPayment(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return new PayResult(false, ex.Message);
        }

        await invoiceRepo.UpdateAsync(invoice);
        return new PayResult(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Invoice CreateInvoiceAtStatus(InvoiceStatus status, DateTime? transitionAt = null)
    {
        var invoice = new Invoice("client_test", 1000m);
        invoice.UpdateStatus(status);
        if (transitionAt.HasValue)
            invoice.OverrideLastStatusTransitionAt(transitionAt.Value);
        return invoice;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: pago desde estados activos → estado queda en Pagado
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PayInvoice_FromPending_StatusBecomePagado()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        var result = await PayInvoiceAsync(repo, invoice.Id);

        // Assert
        result.Success.ShouldBeTrue();
        var updated = await repo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    [Fact]
    public async Task PayInvoice_FromPrimerRecordatorio_StatusBecomePagado()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.PrimerRecordatorio);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        var result = await PayInvoiceAsync(repo, invoice.Id);

        // Assert
        result.Success.ShouldBeTrue();
        var updated = await repo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    [Fact]
    public async Task PayInvoice_FromSegundoRecordatorio_StatusBecomePagado()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.SegundoRecordatorio);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        var result = await PayInvoiceAsync(repo, invoice.Id);

        // Assert
        result.Success.ShouldBeTrue();
        var updated = await repo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    [Fact]
    public async Task PayInvoice_FromDesactivado_StatusBecomePagado()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Desactivado);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        var result = await PayInvoiceAsync(repo, invoice.Id);

        // Assert
        result.Success.ShouldBeTrue();
        var updated = await repo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: error al intentar pagar una factura ya pagada
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PayInvoice_AlreadyPagado_ReturnsError()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pagado);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        var result = await PayInvoiceAsync(repo, invoice.Id);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task PayInvoice_AlreadyPagado_StatusRemainsUnchanged()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pagado);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        await PayInvoiceAsync(repo, invoice.Id);

        // Assert – el estado no debe haber cambiado (permanece Pagado sin error de doble transición)
        var stored = await repo.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: error al pagar factura inexistente
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PayInvoice_NonExistentInvoice_ReturnsNotFound()
    {
        // Arrange
        var repo = new InMemoryInvoiceRepository(); // repositorio vacío

        // Act
        var result = await PayInvoiceAsync(repo, "id-que-no-existe");

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("NotFound");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: LastStatusTransitionAt se actualiza tras el pago
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.Desactivado)]
    public async Task PayInvoice_OnSuccess_UpdatesLastStatusTransitionAt(InvoiceStatus initialStatus)
    {
        // Arrange
        var oldTransitionAt = DateTime.UtcNow.AddDays(-10);
        var invoice = CreateInvoiceAtStatus(initialStatus, oldTransitionAt);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        var result = await PayInvoiceAsync(repo, invoice.Id);

        // Assert
        result.Success.ShouldBeTrue();
        var updated = await repo.GetByIdAsync(invoice.Id);
        updated!.LastStatusTransitionAt.ShouldBeGreaterThan(oldTransitionAt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: el repositorio persiste el cambio de estado
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PayInvoice_AfterPayment_InvoiceIsPersisted()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);

        // Act
        await PayInvoiceAsync(repo, invoice.Id);

        // Assert – la lectura posterior al pago devuelve la factura con estado actualizado
        var stored = await repo.GetByIdAsync(invoice.Id);
        stored.ShouldNotBeNull();
        stored!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // spec 013 [US2]: el pago notifica al cliente y registra el resultado;
    // un fallo de envío no revierte el pago ni falla la operación.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Replica el flujo del endpoint incluyendo la notificación (T023/T030).</summary>
    private static async Task PayWithNotificationAsync(
        IInvoiceRepository invoiceRepo, IInvoiceTransitionNotifier notifier, string invoiceId)
    {
        var invoice = await invoiceRepo.GetByIdAsync(invoiceId);
        if (invoice is null) return;

        var previousStatus = invoice.Status;
        new InvoiceTransitionService().ApplyPayment(invoice);
        await notifier.NotifyTransitionAsync(invoice, previousStatus);
        await invoiceRepo.UpdateAsync(invoice);
    }

    [Fact]
    public async Task PayInvoice_OnSuccess_RecordsPaymentConfirmationResult()
    {
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.SegundoRecordatorio);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);
        var notifier = new InvoiceTransitionNotifier(
            new FakeEmailService(), new FakeClientEmailResolver(), NullLogger<InvoiceTransitionNotifier>.Instance);

        await PayWithNotificationAsync(repo, notifier, invoice.Id);

        var stored = await repo.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.Pagado);
        stored.LastNotificationType.ShouldBe(NotificationType.PaymentConfirmation);
        stored.LastNotificationOutcome.ShouldBe(NotificationOutcome.Sent);
    }

    [Fact]
    public async Task PayInvoice_WhenEmailFails_StillPersistsPagadoAndRecordsFailed()
    {
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending);
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(invoice);
        var notifier = new InvoiceTransitionNotifier(
            new ThrowingEmailService(), new FakeClientEmailResolver("cliente@correo.com"),
            NullLogger<InvoiceTransitionNotifier>.Instance);

        await PayWithNotificationAsync(repo, notifier, invoice.Id);

        var stored = await repo.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.Pagado); // la transición no se revierte
        stored.LastNotificationOutcome.ShouldBe(NotificationOutcome.Failed);
    }
}
