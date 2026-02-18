using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SpeakHalo.API.Infrastructure;

/// <summary>
/// Configures Swagger document generation per API version using proper DI.
/// Avoids the BuildServiceProvider() anti-pattern (ASP0000) by using IConfigureOptions.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "SpeakHalo API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "AI Voice Assistant SaaS Platform API (deprecated)"
                    : "AI Voice Assistant SaaS Platform API"
            });
        }
    }
}
