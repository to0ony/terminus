namespace Terminus.Common.Exceptions;

/// <summary>
/// Zahtjev je ispravan, ali podaci nisu u stanju koje operacija traži (HTTP 422),
/// npr. GTFS feed se ne može izvesti dok se ne isprave navedeni problemi.
/// </summary>
public class UnprocessableException(string message, IReadOnlyList<string> problems)
    : Exception(message)
{
    public IReadOnlyList<string> Problems { get; } = problems;
}
