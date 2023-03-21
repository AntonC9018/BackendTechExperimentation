using System.Numerics;

namespace efcore_transactions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> SelectNonZeroValue<T>(this IEnumerable<T?> source) where T : struct, INumber<T>
    {
        return source
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Where(x => x != default);
    }
}

public static class QueryableExtensions
{
}