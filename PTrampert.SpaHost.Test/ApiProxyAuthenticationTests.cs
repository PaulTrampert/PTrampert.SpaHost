using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace PTrampert.SpaHost.Test
{
    /// <summary>
    /// Requests through an api configured with <see cref="Authentication.OidcBearerAuthHandler"/>, against the real
    /// app. The identity provider and the upstream api are faked by <see cref="FakeNetwork"/>, and the cookie session by
    /// <see cref="TestSessionHandler"/>, so that the real Duende token management runs, refreshes included.
    /// </summary>
    public class ApiProxyAuthenticationTests
    {
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient subject = null!;
        private FakeNetwork network = null!;
        private TestSession session = null!;

        [SetUp]
        public void SetUp()
        {
            network = new FakeNetwork();
            session = new TestSession();
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("AuthConfig:OidcConfig:Authority", FakeNetwork.Authority);
                builder.UseSetting("AuthConfig:OidcConfig:ClientId", "spahost");
                builder.UseSetting("AuthConfig:OidcConfig:ClientSecret", "secret");
                builder.UseSetting("ApiProxy:sample:BaseUrl", FakeNetwork.UpstreamBaseUrl);
                builder.UseSetting("ApiProxy:sample:AuthType", "PTrampert.SpaHost.Authentication.OidcBearerAuthHandler, PTrampert.SpaHost");
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(session);
                    services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestSessionHandler>(TestSessionHandler.Scheme, null);
                    services.PostConfigure<AuthenticationOptions>(opts => opts.DefaultAuthenticateScheme = TestSessionHandler.Scheme);
                    // Configured rather than post-configured: the OpenID Connect handler's own post-configuration builds
                    // its backchannel from this handler.
                    services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, opts => opts.BackchannelHttpHandler = network);
                    services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => network));
                });
            });
            subject = factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            subject.Dispose();
            factory.Dispose();
        }

        [Test]
        public async Task ItProxiesAnAnonymousRequestWithoutAnAuthorizationHeader()
        {
            var response = await subject.GetAsync("/api/sample/things");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(network.UpstreamRequests, Has.Count.EqualTo(1));
                Assert.That(network.UpstreamRequests.Single().Headers.Authorization, Is.Null);
            }
        }

        [Test]
        public async Task ItProxiesASignedInRequestWithTheUsersAccessToken()
        {
            session.SignIn(accessToken: "valid-access-token", expiresAt: DateTimeOffset.UtcNow.AddHours(1), refreshToken: "refresh-token");

            var response = await subject.GetAsync("/api/sample/things");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(network.UpstreamRequests, Has.Count.EqualTo(1));
                Assert.That(network.UpstreamRequests.Single().Headers.Authorization?.ToString(), Is.EqualTo("Bearer valid-access-token"));
            }
        }

        [Test]
        public async Task ItRespondsUnauthorizedWithoutProxyingWhenTheUsersTokenCannotBeRefreshed()
        {
            session.SignIn(accessToken: "expired-access-token", expiresAt: DateTimeOffset.UtcNow.AddHours(-1), refreshToken: "revoked-refresh-token");

            var response = await subject.GetAsync("/api/sample/things");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(network.TokenRequests, Is.EqualTo(1), "the refresh should have been attempted");
                Assert.That(network.UpstreamRequests, Is.Empty);
            }
        }

        [Test]
        public async Task ItRespondsUnauthorizedWithoutProxyingWhenTheUserHasNoTokens()
        {
            session.SignIn(accessToken: null, expiresAt: null, refreshToken: null);

            var response = await subject.GetAsync("/api/sample/things");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(network.UpstreamRequests, Is.Empty);
            }
        }

        [Test]
        public async Task ItSignsOutTheCookieSessionWhenTheUsersTokenCannotBeRefreshed()
        {
            session.SignIn(accessToken: "expired-access-token", expiresAt: DateTimeOffset.UtcNow.AddHours(-1), refreshToken: "revoked-refresh-token");
            var cookieName = factory.Services.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>()
                .Get(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme).Cookie.Name;

            var response = await subject.GetAsync("/api/sample/things");

            Assert.That(response.Headers.GetValues("Set-Cookie"),
                Has.Some.StartsWith($"{cookieName}=;").And.Some.Contains("expires=Thu, 01 Jan 1970"));
        }

        [Test]
        public async Task ItStillRespondsUnauthorizedWhenSigningOutFails()
        {
            network.AdvertiseRevocationEndpoint = false;
            session.SignIn(accessToken: "expired-access-token", expiresAt: DateTimeOffset.UtcNow.AddHours(-1), refreshToken: "revoked-refresh-token");

            var response = await subject.GetAsync("/api/sample/things");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(network.UpstreamRequests, Is.Empty);
            }
        }

        [Test]
        public async Task ItRespondsWithTheStatusOfAProxyException()
        {
            var response = await subject.GetAsync("/api/unconfigured/things");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        /// <summary>
        /// Stands in for the cookie session: whether a user is signed in, and the tokens stored with their session.
        /// </summary>
        public class TestSession
        {
            public AuthenticationTicket? Ticket { get; private set; }

            public void SignIn(string? accessToken, DateTimeOffset? expiresAt, string? refreshToken)
            {
                var tokens = new List<AuthenticationToken>();
                if (accessToken != null) tokens.Add(new AuthenticationToken { Name = "access_token", Value = accessToken });
                if (expiresAt != null) tokens.Add(new AuthenticationToken { Name = "expires_at", Value = expiresAt.Value.ToString("o") });
                if (refreshToken != null) tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
                var properties = new AuthenticationProperties();
                properties.StoreTokens(tokens);

                var identity = new ClaimsIdentity([new Claim("sub", "user-1")], TestSessionHandler.Scheme);
                Ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, TestSessionHandler.Scheme);
            }
        }

        public class TestSessionHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, TestSession session)
            : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
        {
            public const string Scheme = "TestSession";

            protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
                Task.FromResult(session.Ticket == null ? AuthenticateResult.NoResult() : AuthenticateResult.Success(session.Ticket));
        }

        /// <summary>
        /// Answers requests to the identity provider, refusing every refresh with invalid_grant, and records requests to
        /// the upstream api, answering them with 200.
        /// </summary>
        public class FakeNetwork : HttpMessageHandler
        {
            public const string Authority = "https://idp.example";
            public const string UpstreamBaseUrl = "https://upstream.example";

            public bool AdvertiseRevocationEndpoint { get; set; } = true;
            public List<HttpRequestMessage> UpstreamRequests { get; } = [];
            public int TokenRequests { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri!.GetLeftPart(UriPartial.Path);
                if (url.StartsWith(UpstreamBaseUrl))
                {
                    UpstreamRequests.Add(request);
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") });
                }

                switch (url)
                {
                    case $"{Authority}/.well-known/openid-configuration":
                        var revocation = AdvertiseRevocationEndpoint ? $"\"revocation_endpoint\": \"{Authority}/revoke\"," : "";
                        return Json(HttpStatusCode.OK, $$"""
                            {
                              "issuer": "{{Authority}}",
                              "authorization_endpoint": "{{Authority}}/authorize",
                              "token_endpoint": "{{Authority}}/token",
                              {{revocation}}
                              "jwks_uri": "{{Authority}}/jwks"
                            }
                            """);
                    case $"{Authority}/jwks":
                        return Json(HttpStatusCode.OK, """{ "keys": [] }""");
                    case $"{Authority}/token":
                        TokenRequests++;
                        return Json(HttpStatusCode.BadRequest, """{ "error": "invalid_grant" }""");
                    case $"{Authority}/revoke":
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                    default:
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
            }

            protected override void Dispose(bool disposing)
            {
                // Shared by every HttpClient in the app, which the client factory disposes on its own schedule.
            }

            private static Task<HttpResponseMessage> Json(HttpStatusCode status, string body) =>
                Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
