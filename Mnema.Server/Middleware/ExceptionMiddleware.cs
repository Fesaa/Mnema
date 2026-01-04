using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mnema.Common.Exceptions;

namespace Mnema.Server.Middleware;

public record ApiException(int Status, string? Message = null, string? Details = null);

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var errorMessage = string.IsNullOrEmpty(ex.Message) ? "Internal Server Error" : ex.Message;
            var statusCode = ex switch
            {
                MnemaException => HttpStatusCode.BadRequest,
                NotImplementedException => HttpStatusCode.NotImplemented,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                ForbiddenException => HttpStatusCode.Forbidden,
                NotFoundException => HttpStatusCode.NotFound,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                logger.LogError(ex, "An exception occurred while handling an http request.");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var details = context.User.Identity?.IsAuthenticated ?? false ? ex.StackTrace : null;

            var response = new ApiException(context.Response.StatusCode, errorMessage, details);
            var json = JsonSerializer.Serialize(response, JsonSerializerOptions);

            await context.Response.WriteAsync(json);
        }
    }
}