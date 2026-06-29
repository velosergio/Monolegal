using System;
using Monolegal.Api.Endpoints.Invoices;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Mapeo del DTO de envío (spec 019): derivación de sendStatus, email/lastAttempt nulos y lastError
/// sólo en fallido.
/// </summary>
[Trait("Category", "Application")]
public sealed class ShipmentDtoTests
{
    [Theory]
    [InlineData(NotificationOutcome.None, "pending")]
    [InlineData(NotificationOutcome.Sent, "sent")]
    [InlineData(NotificationOutcome.Failed, "failed")]
    [InlineData(NotificationOutcome.Skipped, "skipped")]
    public void ToSendStatus_MapsOutcome(NotificationOutcome outcome, string expected)
        => ShipmentListItemDto.ToSendStatus(outcome).ShouldBe(expected);

    [Theory]
    [InlineData("pending", NotificationOutcome.None)]
    [InlineData("SENT", NotificationOutcome.Sent)]
    [InlineData("failed", NotificationOutcome.Failed)]
    [InlineData("skipped", NotificationOutcome.Skipped)]
    public void ParseSendStatus_ParsesValid(string value, NotificationOutcome expected)
        => ShipmentListItemDto.ParseSendStatus(value).ShouldBe(expected);

    [Theory]
    [InlineData("bogus")]
    [InlineData("")]
    public void ParseSendStatus_ReturnsNull_ForInvalid(string value)
        => ShipmentListItemDto.ParseSendStatus(value).ShouldBeNull();

    [Fact]
    public void FromEntity_FailedInvoice_ExposesError()
    {
        var invoice = new Invoice("cli-1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoice.RecordNotificationResult(NotificationType.Reminder, NotificationOutcome.Failed, DateTime.UtcNow, "SMTP timeout");
        invoice.RecordNotificationRetry();

        var dto = ShipmentListItemDto.FromEntity(invoice, "ACME", "pagos@acme.com");

        dto.SendStatus.ShouldBe("failed");
        dto.ClientName.ShouldBe("ACME");
        dto.ClientEmail.ShouldBe("pagos@acme.com");
        dto.LastError.ShouldBe("SMTP timeout");
        dto.RetryCount.ShouldBe(1);
        dto.LastAttemptAt.ShouldNotBeNull();
    }

    [Fact]
    public void FromEntity_PendingInvoice_HasNoErrorOrAttempt()
    {
        var invoice = new Invoice("cli-1", 100m);
        invoice.UpdateStatus(InvoiceStatus.PrimerRecordatorio);

        var dto = ShipmentListItemDto.FromEntity(invoice, "ACME", null);

        dto.SendStatus.ShouldBe("pending");
        dto.ClientEmail.ShouldBeNull();
        dto.LastError.ShouldBeNull();
        dto.LastAttemptAt.ShouldBeNull();
        dto.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void FromEntity_SentInvoice_DoesNotExposeStaleError()
    {
        var invoice = new Invoice("cli-1", 100m);
        invoice.UpdateStatus(InvoiceStatus.Pagado);
        invoice.RecordNotificationResult(NotificationType.PaymentConfirmation, NotificationOutcome.Sent, DateTime.UtcNow);

        var dto = ShipmentListItemDto.FromEntity(invoice, "ACME", "x@y.com");

        dto.SendStatus.ShouldBe("sent");
        dto.LastError.ShouldBeNull();
    }
}
