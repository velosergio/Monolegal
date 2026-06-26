using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Monolegal.Domain.Email;

/// <summary>
/// Motor de sustitución de variables <c>{{ nombre }}</c> en plantillas de correo (spec 017, D3).
/// Solo admite el catálogo cerrado de <see cref="EmailTemplateVariables"/>; los marcadores válidos
/// con dato ausente se sustituyen por cadena vacía y los no admitidos se reportan en validación.
/// </summary>
public static partial class EmailTemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_.]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    /// <summary>
    /// Renderiza <paramref name="template"/> sustituyendo cada marcador admitido por su valor en
    /// <paramref name="values"/> (ausente ⇒ cadena vacía). Los marcadores no admitidos se dejan tal cual.
    /// </summary>
    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template))
            return template ?? string.Empty;

        return PlaceholderRegex().Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            if (!EmailTemplateVariables.IsAllowed(name))
                return match.Value;
            return values.TryGetValue(name, out var value) ? value ?? string.Empty : string.Empty;
        });
    }

    /// <summary>Devuelve los nombres de variable referenciados en la plantilla (admitidos o no).</summary>
    public static IReadOnlyCollection<string> ExtractVariables(string template)
    {
        var found = new HashSet<string>();
        if (string.IsNullOrEmpty(template))
            return found;

        foreach (Match match in PlaceholderRegex().Matches(template))
            found.Add(match.Groups[1].Value);

        return found;
    }

    /// <summary>Devuelve los nombres de variable NO admitidos presentes en la plantilla (vacío ⇒ válida).</summary>
    public static IReadOnlyCollection<string> FindInvalidVariables(string template)
    {
        var invalid = new List<string>();
        foreach (var name in ExtractVariables(template))
        {
            if (!EmailTemplateVariables.IsAllowed(name))
                invalid.Add(name);
        }

        return invalid;
    }
}
