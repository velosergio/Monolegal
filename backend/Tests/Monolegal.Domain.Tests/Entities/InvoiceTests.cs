using System;
using Shouldly;
using Xunit;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Monolegal.Domain.Tests.Entities;

public class InvoiceTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateInvoice()
    {
        // Arrange
        var clientId = "client_123";
        var amount = 1500.50m;

        // Act
        var invoice = new Invoice(clientId, amount);

        // Assert
        invoice.Id.ShouldNotBeNullOrWhiteSpace();
        invoice.ClientId.ShouldBe(clientId);
        invoice.Amount.ShouldBe(amount);
        invoice.Status.ShouldBe(InvoiceStatus.Draft);
        invoice.CreatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        invoice.UpdatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        invoice.RemindersCount.ShouldBe(0);
        invoice.LastReminderSentAt.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithEmptyClientId_ShouldThrowArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Invoice("", 100m));
        Should.Throw<ArgumentException>(() => new Invoice(null!, 100m));
        Should.Throw<ArgumentException>(() => new Invoice("   ", 100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Constructor_WithZeroOrNegativeAmount_ShouldThrowArgumentException(decimal invalidAmount)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() => new Invoice("client_123", invalidAmount));
        exception.Message.ShouldContain("Amount");
    }

    [Fact]
    public void RecordReminderSent_ShouldIncrementCountAndSetDate()
    {
        // Arrange
        var invoice = new Invoice("client_123", 100m);
        var initialUpdatedAt = invoice.UpdatedAt;

        // Wait a small amount of time to ensure the UpdatedAt changes
        System.Threading.Thread.Sleep(10);

        // Act
        invoice.RecordReminderSent();

        // Assert
        invoice.RemindersCount.ShouldBe(1);
        invoice.LastReminderSentAt.ShouldNotBeNull();
        invoice.LastReminderSentAt.Value.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        invoice.UpdatedAt.ShouldBeGreaterThan(initialUpdatedAt);
    }

    [Fact]
    public void UpdateStatus_ShouldChangeStatusAndUpdateAuditDate()
    {
        // Arrange
        var invoice = new Invoice("client_123", 100m);
        var initialUpdatedAt = invoice.UpdatedAt;

        System.Threading.Thread.Sleep(10);

        // Act
        invoice.UpdateStatus(InvoiceStatus.Pagado);

        // Assert
        invoice.Status.ShouldBe(InvoiceStatus.Pagado);
        invoice.UpdatedAt.ShouldBeGreaterThan(initialUpdatedAt);
    }

    // ── spec 013: resultado de notificación ──────────────────────────────────────

    [Fact]
    public void Constructor_ShouldInitializeNotificationResultAsNone()
    {
        // Act
        var invoice = new Invoice("client_123", 100m);

        // Assert
        invoice.LastNotificationType.ShouldBeNull();
        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.None);
        invoice.LastNotificationAt.ShouldBeNull();
        invoice.LastNotificationError.ShouldBeNull();
    }

    [Fact]
    public void RecordNotificationResult_Sent_ShouldStoreTypeOutcomeAndTimestampWithoutError()
    {
        // Arrange
        var invoice = new Invoice("client_123", 100m);
        var initialUpdatedAt = invoice.UpdatedAt;
        var at = DateTime.UtcNow;
        System.Threading.Thread.Sleep(10);

        // Act
        invoice.RecordNotificationResult(NotificationType.Reminder, NotificationOutcome.Sent, at);

        // Assert
        invoice.LastNotificationType.ShouldBe(NotificationType.Reminder);
        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Sent);
        invoice.LastNotificationAt.ShouldBe(at);
        invoice.LastNotificationError.ShouldBeNull();
        invoice.UpdatedAt.ShouldBeGreaterThan(initialUpdatedAt);
    }

    [Fact]
    public void RecordNotificationResult_Failed_ShouldRetainError()
    {
        // Arrange
        var invoice = new Invoice("client_123", 100m);

        // Act
        invoice.RecordNotificationResult(
            NotificationType.PaymentConfirmation, NotificationOutcome.Failed, DateTime.UtcNow, "SMTP caído");

        // Assert
        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Failed);
        invoice.LastNotificationError.ShouldBe("SMTP caído");
    }

    [Fact]
    public void RecordNotificationResult_NonFailed_ShouldIgnoreError()
    {
        // Arrange
        var invoice = new Invoice("client_123", 100m);

        // Act — un error en un resultado que no es Failed no debe persistirse
        invoice.RecordNotificationResult(
            NotificationType.DeactivationNotice, NotificationOutcome.Skipped, DateTime.UtcNow, "ignorar");

        // Assert
        invoice.LastNotificationOutcome.ShouldBe(NotificationOutcome.Skipped);
        invoice.LastNotificationError.ShouldBeNull();
    }

    [Fact]
    public void RecordNotificationResult_ShouldNotChangeStatusNorReminderCounters()
    {
        // Arrange
        var invoice = new Invoice("client_123", 100m);
        invoice.UpdateStatus(InvoiceStatus.SegundoRecordatorio);

        // Act
        invoice.RecordNotificationResult(NotificationType.Reminder, NotificationOutcome.Failed, DateTime.UtcNow, "x");

        // Assert — el estado y los contadores de recordatorio no cambian al registrar el resultado
        invoice.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
        invoice.RemindersCount.ShouldBe(0);
        invoice.LastReminderSentAt.ShouldBeNull();
    }
}
