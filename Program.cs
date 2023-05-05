using efcore_transactions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(c =>
{
    c.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManagement"));
});

var app = builder.Build();

app.UseDeveloperExceptionPage();

{
    using var scope = app.Services.CreateScope();
    var provider = scope.ServiceProvider;
    var context = provider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    int count = await context.Set<Person>().CountAsync();
    Console.WriteLine(count);
}

app.Run();
