using System.Linq;
using Monolegal.Domain.Email;
using Shouldly;
using Xunit;

namespace Monolegal.Domain.Tests.Email;

/// <summary>
/// Unit tests del catálogo CERRADO de variables admitidas en plantillas de correo
/// (<see cref="EmailTemplateVariables"/>, spec 017). Cubre el inventario C8 de la spec 020.
/// </summary>
[Trait("Category", "Domain")]
public class EmailTemplateVariablesTests
{
    // ── C8.1: el catálogo expone exactamente las 9 variables esperadas, en orden ──
    [Fact]
    public void All_ContainsExactlyTheExpectedCanonicalVariables_InOrder()
    {
        var esperado = new[]
        {
            "factura.id",
            "factura.monto",
            "factura.vencimiento",
            "factura.estado",
            "factura.fechaEmision",
            "cliente.nombre",
            "cliente.email",
            "cliente.empresa",
            "enlacePago",
        };

        EmailTemplateVariables.All.ShouldBe(esperado);
    }

    // ── C8.2: un nombre del catálogo es admitido ──
    [Theory]
    [InlineData("factura.id")]
    [InlineData("cliente.nombre")]
    [InlineData("enlacePago")]
    public void IsAllowed_WithKnownVariable_ReturnsTrue(string name)
    {
        EmailTemplateVariables.IsAllowed(name).ShouldBeTrue();
    }

    // ── C8.3: un nombre fuera del catálogo NO es admitido ──
    [Theory]
    [InlineData("foo.bar")]
    [InlineData("Factura.Id")]   // distingue mayúsculas/minúsculas
    [InlineData("")]
    [InlineData("cliente.telefono")]
    public void IsAllowed_WithUnknownVariable_ReturnsFalse(string name)
    {
        EmailTemplateVariables.IsAllowed(name).ShouldBeFalse();
    }

    // ── C8.4: All y AllowedSet contienen los mismos elementos ──
    [Fact]
    public void AllAndAllowedSet_AreConsistent()
    {
        EmailTemplateVariables.AllowedSet.Count.ShouldBe(EmailTemplateVariables.All.Count);
        EmailTemplateVariables.AllowedSet.OrderBy(x => x)
            .ShouldBe(EmailTemplateVariables.All.OrderBy(x => x));
    }

    // ── Las constantes públicas coinciden con su valor en el catálogo ──
    [Fact]
    public void PublicConstants_AreAllowed()
    {
        EmailTemplateVariables.IsAllowed(EmailTemplateVariables.FacturaMonto).ShouldBeTrue();
        EmailTemplateVariables.IsAllowed(EmailTemplateVariables.ClienteEmpresa).ShouldBeTrue();
        EmailTemplateVariables.IsAllowed(EmailTemplateVariables.FacturaFechaEmision).ShouldBeTrue();
    }
}
