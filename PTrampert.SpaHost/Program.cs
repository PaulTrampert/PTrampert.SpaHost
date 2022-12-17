using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Mcrio.Configuration.Provider.Docker.Secrets;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    var additionalConfigs = Environment.GetEnvironmentVariable("SPAHOST_ADDITIONAL_APPSETTINGS");
    if (additionalConfigs != null)
        foreach (var file in additionalConfigs.Split(",",
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            builder.Configuration.AddJsonFile(file);

    builder.Configuration.AddDockerSecrets();
    builder.Configuration.AddEnvironmentVariables();
    builder.Configuration.AddCommandLine(args);

    var config = builder.Configuration;

    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    var authConfig = config.GetSection("AuthConfig")?.Get<AuthConfig>() ?? new AuthConfig();
    var fhConfig = config.GetSection("ForwardedHeaders").Get<ForwardedHeadersConfig>() ?? new ForwardedHeadersConfig();
    var redisConfig = config.GetSection("RedisConfig").Get<RedisConfig>() ?? new RedisConfig();

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


    builder.Services.AddOptions<JsonSerializerOptions>()
        .Configure(opts => opts.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

    if (config.GetValue("Antiforgery:EnableProtection", true))
    {
        builder.Services.AddAntiforgery(opts =>
        {
            opts.HeaderName = config.GetValue<string>("Antiforgery:HeaderName", "X-XSRF-TOKEN");
            opts.FormFieldName = config.GetValue<string>("Antiforgery:FieldName", "antiforgeryToken")!;
        });
        builder.Services.AddControllersWithViews(opts =>
        {
            opts.Filters.Add<AutoValidateAntiforgeryTokenAttribute>();
        });
    }

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
            .AddOpenIdConnect(authConfig.OidcConfig.ConfigureOidc(builder.Environment));

        if (authConfig.RequireForStaticFiles)
            builder.Services.AddAuthorization(opts =>
            {
                opts.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
    }

    builder.Services.Configure<ForwardedHeadersOptions>(fhConfig.ConfigureForwardedHeaders);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseForwardedHeaders();
    app.UseRouting();
    if (app.Environment.IsDevelopment())
        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Unspecified,
            OnAppendCookie = opts => opts.CookieOptions.SameSite = SameSiteMode.Unspecified,
            OnDeleteCookie = opts => opts.CookieOptions.SameSite = SameSiteMode.Unspecified
        });

    if (authConfig != null)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    app.UseWebSockets();

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