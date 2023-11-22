using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace PTrampert.SpaHost.Configuration
{
    public class OidcConfig
    {
        public string? Authority { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public IEnumerable<string>? Scopes { get; set; }

        public Action<OpenIdConnectOptions> ConfigureOidc(IWebHostEnvironment env)
        {
            return opts =>
            {
                opts.Authority = Authority;
                opts.ClientId = ClientId;
                opts.ClientSecret = ClientSecret;

                opts.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                opts.ResponseMode = OpenIdConnectResponseMode.FormPost;

                opts.Scope.Clear();
                foreach (var scope in Scopes ?? new List<string>())
                {
                    opts.Scope.Add(scope);
                }

                opts.SaveTokens = true;

                opts.GetClaimsFromUserInfoEndpoint = true;
                opts.MapInboundClaims = false;
                opts.TokenValidationParameters.NameClaimType = "name";
                opts.TokenValidationParameters.RoleClaimType = "role";

                if (env.IsDevelopment())
                {
                    opts.RequireHttpsMetadata = false;
                    IdentityModelEventSource.ShowPII = true;
                }

                opts.Events.OnRedirectToIdentityProvider = async e =>
                {
                    if (e.Request.Path.StartsWithSegments("/api", StringComparison.InvariantCultureIgnoreCase)
                        || e.Request.Path.StartsWithSegments("/userinfo", StringComparison.InvariantCultureIgnoreCase)
                        || e.Request.Path.StartsWithSegments("/antiforgery", StringComparison.InvariantCultureIgnoreCase))
                    {
                        e.Response.StatusCode = 401;
                        e.HandleResponse();
                    }
                };
            };
        }
    }
}
