using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Storefront.Web.Clients;
using Storefront.Web.Infrastructure;
using Storefront.Web.Models;

namespace Storefront.Web.Controllers;

/// <summary>Register and login through the gateway; the returned JWT goes into an HttpOnly cookie.</summary>
public sealed class AccountController(IIdentityClient identity, IWebHostEnvironment environment) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true ? RedirectToLocal(returnUrl) : View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var auth = await identity.LoginAsync(new LoginRequest { Email = model.Email, Password = model.Password }, cancellationToken);
            SignIn(auth);
            TempData.Success($"Welcome back, {auth.User.FullName}!");
            return auth.User.Role == UserRole.Admin && string.IsNullOrEmpty(model.ReturnUrl)
                ? RedirectToAction("Chaos", "Admin")
                : RedirectToLocal(model.ReturnUrl);
        }
        catch (GatewayApiException ex) when (ex.StatusCode is 400 or 401 or 429)
        {
            var error = ApiErrors.From(ex);
            ModelState.AddModelError(string.Empty, ex.StatusCode == 401 ? "Email or password is incorrect." : error.Message);
            ViewData["ErrorTitle"] = error.Title;
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var auth = await identity.RegisterAsync(new RegisterRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password
            }, cancellationToken);
            SignIn(auth);
            TempData.Success($"Your account is ready. Welcome, {auth.User.FullName}!");
            return RedirectToAction("Index", "Home");
        }
        catch (GatewayApiException ex) when (ex.StatusCode is 400 or 409)
        {
            var error = ApiErrors.From(ex);
            foreach (var (field, messages) in error.FieldErrors)
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(field, message);
                }
            }

            if (error.FieldErrors.Count == 0)
            {
                ModelState.AddModelError(ex.StatusCode == 409 ? nameof(model.Email) : string.Empty, error.Message);
            }

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        AuthCookie.Delete(Response);
        TempData.Info("You have been signed out.");
        return RedirectToAction("Index", "Home");
    }

    private void SignIn(AuthResponse auth) =>
        AuthCookie.Write(Response, auth.AccessToken, auth.ExpiresAt, secure: Request.IsHttps && !environment.IsDevelopment());

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}
