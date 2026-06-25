using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Application.Notifications;

/// <summary>
/// Orquesta la notificación por correo al ocurrir una transición de estado de una factura
/// (spec 013). Selecciona la plantilla según el nuevo estado, resuelve el correo del cliente,
/// invoca <see cref="IEmailService"/>, registra el resultado en la entidad y actualiza los
/// metadatos de recordatorio en envíos exitosos. NO persiste: muta la entidad en memoria y el
/// llamador (worker o endpoint) ejecuta una única actualización después.
///
/// Reglas (research D4/D5/D7):
///  - Estado sin plantilla ⇒ resultado Skipped, sin invocar al proveedor.
///  - Sin correo de destinatario ⇒ resultado Skipped.
///  - Envío exitoso ⇒ resultado Sent; si es recordatorio, además incrementa contadores.
///  - Fallo de envío ⇒ resultado Failed, sin tocar contadores ni revertir la transición; no se relanza.
///  - Cancelación ⇒ se propaga (apagado ordenado).
/// </summary>
public sealed class InvoiceTransitionNotifier : IInvoiceTransitionNotifier
{
    private readonly IEmailService _emailService;
    private readonly IClientEmailResolver _emailResolver;
    private readonly ILogger<InvoiceTransitionNotifier> _logger;

    public InvoiceTransitionNotifier(
        IEmailService emailService,
        IClientEmailResolver emailResolver,
        ILogger<InvoiceTransitionNotifier> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _emailResolver = emailResolver ?? throw new ArgumentNullException(nameof(emailResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyTransitionAsync(
        Invoice invoice,
        InvoiceStatus previousStatus,
        CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));

        var now = DateTime.UtcNow;

        // 1. Estado sin plantilla aplicable → Skipped.
        if (!TryMapNotificationType(invoice.Status, out var type))
        {
            invoice.RecordNotificationResult(null, NotificationOutcome.Skipped, now);
            LogResult(invoice, previousStatus, type: null, NotificationOutcome.Skipped, error: null);
            return;
        }

        // 2. Resolver el correo del destinatario; sin correo → Skipped.
        var clientEmail = await _emailResolver
            .ResolveEmailAsync(invoice.ClientId, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            invoice.RecordNotificationResult(type, NotificationOutcome.Skipped, now, "sin correo de destinatario");
            LogResult(invoice, previousStatus, type, NotificationOutcome.Skipped, error: "sin correo de destinatario");
            return;
        }

        // 3. Enviar el correo según el tipo.
        try
        {
            await SendAsync(type, clientEmail!, invoice, cancellationToken).ConfigureAwait(false);

            invoice.RecordNotificationResult(type, NotificationOutcome.Sent, now);
            if (type == NotificationType.Reminder)
                invoice.RecordReminderSent();

            LogResult(invoice, previousStatus, type, NotificationOutcome.Sent, error: null);
        }
        catch (OperationCanceledException)
        {
            // Apagado ordenado: no se registra como fallo, se propaga.
            throw;
        }
        catch (Exception ex)
        {
            // Fallo de envío: no se revierte la transición ni se tocan contadores; no se relanza.
            invoice.RecordNotificationResult(type, NotificationOutcome.Failed, now, ex.Message);
            LogResult(invoice, previousStatus, type, NotificationOutcome.Failed, error: ex.Message);
        }
    }

    private Task SendAsync(NotificationType type, string clientEmail, Invoice invoice, CancellationToken ct) =>
        type switch
        {
            NotificationType.Reminder => _emailService.SendReminderAsync(clientEmail, invoice, ct),
            NotificationType.PaymentConfirmation => _emailService.SendPaymentConfirmationAsync(clientEmail, invoice, ct),
            NotificationType.DeactivationNotice => _emailService.SendDeactivationNoticeAsync(clientEmail, invoice, ct),
            _ => Task.CompletedTask
        };

    private static bool TryMapNotificationType(InvoiceStatus status, out NotificationType type)
    {
        switch (status)
        {
            case InvoiceStatus.PrimerRecordatorio:
            case InvoiceStatus.SegundoRecordatorio:
                type = NotificationType.Reminder;
                return true;
            case InvoiceStatus.Pagado:
                type = NotificationType.PaymentConfirmation;
                return true;
            case InvoiceStatus.Desactivado:
                type = NotificationType.DeactivationNotice;
                return true;
            default:
                type = default;
                return false;
        }
    }

    private void LogResult(
        Invoice invoice,
        InvoiceStatus previousStatus,
        NotificationType? type,
        NotificationOutcome outcome,
        string? error)
    {
        // Log estructurado JSON por factura procesada (spec 013, 3.4).
        if (outcome == NotificationOutcome.Failed)
        {
            _logger.LogError(
                "Notificación de transición. InvoiceId={InvoiceId} De={PreviousStatus} A={NewStatus} Tipo={NotificationType} Resultado={NotificationOutcome} Error={Error}",
                invoice.Id, previousStatus, invoice.Status, type, outcome, error);
        }
        else
        {
            _logger.LogInformation(
                "Notificación de transición. InvoiceId={InvoiceId} De={PreviousStatus} A={NewStatus} Tipo={NotificationType} Resultado={NotificationOutcome} Error={Error}",
                invoice.Id, previousStatus, invoice.Status, type, outcome, error);
        }
    }
}
