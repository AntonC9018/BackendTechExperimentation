using efcore_transactions;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string projectManagementConnectionString = builder.Configuration.GetConnectionString("ProjectManagement")!;

void ConfigureDbContext(DbContextOptionsBuilder c)
{
    c.UseSqlServer(projectManagementConnectionString);
}

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(ConfigureDbContext);
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
{
    var graph = builder.Services.AddGraphQLServer();
    graph.AddGraphQLServer();
    graph.AddQueryType<QueryType>();
    graph.AddProjections();
    graph.AddFiltering(o =>
    {
        o.AddDefaults();
    });
    graph.AddSorting();
    graph.InitializeOnStartup();
}

var app = builder.Build();

{
    using var context = app.Services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
        .CreateDbContext();
    await Seeder.Seed(context);
}

app.Map("test", async (IDbContextFactory<ApplicationDbContext> factory) =>
{
    using var context = factory.CreateDbContext();
    
    var q = context.People.AsQueryable();

    q = q.Include(
        p => p.Projects
            .Where(
                pr => pr.ProjectName
                    .Contains(" ")));
    
    var q2 = q.Select(p => new
    {
        Projects = p.Projects.Where(
            pr => pr.ProjectName
                .Contains(" ")),
        p.Id,
    });
    
    var list = await q2.ToListAsync();

    return list;
});

app.MapGraphQL();
app.MapGraphQLSchema();

app.Run();