using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace PTrampert.SpaHost.Controllers;

[Route("[controller]")]
public class LoginController : Controller
{
    [HttpPost]
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public IActionResult Index([FromQuery]string redirectUri = "/")
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            if (!Url.IsLocalUrl(redirectUri))
                redirectUri = "/";
            return Redirect(redirectUri);
        }
        return Challenge();
    }
}