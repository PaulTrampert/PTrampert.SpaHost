namespace PTrampert.SpaHost.Configuration
{
    public class AuthConfig
    {
        public bool RequireForStaticFiles { get; set; }
        public CookieConfig CookieConfig { get; set; } = new CookieConfig();
        public OidcConfig OidcConfig { get; set; } = new OidcConfig();
    }
}
