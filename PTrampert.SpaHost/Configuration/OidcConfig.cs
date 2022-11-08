using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace PTrampert.SpaHost.Configuration
{
    public class OidcConfig
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public IEnumerable<string> Scopes { get; set; }

        public void ConfigureOidc(OpenIdConnectOptions opts)
        {
            opts.Authority = Authority;

            opts.ClientId = ClientId;
            opts.ClientSecret = ClientSecret;

            opts.ResponseType = OpenIdConnectResponseType.CodeIdToken;
            opts.ResponseMode = OpenIdConnectResponseMode.FormPost;

            opts.Scope.Clear();
            foreach (var scope in Scopes)
            {
                opts.Scope.Add(scope);
            }

            opts.SaveTokens = true;
        }
    }
}
