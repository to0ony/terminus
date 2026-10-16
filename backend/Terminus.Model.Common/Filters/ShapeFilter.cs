namespace Terminus.Model.Common.Filters;

public class ShapeFilter
{
    /// <summary>Traži po kodu (bez obzira na velika/mala slova).</summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
