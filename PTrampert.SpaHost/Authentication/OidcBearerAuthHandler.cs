using System.Net.Http.Headers;
using Duende.AccessTokenManagement.OpenIdConnect;
using PTrampert.ApiProxy;

namespace PTrampert.SpaHost.Authentication
{
    public class OidcBearerAuthHandler : IAuthentication
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OidcBearerAuthHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthenticationHeaderValue> GetAuthenticationHeader()
        {
            var token = await _httpContextAccessor.HttpContext!.GetUserAccessTokenAsync();
            if (!token.WasSuccessful(out var userToken))
            {
                return null!;
            }
            return new AuthenticationHeaderValue("Bearer", token.Token.AccessToken);
        }
    }
}
