using RenovoWorkshop.Api.Helpers;

namespace RenovoWorkshop.Tests;

public class DocumentValidatorTests
{
    [Theory]
    [InlineData("529.982.247-25")]          // CPF
    [InlineData("68.699.853/0001-05")]      // CNPJ numérico
    [InlineData("12.ABC.345/01DE-35")]      // CNPJ alfanumérico (exemplo da Receita)
    [InlineData("12abc34501de35")]          // minúsculas e sem pontuação
    public void IsValidCpfOrCnpj_ShouldAcceptValidDocuments(string document)
    {
        Assert.True(DocumentValidator.IsValidCpfOrCnpj(document));
    }

    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("68.699.853/0001-06")]
    [InlineData("12.ABC.345/01DE-36")]
    [InlineData("12.ABC.345/01DE-3A")]      // verificador precisa ser número
    [InlineData("529.98A.247-25")]          // CPF não tem letra
    public void IsValidCpfOrCnpj_ShouldRejectInvalidDocuments(string document)
    {
        Assert.False(DocumentValidator.IsValidCpfOrCnpj(document));
    }

    [Fact]
    public void Normalize_ShouldKeepLettersUppercaseAndDropPunctuation()
    {
        Assert.Equal("12ABC34501DE35", DocumentValidator.Normalize("12.abc.345/01de-35"));
        Assert.Equal("52998224725", DocumentValidator.Normalize("529.982.247-25"));
    }
}
