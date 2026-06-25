using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Tests del mapeo InvoiceStatus ↔ cadena de API en minúscula (spec 009, research.md D1).
/// </summary>
[Trait("Category", "Application")]
public class InvoiceStatusApiTests
{
    [Theory]
    [InlineData(InvoiceStatus.Pending, "pending")]
    [InlineData(InvoiceStatus.PrimerRecordatorio, "primerrecordatorio")]
    [InlineData(InvoiceStatus.SegundoRecordatorio, "segundorecordatorio")]
    [InlineData(InvoiceStatus.Desactivado, "desactivado")]
    [InlineData(InvoiceStatus.Pagado, "pagado")]
    public void ToApiString_ReturnsLowercaseName(InvoiceStatus status, string expected)
    {
        InvoiceStatusApi.ToApiString(status).ShouldBe(expected);
    }

    [Theory]
    [InlineData("pending", InvoiceStatus.Pending)]
    [InlineData("PrimerRecordatorio", InvoiceStatus.PrimerRecordatorio)]
    [InlineData("SEGUNDORECORDATORIO", InvoiceStatus.SegundoRecordatorio)]
    [InlineData("pagado", InvoiceStatus.Pagado)]
    public void TryParse_AcceptsValidCaseInsensitive(string value, InvoiceStatus expected)
    {
        InvoiceStatusApi.TryParse(value, out var status).ShouldBeTrue();
        status.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foo")]
    [InlineData("draft")]   // legacy: no es destino válido en esta feature
    public void TryParse_RejectsInvalidOrLegacy(string? value)
    {
        InvoiceStatusApi.TryParse(value, out _).ShouldBeFalse();
        InvoiceStatusApi.IsValid(value).ShouldBeFalse();
    }
}
