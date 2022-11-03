
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
builder.Services.AddApiProxy(builder.Configuration.GetSection("ApiProxy"));
builder.Services.AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(opts =>
    {
        opts.ClientId = config.GetValue<string>("Auth:ClientId");
        opts.ClientSecret = config.GetValue<string>("Auth:ClientSecret");
        opts.Authority = config.GetValue<string>("Auth:Authority");
    });

var app = builder.Build();

app.UseHttpLogging();

if (config.GetSection("Authentication") != null)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseRouting();
app.UseStaticFiles();
app.UseApiProxy();

app.MapFallbackToFile("index.html");

app.Run();
