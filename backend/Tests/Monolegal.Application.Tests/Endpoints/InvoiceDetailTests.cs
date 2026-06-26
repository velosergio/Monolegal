using System.Linq;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Services;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Pruebas del detalle extendido (spec 015): el DTO incluye el historial de cambios de estado y
/// los destinos válidos; una transición manual registra un evento con origen "manual" y recalcula
/// los destinos para el nuevo estado.
/// </summary>
[Trait("Category", "Application")]
public class InvoiceDetailTests
{
    private readonly InvoiceTransitionService _service = new();

    [Fact]
    public void DetailDto_IncludesHistoryAndAllowedTransitions()
    {
        var invoice = new Invoice("c1", 500m); // Pending

        var dto = InvoiceDetailDto.FromEntity(invoice, _service.GetAllowedTransitions(invoice.Status));

        dto.Status.ShouldBe(InvoiceStatus.Pending);
        dto.AllowedTransitions.ShouldBe(new[] { "primerrecordatorio", "pagado" });
        // Factura recién creada: sin transiciones aún.
        dto.StatusHistory.ShouldBeEmpty();
    }

    [Fact]
    public void ManualTransition_RecordsManualEvent_AndRecalculatesAllowed()
    {
        var invoice = new Invoice("c1", 500m); // Pending

        // Acto manual (endpoint de transición): Pending → PrimerRecordatorio.
        _service.ApplyManualTransition(invoice, InvoiceStatus.PrimerRecordatorio);

        var dto = InvoiceDetailDto.FromEntity(invoice, _service.GetAllowedTransitions(invoice.Status));

        dto.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        dto.StatusHistory.Count.ShouldBe(1);
        var last = dto.StatusHistory.Last();
        last.From.ShouldBe("pending");
        last.To.ShouldBe("primerrecordatorio");
        last.Source.ShouldBe("manual");
        // Destinos recalculados para el nuevo estado.
        dto.AllowedTransitions.ShouldBe(new[] { "segundorecordatorio", "pagado" });
    }

    [Fact]
    public void Payment_FromDesactivado_RecordsManualEvent_AndBecomesTerminal()
    {
        var invoice = new Invoice("c1", 500m);
        // Llevar a Desactivado por la vía automática para preparar el escenario.
        _service.ApplyManualTransition(invoice, InvoiceStatus.PrimerRecordatorio);
        _service.ApplyManualTransition(invoice, InvoiceStatus.SegundoRecordatorio);
        _service.ApplyManualTransition(invoice, InvoiceStatus.Desactivado);

        _service.ApplyPayment(invoice);

        var dto = InvoiceDetailDto.FromEntity(invoice, _service.GetAllowedTransitions(invoice.Status));
        dto.Status.ShouldBe(InvoiceStatus.Pagado);
        dto.AllowedTransitions.ShouldBeEmpty();
        dto.StatusHistory.Last().To.ShouldBe("pagado");
        dto.StatusHistory.Last().Source.ShouldBe("manual");
    }
}
