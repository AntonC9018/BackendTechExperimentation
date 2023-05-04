using efcore_transactions;
using HotChocolate.Data;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.GlobalFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

string projectManagementConnectionString = builder.Configuration.GetConnectionString("ProjectManagement")!;

void ConfigureDbContext(DbContextOptionsBuilder c)
{
    c.UseSqlServer(projectManagementConnectionString);
}

builder.Services.AddOptions<MyAuthenticationOptions>();
builder.Services.AddAuthentication("Test")
    .AddScheme<MyAuthenticationOptions, TestAuthenticationHandler>("Test", o => { });
builder.Services.AddAuthorization();

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(ConfigureDbContext);
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
{
    var graph = builder.Services.AddGraphQLServer();
    graph.AddGraphQLServer();
    graph.AddAuthorization();
    graph.AddQueryType<QueryType>();
    graph.AddProjections(descriptor =>
    {
        var provider = new QueryableProjectionProvider(x =>
        {
            x.AddGlobalFilterHandlers();
            x.AddOwnedEdgeHandlers();
            x.AddDefaults();
        });
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
    graph.UseAutomaticPersistedQueryPipeline();
    graph.AddInMemoryQueryStorage();
    
    builder.Services.AddMemoryCache(options =>
    {
    });
}

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();
app.MapGraphQLSchema();

{
    using var scope = app.Services.CreateScope();
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var dbContext = dbContextFactory.CreateDbContext();

    await Seeder.Seed(dbContext);
}

app.Run();