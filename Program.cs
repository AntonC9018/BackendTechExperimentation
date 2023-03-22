using AutoMapper;
using Microsoft.EntityFrameworkCore;
using efcore_transactions;
using HotChocolate.Execution.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(c =>
{
    c.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManagement"));
});
builder.Services.AddAutoMapper(c =>
{
    c.AddProfile<MapperProfile>();
    
    var config = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<MapperProfile>();
    });
    config.AssertConfigurationIsValid();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

{
    var graph = builder.Services.AddGraphQLServer();
    graph.AddGraphQLServer();
    graph.AddQueryType<QueryType>();
    graph.AddProjections();
    graph.AddFiltering();
    graph.AddSorting();
    graph.InitializeOnStartup();
    graph.AddTransactionScopeHandler<DefaultTransactionScopeHandler>();
    graph.AddMutationConventions(applyToAllMutations: true);
    graph.AddType<ProjectType>();
    graph.AddType<PersonType>();
}

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLSchema();

app.Run();
