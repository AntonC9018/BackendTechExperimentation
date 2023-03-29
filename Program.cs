using efcore_transactions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
{
    var graph = builder.Services.AddGraphQLServer();
    graph.InitializeOnStartup();
    graph.AddQueryType<QueryType>();
    graph.AddType<HumanType>();
    graph.ModifyOptions(o =>
    {
        o.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
}

var app = builder.Build();

app.MapGraphQL();
app.MapGraphQLSchema();

app.Run();