using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace efcore_transactions;

[ApiController]
[Route("api/test")]
public class MyController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    
    public MyController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public void Test([FromQuery] int a)
    {
        
    }
    
    [HttpPost]
    public async Task<PersonResponseDto> CreatePerson(IEnumerable<PersonRequestDto> personRequestDto)
    {
        var person = new Person
        {
            Name = personRequestDto.First().Name
        };
        await _dbContext.People.AddAsync(person);
        await _dbContext.SaveChangesAsync();
        
        return new PersonResponseDto
        {
            Id = person.Id,
            Name = person.Name
        };
    }
}