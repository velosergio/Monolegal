using Backend.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;
using Shouldly;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Regresión (spec 017): la persistencia de plantillas de email fallaba con HTTP 500 porque el
/// diccionario <see cref="SystemSettings.EmailTemplates"/> usa claves enum
/// (<see cref="NotificationType"/>), no representables como claves de documento BSON. Verifica que,
/// con la configuración de serialización registrada, <see cref="SystemSettings"/> serializa y
/// deserializa correctamente con plantillas personalizadas (lo que ejercita exactamente lo que hace
/// <c>ReplaceOneAsync</c> en <c>PUT /api/settings/email/templates/{type}</c>).
/// </summary>
[Trait("Category", "Application")]
public sealed class SystemSettingsSerializationTests
{
    [Fact]
    public void EmailTemplates_ConClaveEnum_SerializaYDeserializaSinError()
    {
        MongoSerializationConfig.Register();

        var settings = new SystemSettings { Id = "singleton-settings" };
        settings.UpdateTemplate(NotificationType.Reminder, "Recordatorio {{factura.id}}", "Hola {{cliente.nombre}}");
        settings.UpdateTemplate(NotificationType.PaymentConfirmation, "Pago recibido", "Gracias por su pago.");

        // Antes del fix, ToBsonDocument() lanzaba BsonSerializationException (claves no string).
        var bson = settings.ToBsonDocument();
        var roundTrip = BsonSerializer.Deserialize<SystemSettings>(bson);

        roundTrip.EmailTemplates.Count.ShouldBe(2);
        roundTrip.EmailTemplates[NotificationType.Reminder].Subject.ShouldBe("Recordatorio {{factura.id}}");
        roundTrip.EmailTemplates[NotificationType.Reminder].Body.ShouldBe("Hola {{cliente.nombre}}");
        roundTrip.EmailTemplates[NotificationType.PaymentConfirmation].Subject.ShouldBe("Pago recibido");
        roundTrip.EmailTemplates[NotificationType.PaymentConfirmation].Body.ShouldBe("Gracias por su pago.");
    }

    [Fact]
    public void EmailTemplates_Vacio_SerializaYDeserializaSinError()
    {
        MongoSerializationConfig.Register();

        var settings = new SystemSettings { Id = "singleton-settings" };

        var bson = settings.ToBsonDocument();
        var roundTrip = BsonSerializer.Deserialize<SystemSettings>(bson);

        roundTrip.EmailTemplates.ShouldBeEmpty();
    }
}
