using Microsoft.AspNetCore.Mvc;

namespace Fightarr.Web.Controllers;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(new { 
            message = "Welcome to Fightarr!", 
            version = "1.0.0",
            timestamp = DateTime.UtcNow 
        });
    }
    
    [HttpGet("health")]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
