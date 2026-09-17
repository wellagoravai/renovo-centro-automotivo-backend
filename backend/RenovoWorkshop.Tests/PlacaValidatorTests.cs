using RenovoWorkshop.Domain.Validation;

namespace RenovoWorkshop.Tests;

public class PlacaValidatorTests
{
    [Theory]
    [InlineData("ABC1234")]
    [InlineData("abc1234")]
    [InlineData("ABC-1234")]
    [InlineData(" ABC1234 ")]
    public void IsValid_ShouldAcceptPadraoAntigo(string placa)
    {
        Assert.True(PlacaValidator.IsValid(placa));
    }

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("abc1d23")]
    [InlineData("ABC-1D23")]
    public void IsValid_ShouldAcceptMercosul(string placa)
    {
        Assert.True(PlacaValidator.IsValid(placa));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ABC123")]
    [InlineData("ABCD1234")]
    [InlineData("ABC12345")]
    [InlineData("1234ABC")]
    [InlineData("AB1C234")]
    [InlineData("ABC1D2E")]
    [InlineData("PLACA-INVALIDA")]
    public void IsValid_ShouldRejectInvalidFormats(string? placa)
    {
        Assert.False(PlacaValidator.IsValid(placa));
    }

    [Fact]
    public void Normalize_ShouldUppercaseAndStripSeparators()
    {
        Assert.Equal("ABC1234", PlacaValidator.Normalize(" abc-1234 "));
        Assert.Equal("ABC1D23", PlacaValidator.Normalize("abc 1d23"));
    }
}
