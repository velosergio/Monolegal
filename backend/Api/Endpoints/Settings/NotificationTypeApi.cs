using System;
using Monolegal.Domain.Enums;

namespace Monolegal.Api.Endpoints.Settings;

/// <summary>
/// Parseo del segmento de ruta <c>{type}</c> de las plantillas de email (spec 017) a
/// <see cref="NotificationType"/>, de forma insensible a mayúsculas. Acepta tanto la forma en
/// minúscula del contrato HTTP (<c>reminder</c>) como el nombre del enum.
/// </summary>
public static class NotificationTypeApi
{
    public static bool TryParse(string? value, out NotificationType type)
    {
        type = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Enum.TryParse(value, ignoreCase: true, out type)
            && Enum.IsDefined(type);
    }
}
