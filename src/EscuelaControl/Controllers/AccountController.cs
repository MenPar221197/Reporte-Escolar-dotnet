using EscuelaControl.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EscuelaControl.Controllers;

public class AccountController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Home") : View(new LoginForm { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var user = await users.FindByEmailAsync(form.Email.Trim());
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await signIn.PasswordSignInAsync(user, form.Password, false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return !string.IsNullOrWhiteSpace(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl)
                ? LocalRedirect(form.ReturnUrl) : RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "No se pudo iniciar sesión. Revisa tus datos o espera unos minutos si hubo varios intentos.");
        return View(form);
    }
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
    [HttpGet]
    public IActionResult CambiarClave() => View(new CambiarClaveForm());

    [HttpPost]
    public async Task<IActionResult> CambiarClave(CambiarClaveForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var user = await users.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await users.ChangePasswordAsync(user, form.Actual, form.Nueva);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "No se cambió la contraseña. Revisa la actual y usa una nueva de al menos 12 caracteres con mayúscula, minúscula, número y símbolo.");
            return View(form);
        }
        await signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Tu contraseña se actualizó.";
        return RedirectToAction("Index", "Home");
    }
}
