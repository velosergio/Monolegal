using System.Threading.Tasks;
using Monolegal.Domain.Entities;

namespace Monolegal.Domain.Repositories;

public interface ISystemSettingsRepository
{
    Task<SystemSettings> GetSettingsAsync();
    Task UpdateSettingsAsync(SystemSettings settings);
}
