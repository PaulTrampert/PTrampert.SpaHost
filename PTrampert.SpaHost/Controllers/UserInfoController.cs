using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PTrampert.SpaHost.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class UserInfoController : ControllerBase
    {
        public IActionResult Get()
        {
            return Ok(User.Claims.Select(c => new SimplifiedClaim(c)));
        }
    }

    public class SimplifiedClaim(Claim c)
    {
        public string Type { get; set; } = c.Type;
        public string Value { get; set; } = c.Value;
        public string ValueType { get; set; } = c.ValueType;
    }
}
