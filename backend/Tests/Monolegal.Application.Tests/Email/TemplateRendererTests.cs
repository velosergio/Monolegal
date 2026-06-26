using System.Collections.Generic;
using Monolegal.Domain.Email;
using Shouldly;
using Xunit;

namespace Backend.Tests.Monolegal.Application.Tests.Email;

/// <summary>
/// Pruebas del motor de plantillas <see cref="EmailTemplateRenderer"/> (spec 017): sustitución de
/// variables del catálogo, dato ausente → cadena vacía, y detección de variables no admitidas.
/// </summary>
[Trait("Category", "Application")]
public class TemplateRendererTests
{
    [Fact]
    public void Render_SustituyeVariablesAdmitidas()
    {
        var values = new Dictionary<string, string>
        {
            [EmailTemplateVariables.FacturaId] = "F-1",
            [EmailTemplateVariables.FacturaMonto] = "$250.000",
        };

        var result = EmailTemplateRenderer.Render("Factura {{factura.id}} por {{ factura.monto }}", values);

        result.ShouldBe("Factura F-1 por $250.000");
    }

    [Fact]
    public void Render_VariableAdmitidaSinDato_SeSustituyePorVacio()
    {
        var result = EmailTemplateRenderer.Render("Hola {{cliente.nombre}}!", new Dictionary<string, string>());

        result.ShouldBe("Hola !");
    }

    [Fact]
    public void Render_VariableNoAdmitida_SeDejaTalCual()
    {
        var result = EmailTemplateRenderer.Render("Valor {{desconocido}}", new Dictionary<string, string>());

        result.ShouldBe("Valor {{desconocido}}");
    }

    [Fact]
    public void FindInvalidVariables_DetectaSoloLasNoAdmitidas()
    {
        var invalid = EmailTemplateRenderer.FindInvalidVariables("{{factura.id}} y {{foo}} y {{bar.baz}}");

        invalid.ShouldContain("foo");
        invalid.ShouldContain("bar.baz");
        invalid.ShouldNotContain(EmailTemplateVariables.FacturaId);
    }

    [Fact]
    public void FindInvalidVariables_TodasAdmitidas_DevuelveVacio()
    {
        var invalid = EmailTemplateRenderer.FindInvalidVariables("{{factura.id}} {{cliente.email}} {{enlacePago}}");

        invalid.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractVariables_DevuelveNombresUnicos()
    {
        var found = EmailTemplateRenderer.ExtractVariables("{{factura.id}} {{factura.id}} {{cliente.email}}");

        found.Count.ShouldBe(2);
    }
}
