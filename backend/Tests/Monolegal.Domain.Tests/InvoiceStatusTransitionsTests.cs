using System;
using Shouldly;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Services;

namespace Monolegal.Domain.Tests;

/// <summary>
/// Unit tests para las reglas de transición de estado de dominio de facturas.
/// Cubre <see cref="InvoiceTransitionService"/> — lógica de evaluación temporal
/// basada en <see cref="InvoiceTransitionsConfig"/> (US1) y pago manual (US2).
///
/// Validates: FR-001, FR-002, FR-003, FR-004, FR-005 (spec.md)
/// </summary>
public class InvoiceStatusTransitionsTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una factura y luego le aplica manualmente el estado <paramref name="status"/>
    /// y la fecha de transición <paramref name="transitionAt"/>, simulando que ya fue
    /// procesada previamente.
    /// </summary>
    private static Invoice CreateInvoiceAtStatus(InvoiceStatus status, DateTime transitionAt)
    {
        var invoice = new Invoice("client_test", 1000m);
        // Forzamos el estado inicial directamente para aislar la prueba del constructor.
        invoice.UpdateStatus(status);
        // Sobreescribimos LastStatusTransitionAt a través de una segunda llamada
        // retroactiva (simulamos que la transición ocurrió en el pasado).
        // Como UpdateStatus siempre pone UtcNow, usamos el helper privado
        // OverrideLastTransitionAt que sólo está disponible en tests vía
        // reflexión mínima, o bien creamos la factura con el estado directamente.
        // Usamos la factura ya en el estado correcto y ajustamos la fecha con
        // la API interna expuesta para tests.
        invoice.OverrideLastStatusTransitionAt(transitionAt);
        return invoice;
    }

    private static InvoiceTransitionsConfig DefaultConfig(
        int pendingDays = 3,
        int firstDays = 3,
        int secondDays = 3) =>
        new()
        {
            PendingToFirstReminderDays = pendingDays,
            FirstToSecondReminderDays = firstDays,
            SecondToDeactivatedDays = secondDays
        };

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – Transición Pending → PrimerRecordatorio (FR-001)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_PendingWithSufficientDays_TransitionsToPrimerRecordatorio()
    {
        // Arrange
        var config = DefaultConfig(pendingDays: 3);
        var transitionAt = DateTime.UtcNow.AddDays(-4); // 4 días atrás > 3 días configurados
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert
        transitioned.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    [Fact]
    public void TryApplyTransition_PendingWithExactDays_Transitions()
    {
        // Arrange – exactamente en el límite (días exactos = transición)
        var config = DefaultConfig(pendingDays: 3);
        var now = DateTime.UtcNow;
        var transitionAt = now.AddDays(-3);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, now);

        // Assert
        transitioned.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    [Fact]
    public void TryApplyTransition_PendingWithInsufficientDays_DoesNotTransition()
    {
        // Arrange – sólo 1 día ha pasado, se requieren 3
        var config = DefaultConfig(pendingDays: 3);
        var transitionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert
        transitioned.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Pending);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – Transición PrimerRecordatorio → SegundoRecordatorio (FR-002)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_PrimerRecordatorioWithSufficientDays_TransitionsToSegundoRecordatorio()
    {
        // Arrange
        var config = DefaultConfig(firstDays: 5);
        var transitionAt = DateTime.UtcNow.AddDays(-6);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.PrimerRecordatorio, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert
        transitioned.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
    }

    [Fact]
    public void TryApplyTransition_PrimerRecordatorioWithInsufficientDays_DoesNotTransition()
    {
        // Arrange
        var config = DefaultConfig(firstDays: 5);
        var transitionAt = DateTime.UtcNow.AddDays(-2);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.PrimerRecordatorio, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert
        transitioned.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – Transición SegundoRecordatorio → Desactivado (FR-003)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_SegundoRecordatorioWithSufficientDays_TransitionsToDesactivado()
    {
        // Arrange
        var config = DefaultConfig(secondDays: 7);
        var transitionAt = DateTime.UtcNow.AddDays(-8);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.SegundoRecordatorio, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert
        transitioned.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Desactivado);
    }

    [Fact]
    public void TryApplyTransition_SegundoRecordatorioWithInsufficientDays_DoesNotTransition()
    {
        // Arrange
        var config = DefaultConfig(secondDays: 7);
        var transitionAt = DateTime.UtcNow.AddDays(-3);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.SegundoRecordatorio, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert
        transitioned.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – Terminales: Desactivado y Pagado no transicionan (FR-005)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_Desactivado_ReturnsFalseAndStatusUnchanged()
    {
        var config = DefaultConfig();
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Desactivado, DateTime.UtcNow.AddDays(-100));
        var service = new InvoiceTransitionService();

        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        transitioned.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Desactivado);
    }

    [Fact]
    public void TryApplyTransition_Pagado_ReturnsFalseAndStatusUnchanged()
    {
        var config = DefaultConfig();
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pagado, DateTime.UtcNow.AddDays(-100));
        var service = new InvoiceTransitionService();

        var transitioned = service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        transitioned.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – Respeta la configuración de días (configurable por administrador)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public void TryApplyTransition_PendingRespectsConfiguredDays(int configuredDays)
    {
        // Arrange: exactamente configuredDays han transcurrido
        var config = DefaultConfig(pendingDays: configuredDays);
        var now = new DateTime(2025, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        var transitionAt = now.AddDays(-configuredDays);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, now);

        // Assert
        transitioned.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public void TryApplyTransition_PendingBelowConfiguredDays_DoesNotTransition(int configuredDays)
    {
        // Arrange: un día menos del configurado → no debe transicionar
        var config = DefaultConfig(pendingDays: configuredDays);
        var now = new DateTime(2025, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        var transitionAt = now.AddDays(-(configuredDays - 1)).AddHours(-23); // aún no llega
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var service = new InvoiceTransitionService();

        // Act
        var transitioned = service.TryApplyTransition(invoice, config, now);

        // Assert
        transitioned.ShouldBeFalse();
        invoice.Status.ShouldBe(InvoiceStatus.Pending);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge case: No se puede saltar estados (FR-005)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_Pending_CannotJumpDirectlyToSegundoRecordatorio()
    {
        // Una factura Pending, aunque hayan pasado muchos días, solo avanza UN estado
        var config = DefaultConfig(pendingDays: 1, firstDays: 1, secondDays: 1);
        var transitionAt = DateTime.UtcNow.AddDays(-100);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var service = new InvoiceTransitionService();

        service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Debe estar en PrimerRecordatorio, no en SegundoRecordatorio ni más allá
        invoice.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US2 – Transición a Pagado desde cualquier estado activo (FR-004)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.Desactivado)]
    public void ApplyPayment_FromAnyActiveStatus_TransitionsToPagado(InvoiceStatus initialStatus)
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(initialStatus, DateTime.UtcNow.AddDays(-1));
        var service = new InvoiceTransitionService();

        // Act
        service.ApplyPayment(invoice);

        // Assert
        invoice.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    [Fact]
    public void ApplyPayment_AlreadyPagado_ThrowsInvalidOperationException()
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pagado, DateTime.UtcNow.AddDays(-1));
        var service = new InvoiceTransitionService();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => service.ApplyPayment(invoice));
        invoice.Status.ShouldBe(InvoiceStatus.Pagado); // estado sin cambios
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Guard clauses
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_NullInvoice_ThrowsArgumentNullException()
    {
        var service = new InvoiceTransitionService();
        Should.Throw<ArgumentNullException>(
            () => service.TryApplyTransition(null!, DefaultConfig(), DateTime.UtcNow));
    }

    [Fact]
    public void TryApplyTransition_NullConfig_ThrowsArgumentNullException()
    {
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, DateTime.UtcNow.AddDays(-5));
        var service = new InvoiceTransitionService();
        Should.Throw<ArgumentNullException>(
            () => service.TryApplyTransition(invoice, null!, DateTime.UtcNow));
    }

    [Fact]
    public void ApplyPayment_NullInvoice_ThrowsArgumentNullException()
    {
        var service = new InvoiceTransitionService();
        Should.Throw<ArgumentNullException>(() => service.ApplyPayment(null!));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – LastStatusTransitionAt se actualiza tras una transición exitosa
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_OnSuccess_UpdatesLastStatusTransitionAt()
    {
        // Arrange
        var config = DefaultConfig(pendingDays: 3);
        var oldTransitionAt = DateTime.UtcNow.AddDays(-5);
        var now = DateTime.UtcNow;
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, oldTransitionAt);
        var service = new InvoiceTransitionService();

        // Act
        service.TryApplyTransition(invoice, config, now);

        // Assert
        invoice.LastStatusTransitionAt.ShouldBeGreaterThan(oldTransitionAt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // US1 – LastStatusTransitionAt no cambia si no hubo transición
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryApplyTransition_OnNoTransition_DoesNotChangeLastStatusTransitionAt()
    {
        // Arrange – sólo han pasado 1 de los 5 días requeridos
        var config = DefaultConfig(pendingDays: 5);
        var oldTransitionAt = DateTime.UtcNow.AddDays(-1);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, oldTransitionAt);
        var service = new InvoiceTransitionService();

        // Act
        service.TryApplyTransition(invoice, config, DateTime.UtcNow);

        // Assert – sin transición, LastStatusTransitionAt debe ser igual
        invoice.LastStatusTransitionAt.ShouldBe(oldTransitionAt, TimeSpan.FromMilliseconds(100));
    }
}
