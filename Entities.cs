using Microsoft.EntityFrameworkCore;

namespace efcore_transactions;

#pragma warning disable CS8618

public class Person : IName
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<Project> Projects { get; set; } = new();
}

public class Project : IName
{
    public long Id { get; set; }
    public long PersonId { get; set; }
    public Person Person { get; set; }
    public string ProjectName { get; set; }
    
    string IName.Name => ProjectName;
}

public interface IName
{
    string Name { get; }
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
    }
}

#pragma warning restore CS8618

