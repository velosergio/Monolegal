using Monolegal.Domain.Enums;
using Monolegal.Domain.Services;
using Shouldly;
using Xunit;

namespace Monolegal.Domain.Tests.Services;

/// <summary>
/// Pruebas de <see cref="InvoiceTransitionService.GetAllowedTransitions"/> (spec 015, FR-012):
/// los destinos válidos por estado son la única fuente de verdad consumida por el frontend.
/// </summary>
public class InvoiceTransitionServiceTests
{
    private readonly InvoiceTransitionService _service = new();

    [Fact]
    public void GetAllowedTransitions_Pending_ReturnsPrimerRecordatorioAndPagado()
    {
        _service.GetAllowedTransitions(InvoiceStatus.Pending)
            .ShouldBe(new[] { InvoiceStatus.PrimerRecordatorio, InvoiceStatus.Pagado });
    }

    [Fact]
    public void GetAllowedTransitions_PrimerRecordatorio_ReturnsSegundoAndPagado()
    {
        _service.GetAllowedTransitions(InvoiceStatus.PrimerRecordatorio)
            .ShouldBe(new[] { InvoiceStatus.SegundoRecordatorio, InvoiceStatus.Pagado });
    }

    [Fact]
    public void GetAllowedTransitions_SegundoRecordatorio_ReturnsDesactivadoAndPagado()
    {
        _service.GetAllowedTransitions(InvoiceStatus.SegundoRecordatorio)
            .ShouldBe(new[] { InvoiceStatus.Desactivado, InvoiceStatus.Pagado });
    }

    [Fact]
    public void GetAllowedTransitions_Desactivado_ReturnsOnlyPagado()
    {
        _service.GetAllowedTransitions(InvoiceStatus.Desactivado)
            .ShouldBe(new[] { InvoiceStatus.Pagado });
    }

    [Fact]
    public void GetAllowedTransitions_Pagado_IsTerminal_ReturnsEmpty()
    {
        _service.GetAllowedTransitions(InvoiceStatus.Pagado).ShouldBeEmpty();
    }
}
