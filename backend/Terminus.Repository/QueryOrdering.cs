using System.Linq.Expressions;

namespace Terminus.Repository;

internal static class QueryOrdering
{
    /// <summary>OrderBy ili OrderByDescending, prema smjeru iz filtra.</summary>
    public static IOrderedQueryable<T> OrderByDirection<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> key,
        bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
