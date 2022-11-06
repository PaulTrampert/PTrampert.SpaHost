
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PTrampert.SpaHost.Configuration;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
builder.Services.AddApiProxy(builder.Configuration.GetSection("ApiProxy"));
var oidcConfig = config.GetSection("Auth")?.Get<OidcConfig>();

if (oidcConfig != null)
{
    builder.Services.AddAuthentication(opts =>
        {
            opts.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opts.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(opts =>
        {
            opts.Authority = oidcConfig.Authority;

            opts.ClientId = oidcConfig.ClientId;
            opts.ClientSecret = oidcConfig.ClientSecret;

            opts.ResponseType = OpenIdConnectResponseType.CodeIdToken;
            opts.ResponseMode = OpenIdConnectResponseMode.FormPost;

            opts.Scope.Clear();
            foreach (var scope in oidcConfig.Scopes)
            {
                opts.Scope.Add(scope);
            }

            opts.SaveTokens = true;
        });

    builder.Services.AddAuthorization(opts =>
    {
        opts.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}

var app = builder.Build();



app.UseHttpLogging();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCookiePolicy(new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.Unspecified,
        OnAppendCookie = opts => opts.CookieOptions.SameSite = SameSiteMode.Unspecified,
        OnDeleteCookie = opts => opts.CookieOptions.SameSite = SameSiteMode.Unspecified,
    });
}

if (oidcConfig != null)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseStaticFiles();
app.UseApiProxy();

app.MapFallbackToFile("index.html");

app.Run();
