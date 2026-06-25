using System;
using System.Threading.Tasks;
using Backend.Tests.Infrastructure.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;
using Monolegal.Infrastructure.Workers;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests;

/// <summary>
/// Pruebas directas sobre el ciclo real del worker (<see cref="InvoiceTransitionsWorker.RunCycleAsync"/>),
/// ejercitando aislamiento de errores por factura, exclusión de terminales y el resumen de ciclo.
///
/// Validates: FR-003..FR-009 | US1, US3 (spec.md 012-worker-state-transitions)
/// </summary>
[Trait("Category", "Worker")]
public class InvoiceTransitionsWorkerCycleTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static InvoiceTransitionsWorker CreateWorker(
        IInvoiceRepository invoiceRepo,
        ISystemSettingsRepository settingsRepo,
        InvoiceTransitionsWorkerOptions? options = null,
        Backend.Application.Abstractions.IInvoiceTransitionNotifier? notifier = null) =>
        new(
            invoiceRepo,
            settingsRepo,
            new InvoiceTransitionService(),
            notifier ?? new FakeTransitionNotifier(),
            Options.Create(options ?? new InvoiceTransitionsWorkerOptions()),
            NullLogger<InvoiceTransitionsWorker>.Instance);

    /// <summary>Crea una factura en el estado indicado con su última transición fechada en el pasado.</summary>
    private static Invoice CreateInvoiceAtStatus(InvoiceStatus status, int daysAgo, string clientId = "client_test")
    {
        var invoice = new Invoice(clientId, 1000m);
        invoice.UpdateStatus(status);
        invoice.OverrideLastStatusTransitionAt(DateTime.UtcNow.AddDays(-daysAgo));
        return invoice;
    }

    // ── T004 [US1] — Transiciones elegibles ──────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.PrimerRecordatorio)]
    [InlineData(InvoiceStatus.PrimerRecordatorio, InvoiceStatus.SegundoRecordatorio)]
    [InlineData(InvoiceStatus.SegundoRecordatorio, InvoiceStatus.Desactivado)]
    public async Task RunCycle_FacturasElegibles_TransicionanAlSiguienteEstado(
        InvoiceStatus from, InvoiceStatus expected)
    {
        // Arrange — 5 días transcurridos con umbrales de 3 días
        var invoice = CreateInvoiceAtStatus(from, daysAgo: 5);
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3, firstDays: 3, secondDays: 3);
        var worker = CreateWorker(invoiceRepo, settingsRepo);

        // Act
        var result = await worker.RunCycleAsync();

        // Assert
        result.Evaluated.ShouldBe(1);
        result.Transitioned.ShouldBe(1);
        result.Errors.ShouldBe(0);
        (await invoiceRepo.GetByIdAsync(invoice.Id))!.Status.ShouldBe(expected);
    }

    // ── T005 [US1] — Terminales no cambian ───────────────────────────────────────

    [Theory]
    [InlineData(InvoiceStatus.Pagado)]
    [InlineData(InvoiceStatus.Desactivado)]
    public async Task RunCycle_FacturasTerminales_NoSeEvaluanNiCambian(InvoiceStatus terminal)
    {
        // Arrange
        var invoice = CreateInvoiceAtStatus(terminal, daysAgo: 100);
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository();
        var worker = CreateWorker(invoiceRepo, settingsRepo);

        // Act
        var result = await worker.RunCycleAsync();

        // Assert — los terminales no entran en GetTransitionableAsync
        result.Evaluated.ShouldBe(0);
        result.Transitioned.ShouldBe(0);
        (await invoiceRepo.GetByIdAsync(invoice.Id))!.Status.ShouldBe(terminal);
    }

    // ── T006 [US1] — Un fallo por factura no aborta el lote ──────────────────────

    [Fact]
    public async Task RunCycle_FacturaQueFalla_NoAbortaElLote()
    {
        // Arrange — 3 facturas pending vencidas; la del medio falla al persistir
        var ok1 = CreateInvoiceAtStatus(InvoiceStatus.Pending, daysAgo: 5, clientId: "client_A");
        var failing = CreateInvoiceAtStatus(InvoiceStatus.Pending, daysAgo: 5, clientId: "client_B");
        var ok2 = CreateInvoiceAtStatus(InvoiceStatus.Pending, daysAgo: 5, clientId: "client_C");

        var inner = new InMemoryInvoiceRepository();
        await inner.AddAsync(ok1);
        await inner.AddAsync(failing);
        await inner.AddAsync(ok2);

        var invoiceRepo = new ThrowingInvoiceRepository(inner, failing.Id);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3);
        var worker = CreateWorker(invoiceRepo, settingsRepo);

        // Act
        var result = await worker.RunCycleAsync();

        // Assert — el lote continúa: 3 evaluadas, 2 transicionadas, 1 error
        result.Evaluated.ShouldBe(3);
        result.Transitioned.ShouldBe(2);
        result.Errors.ShouldBe(1);
        (await inner.GetByIdAsync(ok1.Id))!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        (await inner.GetByIdAsync(ok2.Id))!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    // ── T016 [US3] — Repositorio vacío produce resumen en ceros ──────────────────

    [Fact]
    public async Task RunCycle_RepositorioVacio_ResumenEnCeros()
    {
        // Arrange
        var invoiceRepo = new InMemoryInvoiceRepository();
        var settingsRepo = new InMemorySystemSettingsRepository();
        var worker = CreateWorker(invoiceRepo, settingsRepo);

        // Act
        var result = await worker.RunCycleAsync();

        // Assert
        result.Evaluated.ShouldBe(0);
        result.Transitioned.ShouldBe(0);
        result.Errors.ShouldBe(0);
    }

    // ── T017 [US3] — El resumen separa transiciones exitosas de errores ──────────

    [Fact]
    public async Task RunCycle_ConFalloAislado_ResumenCuentaErroresYTransicionesPorSeparado()
    {
        // Arrange — 1 que transiciona bien, 1 que falla
        var ok = CreateInvoiceAtStatus(InvoiceStatus.Pending, daysAgo: 10, clientId: "client_ok");
        var failing = CreateInvoiceAtStatus(InvoiceStatus.PrimerRecordatorio, daysAgo: 10, clientId: "client_fail");

        var inner = new InMemoryInvoiceRepository();
        await inner.AddAsync(ok);
        await inner.AddAsync(failing);

        var invoiceRepo = new ThrowingInvoiceRepository(inner, failing.Id);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3, firstDays: 3);
        var worker = CreateWorker(invoiceRepo, settingsRepo);

        // Act
        var result = await worker.RunCycleAsync();

        // Assert
        result.Evaluated.ShouldBe(2);
        result.Transitioned.ShouldBe(1);
        result.Errors.ShouldBe(1);
        result.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    // ── spec 013 [US2] — Fallo de envío no revierte la transición ni cuenta como error de ciclo ──

    [Fact]
    public async Task RunCycle_EnvioFallido_MantieneTransicionYNoTocaContadores()
    {
        // Arrange — factura elegible; el proveedor de correo falla en el envío
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, daysAgo: 5, clientId: "client_mail");
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3);

        var notifier = new Backend.Application.Notifications.InvoiceTransitionNotifier(
            new ThrowingEmailService(),
            new FakeClientEmailResolver("cliente@correo.com"),
            NullLogger<Backend.Application.Notifications.InvoiceTransitionNotifier>.Instance);

        var worker = CreateWorker(invoiceRepo, settingsRepo, notifier: notifier);

        // Act
        var result = await worker.RunCycleAsync();

        // Assert — la transición se mantiene; el fallo de correo no es un error del ciclo
        result.Transitioned.ShouldBe(1);
        result.Errors.ShouldBe(0);

        var stored = await invoiceRepo.GetByIdAsync(invoice.Id);
        stored!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        stored.LastNotificationOutcome.ShouldBe(NotificationOutcome.Failed);
        stored.LastNotificationError.ShouldNotBeNullOrEmpty();
        stored.RemindersCount.ShouldBe(0);
        stored.LastReminderSentAt.ShouldBeNull();
    }
}
