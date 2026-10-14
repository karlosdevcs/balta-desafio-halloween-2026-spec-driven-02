using System.Security.Cryptography;

namespace ShortLinks.Api.Application;

// RF-02 e D-03: 7 caracteres do alfabeto da RN-05, com gerador criptográfico.
public class RandomCodeGenerator : ICodeGenerator
{
    public string Generate() => RandomNumberGenerator.GetString(LinkCode.Alphabet, LinkCode.GeneratedLength);
}
