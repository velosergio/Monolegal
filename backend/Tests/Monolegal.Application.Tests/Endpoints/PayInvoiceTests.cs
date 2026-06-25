using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    // ─────────────────────────────────────────────────────────────────────────
    // In-memory fakes (misma estructura que InvoiceWorkerTests)
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class InMemoryInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<string, Invoice> _store = new();

        public IReadOnlyCollection<Invoice> All => _store.Values.ToList().AsReadOnly();

        public Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

        public Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Values.Where(i => i.ClientId == clientId));

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            _store[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            _store[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Values.Where(i =>
                i.Status is InvoiceStatus.Pending
                    or InvoiceStatus.PrimerRecordatorio
                    or InvoiceStatus.SegundoRecordatorio));
    }

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
}
