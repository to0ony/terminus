using Terminus.Model.Common;
using Terminus.Model.Common.Enums;

namespace Terminus.Model;

public class FareModel : IFareModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CurrencyType { get; set; } = string.Empty;
    public FarePaymentMethod PaymentMethod { get; set; }
    public FareTransfers? Transfers { get; set; }
    public Guid? AgencyId { get; set; }
    public int? TransferDuration { get; set; }
    public IReadOnlyList<IFareRuleModel> Rules { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FareRuleModel : IFareRuleModel
{
    public Guid? RouteId { get; set; }
    public string? OriginId { get; set; }
    public string? DestinationId { get; set; }
    public string? ContainsId { get; set; }
}
