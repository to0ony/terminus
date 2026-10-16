namespace Terminus.Model.Common.Enums;

/// <summary>
/// GTFS route_type. Imenovane su osnovne vrijednosti; dopuštene su i proširene
/// vrijednosti (Hierarchical Vehicle Types, npr. 700 = autobus, 200 = međugradski
/// autobus), koje se spremaju kao broj.
/// </summary>
public enum RouteType : short
{
    Tram = 0,
    Subway = 1,
    Rail = 2,
    Bus = 3,
    Ferry = 4,
    CableTram = 5,
    AerialLift = 6,
    Funicular = 7,
    Trolleybus = 11,
    Monorail = 12
}
