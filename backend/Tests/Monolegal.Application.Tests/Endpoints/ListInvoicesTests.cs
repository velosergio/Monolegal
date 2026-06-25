using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Application.Validation;
using Backend.Tests.Infrastructure.Support;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Endpoints;

/// <summary>
/// Tests del listado paginado GET /api/invoices (US1, spec 009).
/// Ejercitan los componentes reales que orquesta el endpoint: validador de parámetros,
/// mapeo de estado y repositorio (GetPagedAsync con filtro, total y orden CreatedAt desc).
/// </summary>
[Trait("Category", "Application")]
public class ListInvoicesTests
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;

    private static ListInvoicesQueryValidator Validator()
        => new(InvoiceStatusApi.IsValid);

    private static Invoice At(string clientId, InvoiceStatus status, DateTime createdAt)
    {
        var invoice = InvoiceTestFactory.Create(clientId, 100m, status);
        invoice.OverrideCreatedAt(createdAt);
        return invoice;
    }

    // ── Filtro por estado ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_WithStatusFilter_ReturnsOnlyMatching()
    {
        var repo = new InMemoryInvoiceRepository();
        await repo.AddAsync(At("c1", InvoiceStatus.Pending, DateTime.UtcNow));
        await repo.AddAsync(At("c2", InvoiceStatus.PrimerRecordatorio, DateTime.UtcNow));
        await repo.AddAsync(At("c3", InvoiceStatus.PrimerRecordatorio, DateTime.UtcNow));

        var (items, total) = await repo.GetPagedAsync(InvoiceStatus.PrimerRecordatorio, 1, 10);

        total.ShouldBe(2);
        items.ShouldAllBe(i => i.Status == InvoiceStatus.PrimerRecordatorio);
    }

    // ── Paginación: total independiente de la página ─────────────────────────────

    [Fact]
    public async Task GetPaged_TotalReflectsAllMatches_NotJustPage()
    {
        var repo = new InMemoryInvoiceRepository();
        for (var i = 0; i < 25; i++)
            await repo.AddAsync(At($"c{i}", InvoiceStatus.Pending, DateTime.UtcNow.AddSeconds(i)));

        var (items, total) = await repo.GetPagedAsync(null, page: 1, pageSize: 10);

        items.Count.ShouldBe(10);
        total.ShouldBe(25);
    }

    [Fact]
    public async Task GetPaged_PageOutOfRange_ReturnsEmptyWithRealTotal()
    {
        var repo = new InMemoryInvoiceRepository();
        for (var i = 0; i < 5; i++)
            await repo.AddAsync(At($"c{i}", InvoiceStatus.Pending, DateTime.UtcNow.AddSeconds(i)));

        var (items, total) = await repo.GetPagedAsync(null, page: 99, pageSize: 10);

        items.ShouldBeEmpty();
        total.ShouldBe(5);
    }

    // ── Orden CreatedAt descendente ───────────────────────────────────────────────

    [Fact]
    public async Task GetPaged_OrdersByCreatedAtDescending()
    {
        var repo = new InMemoryInvoiceRepository();
        var older = At("c1", InvoiceStatus.Pending, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = At("c2", InvoiceStatus.Pending, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        await repo.AddAsync(older);
        await repo.AddAsync(newer);

        var (items, _) = await repo.GetPagedAsync(null, 1, 10);

        items[0].Id.ShouldBe(newer.Id);
        items[1].Id.ShouldBe(older.Id);
    }

    // ── Validación de parámetros ──────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 1)]
    [InlineData(3, 50)]
    public async Task Validator_AcceptsValidPaging(int page, int pageSize)
    {
        var result = await Validator().ValidateAsync(new ListInvoicesQuery(null, page, pageSize));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, 51)]
    public async Task Validator_RejectsInvalidPaging(int page, int pageSize)
    {
        var result = await Validator().ValidateAsync(new ListInvoicesQuery(null, page, pageSize));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_RejectsInvalidStatus()
    {
        var result = await Validator().ValidateAsync(new ListInvoicesQuery("foo", 1, 10));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public async Task Validator_AcceptsValidStatus()
    {
        var result = await Validator().ValidateAsync(new ListInvoicesQuery("primerrecordatorio", 1, 10));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Validator_AcceptsDefaultsWhenParamsAbsent()
    {
        // El endpoint aplica defaults (page=1, pageSize=10) ante ausencia; deben ser válidos.
        var result = await Validator().ValidateAsync(new ListInvoicesQuery(null, DefaultPage, DefaultPageSize));
        result.IsValid.ShouldBeTrue();
    }
}
