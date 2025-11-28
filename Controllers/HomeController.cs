using Microsoft.AspNetCore.Mvc;

namespace AIResumeBuilder.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // REDIRECT to login instead of looking for Index view
        return Redirect("/Account/Login");
    }

    public IActionResult Privacy()
    {
        // Return simple content instead of looking for Privacy view
        return Content("Privacy Policy - AI Resume Builder");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Return simple error message instead of looking for Error view
        return Content("An error occurred. Please go to the login page and try again.");
    }
}
