using System;
using System.Collections.Generic;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Monolegal.Domain.Services;

/// <summary>
/// Servicio de dominio responsable de evaluar y aplicar las transiciones de estado
/// automáticas de las facturas, basándose en el tiempo transcurrido desde la última
/// transición y la configuración de días en <see cref="InvoiceTransitionsConfig"/>.
/// </summary>
public class InvoiceTransitionService
{
    /// <summary>
    /// Evalúa si una factura debe transicionar de estado según los días configurados
    /// y la fecha actual proporcionada. Si corresponde, aplica la transición en la
    /// entidad y devuelve <c>true</c>; de lo contrario devuelve <c>false</c>.
    /// </summary>
    /// <param name="invoice">Factura a evaluar.</param>
    /// <param name="config">Configuración de tiempos de transición.</param>
    /// <param name="now">Momento actual (inyectado para permitir pruebas deterministas).</param>
    /// <returns><c>true</c> si se aplicó una transición; <c>false</c> si no correspondía.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la factura está en un estado que no admite transición automática de tiempo
    /// (p. ej. <see cref="InvoiceStatus.Pagado"/> o <see cref="InvoiceStatus.Desactivado"/>).
    /// </exception>
    public bool TryApplyTransition(Invoice invoice, InvoiceTransitionsConfig config, DateTime now)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));
        if (config == null) throw new ArgumentNullException(nameof(config));

        return invoice.Status switch
        {
            InvoiceStatus.Pending => TryTransition(
                invoice, config.PendingToFirstReminderDays,
                InvoiceStatus.PrimerRecordatorio, now),

            InvoiceStatus.PrimerRecordatorio => TryTransition(
                invoice, config.FirstToSecondReminderDays,
                InvoiceStatus.SegundoRecordatorio, now),

            InvoiceStatus.SegundoRecordatorio => TryTransition(
                invoice, config.SecondToDeactivatedDays,
                InvoiceStatus.Desactivado, now),

            InvoiceStatus.Desactivado => false,
            InvoiceStatus.Pagado => false,

            _ => false
        };
    }

    /// <summary>
    /// Marca una factura como pagada desde cualquier estado activo válido.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la factura ya está en estado <see cref="InvoiceStatus.Pagado"/>.
    /// </exception>
    public void ApplyPayment(Invoice invoice)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));

        if (invoice.Status == InvoiceStatus.Pagado)
            throw new InvalidOperationException(
                $"No se puede marcar como pagada una factura que ya se encuentra en estado '{InvoiceStatus.Pagado}'.");

        invoice.UpdateStatus(InvoiceStatus.Pagado);
    }

    /// <summary>
    /// Aplica una transición de estado solicitada manualmente, validándola contra la matriz
    /// de transiciones permitidas (spec 006). Para <see cref="InvoiceStatus.Pagado"/> delega en
    /// <see cref="ApplyPayment"/>. Cualquier transición no permitida lanza
    /// <see cref="InvalidOperationException"/> (el endpoint la traduce a HTTP 400).
    ///
    /// Matriz: Pending→{PrimerRecordatorio,Pagado}; PrimerRecordatorio→{SegundoRecordatorio,Pagado};
    /// SegundoRecordatorio→{Desactivado,Pagado}; Desactivado→{Pagado}; Pagado→{}.
    /// </summary>
    public void ApplyManualTransition(Invoice invoice, InvoiceStatus newStatus)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));

        if (newStatus == InvoiceStatus.Pagado)
        {
            ApplyPayment(invoice);
            return;
        }

        if (!IsTransitionAllowed(invoice.Status, newStatus))
            throw new InvalidOperationException(
                $"Transición no permitida de '{invoice.Status}' a '{newStatus}'.");

        invoice.UpdateStatus(newStatus);
    }

    private static bool IsTransitionAllowed(InvoiceStatus current, InvoiceStatus next) =>
        (current, next) switch
        {
            (InvoiceStatus.Pending, InvoiceStatus.PrimerRecordatorio) => true,
            (InvoiceStatus.PrimerRecordatorio, InvoiceStatus.SegundoRecordatorio) => true,
            (InvoiceStatus.SegundoRecordatorio, InvoiceStatus.Desactivado) => true,
            _ => false
        };

    /// <summary>
    /// Devuelve los estados destino válidos para una factura en el estado dado, según la matriz de
    /// transiciones del dominio (spec 015, FR-012). Es la única fuente de verdad de la validez; el
    /// frontend consume este conjunto sin replicar la matriz. Incluye <see cref="InvoiceStatus.Pagado"/>
    /// como destino desde cualquier estado activo; los estados terminales devuelven un conjunto vacío.
    ///
    /// Matriz: Pending→{PrimerRecordatorio,Pagado}; PrimerRecordatorio→{SegundoRecordatorio,Pagado};
    /// SegundoRecordatorio→{Desactivado,Pagado}; Desactivado→{Pagado}; Pagado→{}.
    /// </summary>
    public IReadOnlyList<InvoiceStatus> GetAllowedTransitions(InvoiceStatus current) =>
        current switch
        {
            InvoiceStatus.Pending => new[] { InvoiceStatus.PrimerRecordatorio, InvoiceStatus.Pagado },
            InvoiceStatus.PrimerRecordatorio => new[] { InvoiceStatus.SegundoRecordatorio, InvoiceStatus.Pagado },
            InvoiceStatus.SegundoRecordatorio => new[] { InvoiceStatus.Desactivado, InvoiceStatus.Pagado },
            InvoiceStatus.Desactivado => new[] { InvoiceStatus.Pagado },
            _ => Array.Empty<InvoiceStatus>()
        };

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static bool TryTransition(
        Invoice invoice,
        int daysRequired,
        InvoiceStatus nextStatus,
        DateTime now)
    {
        var elapsed = now - invoice.LastStatusTransitionAt;
        if (elapsed.TotalDays >= daysRequired)
        {
            invoice.UpdateStatus(nextStatus, StatusChangeSource.Automatic);
            return true;
        }
        return false;
    }
}
