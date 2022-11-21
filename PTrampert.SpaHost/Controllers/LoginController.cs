using Microsoft.AspNetCore.Mvc;

namespace PTrampert.SpaHost.Controllers;

[Route("[controller]")]
public class LoginController : Controller
{
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return Redirect(string.IsNullOrWhiteSpace(Request.Headers.Referer) ? "/" : Request.Headers.Referer);
        }
        return Challenge();
    }
}