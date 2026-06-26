using System;
using Monolegal.Domain.Entities;
using Shouldly;
using Xunit;

namespace Monolegal.Domain.Tests.Entities;

/// <summary>Tests de la entidad Cliente (spec 018): normalización de email, validación y edición.</summary>
public class ClientTests
{
    [Fact]
    public void Constructor_NormalizesEmailAndTrimsFields()
    {
        var client = new Client("  Acme S.A.  ", "  Contacto@ACME.com ", "  300 ", "  Calle 1 ");

        client.Name.ShouldBe("Acme S.A.");
        client.Email.ShouldBe("contacto@acme.com");
        client.Phone.ShouldBe("300");
        client.Address.ShouldBe("Calle 1");
        client.Id.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Constructor_WithBlankOptionalFields_StoresNull()
    {
        var client = new Client("Acme", "a@b.com", "   ", "");
        client.Phone.ShouldBeNull();
        client.Address.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("Nombre", "")]
    public void Constructor_WithMissingRequiredFields_Throws(string name, string email)
    {
        Should.Throw<ArgumentException>(() => new Client(name, email));
    }

    [Fact]
    public void Update_ChangesFieldsAndRefreshesUpdatedAt()
    {
        var client = new Client("Acme", "a@b.com");
        var before = client.UpdatedAt;

        client.Update("Nuevo", "Nuevo@Correo.com", "555", "Dir");

        client.Name.ShouldBe("Nuevo");
        client.Email.ShouldBe("nuevo@correo.com");
        client.Phone.ShouldBe("555");
        client.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void CreateForSeed_UsesExplicitId()
    {
        var client = Client.CreateForSeed("seed-cliente-a", "Cliente A", "a@b.com");
        client.Id.ShouldBe("seed-cliente-a");
    }
}
