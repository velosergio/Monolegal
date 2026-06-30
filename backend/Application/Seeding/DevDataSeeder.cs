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
/// <remarks>
/// SOLID: DIP — implementa <see cref="IDevDataSeeder"/> y persiste vía <see cref="IInvoiceRepository"/> (abstracción).
/// SRP — única responsabilidad: sembrar el conjunto de datos de desarrollo de forma idempotente.
/// </remarks>
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
    private readonly IClientRepository? _clientRepository;
    private readonly ILogger<DevDataSeeder> _logger;

    /// <param name="clientRepository">
    /// Repositorio de clientes (spec 018). Opcional para no romper construcciones de test previas:
    /// cuando se provee, el seeder también crea los documentos <c>Client</c> con IDs estables.
    /// </param>
    public DevDataSeeder(
        IInvoiceRepository invoiceRepository,
        ILogger<DevDataSeeder> logger,
        IClientRepository? clientRepository = null)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
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

        // Clientes primero (si hay repositorio): las facturas referencian sus IDs estables.
        if (_clientRepository is not null)
        {
            foreach (var clientPlan in SeedDataDefinition.Clients)
            {
                var client = Client.CreateForSeed(
                    clientPlan.Id, clientPlan.Name, clientPlan.Email, clientPlan.Phone, clientPlan.Address);
                await _clientRepository.AddAsync(client, cancellationToken).ConfigureAwait(false);
            }
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
