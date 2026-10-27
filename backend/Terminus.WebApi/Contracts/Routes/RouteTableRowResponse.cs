namespace Terminus.WebApi.Contracts.Routes;

/// <summary>Redak tablice linija.</summary>
/// <param name="ValidFrom">Najraniji početak kalendara po kojima voze vožnje linije; null bez vožnji.</param>
/// <param name="ValidTo">Najkasniji kraj tih kalendara; null bez vožnji.</param>
public record RouteTableRowResponse(
    RouteResponse Route,
    string AgencyName,
    int VariantCount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo);
