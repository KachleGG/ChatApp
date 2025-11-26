using Microsoft.AspNetCore.Mvc;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ConfigController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // GET api/config
    // Returns a small subset of server-side settings the frontend needs.
    [HttpGet]
    public IActionResult Get()
    {
        var prohibitGroups = _configuration.GetValue<bool>("ServerSettings:ProhibitGroups");
        var privateMode = _configuration.GetValue<bool>("ServerSettings:PrivateMode");
        var prohibitGeneral = _configuration.GetValue<bool>("ServerSettings:ProhibitGeneral");

        return Ok(new
        {
            prohibitGroups,
            privateMode,
            prohibitGeneral
        });
    }
}
