using System;
using Shouldly;
using Xunit;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Monolegal.Domain.Tests;

/// <summary>
/// Contador de reintentos de notificación del aviso vigente (spec 019, data-model.md).
/// Reset al entrar en estado notificable; incremento explícito por reintento; el registro de
/// resultado NO toca el contador.
/// </summary>
public class InvoiceNotificationRetryTests
{
    [Fact]
    public void NewInvoice_ShouldStartWithZeroRetries()
    {
        var invoice = new Invoice("client_1", 100m);

        invoice.NotificationRetryCount.ShouldBe(0);
    }

    [Fact]
    public void RecordNotificationRetry_ShouldIncrementCounter()
    {
        var invoice = new Invoice("client_1", 100m);

        invoice.RecordNotificationRetry();
        invoice.RecordNotificationRetry();

        invoice.NotificationRetryCount.ShouldBe(2);
    }

    [Fact]
    public void RecordNotificationResult_ShouldNotChangeRetryCounter()
    {
        var invoice = new Invoice("client_1", 100m);
        invoice.RecordNotificationRetry(); // contador = 1

        invoice.RecordNotificationResult(
            NotificationType.Reminder, NotificationOutcome.Failed, DateTime.UtcNow, "boom");

        invoice.NotificationRetryCount.ShouldBe(1);
    }

    [Fact]
    public void UpdateStatus_ToNotifiableStatus_ShouldResetRetryCounter()
    {
        var invoice = new Invoice("client_1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoice.RecordNotificationRetry();
        invoice.RecordNotificationRetry();
        invoice.NotificationRetryCount.ShouldBe(2);

        // Entrar en un nuevo estado notificable inicia el conteo del nuevo aviso.
        invoice.UpdateStatus(InvoiceStatus.SegundoRecordatorio);

        invoice.NotificationRetryCount.ShouldBe(0);
    }

    [Fact]
    public void UpdateStatus_ToNonNotifiableStatus_ShouldNotResetRetryCounter()
    {
        // Pending no es notificable; un cambio hacia él no debe reiniciar el conteo.
        var invoice = new Invoice("client_1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoice.RecordNotificationRetry();

        invoice.UpdateStatus(InvoiceStatus.Pending);

        invoice.NotificationRetryCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.Pagado)]
    [InlineData(InvoiceStatus.Desactivado)]
    public void UpdateStatus_ToAnyNotifiableStatus_ShouldResetCounter(InvoiceStatus status)
    {
        var invoice = new Invoice("client_1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoice.RecordNotificationRetry();

        invoice.UpdateStatus(status);

        invoice.NotificationRetryCount.ShouldBe(0);
    }
}
