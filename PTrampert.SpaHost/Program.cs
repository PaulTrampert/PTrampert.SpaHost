
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PTrampert.SpaHost.Configuration;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
builder.Services.AddApiProxy(builder.Configuration.GetSection("ApiProxy"));
var authConfig = config.GetSection("AuthConfig")?.Get<AuthConfig>();
var fhConfig = config.GetSection("ForwardedHeaders").Get<ForwardedHeadersConfig>();
var redisConfig = config.GetSection("RedisConfig").Get<RedisConfig>();

if (authConfig != null)
{
    if (redisConfig.UseForDataProtection)
    {
        var redis = ConnectionMultiplexer.Connect(redisConfig.DataProtectionConnectionString);
        builder.Services.AddDataProtection()
            .SetApplicationName(builder.Environment.ApplicationName)
            .PersistKeysToStackExchangeRedis(redis, $"{builder.Environment.ApplicationName}_dpapi");
    }

    builder.Services.AddAuthentication(opts =>
        {
            opts.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opts.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            opts.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(authConfig.CookieConfig.ConfigureCookies)
        .AddOpenIdConnect(authConfig.OidcConfig.ConfigureOidc);

    if (authConfig.RequireForStaticFiles)
    {
        builder.Services.AddAuthorization(opts =>
        {
            opts.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}

builder.Services.Configure<ForwardedHeadersOptions>(fhConfig.ConfigureForwardedHeaders);

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

if (authConfig != null)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseStaticFiles();
app.UseApiProxy();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
