using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Monolegal.Domain.Entities;
using Monolegal.Domain.Enums;

namespace Backend.Infrastructure.Persistence;

/// <summary>
/// Registro idempotente de mapeos BSON personalizados (spec 017). El diccionario
/// <see cref="SystemSettings.EmailTemplates"/> usa claves enum (<see cref="NotificationType"/>),
/// que MongoDB no admite como claves de documento (estas deben ser <c>string</c>). Con la
/// representación por defecto (<see cref="DictionaryRepresentation.Document"/>) la persistencia
/// lanzaba <see cref="MongoDB.Bson.BsonSerializationException"/> al guardar una plantilla, lo que
/// se manifestaba como un HTTP 500 en <c>PUT /api/settings/email/templates/{type}</c>. Se serializa
/// como arreglo de documentos <c>{ k, v }</c> para soportar claves no string.
/// </summary>
public static class MongoSerializationConfig
{
    private static readonly object Gate = new();
    private static bool _registered;

    /// <summary>Registra los mapeos una sola vez por proceso. Seguro de invocar varias veces.</summary>
    public static void Register()
    {
        if (_registered)
            return;

        lock (Gate)
        {
            if (_registered)
                return;

            // Líneas de detalle de factura (spec 018): el subtotal es una propiedad calculada sin
            // setter, por lo que no debe (de)serializarse; el constructor reconstruye la línea.
            if (!BsonClassMap.IsClassMapRegistered(typeof(InvoiceItem)))
            {
                BsonClassMap.RegisterClassMap<InvoiceItem>(cm =>
                {
                    cm.AutoMap();
                    cm.MapCreator(i => new InvoiceItem(i.Description, i.Quantity, i.UnitPrice));
                    cm.UnmapProperty(c => c.Subtotal);
                    cm.SetIgnoreExtraElements(true);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(SystemSettings)))
            {
                // Serializadores explícitos de clave/valor: evita que el valor se serialice vía
                // ObjectSerializer (que en MongoDB.Driver v3 restringe los tipos permitidos y
                // rechaza EmailTemplate por defecto).
                var keySerializer = BsonSerializer.LookupSerializer<NotificationType>();
                var valueSerializer = BsonSerializer.LookupSerializer<EmailTemplate>();
                var templatesSerializer =
                    new DictionaryInterfaceImplementerSerializer<Dictionary<NotificationType, EmailTemplate>>(
                        DictionaryRepresentation.ArrayOfDocuments,
                        keySerializer,
                        valueSerializer);

                BsonClassMap.RegisterClassMap<SystemSettings>(cm =>
                {
                    cm.AutoMap();
                    cm.MapMember(c => c.EmailTemplates).SetSerializer(templatesSerializer);
                });
            }

            _registered = true;
        }
    }
}
