using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

// Routes, authentication per route, rate limiting and QoS live in ocelot.json.
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.AddStoreObservability();

var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"];
if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters.");
}

// "Bearer" is the AuthenticationProviderKey used by protected routes in ocelot.json. Claim types are kept
// as issued, so RouteClaimsRequirement { "role": "Admin" } matches the role claim from Identity.
builder.Services.AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "email",
            RoleClaimType = "role"
        };
    });

builder.Services.AddOcelot(builder.Configuration).AddPolly();
builder.Services.AddHealthChecks();

var app = builder.Build();

// X-Correlation-ID is created here, at the edge, when the caller did not send one. Ocelot forwards it
// downstream (RequestIdKey) and every service propagates it further over HTTP and RabbitMQ.
app.UseStoreObservability();

app.UseRouting();
#pragma warning disable ASP0014 // Endpoints must run before Ocelot, which terminates the pipeline.
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
    endpoints.MapGet("/", () => Results.Ok(new
    {
        service = "Store Platform API gateway",
        routes = "/identity/v1, /catalog/v1, /inventory/v1, /orders/v1, /payments/v1"
    }));
});
#pragma warning restore ASP0014

await app.UseOcelot();
await app.RunAsync();
