namespace Terminus.WebApi.Contracts.Gtfs;

/// <param name="Problems">Zbog čega se feed ne bi izvezao; prazno ako je spreman.</param>
public record GtfsCheckResponse(IReadOnlyList<string> Problems);
