using System.Collections.Generic;
using Monolegal.Domain.Enums;

namespace Backend.Application.Seeding;

/// <summary>
/// Definición fija del conjunto de datos de desarrollo (spec 008, data-model.md).
/// 3 clientes (A, B, C) y 8 facturas con distribución 3 / 2 / 3, cubriendo estados
/// variados y garantizando al menos una factura en <see cref="InvoiceStatus.PrimerRecordatorio"/>
/// y otra en <see cref="InvoiceStatus.SegundoRecordatorio"/>.
/// </summary>
/// <remarks>
/// No existe entidad/colección Cliente: los clientes se representan por estos
/// identificadores estables (research.md D1).
/// </remarks>
public static class SeedDataDefinition
{
    public const string ClienteA = "seed-cliente-a";
    public const string ClienteB = "seed-cliente-b";
    public const string ClienteC = "seed-cliente-c";

    /// <summary>Las 8 facturas a sembrar (fuente de verdad de los tests).</summary>
    public static IReadOnlyList<SeedInvoicePlan> Invoices { get; } = new List<SeedInvoicePlan>
    {
        // Cliente A — 3 facturas en estados variados
        new(ClienteA, 150.00m, InvoiceStatus.Pending),
        new(ClienteA, 320.00m, InvoiceStatus.PrimerRecordatorio),
        new(ClienteA, 90.00m, InvoiceStatus.Pagado),

        // Cliente B — 2 facturas
        new(ClienteB, 540.00m, InvoiceStatus.SegundoRecordatorio),
        new(ClienteB, 75.00m, InvoiceStatus.Desactivado),

        // Cliente C — 3 facturas
        new(ClienteC, 210.00m, InvoiceStatus.Pending),
        new(ClienteC, 410.00m, InvoiceStatus.PrimerRecordatorio),
        new(ClienteC, 1200.00m, InvoiceStatus.SegundoRecordatorio),
    };
}
