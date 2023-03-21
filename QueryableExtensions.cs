using System.Numerics;
using AutoMapper.QueryableExtensions;

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
    public static IQueryable<string> SelectNameByInterface(this IQueryable<IName> q)
    {
        return q.Select(x => x.Name);
    }

    public static IQueryable<string> SelectNameByInterfaceGeneric<T>(this IQueryable<T> q) where T : IName
    {
        return q.Select(x => x.Name);
    }

    // public static IQueryable<string> SelectNameAutomapper<T>(this IQueryable<T> q, IConfigurationProvider mapper)
    //     where T : IName
    // {
    //     
        // return q.ProjectTo<CastAndValue<T, NameDto>>();
        // T -> new { Value=T, Name=mapper(T) -> NameDto} -> where -> select Value -> T
    // }
}