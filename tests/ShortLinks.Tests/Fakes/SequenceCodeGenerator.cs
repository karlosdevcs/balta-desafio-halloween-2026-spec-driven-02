using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Fakes;

public sealed class SequenceCodeGenerator(params string[] codes) : ICodeGenerator
{
    private readonly Queue<string> _codes = new(codes);

    public int Calls { get; private set; }

    public string Generate()
    {
        Calls++;
        return _codes.Dequeue();
    }
}
