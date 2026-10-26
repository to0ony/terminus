namespace Terminus.DAL.Entities;

/// <summary>
/// Postavke instance za javne stranice i PDF (naziv, boje, logo, javni URL).
/// Tablica ima najviše jedan redak (Id = 1).
/// </summary>
public class InstanceSettings : IAuditable
{
    public const short SingletonId = 1;

    public short Id { get; set; } = SingletonId;
    public string? DisplayName { get; set; }
    public string? PrimaryColor { get; set; }
    public string? PrimaryTextColor { get; set; }
    public string? PublicUrl { get; set; }
    public bool PublicGtfsEnabled { get; set; }
    public bool PublicMapEnabled { get; set; }
    public byte[]? Logo { get; set; }
    public string? LogoContentType { get; set; }
    public DateTime? LogoUpdatedAt { get; set; }

    /// <summary>Kad je čarobnjak prvog pokretanja završen (uvoz GTFS-a ili početak od nule).</summary>
    public DateTime? SetupCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
