using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.EfQueryEntity<Person>();
        descriptor.EfQueryEntity<Project>();
        descriptor.EfQueryEntity<Person>(middlewareFlags: MiddlewareFlags.All & ~MiddlewareFlags.Paging);
        descriptor.EfQueryEntity<Project>(middlewareFlags: MiddlewareFlags.All & ~MiddlewareFlags.Paging);
    }
}

public class EfObjectType<T> : ObjectType<T>
    where T : class
{
    protected readonly IReadOnlyList<PropertyInfo> _idProperties;
    protected readonly IReadOnlyList<PropertyInfo> _ignoredProperties;
    
    public EfObjectType()
    {
        var properties = typeof(T).GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
        
        _idProperties = properties.Where(
            p => p.Name.EndsWith("Id")
                 || p.GetCustomAttributesData()
                     .Any(a => 
                         a.AttributeType == typeof(KeyAttribute)
#pragma warning disable EF1001
                         || a.AttributeType == typeof(ForeignKey)))
#pragma warning restore EF1001
            .ToArray();
        _ignoredProperties = properties.Where(
            p => p.GetCustomAttributesData()
                .Any(a => a.AttributeType == typeof(PersonalDataAttribute)))
            .ToArray();
    }
    
    protected override void Configure(IObjectTypeDescriptor<T> descriptor)
    {
        descriptor.BindFieldsImplicitly();

        foreach (var idProp in _IdProperties)
            descriptor.Field(idProp).ID();
        
        foreach (var ignoredProp in _IgnoredProperties)
            descriptor.Field(ignoredProp).Ignore();
    }
}