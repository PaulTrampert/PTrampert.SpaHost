
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using Mcrio.Configuration.Provider.Docker.Secrets;
using PTrampert.SpaHost.Configuration;
using Serilog;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.ConfigureAppConfiguration((builderCtx, configurationBuilder) =>
    {
        var additionalConfigs = Environment.GetEnvironmentVariable("SPAHOST_ADDITIONAL_APPSETTINGS");
        if (additionalConfigs != null)
        {
            foreach (var file in additionalConfigs.Split(",",
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                configurationBuilder.AddJsonFile(file);
            }
        }

        configurationBuilder.AddDockerSecrets();
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCommandLine(args);
    });
    var config = builder.Configuration;

    builder.Host.UseSerilog((ctx, lc) => 
        lc.ReadFrom.Configuration(ctx.Configuration));

    var authConfig = config.GetSection("AuthConfig")?.Get<AuthConfig>();
    var fhConfig = config.GetSection("ForwardedHeaders").Get<ForwardedHeadersConfig>();
    var redisConfig = config.GetSection("RedisConfig").Get<RedisConfig>();

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

    builder.Services.AddOptions<JsonSerializerOptions>()
        .Configure(opts => opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
    builder.Services.AddApiProxy(builder.Configuration.GetSection("ApiProxy"));

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

    app.UseSerilogRequestLogging();

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
}
catch (Exception e)
{
    Log.Fatal(e, "Unhandled exception, shutting down");
}
finally
{
    Log.Information("Shutdown complete");
    Log.CloseAndFlush();
}
