namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Oznaka linije u retku tablice.</summary>
public record RouteTagResponse(Guid Id, string Code, string? ShortName, string? Color, string? TextColor);
