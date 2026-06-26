using System;
using System.Collections.Generic;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Monolegal.Domain.Tests.Entities;

/// <summary>
/// Tests del modelo ampliado de factura (spec 018): items, vencimiento, monto derivado y bloqueo
/// de edición en estado terminal. Valida RF-001/RF-003/RF-004a/RF-011.
/// </summary>
public class InvoiceItemsTests
{
    private static List<InvoiceItem> SampleItems() => new()
    {
        new InvoiceItem("Asesoría", 2m, 150m),  // 300
        new InvoiceItem("Trámite", 1m, 50m),    // 50
    };

    // ── InvoiceItem ──────────────────────────────────────────────────────────────

    [Fact]
    public void InvoiceItem_Subtotal_IsQuantityTimesUnitPrice()
    {
        var item = new InvoiceItem("Asesoría", 3m, 100m);
        item.Subtotal.ShouldBe(300m);
    }

    [Theory]
    [InlineData("", 1, 100)]
    [InlineData("Concepto", 0, 100)]
    [InlineData("Concepto", 1, 0)]
    [InlineData("Concepto", -1, 100)]
    public void InvoiceItem_WithInvalidValues_Throws(string description, decimal quantity, decimal unitPrice)
    {
        Should.Throw<ArgumentException>(() => new InvoiceItem(description, quantity, unitPrice));
    }

    // ── Invoice.Create (alta) ──────────────────────────────────────────────────────

    [Fact]
    public void Create_DerivesAmountFromItems()
    {
        var dueDate = DateTime.UtcNow.AddDays(15);
        var invoice = Invoice.Create("client_1", SampleItems(), dueDate);

        invoice.Amount.ShouldBe(350m); // 300 + 50
        invoice.Items.Count.ShouldBe(2);
        invoice.DueDate.ShouldBe(dueDate);
        invoice.Status.ShouldBe(InvoiceStatus.Pending);
    }

    [Fact]
    public void Create_WithNoItems_Throws()
    {
        Should.Throw<ArgumentException>(
            () => Invoice.Create("client_1", new List<InvoiceItem>(), DateTime.UtcNow));
    }

    // ── Invoice.UpdateDetails (edición) ────────────────────────────────────────────

    [Fact]
    public void UpdateDetails_RecalculatesAmount()
    {
        var invoice = Invoice.Create("client_1", SampleItems(), DateTime.UtcNow.AddDays(10));

        var newItems = new List<InvoiceItem> { new("Nuevo", 4m, 100m) }; // 400
        invoice.UpdateDetails("client_2", newItems, DateTime.UtcNow.AddDays(20));

        invoice.Amount.ShouldBe(400m);
        invoice.ClientId.ShouldBe("client_2");
        invoice.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(InvoiceStatus.Pagado)]
    [InlineData(InvoiceStatus.Desactivado)]
    public void UpdateDetails_WhenTerminal_Throws(InvoiceStatus terminalStatus)
    {
        var invoice = Invoice.Create("client_1", SampleItems(), DateTime.UtcNow.AddDays(10));
        invoice.UpdateStatus(terminalStatus);

        invoice.IsTerminal.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(
            () => invoice.UpdateDetails("client_1", SampleItems(), DateTime.UtcNow));
    }

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio)]
    public void UpdateDetails_WhenNotTerminal_Succeeds(InvoiceStatus status)
    {
        var invoice = Invoice.Create("client_1", SampleItems(), DateTime.UtcNow.AddDays(10));
        if (status != invoice.Status)
            invoice.UpdateStatus(status);

        Should.NotThrow(() => invoice.UpdateDetails("client_1", SampleItems(), DateTime.UtcNow.AddDays(5)));
    }

    // ── Compatibilidad: constructor legacy mantiene el invariante ──────────────────

    [Fact]
    public void LegacyConstructor_CreatesSingleSyntheticItemMatchingAmount()
    {
        var invoice = new Invoice("client_1", 500m);

        invoice.Amount.ShouldBe(500m);
        invoice.Items.Count.ShouldBe(1);
        invoice.Items[0].Subtotal.ShouldBe(500m);
    }
}
