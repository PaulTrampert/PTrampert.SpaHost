using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PTrampert.ApiProxy.Exceptions;

namespace PTrampert.SpaHost.Filters
{
    /// <summary>
    /// Responds with the status a <see cref="ProxyException"/> carries. ApiProxy throws it for errors the client should
    /// see, and leaves mapping it to the consuming app; otherwise it would surface as an unhandled 500.
    /// </summary>
    public class ProxyExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ProxyExceptionFilter> _logger;

        public ProxyExceptionFilter(ILogger<ProxyExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not ProxyException proxyException)
            {
                return;
            }

            _logger.LogInformation("Proxy request failed with {Status}: {Message}", proxyException.Status, proxyException.Message);
            context.Result = new StatusCodeResult(proxyException.Status);
            context.ExceptionHandled = true;
        }
    }
}
