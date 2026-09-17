using RenovoWorkshop.Domain.Constants;

namespace RenovoWorkshop.Tests;

public class PedagioEixoMultiplicadorTests
{
    [Fact]
    public void ParaEixos_ShouldReturnDefault_WhenNull()
    {
        Assert.Equal(PedagioEixoMultiplicador.Default, PedagioEixoMultiplicador.ParaEixos(null));
    }

    [Theory]
    [InlineData(2, 1.5)]
    [InlineData(3, 2.0)]
    [InlineData(4, 3.0)]
    [InlineData(6, 3.0)]
    [InlineData(9, 3.0)]
    public void ParaEixos_ShouldReturnExpectedMultiplier(int eixos, decimal esperado)
    {
        Assert.Equal(esperado, PedagioEixoMultiplicador.ParaEixos(eixos));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ParaEixos_ShouldReturnDefault_ForUnmappedLowValues(int eixos)
    {
        Assert.Equal(PedagioEixoMultiplicador.Default, PedagioEixoMultiplicador.ParaEixos(eixos));
    }
}
