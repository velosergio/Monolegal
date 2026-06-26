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

    /// <summary>
    /// Definiciones de los 3 clientes a sembrar (spec 018, research D11). Usan los IDs estables
    /// referenciados por las facturas, con emails coherentes para la resolución de notificaciones.
    /// </summary>
    public static IReadOnlyList<SeedClientPlan> Clients { get; } = new List<SeedClientPlan>
    {
        new(ClienteA, "Cliente A", "cliente.a@monolegal.test", "+57 300 000 0001", "Calle A #1-11"),
        new(ClienteB, "Cliente B", "cliente.b@monolegal.test", "+57 300 000 0002", "Calle B #2-22"),
        new(ClienteC, "Cliente C", "cliente.c@monolegal.test", "+57 300 000 0003", "Calle C #3-33"),
    };

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
