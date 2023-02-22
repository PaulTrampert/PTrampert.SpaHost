using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
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
            return new AuthenticationHeaderValue("Bearer", token.AccessToken);
        }
    }
}
