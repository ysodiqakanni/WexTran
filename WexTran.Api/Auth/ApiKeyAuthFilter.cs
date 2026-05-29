using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace WexTran.Api.Auth
{
    public class ApiKeyAuthFilter : IAuthorizationFilter
    {
        public const string HeaderName = "X-Api-Key";

        private readonly string _configuredKey;

        public ApiKeyAuthFilter(IConfiguration configuration)
        {
            _configuredKey = configuration["ApiKey"]
                ?? throw new InvalidOperationException("ApiKey is not configured.");
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey)
                || providedKey != _configuredKey)
            {
                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "A valid API key must be provided in the X-Api-Key header."
                });
            }
        }
    }
}
