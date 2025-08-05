using Microsoft.AspNetCore.Mvc;

namespace Fightarr.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Events()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View();
    }
}

// Keep API controller separate
[ApiController]
[Route("api")]
public class ApiHomeController : ControllerBase
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