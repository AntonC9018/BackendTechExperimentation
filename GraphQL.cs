using System.Reflection;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore.Internal;

namespace efcore_transactions;

[Flags]
public enum MiddlewareFlags
{
    Paging = 1 << 0,
    Projection = 1 << 1,
    Filtering = 1 << 2,
    Sorting = 1 << 3,
    All = Paging | Projection | Filtering | Sorting,
}

public static class GraphQlHelper
{
    public static IObjectFieldDescriptor EfQueryEntity<TEntity>(
        this IObjectTypeDescriptor descriptor,
        string? name = null,
        MiddlewareFlags middlewareFlags = MiddlewareFlags.All)
    
        where TEntity : class
    {
        var entityType = typeof(TEntity);
        name ??= GetQueryNameForType(entityType, middlewareFlags);
        var f = ConfigureEfQuery<ObjectType<TEntity>>(descriptor, name, middlewareFlags);
        return f.Resolve(GetSetAsQueryable<TEntity>);
    }
    
    public static IObjectFieldDescriptor EfQueryType<TQuery>(
        this IObjectTypeDescriptor descriptor,
        string? name = null,
        MiddlewareFlags middlewareFlags = MiddlewareFlags.All)
    
        where TQuery : ObjectType
    {
        // Pain point #1
        var objectTypeType = typeof(TQuery);
        while (!objectTypeType.IsGenericType)
            objectTypeType = objectTypeType.BaseType!;
        var entityType = objectTypeType.GetGenericArguments()[0];
        
        name ??= GetQueryNameForType(entityType, middlewareFlags);
        var f = ConfigureEfQuery<TQuery>(descriptor, name, middlewareFlags);
        return f.Resolve(GetResolver(entityType), typeof(IQueryable<>).MakeGenericType(entityType));
    }
    
    public static IObjectFieldDescriptor EfQuery<TEntity, TQuery>(
        this IObjectTypeDescriptor descriptor,
        string? name = null,
        MiddlewareFlags middlewareFlags = MiddlewareFlags.All)
    
        where TQuery : ObjectType<TEntity>
        where TEntity : class
    {
        name ??= GetQueryNameForType(typeof(TEntity), middlewareFlags);
        var f = ConfigureEfQuery<TQuery>(descriptor, name, middlewareFlags);
        return f.Resolve(GetSetAsQueryable<TEntity>);
    }
    
    // How do we apply the convention that hot chocolate uses, instead of redefining it??
    // Pain point #2
    private static string GetQueryNameForType(Type entityType, MiddlewareFlags middlewareFlags)
    {
        var name = entityType.Name;
        name = name[.. 1].ToLower() + name[1 ..];
        
        if ((middlewareFlags & MiddlewareFlags.Paging) != 0)
            name += "Paged";
        
        return name;
    }
    
    private static IObjectFieldDescriptor ConfigureEfQuery<TQuery>(
        this IObjectTypeDescriptor descriptor,
        string name,
        MiddlewareFlags middlewareFlags)

        where TQuery : ObjectType
    {
        var f = descriptor
            .Field(name)
            .Type<NonNullType<ListType<NonNullType<TQuery>>>>()
            .UseDbContext<ApplicationDbContext>();

        if ((middlewareFlags & MiddlewareFlags.Paging) != 0)
            f = f.UsePaging<NonNullType<TQuery>>();
        if ((middlewareFlags & MiddlewareFlags.Projection) != 0)
            f = f.UseProjection();
        if ((middlewareFlags & MiddlewareFlags.Filtering) != 0)
            f = f.UseFiltering();
        if ((middlewareFlags & MiddlewareFlags.Sorting) != 0)
            f = f.UseSorting();

        return f;
    }

    // Pain point #1: the continuation
    private static IQueryable<T> GetSetAsQueryable<T>(IResolverContext ctx)
        where T : class
    {
        var q = ctx
            .DbContext<ApplicationDbContext>()
            .Set<T>()
            .AsQueryable();
        return q;
    }
    
    private static ValueTask<object?> GetSetAsQueryableObjectTask<T>(IResolverContext ctx)
        where T : class
    {
        return ValueTask.FromResult<object?>(GetSetAsQueryable<T>(ctx));
    }

    private static FieldResolverDelegate GetResolver(Type entityType)
    {
        var method = typeof(GraphQlHelper).GetMethod("GetSetAsQueryableObjectTask", BindingFlags.NonPublic | BindingFlags.Static)!;
        var genericMethod = method.MakeGenericMethod(entityType);
        var resolverFunc = genericMethod.CreateDelegate<FieldResolverDelegate>();
        return resolverFunc;
    }

    private static void EfResolve(IObjectFieldDescriptor descriptor, Type entityType)
    {
        
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.EfQuery<Person, PersonType>();
        descriptor.EfQueryType<ProjectType>();
        descriptor.EfQueryType<PersonType>(middlewareFlags: MiddlewareFlags.All & ~MiddlewareFlags.Paging);
        descriptor.EfQueryEntity<Project>(middlewareFlags: MiddlewareFlags.All & ~MiddlewareFlags.Paging);
    }
}

public class PersonType : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.BindFieldsImplicitly();
    }
}

public class ProjectType : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.BindFieldsImplicitly();
    }
}