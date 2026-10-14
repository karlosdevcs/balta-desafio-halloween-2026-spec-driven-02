using ShortLinks.Api.Application;

namespace ShortLinks.Tests.Unit;

public class ValidationResultTests
{
    [Fact]
    public void Success_exposes_value_and_no_error()
    {
        var result = ValidationResult<int>.Success(42);

        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_exposes_error_and_refuses_value()
    {
        var result = ValidationResult<int>.Failure("Valor inválido.");

        Assert.False(result.IsValid);
        Assert.Equal("Valor inválido.", result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
