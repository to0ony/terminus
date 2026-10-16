using Terminus.Model.Common.Enums;

namespace Terminus.Model.Common;

/// <summary>Cijena karte (GTFS fare_attributes.txt) s pravilima primjene (fare_rules.txt).</summary>
public interface IFareModel
{
    Guid Id { get; set; }

    /// <summary>GTFS fare_id, jedinstven.</summary>
    string Code { get; set; }

    decimal Price { get; set; }

    /// <summary>ISO 4217 oznaka valute, npr. "EUR".</summary>
    string CurrencyType { get; set; }

    FarePaymentMethod PaymentMethod { get; set; }

    /// <summary>Broj dopuštenih presjedanja; null = neograničeno.</summary>
    FareTransfers? Transfers { get; set; }

    /// <summary>Prijevoznik na koji se cijena odnosi; obavezan ako feed ima više prijevoznika.</summary>
    Guid? AgencyId { get; set; }

    /// <summary>Koliko sekundi karta vrijedi za presjedanja.</summary>
    int? TransferDuration { get; set; }

    /// <summary>Kad se cijena primjenjuje. Bez pravila vrijedi za sve vožnje.</summary>
    IReadOnlyList<IFareRuleModel> Rules { get; set; }

    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Jedno pravilo (GTFS fare_rules.txt). Zadana polja moraju sva vrijediti; zone
/// su vrijednosti <see cref="IStopModel.ZoneId"/>.
/// </summary>
public interface IFareRuleModel
{
    Guid? RouteId { get; set; }
    string? OriginId { get; set; }
    string? DestinationId { get; set; }
    string? ContainsId { get; set; }
}
