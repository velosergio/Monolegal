using System.Threading.Tasks;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Backend.Tests.Infrastructure.Support;

/// <summary>
/// Implementación en memoria de <see cref="ISystemSettingsRepository"/> compartida por los tests
/// del worker de transiciones. Permite configurar los umbrales de días por transición.
/// </summary>
public sealed class InMemorySystemSettingsRepository : ISystemSettingsRepository
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
