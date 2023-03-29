using System.Linq.Expressions;
using AutoMapper;
using efcore_transactions;
using HotChocolate.Execution.Processing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string projectManagementConnectionString = builder.Configuration.GetConnectionString("ProjectManagement")!;

void ConfigureDbContext(DbContextOptionsBuilder c)
{
    c.UseSqlServer(projectManagementConnectionString);
}

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(ConfigureDbContext);
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
    // graph.AddGlobalObjectIdentification();
    // graph.AddDirectiveType<MyDirectiveType>();
    // graph.ModifyOptions(opt => opt.UseXmlDocumentation = true);
    // graph.AddType<ProjectDtoType>();
}

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLSchema();

app.Run();