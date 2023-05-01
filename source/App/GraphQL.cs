using System;
using HotChocolate.Data;
using HotChocolate.GlobalFilters;
using HotChocolate.GlobalFilters.Internal;
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

public sealed class PersonType : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        descriptor.GlobalFilterIgnoreCondition(IsUserAdminIgnoreCondition.Instance);
        
        var filter = descriptor.CreateGlobalFilterWithContext(
            UserIdExtractor.Instance,
            (person, userId) => userId != null && person.Id == userId);
        descriptor.Owner(filter);
        
        // var rootFilter = ExpressionGlobalFilter.Create((Person p) => !p.Name.Contains("John"));
        // descriptor.PublicWithRootFilter(rootFilter);
        descriptor.Public();

        descriptor.Field(x => x.Password).Owned();
        descriptor.Field(x => x.Name).Owned();
    }
}

public sealed class ProjectType : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        descriptor.OwnedBy(p => p.Person);
    }
}

public sealed class CountryType : ObjectType<Country>
{
    protected override void Configure(IObjectTypeDescriptor<Country> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        descriptor.Public();
    }
}

public sealed class PersonCitizenshipType : ObjectType<PersonCitizenship>
{
    protected override void Configure(IObjectTypeDescriptor<PersonCitizenship> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        descriptor.OwnedBy(p => p.Person);
    }
}

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        void CreateField<TQuery, TEntity>() 
            where TQuery : ObjectType<TEntity>
            where TEntity : class
        {
            var name = typeof(TEntity).Name;
            descriptor
                .Field(name)
                .Type<NonNullType<ListType<NonNullType<TQuery>>>>()
                .UseDbContext<ApplicationDbContext>()
                .UseProjection()
                .UseGlobalFilter()
                .UseFiltering()
                .UseSorting()
                .Resolve(ctx => ctx
                    .DbContext<ApplicationDbContext>()
                    .Set<TEntity>()
                    .AsQueryable()
                    .AsNoTracking());
        }
        
        CreateField<PersonType, Person>();
        CreateField<ProjectType, Project>();
        CreateField<CountryType, Country>();
        CreateField<PersonCitizenshipType, PersonCitizenship>();
    }
}
