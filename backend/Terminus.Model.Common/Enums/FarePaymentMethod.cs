namespace Terminus.Model.Common.Enums;

/// <summary>GTFS fare_attributes.payment_method.</summary>
public enum FarePaymentMethod : short
{
    /// <summary>Plaća se u vozilu.</summary>
    OnBoard = 0,

    /// <summary>Plaća se prije ukrcavanja.</summary>
    BeforeBoarding = 1
}
