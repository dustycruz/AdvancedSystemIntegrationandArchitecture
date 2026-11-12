using Asp.Versioning;
using Identity.Api.Contracts;
using Identity.Api.Data;
using Identity.Api.Domain;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Store.Observability.ProblemDetails;

namespace Identity.Api.Controllers;

/// <summary>Registration and login (tag Auth in contracts/identity.v1.yaml). Both are public.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController(
    IdentityDbContext db,
    IPasswordHasher<User> passwordHasher,
    JwtTokenService tokens,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    [Consumes("application/json")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        foreach (var error in PasswordPolicy.Validate(request.Password))
        {
            ModelState.AddModelError(nameof(request.Password), error);
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalized = Domain.User.Normalize(request.Email);
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists. Sign in instead.");
        }

        var user = new Domain.User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            FullName = request.FullName.Trim(),
            Role = UserRole.Customer,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("An account with this email already exists. Sign in instead.");
        }

        logger.LogInformation("Registered customer {UserId} {Email}", user.Id, user.Email);
        return CreatedAtAction(nameof(UsersController.GetCurrentUser), "Users",
            new { version = RouteData.Values["version"] }, CreateAuthResponse(user));
    }

    [HttpPost("login")]
    [Consumes("application/json")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalized = Domain.User.Normalize(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);

        var verification = user is null
            ? PasswordVerificationResult.Failed
            : passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (user is null || verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Failed login for {Email}", normalized);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Login failed.",
                detail: "Email or password is incorrect.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("User {UserId} signed in as {Role}", user.Id, user.Role);
        return Ok(CreateAuthResponse(user));
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = tokens.CreateToken(user);
        return new AuthResponse(token.AccessToken, "Bearer", token.ExpiresAt, UserResponse.From(user));
    }
}
