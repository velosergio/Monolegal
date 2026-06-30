using System.Threading.Tasks;
using MongoDB.Driver;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Repositories;

namespace Monolegal.Infrastructure.Repositories;

/// <summary>
/// Implementación MongoDB de <see cref="ISystemSettingsRepository"/>: persiste y recupera la
/// configuración del sistema (<c>SystemSettings</c>) como documento único.
/// </summary>
/// <remarks>
/// SOLID: DIP — implementa <see cref="ISystemSettingsRepository"/>; los consumidores dependen de la
/// abstracción, no de MongoDB. SRP — única responsabilidad: persistir la configuración del sistema.
/// </remarks>
public class MongoSystemSettingsRepository : ISystemSettingsRepository
{
    private readonly IMongoCollection<SystemSettings> _collection;
    private const string SettingsId = "singleton-settings";

    public MongoSystemSettingsRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<SystemSettings>("SystemSettings");
    }

    public async Task<SystemSettings> GetSettingsAsync()
    {
        var settings = await _collection.Find(x => x.Id == SettingsId).FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new SystemSettings { Id = SettingsId };
            await _collection.InsertOneAsync(settings);
        }

        return settings;
    }

    public async Task UpdateSettingsAsync(SystemSettings settings)
    {
        settings.Id = SettingsId; // Ensure singleton id
        await _collection.ReplaceOneAsync(
            x => x.Id == SettingsId,
            settings,
            new ReplaceOptions { IsUpsert = true }
        );
    }
}
