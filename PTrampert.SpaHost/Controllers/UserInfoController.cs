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

    public class SimplifiedClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }

        public SimplifiedClaim(){}

        public SimplifiedClaim(Claim c)
        {
            Type = c.Type;
            Value = c.Value;
            ValueType = c.ValueType;
        }
    }
}
