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

app.MapGraphQL();
app.MapGraphQLSchema();

app.Run();