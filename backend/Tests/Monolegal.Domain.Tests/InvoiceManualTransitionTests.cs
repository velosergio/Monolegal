using System;
using Shouldly;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Services;

namespace Monolegal.Domain.Tests;

/// <summary>
/// Unit tests de <see cref="InvoiceTransitionService.ApplyManualTransition"/> — transición
/// manual de estado validada contra la matriz de transiciones permitidas (spec 006/009 US3).
///
/// Validates: FR-011, FR-012, FR-013 (spec 009)
/// </summary>
public class InvoiceManualTransitionTests
{
    private static Invoice At(InvoiceStatus status)
    {
        var invoice = new Invoice("client_test", 1000m);
        invoice.UpdateStatus(status);
        return invoice;
    }

    // ── Transiciones permitidas ───────────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.PrimerRecordatorio, InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio, InvoiceStatus.Desactivado)]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.Pagado)]
    [InlineData(InvoiceStatus.PrimerRecordatorio, InvoiceStatus.Pagado)]
    [InlineData(InvoiceStatus.SegundoRecordatorio, InvoiceStatus.Pagado)]
    [InlineData(InvoiceStatus.Desactivado, InvoiceStatus.Pagado)]
    public void AllowedTransition_AppliesNewStatus(InvoiceStatus from, InvoiceStatus to)
    {
        var invoice = At(from);
        var service = new InvoiceTransitionService();

        service.ApplyManualTransition(invoice, to);

        invoice.Status.ShouldBe(to);
    }

    // ── Transiciones no permitidas → InvalidOperationException ─────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.Desactivado)]
    [InlineData(InvoiceStatus.PrimerRecordatorio, InvoiceStatus.Desactivado)]
    [InlineData(InvoiceStatus.SegundoRecordatorio, InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.Pagado, InvoiceStatus.PrimerRecordatorio)]
    public void DisallowedTransition_ThrowsAndDoesNotChangeStatus(InvoiceStatus from, InvoiceStatus to)
    {
        var invoice = At(from);
        var service = new InvoiceTransitionService();

        Should.Throw<InvalidOperationException>(() => service.ApplyManualTransition(invoice, to));
        invoice.Status.ShouldBe(from);
    }

    [Fact]
    public void AlreadyPagado_ToPagado_Throws()
    {
        var invoice = At(InvoiceStatus.Pagado);
        var service = new InvoiceTransitionService();

        Should.Throw<InvalidOperationException>(() => service.ApplyManualTransition(invoice, InvoiceStatus.Pagado));
    }
}
