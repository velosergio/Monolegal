using System.Collections.Generic;
using System.Text.Json;
using Monolegal.Domain.Enums;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// Mapeo entre <see cref="InvoiceStatus"/> y su representación en el contrato HTTP
/// (cadena en minúscula del nombre del miembro). Único punto de verdad para entrada
/// (query param <c>status</c>, body <c>newStatus</c>) y salida (respuestas).
///
/// Ver specs/009-invoice-api-endpoints/research.md (D1).
/// </summary>
public static class InvoiceStatusApi
{
    /// <summary>
    /// Conjunto cerrado de estados válidos aceptados como filtro o destino de transición.
    /// Los estados legacy (Draft/Overdue/Cancelled) se retiraron del sistema (spec 015, FR-031).
    /// </summary>
    public static readonly IReadOnlyCollection<InvoiceStatus> ValidStatuses = new[]
    {
        InvoiceStatus.Pending,
        InvoiceStatus.PrimerRecordatorio,
        InvoiceStatus.SegundoRecordatorio,
        InvoiceStatus.Desactivado,
        InvoiceStatus.Pagado
    };

    /// <summary>
    /// Devuelve la representación de API (minúscula) de un estado, p. ej.
    /// <c>PrimerRecordatorio → "primerrecordatorio"</c>.
    /// </summary>
    public static string ToApiString(InvoiceStatus status)
        => status.ToString().ToLowerInvariant();

    /// <summary>
    /// Intenta parsear una cadena de API (case-insensitive) a un estado válido del conjunto
    /// aceptado. Devuelve <c>false</c> si la cadena es nula, vacía o no corresponde a un
    /// estado válido de la feature.
    /// </summary>
    public static bool TryParse(string? value, out InvoiceStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var candidate in ValidStatuses)
        {
            if (string.Equals(candidate.ToString(), value, System.StringComparison.OrdinalIgnoreCase))
            {
                status = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Indica si la cadena dada corresponde a un estado válido de la feature.
    /// </summary>
    public static bool IsValid(string? value) => TryParse(value, out _);
}

/// <summary>
/// Política de nomenclatura JSON que emite los nombres de enum en minúsculas, de modo que
/// <see cref="InvoiceStatus"/> se serialice como <c>"primerrecordatorio"</c>, <c>"pagado"</c>, etc.
/// Se registra junto a <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>
/// en Program.cs.
/// </summary>
public sealed class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToLowerInvariant();
}
