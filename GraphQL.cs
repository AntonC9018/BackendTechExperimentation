using System.Reflection;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
        return EfQuery<TEntity, EfObjectType<TEntity>>(descriptor, name, middlewareFlags);
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
        
        return (IObjectFieldDescriptor) typeof(GraphQlHelper)
            .GetMethod("EfQuery", BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(entityType, typeof(TQuery))
            .Invoke(null, new object?[] { descriptor, name, middlewareFlags })!;
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
    
    
    public static IObjectFieldDescriptor EfQueryDto<TEntity, TDto, TQuery>(
        this IObjectTypeDescriptor descriptor)
    
        where TEntity : class
        where TDto : class
        where TQuery : ObjectType<TDto>
    {
        return descriptor
            .Field("test")
            .Type<NonNullType<ListType<NonNullType<TQuery>>>>()
            .UseDbContext<ApplicationDbContext>()
            .UsePaging<NonNullType<TQuery>>()
            .UseProjection()
            .UseFiltering()
            .UseSorting()
            .Resolve(ctx => ctx
                .DbContext<ApplicationDbContext>()
                .Set<TEntity>()
                .AsQueryable()
                .ProjectTo<TEntity, TDto>(ctx));
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("test")
            .Type<NonNullType<ListType<NonNullType<ObjectType<GraphQlPersonDto>>>>>()
            .UseDbContext<ApplicationDbContext>()
            // .UsePaging<NonNullType<ObjectType<GraphQlPersonDto>>>()
            .UseProjection()
            .UseFiltering()
            .UseSorting()
            .Resolve(ctx => ctx
                .DbContext<ApplicationDbContext>()
                .Set<Person>()
                .AsQueryable()
                .ProjectTo<Person, GraphQlPersonDto>(ctx));
        
        descriptor
            .Field("test2")
            .Type<NonNullType<ListType<NonNullType<ObjectType<Person>>>>>()
            .UseDbContext<ApplicationDbContext>()
            // .UsePaging<NonNullType<ObjectType<GraphQlPersonDto>>>()
            .UseProjection()
            .UseFiltering()
            .UseSorting()
            .Resolve(ctx => ctx
                .DbContext<ApplicationDbContext>()
                .Set<Person>()
                .AsQueryable());
    }
}

public class ProjectDtoType : ObjectType<GraphQlProjectDto>
{
    protected override void Configure(IObjectTypeDescriptor<GraphQlProjectDto> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        // descriptor.Ignore(x => x.Name);
    }
}

public class EfObjectType<T> : ObjectType<T>
    where T : class
{
    protected readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    
    public EfObjectType(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        //
        // using var dbContext = _dbContextFactory.CreateDbContext();
        // var model = dbContext.Model;
        //
        // IObjectFieldDescriptor CreateKeyField(IReadOnlyList<IProperty> props, Type targetEntityType)
        // {
        //     if (props.Count > 1)
        //         throw new NotImplementedException();
        //
        //     var prop = props[0];
        //     var pointingAtEntity = targetEntityType;
        //     return descriptor.Field(prop.PropertyInfo!).ID(pointingAtEntity.Name);
        // }
        //
        // var entityModel = model.FindEntityType(typeof(T))!;
        //
        // {
        //     var key = entityModel.GetKeys().Single(k => k.IsPrimaryKey());
        //     var props = key.Properties;
        //     if (props.Count > 1)
        //         throw new NotImplementedException();
        //     
        //     var prop = props[0];
        //     descriptor
        //         .Field(prop.PropertyInfo!)
        //         .ID(typeof(T).Name);
        //
        //     // Nodes won't work with ef core
        //     // https://github.com/ChilliCream/graphql-platform/issues/5966
        //     // descriptor
        //     //     .ImplementsNode()
        //     //     .IdField(prop.PropertyInfo!)
        //     //     .ResolveNode(async (ctx, id) =>
        //     //     {
        //     //         return ctx.DbContext<ApplicationDbContext>().Set<T>();
        //     //     });
        // }
        //
        // foreach (var key in entityModel.GetForeignKeys())
        // {
        //     var props = key.Properties;
        //     if (props.Count > 1)
        //         continue;
        //
        //     var prop = props[0];
        //     descriptor
        //         .Field(prop.PropertyInfo!)
        //         .ID(key.PrincipalEntityType.Name);
        // }
    }
}