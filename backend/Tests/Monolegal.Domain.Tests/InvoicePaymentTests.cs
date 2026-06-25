using System;
using Shouldly;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Services;

namespace Monolegal.Domain.Tests;

/// <summary>
/// Unit tests para la lógica de pago manual de facturas (US2).
/// Cubre <see cref="InvoiceTransitionService.ApplyPayment"/>.
///
/// Validates: FR-004 (spec.md) — Transición a Pagado desde cualquier estado activo.
/// </summary>
public class InvoicePaymentTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helper
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
    // US2 – Transición a Pagado desde cualquier estado activo (FR-004)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.Desactivado)]
    public void ApplyPayment_FromAnyActiveStatus_TransitionsToPagado(InvoiceStatus initialStatus)
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(initialStatus);
        var service = new InvoiceTransitionService();

        // Act
        service.ApplyPayment(invoice);

        // Assert
        invoice.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US2 – Error al pagar una factura ya pagada (FR-004)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPayment_AlreadyPagado_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pagado);
        var service = new InvoiceTransitionService();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.ApplyPayment(invoice));
    }

    [Fact]
    public void ApplyPayment_AlreadyPagado_StatusRemainsUnchanged()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pagado);
        var service = new InvoiceTransitionService();

        // Act – ignoramos la excepción, nos importa que el estado no cambió
        try { service.ApplyPayment(invoice); } catch (InvalidOperationException) { }

        // Assert
        invoice.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US2 – Guard clause null
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyPayment_NullInvoice_ThrowsArgumentNullException()
    {
        var service = new InvoiceTransitionService();

        Should.Throw<ArgumentNullException>(() => service.ApplyPayment(null!));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US2 – LastStatusTransitionAt se actualiza tras el pago
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.Desactivado)]
    public void ApplyPayment_OnSuccess_UpdatesLastStatusTransitionAt(InvoiceStatus initialStatus)
    {
        // Arrange
        var oldTransitionAt = DateTime.UtcNow.AddDays(-10);
        var invoice = CreateInvoiceAtStatus(initialStatus, oldTransitionAt);
        var service = new InvoiceTransitionService();

        // Act
        service.ApplyPayment(invoice);

        // Assert
        invoice.LastStatusTransitionAt.ShouldBeGreaterThan(oldTransitionAt);
    }
}
