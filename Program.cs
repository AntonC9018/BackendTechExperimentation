using AutoMapper;
using AutoMapper.QueryableExtensions;
using efcore_transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    o.RoutePrefix = string.Empty;
});

app.MapGet("", () =>
{
    return "Hello";
});

app.MapGet("/people/{id?}", async (
    [FromServices] ApplicationDbContext data,
    [FromServices] IMapper mapper,
    int? id) =>
{
    IQueryable<Person> q = data.Set<Person>()
        .Include(p => p.Projects);

    if (id is not null)
        q = q.Where(p => p.Id == id.Value);

    var dtoq = mapper.ProjectTo<PersonResponseDto>(q);
    
    return await dtoq.ToListAsync();
});

app.MapPost("/people", async(
    [FromServices] ApplicationDbContext data,
    [FromBody] PersonRequestDto person,
    [FromServices] IMapper mapper) =>
{
    Person entity;
    if (person.Id == default)
    {
        entity = mapper.Map<Person>(person);
        data.ChangeTracker.TrackGraph(entity, e =>
        {
            e.Entry.State = EntityState.Added;
        });
    }
    else
    {
        person.Projects.Sort((a, b) =>
        {
            var v0 = a.Id ?? default;
            var v1 = b.Id ?? default;
            return v1.CompareTo(v0);
        });
        
        var projectIds = person.Projects
            .Select(p => p.Id)
            .SelectNonZeroValue()
            .ToHashSet();
        
        entity = await data.Set<Person>()
            .Include(p => p.Projects
                .OrderBy(proj => -proj.Id)
                .Where(proj => projectIds.Contains(proj.Id)))
            .FirstAsync(p => p.Id == person.Id);

        if (entity.Projects.Count != projectIds.Count)
            throw new Exception("Invalid project ids present in request.");
        
        mapper.Map(person, entity);
        
        data.ChangeTracker.TrackGraph(entity, e =>
        {
            if (e.Entry.IsKeySet)
                e.Entry.State = EntityState.Modified;
            else
                e.Entry.State = EntityState.Added;
        });
    }
    
    await data.SaveChangesAsync();
    return mapper.Map<PersonResponseDto>(entity);
});


app.MapGet("names", async (
    [FromServices] ApplicationDbContext context,
    [FromServices] IMapper mapper) =>
{

    var q = context.Set<Project>()
        .Select(x => new { Value = x, Name = x.ProjectName })
        .Where(x => x.Name == "A")
        .Select(x => x.Value.Id);
    return await q.ToListAsync();
    // var q1 = context
    //     .Set<Project>()
    //     .ProjectTo<NameDto>(mapper.ConfigurationProvider)
    //     .Where(x => x.Name == "A")
    //     .ProjectTo<Project>(mapper.ConfigurationProvider);
    // return await q1.ToListAsync();
    // return new
    // {
    //     @interface = new
    //     {
    //         people = await context.Set<Person>().SelectNameByInterface().ToListAsync(),
    //         projects = await context.Set<Project>().SelectNameByInterface().ToListAsync()  
    //     },
    //     generic = new
    //     {
    //         people = await context.Set<Person>().SelectNameByInterfaceGeneric().ToListAsync(),
    //         projects = await context.Set<Project>().SelectNameByInterfaceGeneric().ToListAsync()  
    //     },
    // };
});

app.Run();
