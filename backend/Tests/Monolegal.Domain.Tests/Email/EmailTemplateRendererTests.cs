using System.Collections.Generic;
using System.Linq;
using Monolegal.Domain.Email;
using Shouldly;
using Xunit;

namespace Monolegal.Domain.Tests.Email;

/// <summary>
/// Unit tests del motor de sustitución de variables <c>{{ nombre }}</c>
/// (<see cref="EmailTemplateRenderer"/>, spec 017, D3). Cubre el inventario C9 de la spec 020.
/// </summary>
[Trait("Category", "Domain")]
public class EmailTemplateRendererTests
{
    private static IReadOnlyDictionary<string, string> Values(params (string Key, string Value)[] pairs)
        => pairs.ToDictionary(p => p.Key, p => p.Value);

    // ── C9.1: marcador admitido con valor presente → se sustituye ──
    [Fact]
    public void Render_AllowedPlaceholderWithValue_SubstitutesValue()
    {
        var values = Values(("cliente.nombre", "Ada Lovelace"));

        var result = EmailTemplateRenderer.Render("Hola {{ cliente.nombre }}", values);

        result.ShouldBe("Hola Ada Lovelace");
    }

    // ── C9.2: marcador admitido sin valor en el diccionario → cadena vacía ──
    [Fact]
    public void Render_AllowedPlaceholderWithoutValue_SubstitutesEmptyString()
    {
        var result = EmailTemplateRenderer.Render("Total: {{ factura.monto }}.", Values());

        result.ShouldBe("Total: .");
    }

    // ── Marcador admitido con la clave presente pero valor null → cadena vacía ──
    [Fact]
    public void Render_AllowedPlaceholderWithNullValue_SubstitutesEmptyString()
    {
        var values = new Dictionary<string, string> { ["cliente.nombre"] = null! };

        var result = EmailTemplateRenderer.Render("Hola {{ cliente.nombre }}.", values);

        result.ShouldBe("Hola .");
    }

    // ── C9.3: marcador NO admitido → se deja intacto ──
    [Fact]
    public void Render_UnknownPlaceholder_IsLeftUnchanged()
    {
        var result = EmailTemplateRenderer.Render("Valor {{ foo.bar }} fin", Values(("foo.bar", "x")));

        result.ShouldBe("Valor {{ foo.bar }} fin");
    }

    // ── C9.4: plantilla null/vacía → cadena vacía ──
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Render_NullOrEmptyTemplate_ReturnsEmptyString(string? template)
    {
        EmailTemplateRenderer.Render(template!, Values()).ShouldBe(string.Empty);
    }

    // ── C9.5: tolerancia a espacios dentro del marcador ──
    [Fact]
    public void Render_PlaceholderWithSurroundingWhitespace_IsRecognized()
    {
        var values = Values(("factura.id", "F-001"));

        var result = EmailTemplateRenderer.Render("Ref {{   factura.id   }}", values);

        result.ShouldBe("Ref F-001");
    }

    // ── Varios marcadores admitidos en la misma plantilla ──
    [Fact]
    public void Render_MultipleAllowedPlaceholders_AllSubstituted()
    {
        var values = Values(
            ("cliente.nombre", "Grace"),
            ("factura.estado", "pagado"));

        var result = EmailTemplateRenderer.Render(
            "{{cliente.nombre}}: factura {{factura.estado}}", values);

        result.ShouldBe("Grace: factura pagado");
    }

    // ── C9.6: ExtractVariables devuelve nombres sin duplicados ──
    [Fact]
    public void ExtractVariables_WithRepeatedPlaceholders_ReturnsDistinctNames()
    {
        var found = EmailTemplateRenderer.ExtractVariables(
            "{{factura.id}} y otra vez {{ factura.id }} y {{cliente.email}}");

        found.Count.ShouldBe(2);
        found.ShouldContain("factura.id");
        found.ShouldContain("cliente.email");
    }

    [Fact]
    public void ExtractVariables_OnTemplateWithoutPlaceholders_ReturnsEmpty()
    {
        EmailTemplateRenderer.ExtractVariables("texto plano sin marcadores").ShouldBeEmpty();
        EmailTemplateRenderer.ExtractVariables(null!).ShouldBeEmpty();
    }

    // ── C9.7: FindInvalidVariables devuelve sólo los no admitidos ──
    [Fact]
    public void FindInvalidVariables_ReturnsOnlyDisallowedNames()
    {
        var invalid = EmailTemplateRenderer.FindInvalidVariables(
            "{{ cliente.nombre }} {{ foo }} {{ bar.baz }}");

        invalid.ShouldContain("foo");
        invalid.ShouldContain("bar.baz");
        invalid.ShouldNotContain("cliente.nombre");
        invalid.Count.ShouldBe(2);
    }

    // ── C9.8: plantilla 100% válida → colección vacía ──
    [Fact]
    public void FindInvalidVariables_OnFullyValidTemplate_ReturnsEmpty()
    {
        var invalid = EmailTemplateRenderer.FindInvalidVariables(
            "{{ factura.id }} para {{ cliente.nombre }} — {{ enlacePago }}");

        invalid.ShouldBeEmpty();
    }
}
