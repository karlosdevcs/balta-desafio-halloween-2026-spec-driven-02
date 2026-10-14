using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Unit;

public class RandomCodeGeneratorTests
{
    private readonly RandomCodeGenerator _generator = new();

    [Fact]
    public void Generates_seven_characters()
    {
        Assert.Equal(7, _generator.Generate().Length);
    }

    [Fact]
    public void Uses_only_uppercase_letters_and_digits_1_to_9()
    {
        var generated = string.Concat(Enumerable.Range(0, 1000).Select(_ => _generator.Generate()));

        Assert.All(generated, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789"));
        Assert.DoesNotContain('0', generated);
    }

    [Fact]
    public void Generated_code_passes_format_rule()
    {
        Assert.True(LinkCode.HasValidFormat(_generator.Generate()));
    }

    [Fact]
    public void Alphabet_has_35_symbols_without_zero()
    {
        Assert.Equal(35, LinkCode.Alphabet.Distinct().Count());
        Assert.DoesNotContain('0', LinkCode.Alphabet);
    }
}
