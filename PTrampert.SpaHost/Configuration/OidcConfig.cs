namespace PTrampert.SpaHost.Configuration
{
    public class OidcConfig
    {
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public IEnumerable<string> Scopes { get; set; }
    }
}
