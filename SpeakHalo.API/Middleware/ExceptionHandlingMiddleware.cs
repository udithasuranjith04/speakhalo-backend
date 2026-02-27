using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using SpeakHalo.Shared.Models;

namespace SpeakHalo.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = exception switch
        {
            ArgumentException or ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        string errorMessage;
        object? errorDetails = null;

        // Always include exception type and message for diagnostics
        errorMessage = exception.Message;
        errorDetails = new
        {
            type = exception.GetType().Name,
            stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
            innerException = exception.InnerException?.Message
        };

        object result;
        if (_environment.IsDevelopment() && errorDetails != null)
        {
            result = new
            {
                isSuccess = false,
                errors = new[] { errorMessage },
                details = errorDetails
            };
        }
        else
        {
            result = new
            {
                isSuccess = false,
                errors = new[] { errorMessage }
            };
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(result, options);

        return context.Response.WriteAsync(json);
    }
}
