using Microsoft.AspNetCore.Mvc;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _auth;
        private readonly IConfiguration _configuration;

        public AccountController(AuthService auth, IConfiguration configuration)
        {
            _auth = auth;
            _configuration = configuration;
        }
      
        // ── LOGIN ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_auth.IsLoggedIn())
                return RedirectToDashboard();

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var (success, error) = _auth.Login(model.Email, model.Password);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Login failed.");
                return View(model);
            }

            // Force password change on first login
            if (_auth.IsFirstLogin())
                return RedirectToAction(nameof(ChangePassword), new { firstLogin = true });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToDashboard();
        }

        // ── LOGOUT ────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            _auth.Logout();
            return RedirectToAction(nameof(Login));
        }

        // ── CHANGE PASSWORD ───────────────────────────────────────
        [HttpGet]
        public IActionResult ChangePassword(bool firstLogin = false)
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction(nameof(Login));
            ViewBag.FirstLogin = firstLogin;
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model, bool firstLogin = false)
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!ModelState.IsValid) { ViewBag.FirstLogin = firstLogin; return View(model); }

            var userId = _auth.GetCurrentUserId()!.Value;
            var (ok, err) = _auth.ChangePassword(userId, model.CurrentPassword, model.NewPassword);

            if (!ok)
            {
                ModelState.AddModelError(string.Empty, err ?? "Could not change password.");
                ViewBag.FirstLogin = firstLogin;
                return View(model);
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToDashboard();
        }

        // ── CHANGE EMAIL (USERNAME) ───────────────────────────────
        [HttpGet]
        public IActionResult ChangeEmail()
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction(nameof(Login));
            return View(new ChangeEmailViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeEmail(ChangeEmailViewModel model)
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction(nameof(Login));
            if (!ModelState.IsValid) return View(model);

            // Optional: extra server-side confirmation
            if (model.NewEmail != model.ConfirmEmail)
            {
                ModelState.AddModelError("ConfirmEmail", "Email addresses do not match.");
                return View(model);
            }

            var userId = _auth.GetCurrentUserId()!.Value;
            var (ok, err) = _auth.ChangeEmail(userId, model.CurrentPassword, model.NewEmail);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, err ?? "Could not update email.");
                return View(model);
            }

            TempData["Success"] = "Email address updated successfully.";
            return RedirectToDashboard();
        }
    

        // ── FORGOT PASSWORD ───────────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword()
            => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var token = _auth.CreatePasswordResetToken(model.Email);
            if (token != null)
            {
                var resetUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { email = model.Email, token },
                    Request.Scheme);
                if (string.IsNullOrEmpty(resetUrl))
                    throw new InvalidOperationException("Could not generate the password reset URL.");

                var publicOrigin = _configuration["PasswordReset:PublicOrigin"];
                if (!string.IsNullOrWhiteSpace(publicOrigin))
                {
                    if (!Uri.TryCreate(publicOrigin, UriKind.Absolute, out var origin) ||
                        (origin.Scheme != Uri.UriSchemeHttps && origin.Scheme != Uri.UriSchemeHttp))
                        throw new InvalidOperationException("PasswordReset:PublicOrigin must be an absolute HTTP or HTTPS origin.");

                    var resetUri = new UriBuilder(resetUrl)
                    {
                        Scheme = origin.Scheme,
                        Host = origin.Host,
                        Port = origin.IsDefaultPort ? -1 : origin.Port
                    };
                    resetUrl = resetUri.Uri.ToString();
                }

                _auth.SendPasswordResetEmail(model.Email, resetUrl);
            }

            TempData["Success"] = "If that email is registered, a reset link has been sent.";
            return RedirectToAction(nameof(Login));
        }
		


		// ── RESET PASSWORD ────────────────────────────────────────
		[HttpGet]
        public IActionResult ResetPassword(string token, string email)
            => View(new ResetPasswordViewModel { Token = token, Email = email });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

			var (ok, err) = _auth.ResetPassword(model.Email, model.Token, model.NewPassword);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, err ?? "Reset failed.");
                return View(model);
            }

            TempData["Success"] = "Password reset. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // ── ACCESS DENIED ─────────────────────────────────────────
        public IActionResult AccessDenied()
            => View();

        // ── HELPERS ───────────────────────────────────────────────
        private IActionResult RedirectToDashboard()
        {
            return _auth.GetCurrentRole() switch
            {
                "Manager" => RedirectToAction("Index", "Manager"),
                "Technician" => RedirectToAction("Dashboard", "Technician"),
                "Doctor" => RedirectToAction("Index", "Doctor"),
                "Patient" => RedirectToAction("Dashboard", "Patient"),
                "Admin" => RedirectToAction("Index", "Admin"),
                _ => RedirectToAction(nameof(Login))
            };
        }
    }
}