namespace Terminus.Common.Exceptions;

/// <summary>Podaci ne zadovoljavaju poslovna pravila (HTTP 400). Greške su grupirane po svojstvu.</summary>
public class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Neispravni podaci.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
