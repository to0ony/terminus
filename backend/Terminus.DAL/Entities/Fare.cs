namespace Terminus.DAL.Entities;

/// <summary>GTFS fare_attributes.</summary>
public class Fare : IAuditable
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public decimal Price { get; set; }
    public string CurrencyType { get; set; } = null!;
    public short PaymentMethod { get; set; }
    public short? Transfers { get; set; }
    public Guid? AgencyId { get; set; }
    public int? TransferDuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Agency? Agency { get; set; }
    public ICollection<FareRule> Rules { get; set; } = new List<FareRule>();
}
