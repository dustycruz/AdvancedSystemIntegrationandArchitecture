using Identity.Api.Data;
using Identity.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Store.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddStoreObservability();

var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");
builder.Services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.Configure<SeedAccountOptions>(builder.Configuration.GetSection(SeedAccountOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("identity-db");

var app = builder.Build();

app.UseStoreObservability();
app.MapHealthChecks("/health");

await app.InitializeDatabaseAsync();

app.Run();
