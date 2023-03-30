using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace efcore_transactions;

public class CustomQueryFilterProvider : QueryableFilterProvider
{
    /// <summary>
    /// Creates a new instance
    /// </summary>
    public CustomQueryFilterProvider()
    {
    }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="configure">Configures the provider</param>
    public CustomQueryFilterProvider(
        Action<IFilterProviderDescriptor<QueryableFilterContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to visit the input
    /// </summary>
    protected virtual FilterVisitor<QueryableFilterContext, Expression> Visitor { get; } =
        new CustomFilterVisitor(new QueryableCombinator());
}

public class CustomFilterVisitor : FilterVisitor<QueryableFilterContext, Expression>
{
    public CustomFilterVisitor(FilterOperationCombinator<QueryableFilterContext, Expression> combinator) : base(combinator)
    {
    }
}