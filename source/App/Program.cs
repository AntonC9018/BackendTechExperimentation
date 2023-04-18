using efcore_transactions;
using HotChocolate.Data;
using HotChocolate.Data.Projections.Expressions;
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
    graph.AddProjections(descriptor =>
    {
        var provider = new QueryableProjectionProvider(x => x
            .RegisterFieldInterceptor<GlobalFilterProjectionFieldInterceptor>()
            .AddDefaults());
        descriptor.Provider(provider);
    });
    graph.AddFiltering(o =>
    {
        o.AddDefaults();
    });
    graph.AddSorting();
    graph.InitializeOnStartup();
    graph.AddType<PersonType>();
    graph.AddType<ProjectType>();
}

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLSchema();

app.Run();