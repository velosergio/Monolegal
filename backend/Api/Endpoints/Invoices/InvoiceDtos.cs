using System;
using System.Collections.Generic;
using System.Linq;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Monolegal.Api.Endpoints.Invoices;

/// <summary>
/// DTOs de transporte HTTP para los endpoints de facturas (spec 009, data-model.md).
/// El campo <c>Status</c> se serializa como cadena en minúscula gracias al
/// JsonStringEnumConverter global (research.md D1).
/// </summary>
public sealed record InvoiceListItemDto(
    string Id,
    string ClientId,
    decimal Amount,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime LastStatusTransitionAt)
{
    public static InvoiceListItemDto FromEntity(Invoice invoice) => new(
        invoice.Id,
        invoice.ClientId,
        invoice.Amount,
        invoice.Status,
        invoice.CreatedAt,
        invoice.LastStatusTransitionAt);
}

/// <summary>Respuesta paginada genérica: datos de la página, total de coincidencias y tamaño de página.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Data, long Total, int PageSize);

/// <summary>Un evento del historial de cambios de estado (spec 015).</summary>
public sealed record StatusChangeDto(
    string From,
    string To,
    DateTime At,
    string Source)
{
    public static StatusChangeDto FromEntity(StatusChange change) => new(
        InvoiceStatusApi.ToApiString(change.From),
        InvoiceStatusApi.ToApiString(change.To),
        change.At,
        change.Source.ToString().ToLowerInvariant());
}

/// <summary>
/// Objeto completo de una factura para el endpoint de detalle, extendido (spec 015) con el
/// historial de cambios de estado y los estados destino válidos.
/// </summary>
public sealed record InvoiceDetailDto(
    string Id,
    string ClientId,
    decimal Amount,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int RemindersCount,
    DateTime? LastReminderSentAt,
    DateTime LastStatusTransitionAt,
    IReadOnlyList<StatusChangeDto> StatusHistory,
    IReadOnlyList<string> AllowedTransitions)
{
    public static InvoiceDetailDto FromEntity(
        Invoice invoice,
        IReadOnlyList<InvoiceStatus> allowedTransitions) => new(
        invoice.Id,
        invoice.ClientId,
        invoice.Amount,
        invoice.Status,
        invoice.CreatedAt,
        invoice.UpdatedAt,
        invoice.RemindersCount,
        invoice.LastReminderSentAt,
        invoice.LastStatusTransitionAt,
        invoice.StatusHistory.Select(StatusChangeDto.FromEntity).ToList(),
        allowedTransitions.Select(InvoiceStatusApi.ToApiString).ToList());
}

/// <summary>Cuerpo de la petición de transición manual de estado.</summary>
public sealed record TransitionRequest(string? NewStatus);

/// <summary>Estadísticas agregadas para el dashboard.</summary>
public sealed record InvoiceStatsDto(
    long TotalInvoices,
    IReadOnlyDictionary<string, long> ByStatus,
    IReadOnlyDictionary<string, long> ByClient);
