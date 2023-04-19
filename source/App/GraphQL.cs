using System.Linq.Expressions;
using System.Security.Claims;
using HotChocolate.Data;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

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
    public static IObjectFieldDescriptor EfQuery<TEntity, TQuery>(
        this IObjectTypeDescriptor descriptor,
        string? name = null,
        MiddlewareFlags middlewareFlags = MiddlewareFlags.All)
    
        where TQuery : ObjectType<TEntity>
        where TEntity : class
    {
        name ??= GetQueryNameForType(typeof(TEntity), middlewareFlags);
        var f = ConfigureEfQuery<TQuery>(descriptor, name, middlewareFlags);
        return f.Resolve(ctx => ctx
            .DbContext<ApplicationDbContext>()
            .Set<TEntity>()
            .AsQueryable());
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
}



public class PersonType : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Authorize();
        descriptor.GlobalFilterIgnoreCondition(IsUserAdminIgnoreCondition.Instance);
        descriptor.GlobalFilter(UserNameExtractor.Instance,
            (p, name) => p.Name.Contains(name));
        descriptor.BindFieldsImplicitly();
    }
}

public class ProjectType : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.GlobalFilterIgnoreCondition(IsUserAdminIgnoreCondition.Instance);
        descriptor.GlobalFilter(p => p.ProjectName.Contains(" "));
        descriptor.Field(x => x.ProjectName);
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("test")
            .Type<NonNullType<ListType<NonNullType<PersonType>>>>()
            .UseDbContext<ApplicationDbContext>()
            .UseGlobalFilter()
            .Use(next => async ctx =>
            {
                await next(ctx);
                Console.WriteLine("Breakpoint");
            })
            // .UsePaging<NonNullType<ObjectType<GraphQlPersonDto>>>()
            // .Use<WhereMiddleware>()
            .UseProjection()
            .Use(next => async ctx =>
            {
                await next(ctx);
                Console.WriteLine("Breakpoint");
            })
            .UseFiltering()
            .UseSorting()
            .Resolve(ctx => ctx
                .DbContext<ApplicationDbContext>()
                .Set<Person>()
                .AsQueryable()
                .AsNoTracking());
    }
}
