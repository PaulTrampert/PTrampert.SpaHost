using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace PTrampert.SpaHost.Configuration
{
    public class ForwardedHeadersConfig
    {
        public IEnumerable<string> AllowedHosts { get; set; } = new[] {"*"};

        public string ForwardedForHeaderName { get; set; } = "X-Forwarded-For";
        
        public string ForwardedHostHeaderName { get; set; } = "X-Forwarded-Host";
        
        public string ForwardedProtoHeaderName { get; set; } = "X-Forwarded-Proto";

        public int? ForwardLimit { get; set; } = 1;

        public IEnumerable<string>? KnownNetworks { get; set; }

        public IEnumerable<IPNetwork> KnownNetworksParsed => KnownNetworks?.Select(v =>
        {
            var prefixAndLength = v.Split('/');
            if (prefixAndLength.Length != 2)
            {
                throw new FormatException($"{v} is not a valid CIDR network string.");
            }

            var prefix = IPAddress.Parse(prefixAndLength[0]);
            var prefixLength = int.Parse(prefixAndLength[1]);
            return new IPNetwork(prefix, prefixLength);
        }) ?? new List<IPNetwork>();


        public IEnumerable<string>? KnownProxies { get; set; }

        public IEnumerable<IPAddress> KnownProxiesParsed => KnownProxies?.Select(IPAddress.Parse) ?? new List<IPAddress>();

        public string OriginalForHeaderName { get; set; } = "X-Original-For";

        public string OriginalHostHeaderName { get; set; } = "X-Original-Host";

        public string OriginalProtoHeaderName { get; set; } = "X-Original-Proto";

        public bool RequireHeaderSymmetry { get; set; } = false;
    }
}
