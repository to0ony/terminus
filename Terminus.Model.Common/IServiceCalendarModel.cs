namespace Terminus.Model.Common;

public interface IServiceCalendarModel
{
    Guid Id { get; set; }
    Guid AgencyId { get; set; }

    /// <summary>GTFS service_id, jedinstven unutar agencije.</summary>
    string Code { get; set; }

    /// <summary>Naziv za prikaz, npr. "Radni dan" ili "Nedjelja i praznici".</summary>
    string Name { get; set; }

    bool Monday { get; set; }
    bool Tuesday { get; set; }
    bool Wednesday { get; set; }
    bool Thursday { get; set; }
    bool Friday { get; set; }
    bool Saturday { get; set; }
    bool Sunday { get; set; }
    DateOnly StartDate { get; set; }
    DateOnly EndDate { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
