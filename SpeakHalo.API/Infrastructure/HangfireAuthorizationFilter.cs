using Hangfire.Dashboard;

namespace SpeakHalo.API.Infrastructure;

/// <summary>
/// Authorization filter for Hangfire dashboard.
/// Requires authenticated users with Admin role to access the dashboard.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access only to authenticated users
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Require Admin role for production environments
        // In development, any authenticated user can access
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        if (isProduction)
        {
            // Production: Only admins can access
            return httpContext.User.IsInRole("Admin");
        }
        else
        {
            // Development: Any authenticated user can access
            return true;
        }
    }
}
