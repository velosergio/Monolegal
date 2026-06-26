using System.Collections.Generic;

namespace Monolegal.Domain.Email;

/// <summary>
/// Catálogo CERRADO de variables admitidas en las plantillas de correo (spec 017, clarificación
/// 2026-06-26). Cualquier marcador <c>{{ nombre }}</c> cuyo nombre no esté en este conjunto se
/// considera inválido en la validación de plantillas. Los marcadores válidos con dato ausente se
/// sustituyen por cadena vacía al renderizar (no fallan).
/// </summary>
public static class EmailTemplateVariables
{
    public const string FacturaId = "factura.id";
    public const string FacturaMonto = "factura.monto";
    public const string FacturaVencimiento = "factura.vencimiento";
    public const string FacturaEstado = "factura.estado";
    public const string FacturaFechaEmision = "factura.fechaEmision";
    public const string ClienteNombre = "cliente.nombre";
    public const string ClienteEmail = "cliente.email";
    public const string ClienteEmpresa = "cliente.empresa";
    public const string EnlacePago = "enlacePago";

    /// <summary>Conjunto canónico de nombres de variable admitidos (orden estable para la UI).</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        FacturaId,
        FacturaMonto,
        FacturaVencimiento,
        FacturaEstado,
        FacturaFechaEmision,
        ClienteNombre,
        ClienteEmail,
        ClienteEmpresa,
        EnlacePago,
    };

    /// <summary>Conjunto de búsqueda O(1) para validar nombres de variable.</summary>
    public static readonly IReadOnlySet<string> AllowedSet = new HashSet<string>(All);

    /// <summary>Indica si <paramref name="name"/> es una variable admitida.</summary>
    public static bool IsAllowed(string name) => AllowedSet.Contains(name);
}
