using System;
using Monolegal.Domain.Enums;

namespace Monolegal.Domain.Entities;

/// <summary>
/// Registro inmutable de una transición de estado de una factura (spec 015).
/// Vive embebido en <see cref="Invoice.StatusHistory"/>; conforma el historial
/// (audit log) de cambios de estado.
/// </summary>
public sealed class StatusChange
{
    public InvoiceStatus From { get; private set; }
    public InvoiceStatus To { get; private set; }
    public DateTime At { get; private set; }
    public StatusChangeSource Source { get; private set; }

    public StatusChange(InvoiceStatus from, InvoiceStatus to, DateTime at, StatusChangeSource source)
    {
        From = from;
        To = to;
        At = at;
        Source = source;
    }
}
