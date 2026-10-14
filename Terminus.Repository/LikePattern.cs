namespace Terminus.Repository;

internal static class LikePattern
{
    /// <summary>Escape znak koji se predaje ILike(..., EscapeCharacter).</summary>
    public const string EscapeCharacter = @"\";

    /// <summary>Uzorak "sadrži" za ILIKE, s escapeanim %, _ i \ iz korisničkog unosa.</summary>
    public static string Contains(string value) =>
        "%" + value.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
}
