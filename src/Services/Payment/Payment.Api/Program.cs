using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Payment.Api.Data;
using Payment.Api.Infrastructure;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("PaymentDb")
    ?? throw new InvalidOperationException("Connection string 'PaymentDb' is not configured.");
builder.Services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
builder.Services.AddVersionedSwagger();

builder.Services.AddStoreJwtAuthentication(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentDbContext>("payment-db");

var app = builder.Build();

app.UseStoreObservability();

if (app.Environment.IsDevelopment())
{
    app.UseVersionedSwaggerUi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
