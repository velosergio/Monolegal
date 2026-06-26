using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Services;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Tests del detalle GET /api/invoices/{id} (US2, spec 009).
/// </summary>
[Trait("Category", "Application")]
public class GetInvoiceByIdTests
{
    [Fact]
    public async Task ExistingId_ReturnsCompleteDto()
    {
        var repo = new InMemoryInvoiceRepository();
        var invoice = InvoiceTestFactory.Create("c1", 250m, InvoiceStatus.PrimerRecordatorio);
        await repo.AddAsync(invoice);

        var found = await repo.GetByIdAsync(invoice.Id);
        found.ShouldNotBeNull();

        var transitionService = new InvoiceTransitionService();
        var dto = InvoiceDetailDto.FromEntity(found!, transitionService.GetAllowedTransitions(found!.Status));
        dto.Id.ShouldBe(invoice.Id);
        dto.ClientId.ShouldBe("c1");
        dto.Amount.ShouldBe(250m);
        dto.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        dto.LastStatusTransitionAt.ShouldBe(invoice.LastStatusTransitionAt);
        // PrimerRecordatorio → {SegundoRecordatorio, Pagado} (spec 015)
        dto.AllowedTransitions.ShouldBe(new[] { "segundorecordatorio", "pagado" });
        dto.StatusHistory.ShouldNotBeNull();
    }

    [Fact]
    public async Task NonExistentId_ReturnsNull_MapsTo404()
    {
        var repo = new InMemoryInvoiceRepository();

        var found = await repo.GetByIdAsync("no-existe");

        // El endpoint traduce null → 404 (también para id con formato inválido, Q4).
        found.ShouldBeNull();
    }
}
