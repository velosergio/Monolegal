using System;
using System.Threading.Tasks;
using Backend.Application.Validation;
using Backend.Tests.Infrastructure.Support;
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
}
