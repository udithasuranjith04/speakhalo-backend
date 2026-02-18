using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Twilio.Security;

namespace SpeakHalo.API.Filters;

/// <summary>
/// Action filter that validates incoming Twilio webhook requests
/// by verifying the X-Twilio-Signature header against the auth token.
/// Apply to all controllers/actions that receive Twilio callbacks.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateTwilioSignatureAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateTwilioSignatureAttribute>>();

        var authToken = configuration["Twilio:AuthToken"];
        if (string.IsNullOrEmpty(authToken))
        {
            logger.LogError("Twilio AuthToken not configured - cannot validate webhook signature");
            context.Result = new StatusCodeResult(500);
            return;
        }

        var signature = context.HttpContext.Request.Headers["X-Twilio-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
        {
            logger.LogWarning("Twilio webhook request missing X-Twilio-Signature header from {IP}",
                context.HttpContext.Connection.RemoteIpAddress);
            context.Result = new UnauthorizedResult();
            return;
        }

        // Reconstruct the full URL that Twilio used to sign the request
        var request = context.HttpContext.Request;
        var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";

        // Read form parameters for signature validation
        var parameters = new Dictionary<string, string>();
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            foreach (var key in form.Keys)
            {
                parameters[key] = form[key].ToString();
            }
        }

        var validator = new RequestValidator(authToken);
        var isValid = validator.Validate(url, parameters, signature);

        if (!isValid)
        {
            logger.LogWarning("Invalid Twilio signature for webhook request to {Url} from {IP}",
                url, context.HttpContext.Connection.RemoteIpAddress);
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
