using Shiny.Extensions.Push;

namespace Sample.Api.Controllers;


[ApiController]
[Route("[controller]")]
public class PushController : Controller
{
    readonly IPushManager pushManager;

    public PushController(IPushManager pushManager)
    {
        this.pushManager = pushManager;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterArgs args)
    {
        return this.Ok(null);
    }


    [HttpPost("send")]
    public async Task<ActionResult<object>> Send([FromBody] SendArgs args)
    {
        return this.Ok(null);
    }
}

public record RegisterArgs(
    bool IsApple,
    string RegistrationToken
);


public record SendArgs(
    string Message
);