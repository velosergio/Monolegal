using Shouldly;
using Xunit;

namespace Backend.Tests.Dependencies;

/// <summary>
/// US5 - Smoke test del framework de pruebas.
/// Verifica que el runner de xUnit descubre y ejecuta pruebas y que las aserciones
/// legibles de Shouldly compilan y evalúan correctamente.
/// </summary>
public class TestFrameworkAvailabilityTests
{
    [Fact]
    public void Shouldly_EvaluaAserciones()
    {
        var resultado = 2 + 2;

        resultado.ShouldBe(4);
        "monolegal".ShouldStartWith("mono");
        new[] { 1, 2, 3 }.ShouldContain(2);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 3, 5)]
    public void Runner_DescubreTeoriasParametrizadas(int a, int b, int esperado)
    {
        (a + b).ShouldBe(esperado);
    }
}
