using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WexTran.Api.Exceptions;

namespace WexTran.Api
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title) = exception switch
            {
                InvalidTransactionException => (StatusCodes.Status400BadRequest, "Bad Request"),
                TransactionNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                CurrencyConversionUnavailableException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            var method = httpContext.Request.Method;
            var path = httpContext.Request.Path;

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}", method, path);
            else
                _logger.LogWarning("{ExceptionType} on {Method} {Path}: {Message}", exception.GetType().Name, method, path, exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : exception.Message
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}