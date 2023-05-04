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
public class ABinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;

        // Try to fetch the value of the argument by name
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;

        // Check if the argument value is null or empty
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        if (!int.TryParse(value, out var id))
        {
            // Non-integer arguments result in model state errors
            bindingContext.ModelState.TryAddModelError(
                modelName, "Author Id must be an integer.");

            return Task.CompletedTask;
        }

        // Model will be null if not found, including for
        // out of range id values (0, -3, etc.)
        var model = _context.Authors.Find(id);
        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}