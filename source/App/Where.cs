using System.Linq.Expressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

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
    protected CustomFilterVisitor _visitor = new CustomFilterVisitor(new QueryableCombinator());
    
    
    /// <inheritdoc />
    public override void ConfigureField(
        string argumentName,
        IObjectFieldDescriptor descriptor)
    {
        QueryableFilterContext VisitFilterArgumentExecutor(
            IValueNode valueNode,
            IFilterInputType filterInput,
            bool inMemory)
        {
            var visitorContext = new CustomQueryableFilterContext(filterInput, inMemory);

            // rewrite GraphQL input object into expression tree.
            _visitor.Visit(valueNode, visitorContext);

            return visitorContext;
        }

        var contextData = descriptor.Extend().Definition.ContextData;
        var argumentKey = (VisitFilterArgument)VisitFilterArgumentExecutor;
        contextData[ContextVisitFilterArgumentKey] = argumentKey;
        contextData[ContextArgumentNameKey] = argumentName;
    }
}

public class CustomQueryableFilterContext : QueryableFilterContext
{
    public CustomQueryableFilterContext(IFilterInputType initialType, bool inMemory) : base(initialType, inMemory)
    {
    }
}

public class CustomFilterVisitor : FilterVisitor<CustomQueryableFilterContext, Expression>
{
    public CustomFilterVisitor(IFilterOperationCombinator<CustomQueryableFilterContext, Expression> combinator) : base(combinator)
    {
    }
    
    protected override ISyntaxVisitorAction OnFieldEnter(
        CustomQueryableFilterContext context,
        IFilterField field,
        ObjectFieldNode node)
    {
        var fieldType = context.Types.Peek();
        var fieldType2 = field.Type;
        
        if (field.Handler is IFilterFieldHandler<QueryableFilterContext, Expression> handler &&
            handler.TryHandleEnter(
                context,
                field,
                node,
                out var action))
        {
            return action;
        }
        return SyntaxVisitor.SkipAndLeave;
    }
}