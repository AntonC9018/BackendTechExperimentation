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
}

public class Project
{
    public long Id { get; set; }
    public long PersonId { get; set; }
    public Person Person { get; set; }
    public string ProjectName { get; set; }
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
    }
}

public static class Seeder
{
    public static async Task Seed(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
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
}

#pragma warning restore CS8618

