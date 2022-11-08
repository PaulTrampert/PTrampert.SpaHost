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

        private IEnumerable<IPNetwork> knownNetworks = new[] {new IPNetwork(IPAddress.Loopback, 8)};

        public IEnumerable<string> KnownNetworks
        {
            get => knownNetworks.Select(n => n.ToString())!;
            set
            {
                knownNetworks = value.Select(v =>
                {
                    var prefixAndLength = v.Split('/');
                    if (prefixAndLength.Length != 2)
                    {
                        throw new FormatException($"{v} is not a valid CIDR network string.");
                    }

                    var prefix = IPAddress.Parse(prefixAndLength[0]);
                    var prefixLength = int.Parse(prefixAndLength[1]);
                    return new IPNetwork(prefix, prefixLength);
                });
            }
        }

        public IEnumerable<IPNetwork> KnownNetworksParsed => knownNetworks;

        private IEnumerable<IPAddress> knownProxies = new[] {IPAddress.IPv6Loopback};

        public IEnumerable<string> KnownProxies
        {
            get => knownProxies.Select(p => p.ToString());
            set => knownProxies = value.Select(IPAddress.Parse);
        }

        public IEnumerable<IPAddress> KnownProxiesParsed => knownProxies;

        public string OriginalForHeaderName { get; set; } = "X-Original-For";

        public string OriginalHostHeaderName { get; set; } = "X-Original-Host";

        public string OriginalProtoHeaderName { get; set; } = "X-Original-Proto";

        public bool RequireHeaderSymmetry { get; set; } = false;
    }
}
