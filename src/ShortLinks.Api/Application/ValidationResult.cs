using System.Diagnostics.CodeAnalysis;

namespace ShortLinks.Api.Application;

public sealed class ValidationResult<T>
{
    private readonly T? _value;

    private ValidationResult(T? value, string? error)
    {
        _value = value;
        Error = error;
    }

    public string? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsValid => Error is null;

    public T Value => IsValid
        ? _value!
        : throw new InvalidOperationException($"Um resultado inválido não tem valor: {Error}");

    public static ValidationResult<T> Success(T value) => new(value, error: null);

    public static ValidationResult<T> Failure(string error) => new(default, error);
}
