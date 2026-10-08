using System.Net.Http.Headers;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PTrampert.ApiProxy;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.SpaHost.Authentication
{
    public class OidcBearerAuthHandler : IAuthentication
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<OidcBearerAuthHandler> _logger;

        public OidcBearerAuthHandler(IHttpContextAccessor httpContextAccessor, ILogger<OidcBearerAuthHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeader(CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext!;
            var token = await httpContext.GetUserAccessTokenAsync(ct: cancellationToken);
            if (token.WasSuccessful(out var userToken, out var failure))
            {
                return new AuthenticationHeaderValue("Bearer", userToken.AccessToken);
            }

            // Visitors who have not signed in are proxied without a token, so the api can still serve them public content.
            if (httpContext.User.Identity is not { IsAuthenticated: true })
            {
                return null!;
            }

            // A signed-in user whose token cannot be obtained (typically an expired or revoked refresh token) has a dead
            // session. Proxying anonymously would hide that from the SPA, so fail with a 401 instead, and end the cookie
            // session so that /userinfo agrees.
            _logger.LogWarning("Could not obtain an access token for the signed-in user: {Error}", failure.Error);
            try
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Signing out revokes the refresh token at the identity provider, which may be what is failing. The 401
                // matters more than the sign out: signing in again replaces the session anyway.
                _logger.LogWarning(e, "Could not sign out the user whose access token could not be obtained");
            }
            throw new ProxyException("The user's access token could not be obtained. Sign in again.", StatusCodes.Status401Unauthorized);
        }
    }
}
