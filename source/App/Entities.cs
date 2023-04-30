using Microsoft.EntityFrameworkCore;

namespace efcore_transactions;

#pragma warning disable CS8618

public class Person
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<Project> Projects { get; set; } = new();
    
    public long? ParentId { get; set; }
    public Person? Parent { get; set; }
    public List<Person> Children { get; set; } = new();
    
    public List<PersonCitizenship> Citizenships { get; set; }
}

public class PersonCitizenship
{
    public long PersonId { get; set; }
    public Person Person { get; set; }
    public long CountryId { get; set; }
    public Country Country { get; set; }
}

public class Project
{
    public long Id { get; set; }
    public long PersonId { get; set; }
    public Person Person { get; set; }
    public string ProjectName { get; set; }
}

public class Country
{
    public long Id { get; set; }
    public string Name { get; set; }
    
    public List<PersonCitizenship> Citizenships { get; set; } = new();
}

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Person> People { get; set; }
    public DbSet<Project> Projects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>()
            .HasMany(p => p.Projects)
            .WithOne(p => p.Person);

        modelBuilder.Entity<Person>()
            .HasOne(p => p.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(p => p.ParentId);

        modelBuilder.Entity<Person>()
            .HasMany(p => p.Citizenships)
            .WithOne(c => c.Person);

        modelBuilder.Entity<PersonCitizenship>()
            .HasOne(p => p.Country)
            .WithMany(c => c.Citizenships);

        modelBuilder.Entity<PersonCitizenship>()
            .HasKey(pc => new { pc.PersonId, pc.CountryId });
    }
}

public static class Seeder
{
    public static async Task Seed(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await SeedPeople(context);
        await SeedCountries(context);
    }
    
    public static async Task SeedPeople(ApplicationDbContext context)
    {
        int count = await context.People.CountAsync();
        if (count >= 5)
            return;

        var people = new List<Person>
        {
            new()
            {
                Name = "John Doe 1",
                Projects = new()
                {
                    new() { ProjectName = "Math Slideshow" },
                    new() { ProjectName = "Math Theorem Proof" },
                }
            },
            new()
            {
                Name = "Jane Doe 2",
                Projects = new()
                {
                    new() { ProjectName = "Geogebra Presentation" },
                }
            },
            new()
            {
                Name = "John Smith 1",
                Projects = new()
                {
                    new() { ProjectName = "The World Map" },
                    new() { ProjectName = "The Biology of Europe" },
                }
            },
            new()
            {
                Name = "Jane Smith 2",
                Projects = new()
                {
                    new() { ProjectName = "Ants" },
                    new() { ProjectName = "Dinosaurs" },
                }
            },
            new() { Name = "John Doe Jr.", },
        };
        context.People.AddRange(people);
        await context.SaveChangesAsync();

        people[0].ParentId = people[1].Id;
        people[1].ParentId = people[2].Id;
        people[2].ParentId = people[3].Id;
        people[4].ParentId = people[3].Id;
        await context.SaveChangesAsync();
    }

    public static async Task SeedCountries(ApplicationDbContext context)
    {
        int count = await context.Set<Country>().CountAsync();
        if (count >= 5)
            return;

        var countries = new List<Country>
        {
            new()
            {
                Name = "USA",
            },
            new()
            {
                Name = "Canada",
            },
            new()
            {
                Name = "Mexico",
            },
            new()
            {
                Name = "France",
            },
            new()
            {
                Name = "Germany",
            },
        };
        context.Set<Country>().AddRange(countries);
        await context.SaveChangesAsync();

        var people = await context.People.ToListAsync();
        people[0].Citizenships = new List<PersonCitizenship>
        {
            new()
            {
                CountryId = 1,
            },
            new()
            {
                CountryId = 2,
            },
            new()
            {
                CountryId = 3,
            }
        };
        for (int i = 1; i < people.Count; i++)
        {
            people[i].Citizenships = new List<PersonCitizenship>
            {
                new()
                {
                    CountryId = i,
                }
            };
        }
        await context.SaveChangesAsync();
    }
}

#pragma warning restore CS8618

