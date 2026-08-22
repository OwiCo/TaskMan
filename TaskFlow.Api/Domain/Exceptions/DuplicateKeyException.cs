namespace TaskFlow.Api.Domain.Exceptions;

public sealed class DuplicateKeyException(string message, string value) : Exception(message)
{
    public string Value { get; } = value;
}
