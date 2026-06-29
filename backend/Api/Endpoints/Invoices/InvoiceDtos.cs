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
    string ClientName,
    decimal Amount,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime LastStatusTransitionAt)
{
    /// <summary>
    /// Mapea la entidad al DTO de listado. <paramref name="clientName"/> es el nombre legible del
    /// cliente resuelto desde la colección <c>Clients</c>; ante un cliente inexistente se recurre al
    /// propio <see cref="Invoice.ClientId"/> como respaldo.
    /// </summary>
    public static InvoiceListItemDto FromEntity(Invoice invoice, string clientName) => new(
        invoice.Id,
        invoice.ClientId,
        clientName,
        invoice.Amount,
        invoice.Status,
        invoice.CreatedAt,
        invoice.LastStatusTransitionAt);
}

/// <summary>Respuesta paginada genérica: datos de la página, total de coincidencias y tamaño de página.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Data, long Total, int PageSize);

/// <summary>Línea de detalle de una factura para transporte HTTP (spec 018). El subtotal es derivado.</summary>
public sealed record InvoiceItemDto(string Description, decimal Quantity, decimal UnitPrice, decimal Subtotal)
{
    public static InvoiceItemDto FromEntity(InvoiceItem item) =>
        new(item.Description, item.Quantity, item.UnitPrice, item.Subtotal);
}

/// <summary>Línea de detalle recibida en creación/edición (sin subtotal: se deriva).</summary>
public sealed record InvoiceItemInput(string? Description, decimal Quantity, decimal UnitPrice);

/// <summary>Cuerpo de POST /api/invoices (spec 018, RF-001). El monto NO se captura: se deriva de los items.</summary>
public sealed record CreateInvoiceRequest(string? ClientId, DateTime? DueDate, IReadOnlyList<InvoiceItemInput>? Items);

/// <summary>Cuerpo de PUT /api/invoices/{id} (spec 018, RF-003). Sin status ni amount.</summary>
public sealed record UpdateInvoiceRequest(string? ClientId, DateTime? DueDate, IReadOnlyList<InvoiceItemInput>? Items);

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
    string ClientName,
    decimal Amount,
    DateTime DueDate,
    IReadOnlyList<InvoiceItemDto> Items,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int RemindersCount,
    DateTime? LastReminderSentAt,
    DateTime LastStatusTransitionAt,
    IReadOnlyList<StatusChangeDto> StatusHistory,
    IReadOnlyList<string> AllowedTransitions)
{
    /// <summary>
    /// Mapea la entidad al DTO de detalle. <paramref name="clientName"/> es el nombre legible del
    /// cliente; cuando es <c>null</c> (cliente no resuelto) se recurre al <see cref="Invoice.ClientId"/>.
    /// </summary>
    public static InvoiceDetailDto FromEntity(
        Invoice invoice,
        IReadOnlyList<InvoiceStatus> allowedTransitions,
        string? clientName = null) => new(
        invoice.Id,
        invoice.ClientId,
        clientName ?? invoice.ClientId,
        invoice.Amount,
        invoice.DueDate,
        invoice.Items.Select(InvoiceItemDto.FromEntity).ToList(),
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

/// <summary>
/// Ítem del listado de envíos (spec 019). El <c>SendStatus</c> se deriva del resultado de la última
/// notificación; el estado "reintentando" NO existe aquí: es transitorio en el cliente mientras una
/// mutación de reenvío está en curso. <c>ClientEmail</c> y <c>LastAttemptAt</c> pueden ser nulos;
/// <c>LastError</c> sólo es no nulo cuando <c>SendStatus == "failed"</c>.
/// </summary>
public sealed record ShipmentListItemDto(
    string Id,
    string ClientId,
    string ClientName,
    string? ClientEmail,
    InvoiceStatus Status,
    string SendStatus,
    DateTime? LastAttemptAt,
    int RetryCount,
    string? LastError)
{
    /// <summary>Mapea una factura y su correo/nombre de cliente resueltos al ítem de envío.</summary>
    public static ShipmentListItemDto FromEntity(Invoice invoice, string clientName, string? clientEmail) => new(
        invoice.Id,
        invoice.ClientId,
        clientName,
        clientEmail,
        invoice.Status,
        ToSendStatus(invoice.LastNotificationOutcome),
        invoice.LastNotificationAt,
        invoice.NotificationRetryCount,
        invoice.LastNotificationOutcome == NotificationOutcome.Failed ? invoice.LastNotificationError : null);

    /// <summary>Deriva el estado de envío de la vista a partir del resultado de notificación.</summary>
    public static string ToSendStatus(NotificationOutcome outcome) => outcome switch
    {
        NotificationOutcome.None => "pending",
        NotificationOutcome.Sent => "sent",
        NotificationOutcome.Failed => "failed",
        NotificationOutcome.Skipped => "skipped",
        _ => "pending",
    };

    /// <summary>Parsea el filtro <c>sendStatus</c> de la API al <see cref="NotificationOutcome"/>. Null si no es válido.</summary>
    public static NotificationOutcome? ParseSendStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "pending" => NotificationOutcome.None,
        "sent" => NotificationOutcome.Sent,
        "failed" => NotificationOutcome.Failed,
        "skipped" => NotificationOutcome.Skipped,
        _ => null,
    };

    /// <summary>Indica si la cadena es un filtro de sendStatus válido (o ausente).</summary>
    public static bool IsValidSendStatusFilter(string? value)
        => string.IsNullOrWhiteSpace(value) || ParseSendStatus(value) is not null;
}
