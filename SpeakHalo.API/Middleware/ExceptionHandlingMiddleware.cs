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
            // Log the full exception with stack trace for debugging
            _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Determine status code based on exception type
        var statusCode = exception switch
        {
            ArgumentException or ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            InvalidOperationException => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = (int)statusCode;

        // In production, hide detailed error messages
        string errorMessage;
        object? errorDetails = null;

        if (_environment.IsDevelopment())
        {
            // Development: Show full error details for debugging
            errorMessage = exception.Message;
            errorDetails = new
            {
                type = exception.GetType().Name,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message
            };
        }
        else
        {
            // Production: Generic error messages only
            errorMessage = statusCode switch
            {
                HttpStatusCode.BadRequest => "The request was invalid. Please check your input and try again.",
                HttpStatusCode.Unauthorized => "You are not authorized to perform this action.",
                HttpStatusCode.NotFound => "The requested resource was not found.",
                HttpStatusCode.Conflict => "The operation could not be completed due to a conflict.",
                _ => "An error occurred while processing your request. Please try again later."
            };
        }

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
