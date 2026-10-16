using System.ComponentModel.DataAnnotations;
using Terminus.Model.Common.Enums;

namespace Terminus.WebApi.Contracts.Fares;

/// <summary>Tijelo zahtjeva za stvaranje (POST) i potpunu izmjenu (PUT) cijene karte.</summary>
public class FareRequest
{
    /// <summary>GTFS fare_id, jedinstven.</summary>
    [Required]
    public string? Code { get; set; }

    [Required]
    public decimal? Price { get; set; }

    /// <summary>ISO 4217 oznaka valute, npr. "EUR".</summary>
    [Required]
    public string? CurrencyType { get; set; }

    public FarePaymentMethod PaymentMethod { get; set; } = FarePaymentMethod.OnBoard;

    /// <summary>Broj dopuštenih presjedanja (0, 1, 2); izostavljeno znači neograničeno.</summary>
    public FareTransfers? Transfers { get; set; }

    /// <summary>Obavezno ako instanca ima više prijevoznika.</summary>
    public Guid? AgencyId { get; set; }

    /// <summary>Koliko sekundi karta vrijedi za presjedanja.</summary>
    public int? TransferDuration { get; set; }

    /// <summary>Kad se cijena primjenjuje. Prazan popis znači sve vožnje.</summary>
    public List<FareRuleRequest> Rules { get; set; } = [];
}

/// <summary>Pravilo cijene: sva zadana polja moraju vrijediti. Zone su vrijednosti ZoneId stanica.</summary>
public class FareRuleRequest
{
    public Guid? RouteId { get; set; }
    public string? OriginId { get; set; }
    public string? DestinationId { get; set; }
    public string? ContainsId { get; set; }
}

/// <summary>Query parametri za popis cijena.</summary>
public class FareListQuery
{
    public string? Search { get; set; }
    public Guid? AgencyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record FareResponse(
    Guid Id,
    string Code,
    decimal Price,
    string CurrencyType,
    FarePaymentMethod PaymentMethod,
    FareTransfers? Transfers,
    Guid? AgencyId,
    int? TransferDuration,
    IReadOnlyList<FareRuleResponse> Rules,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record FareRuleResponse(
    Guid? RouteId,
    string? OriginId,
    string? DestinationId,
    string? ContainsId);
