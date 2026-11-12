using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Inventory.Api.Infrastructure;

/// <summary>One Swagger document per API version, with the operation ids from the contract and a Bearer scheme.</summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "Inventory API",
                Version = description.ApiVersion.ToString(),
                Description = "Stock levels and reservations. Contract: contracts/inventory.v1.yaml. Through the gateway: /inventory/v1/..."
                    + (description.IsDeprecated ? " This version is deprecated." : string.Empty)
            });
        }

        // operationId = action name, which matches the operationIds in the contract (ListProducts, GetProductById, ...).
        options.CustomOperationIds(api => api.ActionDescriptor.RouteValues["action"]);

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT from POST /identity/v1/auth/login"
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    }
}

public static class SwaggerConfigurationExtensions
{
    public static IServiceCollection AddVersionedSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
        return services;
    }

    public static WebApplication UseVersionedSwaggerUi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in app.DescribeApiVersions())
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    $"Inventory API {description.GroupName.ToUpperInvariant()}");
            }

            options.DocumentTitle = "Inventory API";
        });
        return app;
    }
}
