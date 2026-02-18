using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SpeakHalo.API.Filters;

/// <summary>
/// Action filter that enforces API key scope requirements on endpoints.
/// When a request is authenticated via API key (not JWT), this checks that
/// the API key has the required scope. JWT-authenticated requests pass through.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireApiScopeAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _requiredScope;

    public RequireApiScopeAttribute(string requiredScope)
    {
        _requiredScope = requiredScope;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Only enforce on API key-authenticated requests
        var authMethod = context.HttpContext.Items["AuthMethod"] as string;
        if (authMethod != "ApiKey")
        {
            // JWT or other auth — no scope check needed
            await next();
            return;
        }

        var scopes = context.HttpContext.Items["ApiKeyScopes"] as List<string>;
        if (scopes == null || !scopes.Contains(_requiredScope, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new
            {
                error = "Insufficient API key scope",
                requiredScope = _requiredScope,
                message = $"This endpoint requires the '{_requiredScope}' scope. Please create an API key with this scope."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
