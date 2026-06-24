using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Backend.Tests.Dependencies;

/// <summary>
/// US1 - Smoke test de disponibilidad del driver de base de datos documental.
/// Verifica que los tipos de MongoDB.Driver resuelven y son instanciables.
/// No requiere un servidor MongoDB en ejecución (no se realiza ninguna operación de red).
/// </summary>
public class MongoDriverAvailabilityTests
{
    [Fact]
    public void MongoClient_PuedeInstanciarseYObtenerBaseDeDatos()
    {
        var client = new MongoClient("mongodb://localhost:27017");

        IMongoDatabase database = client.GetDatabase("monolegal_smoke");

        client.ShouldNotBeNull();
        database.ShouldNotBeNull();
        database.DatabaseNamespace.DatabaseName.ShouldBe("monolegal_smoke");
    }
}
