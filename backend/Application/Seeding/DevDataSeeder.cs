using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;

namespace Backend.Application.Seeding;

/// <summary>
/// Implementación del sembrador de datos de desarrollo (spec 008, US1/US2).
/// Siembra el conjunto fijo de <see cref="SeedDataDefinition"/> sólo si la colección
/// de facturas está vacía (idempotencia), persistiendo vía <see cref="IInvoiceRepository"/>
/// y registrando el resultado con logging estructurado.
/// </summary>
public sealed class DevDataSeeder : IDevDataSeeder
{
    /// <summary>
    /// Cantidad de recordatorios coherente con cada estado del ciclo de vida
    /// (data-model.md, RF-007). Los estados no listados implican 0 recordatorios.
    /// </summary>
    private static readonly IReadOnlyDictionary<InvoiceStatus, int> RemindersByStatus =
        new Dictionary<InvoiceStatus, int>
        {
            [InvoiceStatus.PrimerRecordatorio] = 1,
            [InvoiceStatus.SegundoRecordatorio] = 2,
            [InvoiceStatus.Desactivado] = 2,
        };

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(IInvoiceRepository invoiceRepository, ILogger<DevDataSeeder> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    public async Task<SeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _invoiceRepository.CountAsync(cancellationToken).ConfigureAwait(false);
        if (existing > 0)
        {
            var skipped = new SeedResult(false, "datos existentes", 0, 0);
            _logger.LogInformation(
                "Seed de desarrollo omitido. Sembrado={Seeded} Motivo={Reason} FacturasExistentes={Existing}",
                skipped.Seeded, skipped.Reason, existing);
            return skipped;
        }

        var plans = SeedDataDefinition.Invoices;
        foreach (var plan in plans)
        {
            var invoice = BuildInvoice(plan);
            await _invoiceRepository.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
        }

        var clientsCreated = plans.Select(p => p.ClientId).Distinct().Count();
        var result = new SeedResult(true, "base vacía", clientsCreated, plans.Count);

        _logger.LogInformation(
            "Seed de desarrollo completado. Sembrado={Seeded} Motivo={Reason} Clientes={Clients} Facturas={Invoices}",
            result.Seeded, result.Reason, result.ClientsCreated, result.InvoicesCreated);

        return result;
    }

    /// <summary>
    /// Materializa un <see cref="SeedInvoicePlan"/> en una entidad <see cref="Invoice"/>,
    /// llevándola al estado objetivo y registrando una cantidad de recordatorios coherente.
    /// </summary>
    private static Invoice BuildInvoice(SeedInvoicePlan plan)
    {
        var invoice = new Invoice(plan.ClientId, plan.Amount);

        if (plan.Status != invoice.Status)
            invoice.UpdateStatus(plan.Status);

        if (RemindersByStatus.TryGetValue(plan.Status, out var reminders))
        {
            for (var i = 0; i < reminders; i++)
                invoice.RecordReminderSent();
        }

        return invoice;
    }
}
