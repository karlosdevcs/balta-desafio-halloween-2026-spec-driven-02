using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Unit;

public class CustomCodeValidatorTests
{
    [Theory]
    [InlineData("ABCD")]
    [InlineData("HALLOWEEN26")]
    [InlineData("ABCDEFGHIJ123456789Z")]
    public void Accepts_valid_codes(string code)
    {
        var result = CustomCodeValidator.Validate(code);

        Assert.True(result.IsValid);
        Assert.Equal(code, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJ123456789ZX")]
    public void Rejects_codes_outside_4_to_20_characters(string code)
    {
        Assert.Equal("O código deve ter entre 4 e 20 caracteres.", CustomCodeValidator.Validate(code).Error);
    }

    [Theory]
    [InlineData("ABC0")]
    [InlineData("abcd")]
    [InlineData("ABcD")]
    [InlineData("AB CD")]
    [InlineData("AB-CD")]
    [InlineData("ÁBCD")]
    public void Rejects_characters_outside_alphabet(string code)
    {
        Assert.Equal(
            "O código deve conter apenas letras maiúsculas de A a Z e números de 1 a 9, sem espaços.",
            CustomCodeValidator.Validate(code).Error);
    }
}
