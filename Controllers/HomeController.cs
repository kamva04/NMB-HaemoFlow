using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;   // (kept — harmless duplicate)
using NMB_HLabSys.Data;
using NMB_HLabSys.Models;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;
using System.Diagnostics;

namespace NMB_HLabSys.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AuthService _auth;

        public HomeController(ILogger<HomeController> logger, AuthService auth)
        {
            _logger = logger;
            _auth = auth;
        }

        // =============================================================
        // LANDING PAGE  —  "/"
        //   • Logged-in users  → redirected to their role dashboard
        //   • Everyone else    → sees the marketing / welcome page
        // =============================================================
        public IActionResult Index()
        {
            if (_auth.IsLoggedIn())
                return RedirectToRoleDashboard();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

         //=============================================================
         //MODEL DUMP  —  temporary diagnostic endpoint
        // //=============================================================
        //[HttpGet]
        //public IActionResult _DumpModel([FromServices] ApplicationDbContext db)
        //{
        //    var sb = new System.Text.StringBuilder();
        //    var model = db.Model;

        //    // Enums first
        //    sb.AppendLine("=== ENUMS ===");
        //    var enumTypes = AppDomain.CurrentDomain.GetAssemblies()
        //        .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
        //        .Where(t => t.IsEnum && t.Namespace != null && t.Namespace.StartsWith("NMB_HLabSys"))
        //        .OrderBy(t => t.Name)
        //        .ToList();

        //    foreach (var e in enumTypes)
        //    {
        //        sb.AppendLine($"### {e.Name}");
        //        foreach (var name in Enum.GetNames(e))
        //            sb.AppendLine($"  - {name} = {(int)Enum.Parse(e, name)}");
        //    }
        //    sb.AppendLine();

        //    // Entities
        //    sb.AppendLine("=== ENTITIES ===");
        //    foreach (var entity in model.GetEntityTypes()
        //        .Where(e => e.ClrType.Namespace != null && e.ClrType.Namespace.StartsWith("NMB_HLabSys"))
        //        .OrderBy(e => e.ClrType.Name))
        //    {
        //        sb.AppendLine($"### {entity.ClrType.Name}");

        //        foreach (var prop in entity.GetProperties().OrderBy(p => p.Name))
        //        {
        //            var nullable = prop.IsNullable ? "?" : "";
        //            var maxLen = prop.GetMaxLength();
        //            var lenText = maxLen.HasValue ? $" max={maxLen}" : "";
        //            var key = prop.IsPrimaryKey() ? " [PK]" : "";
        //            var fk = prop.IsForeignKey() ? " [FK]" : "";
        //            sb.AppendLine($"  - {prop.Name}: {prop.ClrType.Name}{nullable}{lenText}{key}{fk}");
        //        }

        //        var navs = entity.GetNavigations().ToList();
        //        if (navs.Any())
        //        {
        //            sb.AppendLine("  -- navigations --");
        //            foreach (var nav in navs)
        //                sb.AppendLine($"    ~ {nav.Name} → {nav.TargetEntityType.ClrType.Name} ({(nav.IsCollection ? "many" : "one")})");
        //        }

        //        sb.AppendLine();
        //    }

        //    return Content(sb.ToString(), "text/plain");
        //}

        // =============================================================
        // HELPERS
        // =============================================================
        private IActionResult RedirectToRoleDashboard()
        {
            return _auth.GetCurrentRole() switch
            {
                "Manager" => RedirectToAction("Index", "Manager"),
                "Technician" => RedirectToAction("Dashboard", "Technician"),
                "Doctor" => RedirectToAction("Index", "Doctor"),
                "Patient" => RedirectToAction("Dashboard", "Patient"),
                "Admin" => RedirectToAction("Index", "Admin"),
                _ => View("Index")   // fallback: show the landing page
            };
        }
    }
}