using Microsoft.AspNetCore.Mvc;

namespace Chatter.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessagesController : ControllerBase
{
    [HttpPost]
    public void PostMessage() {

    }
}
