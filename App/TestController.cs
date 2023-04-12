using Microsoft.AspNetCore.Mvc;

namespace WebApplication1;

public class UserDto
{
    public string Name { get; set; }
}

public class User
{
    public string Name { get; set; }
}

[Route("test")]
[ApiController]
public class TestController : Controller
{
    [HttpGet]
    public ActionResult<UserDto> Get([UserBinder] User user)
    {
        return new UserDto { Name = user.Name };
    }
}