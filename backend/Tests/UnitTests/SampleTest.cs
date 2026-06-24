using Xunit;

namespace Backend.Tests.UnitTests;

/// <summary>
/// Sample test to validate xUnit setup is working correctly.
/// </summary>
public class SampleTest
{
    [Fact]
    public void Truth_ShouldBeTrue()
    {
        // Arrange
        var value = true;

        // Act & Assert
        Assert.True(value);
    }

    [Fact]
    public void Addition_ShouldWork()
    {
        // Arrange
        var a = 2;
        var b = 3;

        // Act
        var result = a + b;

        // Assert
        Assert.Equal(5, result);
    }
}
