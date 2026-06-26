using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Monolegal.Domain.Repositories;
using Monolegal.Domain.Services;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests;

/// <summary>
/// Integration tests para el handler de trigger manual de transiciones de facturas.
/// Ejercitan la capa de aplicación end-to-end (InvoiceTransitionService + repositorios)
/// sin depender de MongoDB ni de la capa HTTP, modelando exactamente el flujo que
/// ejecutará el endpoint POST /api/workers/trigger-transitions (T014).
///
/// Validates: FR-001, FR-002, FR-003, FR-005 | US1 (spec.md)
/// </summary>
[Trait("Category", "Application")]
public class InvoiceWorkerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // In-memory fakes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Repositorio de facturas en memoria para pruebas de integración.
    /// Implementa IInvoiceRepository sin persistencia real.
    /// </summary>
    private sealed class InMemoryInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<string, Invoice> _store = new();

        public IReadOnlyCollection<Invoice> All => _store.Values.ToList().AsReadOnly();

        public Task<Invoice?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

        public Task<IEnumerable<Invoice>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Values.Where(i => i.ClientId == clientId));

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            _store[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            _store[invoice.Id] = invoice;
            return Task.CompletedTask;
        }

        /// <summary>Retorna todas las facturas activas (estados que admiten transición automática).</summary>
        public IEnumerable<Invoice> GetTransitionable() =>
            _store.Values.Where(i =>
                i.Status is InvoiceStatus.Pending
                    or InvoiceStatus.PrimerRecordatorio
                    or InvoiceStatus.SegundoRecordatorio);

        public Task<IEnumerable<Invoice>> GetTransitionableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetTransitionable());

        public Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Values.Where(i => i.Status == status));

        public Task<IEnumerable<Invoice>> GetByNotificationOutcomeAsync(NotificationOutcome outcome, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Values.Where(i => i.LastNotificationOutcome == outcome));

        public Task<long> CountAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((long)_store.Count);

        public Task<long> DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            var removed = (long)_store.Count;
            _store.Clear();
            return Task.FromResult(removed);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(_store.Remove(id));

        public Task<long> CountByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
            => Task.FromResult((long)_store.Values.Count(i => i.ClientId == clientId));

        public Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
            InvoiceStatus? status, string? clientSearch, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = status.HasValue ? _store.Values.Where(i => i.Status == status.Value) : _store.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(clientSearch))
                query = query.Where(i => i.ClientId.Contains(clientSearch.Trim(), System.StringComparison.OrdinalIgnoreCase));
            var filtered = query.ToList();
            var items = filtered
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return Task.FromResult(((IReadOnlyList<Invoice>)items, (long)filtered.Count));
        }

        public Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyDictionary<InvoiceStatus, long>)_store.Values
                .GroupBy(i => i.Status).ToDictionary(g => g.Key, g => (long)g.Count()));

        public Task<IReadOnlyDictionary<string, long>> CountByClientAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyDictionary<string, long>)_store.Values
                .GroupBy(i => i.ClientId).ToDictionary(g => g.Key, g => (long)g.Count()));
    }    /// <summary>
         /// Repositorio de configuración en memoria con valores predeterminados.
         /// </summary>
    private sealed class InMemorySystemSettingsRepository : ISystemSettingsRepository
    {
        private SystemSettings _settings;

        public InMemorySystemSettingsRepository(
            int pendingDays = 3,
            int firstDays = 3,
            int secondDays = 3)
        {
            _settings = new SystemSettings
            {
                Id = "settings",
                InvoiceTransitions = new InvoiceTransitionsConfig
                {
                    PendingToFirstReminderDays = pendingDays,
                    FirstToSecondReminderDays = firstDays,
                    SecondToDeactivatedDays = secondDays
                }
            };
        }

        public Task<SystemSettings> GetSettingsAsync() => Task.FromResult(_settings);

        public Task UpdateSettingsAsync(SystemSettings settings)
        {
            _settings = settings;
            return Task.CompletedTask;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Handler (reproduce la lógica que tendrá el endpoint T014)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simula el handler del endpoint POST /api/workers/trigger-transitions.
    /// Carga la configuración, evalúa todas las facturas transicionables y
    /// persiste los cambios. Devuelve un resumen con la cantidad de
    /// facturas procesadas y las que efectivamente cambiaron de estado.
    /// </summary>
    private static async Task<TriggerResult> TriggerTransitionsAsync(
        InMemoryInvoiceRepository invoiceRepo,
        ISystemSettingsRepository settingsRepo,
        DateTime? now = null)
    {
        var currentNow = now ?? DateTime.UtcNow;
        var settings = await settingsRepo.GetSettingsAsync();
        var config = settings.InvoiceTransitions;
        var service = new InvoiceTransitionService();

        var candidates = invoiceRepo.GetTransitionable().ToList();
        int transitioned = 0;

        foreach (var invoice in candidates)
        {
            if (service.TryApplyTransition(invoice, config, currentNow))
            {
                await invoiceRepo.UpdateAsync(invoice);
                transitioned++;
            }
        }

        return new TriggerResult(candidates.Count, transitioned);
    }

    private record TriggerResult(int Evaluated, int Transitioned);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una factura con el estado e instante de última transición especificados.
    /// </summary>
    private static Invoice CreateInvoiceAtStatus(InvoiceStatus status, DateTime transitionAt)
    {
        var invoice = new Invoice("client_test", 1000m);
        invoice.UpdateStatus(status);
        invoice.OverrideLastStatusTransitionAt(transitionAt);
        return invoice;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: trigger con facturas vencidas
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_PendingInvoiceOverdue_TransitionsToPrimerRecordatorio()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, now.AddDays(-4)); // 4 días > 3 configurados
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(1);
        var updated = await invoiceRepo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    [Fact]
    public async Task TriggerTransitions_PrimerRecordatorioInvoiceOverdue_TransitionsToSegundoRecordatorio()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.PrimerRecordatorio, now.AddDays(-6));
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(firstDays: 5);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(1);
        var updated = await invoiceRepo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
    }

    [Fact]
    public async Task TriggerTransitions_SegundoRecordatorioInvoiceOverdue_TransitionsToDesactivado()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.SegundoRecordatorio, now.AddDays(-8));
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(secondDays: 7);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(1);
        var updated = await invoiceRepo.GetByIdAsync(invoice.Id);
        updated!.Status.ShouldBe(InvoiceStatus.Desactivado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: trigger con facturas aún no vencidas (no deben cambiar)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_InvoiceNotYetOverdue_StatusUnchanged()
    {
        // Arrange – solo 1 día ha pasado, se necesitan 3
        var now = DateTime.UtcNow;
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, now.AddDays(-1));
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(0);
        var notUpdated = await invoiceRepo.GetByIdAsync(invoice.Id);
        notUpdated!.Status.ShouldBe(InvoiceStatus.Pending);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: trigger con múltiples facturas en distintos estados
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_MultipleMixedInvoices_OnlyOverdueOnesTransition()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Overdue → debe transicionar
        var overdue = CreateInvoiceAtStatus(InvoiceStatus.Pending, now.AddDays(-5));
        // No vencida → no debe transicionar
        var notOverdue = CreateInvoiceAtStatus(InvoiceStatus.Pending, now.AddDays(-1));
        // Ya en segundo recordatorio y vencida → debe transicionar
        var secondOverdue = CreateInvoiceAtStatus(InvoiceStatus.SegundoRecordatorio, now.AddDays(-10));

        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(overdue);
        await invoiceRepo.AddAsync(notOverdue);
        await invoiceRepo.AddAsync(secondOverdue);

        var settingsRepo = new InMemorySystemSettingsRepository(
            pendingDays: 3,
            firstDays: 3,
            secondDays: 7);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Evaluated.ShouldBe(3);
        result.Transitioned.ShouldBe(2);

        (await invoiceRepo.GetByIdAsync(overdue.Id))!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        (await invoiceRepo.GetByIdAsync(notOverdue.Id))!.Status.ShouldBe(InvoiceStatus.Pending);
        (await invoiceRepo.GetByIdAsync(secondOverdue.Id))!.Status.ShouldBe(InvoiceStatus.Desactivado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: facturas terminales no se incluyen en la evaluación
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_PagadoInvoice_IsNotEvaluatedAndStatusUnchanged()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var pagada = CreateInvoiceAtStatus(InvoiceStatus.Pagado, now.AddDays(-100));
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(pagada);
        var settingsRepo = new InMemorySystemSettingsRepository();

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert – pagada no está en los candidatos a transicionar
        result.Evaluated.ShouldBe(0);
        result.Transitioned.ShouldBe(0);
        (await invoiceRepo.GetByIdAsync(pagada.Id))!.Status.ShouldBe(InvoiceStatus.Pagado);
    }

    [Fact]
    public async Task TriggerTransitions_DesactivadoInvoice_IsNotEvaluatedAndStatusUnchanged()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var desactivada = CreateInvoiceAtStatus(InvoiceStatus.Desactivado, now.AddDays(-100));
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(desactivada);
        var settingsRepo = new InMemorySystemSettingsRepository();

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Evaluated.ShouldBe(0);
        result.Transitioned.ShouldBe(0);
        (await invoiceRepo.GetByIdAsync(desactivada.Id))!.Status.ShouldBe(InvoiceStatus.Desactivado);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: la configuración de días es respetada por el handler
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public async Task TriggerTransitions_RespectsConfiguredDaysFromSettings(int configuredDays)
    {
        // Arrange – exactamente configuredDays han pasado
        var now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, now.AddDays(-configuredDays));
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: configuredDays);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(1);
        (await invoiceRepo.GetByIdAsync(invoice.Id))!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(30)]
    public async Task TriggerTransitions_BelowConfiguredDays_DoesNotTransition(int configuredDays)
    {
        // Arrange – un día menos del configurado (hora exacta para evitar falsos positivos)
        var now = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var transitionAt = now.AddDays(-(configuredDays - 1)).AddHours(-23);
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, transitionAt);
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: configuredDays);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(0);
        (await invoiceRepo.GetByIdAsync(invoice.Id))!.Status.ShouldBe(InvoiceStatus.Pending);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: repositorio es persistido después de la transición
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_AfterTransition_LastStatusTransitionAtIsUpdated()
    {
        // Arrange
        var oldTransitionAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc); // 9 días después
        var invoice = CreateInvoiceAtStatus(InvoiceStatus.Pending, oldTransitionAt);
        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoice);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3);

        // Act
        await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert – la fecha de transición debería haberse actualizado
        var updated = await invoiceRepo.GetByIdAsync(invoice.Id);
        updated!.LastStatusTransitionAt.ShouldBeGreaterThan(oldTransitionAt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: sin facturas en el repositorio
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_EmptyRepository_ReturnsZeroProcessed()
    {
        // Arrange
        var invoiceRepo = new InMemoryInvoiceRepository();
        var settingsRepo = new InMemorySystemSettingsRepository();

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo);

        // Assert
        result.Evaluated.ShouldBe(0);
        result.Transitioned.ShouldBe(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests: el trigger procesa facturas de múltiples clientes
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TriggerTransitions_MultipleClients_AllOverdueInvoicesTransition()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var invoiceA = new Invoice("client_A", 500m);
        invoiceA.UpdateStatus(InvoiceStatus.Pending);
        invoiceA.OverrideLastStatusTransitionAt(now.AddDays(-5));

        var invoiceB = new Invoice("client_B", 750m);
        invoiceB.UpdateStatus(InvoiceStatus.PrimerRecordatorio);
        invoiceB.OverrideLastStatusTransitionAt(now.AddDays(-7));

        var invoiceRepo = new InMemoryInvoiceRepository();
        await invoiceRepo.AddAsync(invoiceA);
        await invoiceRepo.AddAsync(invoiceB);
        var settingsRepo = new InMemorySystemSettingsRepository(pendingDays: 3, firstDays: 5);

        // Act
        var result = await TriggerTransitionsAsync(invoiceRepo, settingsRepo, now);

        // Assert
        result.Transitioned.ShouldBe(2);
        (await invoiceRepo.GetByIdAsync(invoiceA.Id))!.Status.ShouldBe(InvoiceStatus.PrimerRecordatorio);
        (await invoiceRepo.GetByIdAsync(invoiceB.Id))!.Status.ShouldBe(InvoiceStatus.SegundoRecordatorio);
    }
}
