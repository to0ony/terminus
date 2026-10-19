using Terminus.Service.Common;

namespace Terminus.WebApi.Contracts.Departures;

public static class DepartureMappings
{
    public static DeparturesResponse ToResponse(this StopDepartures result) => new(
        new DepartureStopResponse(result.Stop.Code, result.Stop.Name),
        result.AgencyName,
        result.Now,
        result.Departures.Select(d => d.ToResponse(result.Now)).ToList(),
        result.Recent.Select(d => d.ToResponse(result.Now)).ToList());

    public static DepartureResponse ToResponse(this Departure departure, DateTimeOffset now) => new(
        departure.RouteShortName ?? departure.RouteLongName,
        departure.RouteColor,
        departure.RouteTextColor,
        departure.Headsign,
        departure.PlatformCode,
        departure.DepartureTime,
        (int)Math.Floor((departure.DepartureTime - now).TotalMinutes),
        departure.ServiceDate);
}
