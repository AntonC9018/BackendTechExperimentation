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