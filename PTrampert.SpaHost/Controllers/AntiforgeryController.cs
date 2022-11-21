using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PTrampert.SpaHost.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class AntiforgeryController : ControllerBase
    {
        private readonly IAntiforgery antiforgeryService;

        public AntiforgeryController(IAntiforgery antiforgeryService)
        {
            this.antiforgeryService = antiforgeryService;
        }

        public async Task Get()
        {
            var tokens = antiforgeryService.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
            });
        }
    }
}
