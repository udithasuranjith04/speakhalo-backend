using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SpeakHalo.API.Attributes;

/// <summary>
/// Authorization filter that validates API key scopes
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireScopeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _requiredScopes;

    public RequireScopeAttribute(params string[] scopes)
    {
        _requiredScopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip scope validation if user is authenticated via JWT (full access)
        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        // Check if request is authenticated via API key
        var authMethod = context.HttpContext.Items["AuthMethod"] as string;
        if (authMethod != "ApiKey")
        {
            // Not authenticated via API key, let other auth mechanisms handle it
            return;
        }

        // Get scopes from the API key
        var apiKeyScopes = context.HttpContext.Items["ApiKeyScopes"] as List<string>;

        if (apiKeyScopes == null || !apiKeyScopes.Any())
        {
            context.Result = new JsonResult(new
            {
                isSuccess = false,
                errors = new[] { "API key has no scopes assigned." }
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        // Check if API key has at least one of the required scopes
        var hasRequiredScope = _requiredScopes.Any(requiredScope =>
            apiKeyScopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase) ||
            apiKeyScopes.Contains("*", StringComparer.OrdinalIgnoreCase)); // Wildcard scope for admin keys

        if (!hasRequiredScope)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireScopeAttribute>>();
            logger?.LogWarning("API key scope validation failed. Required: {RequiredScopes}, Available: {AvailableScopes}",
                string.Join(", ", _requiredScopes),
                string.Join(", ", apiKeyScopes));

            context.Result = new JsonResult(new
            {
                isSuccess = false,
                errors = new[] { $"API key does not have required permissions. Required scopes: {string.Join(", ", _requiredScopes)}" }
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
