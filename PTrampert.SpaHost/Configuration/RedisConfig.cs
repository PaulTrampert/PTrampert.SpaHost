namespace PTrampert.SpaHost.Configuration
{
    public class RedisConfig
    {
        public bool UseForDataProtection { get; set; }
        public string DataProtectionConnectionString { get; set; } = "redis:6379";
    }
}
