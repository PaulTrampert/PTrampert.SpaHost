
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
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
        .AddCookie(opts =>
        {
            
        })
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

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    var fhConfig = config.GetSection("ForwardedHeaders").Get<ForwardedHeadersConfig>();
    opts.ForwardedHeaders = ForwardedHeaders.All;
    opts.AllowedHosts = fhConfig.AllowedHosts.ToList();
    opts.ForwardedForHeaderName = fhConfig.ForwardedForHeaderName;
    opts.ForwardedHostHeaderName = fhConfig.ForwardedHostHeaderName;
    opts.ForwardedProtoHeaderName = fhConfig.ForwardedProtoHeaderName;
    opts.KnownNetworks.Clear();
    foreach (var net in fhConfig.KnownNetworksParsed)
    {
        opts.KnownNetworks.Add(net);
    }
    opts.KnownProxies.Clear();
    foreach (var proxy in fhConfig.KnownProxiesParsed)
    {
        opts.KnownProxies.Add(proxy);
    }
    opts.OriginalForHeaderName = fhConfig.OriginalForHeaderName;
    opts.OriginalHostHeaderName = fhConfig.OriginalHostHeaderName;
    opts.OriginalProtoHeaderName = fhConfig.OriginalProtoHeaderName;
    opts.RequireHeaderSymmetry = fhConfig.RequireHeaderSymmetry;
});

var app = builder.Build();



app.UseHttpLogging();

app.UseForwardedHeaders();

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
