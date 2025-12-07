using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PTrampert.SpaHost.Configuration;

public class CookieConfig
{
    public TimeSpan? ExpireTimeSpan { get; set; }

    public bool SlidingExpiration { get; set; } = true;
        
    public void ConfigureCookies(CookieAuthenticationOptions opts)
    {
        opts.ExpireTimeSpan = ExpireTimeSpan ?? TimeSpan.FromMinutes(30);
        opts.SlidingExpiration = SlidingExpiration;
        opts.Events.OnSigningOut = async e =>
        {
            await e.HttpContext.RevokeRefreshTokenAsync();
        };
    }
}