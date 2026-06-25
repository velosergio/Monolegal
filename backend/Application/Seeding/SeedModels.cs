using Monolegal.Domain.Enums;

namespace Backend.Application.Seeding;

/// <summary>
/// Definición declarativa de una factura a sembrar, antes de materializarse en la
/// entidad de dominio <c>Invoice</c>. Ver specs/008-seed-data-clientes/data-model.md.
/// </summary>
public sealed record SeedInvoicePlan(string ClientId, decimal Amount, InvoiceStatus Status);

/// <summary>
/// Resultado observable de una ejecución del sembrador de datos de desarrollo.
/// </summary>
/// <param name="Seeded"><c>true</c> si se sembró; <c>false</c> si se omitió.</param>
/// <param name="Reason">Motivo legible del resultado (p. ej. "base vacía" / "datos existentes").</param>
/// <param name="ClientsCreated">Cantidad de clientes distintos creados (0 u 3).</param>
/// <param name="InvoicesCreated">Cantidad de facturas creadas (0 u 8).</param>
public sealed record SeedResult(bool Seeded, string Reason, int ClientsCreated, int InvoicesCreated);
