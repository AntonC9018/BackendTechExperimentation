using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace efcore_transactions;

public static class TestProjections
{
    public static async Task Test(ApplicationDbContext context)
    {
        // This filters correctly on the server.
        var peopleAQuery = context.People.Select(p => new Person
        {
            Id = p.Id,
            Name = (p.Id == 1 ? p.Name : default)!,
            ParentId = (p.Id == 1 ? p.ParentId : default),
            Projects = p.Projects,
        });
        var peopleA = await peopleAQuery.ToListAsync();

        // This seems to filter on the client.
        var peopleBQuery = context.People.Select(p =>
            p.Id == 1
                ? new Person
                {
                    Id = p.Id,
                    Name = p.Name,
                    ParentId = p.ParentId,
                    Projects = p.Projects,
                }
                : new Person
                {
                    Id = p.Id,
                    Name = null!,
                    ParentId = default,
                    Projects = p.Projects,
                });
        var peopleB = await peopleBQuery.ToListAsync();
    }
}