using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Filters
{
    /// <summary>
    /// Runs on every MVC action. If the logged-in user still has IsFirstLogin = true,
    /// every request is redirected to Account/ChangePassword until they change it —
    /// closing the gap where a first-login user could reach any page just by
    /// navigating there directly instead of going through the Login redirect.
    /// </summary>
    public class ForcePasswordChangeFilter : IActionFilter
    {
        private readonly AuthService _auth;

        // Actions a first-login user must still be able to reach.
        private static readonly (string Controller, string Action)[] Allowed =
        {
            ("Account", "ChangePassword"),
            ("Account", "Logout"),
            ("Account", "AccessDenied"),
        };

        public ForcePasswordChangeFilter(AuthService auth)
        {
            _auth = auth;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_auth.IsLoggedIn() || !_auth.IsFirstLogin())
                return;

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            foreach (var (c, a) in Allowed)
            {
                if (string.Equals(controller, c, System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(action, a, System.StringComparison.OrdinalIgnoreCase))
                    return;
            }

            context.Result = new RedirectToActionResult("ChangePassword", "Account", new { firstLogin = true });
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No-op
        }
    }
}
