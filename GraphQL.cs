using HotChocolate;

namespace efcore_transactions;

public class Query
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Person> Person([Service] ApplicationDbContext context) =>
        context.Set<Person>();
    
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Project> Project([Service] ApplicationDbContext context) =>
        context.Set<Project>();
}

public class QueryType : ObjectType
{
    private IObjectFieldDescriptor EfQuery<TGraph, TEntity>(IObjectTypeDescriptor descriptor) where TGraph : ObjectType<TEntity> where TEntity : class
    {
        var name = typeof(TEntity).Name;
        name = name[.. 1].ToLower() + name[1 ..];
        
        return descriptor
            .Field(name)
            .Type<TGraph>()
            .Resolve(ctx =>
            {
                return ctx
                    .Service<ApplicationDbContext>()
                    .Set<TEntity>()
                    .AsQueryable();
            })
            .UseProjection()
            .UseFiltering()
            .UseSorting();
    }
    
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        EfQuery<PersonType, Person>(descriptor);
        EfQuery<ProjectType, Project>(descriptor);
    }
}

public class PersonType : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.BindFieldsImplicitly();
        descriptor.Ignore(x => x.Id);
    }
}

public class ProjectType : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.BindFieldsImplicitly();
    }
}