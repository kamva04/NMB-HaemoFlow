using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models.Domain;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NMB_HLabSys.Controllers
{
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _auth;
        private readonly EmailService _email;
        private readonly NotificationService _notif;                 
        private readonly PdfService _pdf;

        public ManagerController(
            ApplicationDbContext context,
            AuthService auth,
            EmailService email,
            NotificationService notif,
            PdfService pdf)                                         
        {
            _context = context;
            _auth = auth;
            _email = email;
            _notif = notif;                                          
            _pdf = pdf;
        }

       
        private IActionResult? RequireManager()
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!_auth.IsInRole("Manager")) return RedirectToAction("AccessDenied", "Account");
            return null;
        }

        // ─── DASHBOARD ─────────────────────────────────────────────
        public IActionResult Index()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var userId = _auth.GetCurrentUserId()!.Value;
            var managerName = _context.Managers
                .Where(m => m.UserId == userId)
                .Select(m => m.Name)
                .SingleOrDefault() ?? "Manager";

            var model = new ManagerDashboardViewModel
            {
                ManagerName = managerName,
                ActiveTestTypes = _context.TestTypes.Count(t => t.IsActive),
                PendingOrderCount = _context.StockOrders.Count(o => o.Status == "Ordered" || o.Status == "PartiallyComplete"),
                StaffCount = _context.Doctors.Count(d => !d.IsDeleted) + _context.Technicians.Count(t => !t.IsDeleted),
                CategoryCount = _context.TestCategories.Count(),
                SupplierCount = _context.Suppliers.Count(s => !s.IsDeleted),
                LowStockItems = _context.Consumables
                    .Where(c => c.IsActive && c.QuantityOnHand <= c.ReorderLevel * 1.1m)
                    .OrderBy(c => c.QuantityOnHand)
                    .Select(c => new ManagerLowStockItem
                    {
                        ConsumableId = c.ConsumableId,
                        ConsumableName = c.ConsumableName,
                        QuantityOnHand = c.QuantityOnHand,
                        ReorderLevel = c.ReorderLevel,
                        SupplierName = c.Supplier != null ? c.Supplier.SupplierName : "Not assigned"
                    })
                    .ToList()
            };
            model.StockAttentionCount = model.LowStockItems.Count;

            // ═══ NEW — supply-request + stock-problem alert counts ═══
            model.PendingSupplyRequestCount = _context.ConsumableRequests
                .Count(r => r.Status == "Pending");
            model.OpenStockProblemCount = _context.StockProblemReports
                .Count(r => r.Status == "Open" || r.Status == "Investigating");

            return View(model);
        }

        // ─── MANAGER PROFILE ───────────────────────────────────────
        [HttpGet]
        public IActionResult Profile()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var userId = _auth.GetCurrentUserId()!.Value;
            var manager = _context.Managers.Include(m => m.User).SingleOrDefault(m => m.UserId == userId);
            if (manager == null) return NotFound();

            return View(new ManagerProfileViewModel
            {
                Name = manager.Name,
                Surname = manager.Surname,
                Email = manager.User?.Email ?? string.Empty
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Profile(ManagerProfileViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var userId = _auth.GetCurrentUserId()!.Value;
            var manager = _context.Managers.Include(m => m.User).SingleOrDefault(m => m.UserId == userId);
            if (manager == null) return NotFound();

            model.Email = manager.User?.Email ?? string.Empty;
            if (!ModelState.IsValid) return View(model);

            manager.Name = model.Name.Trim();
            manager.Surname = model.Surname.Trim();
            _context.SaveChanges();
            TempData["Success"] = "Manager profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        // ═══════════════════════════════════════════════════════════
        // TEST CATEGORIES
        // ═══════════════════════════════════════════════════════════
        public IActionResult Categories()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            return View(_context.TestCategories.OrderBy(c => c.CategoryName).ToList());
        }

        [HttpGet]
        public IActionResult CreateCategory()
        {
            var g = RequireManager(); if (g != null) return g;
            return View(new TestCategory());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateCategory(TestCategory model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            if (_context.TestCategories.Any(c => c.CategoryName == model.CategoryName))
            {
                ModelState.AddModelError("CategoryName", "Category name already exists.");
                return View(model);
            }

            _context.TestCategories.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public IActionResult EditCategory(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var cat = _context.TestCategories.Find(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditCategory(TestCategory model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            _context.TestCategories.Update(model);
            _context.SaveChanges();
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public IActionResult CategoryCountsJson()
        {
            var guard = RequireManager(); if (guard != null) return Json(Array.Empty<object>());

            var data = _context.TestCategories
                .Select(tc => new
                {
                    categoryId = tc.CategoryId,
                    count = _context.TestTypes.Count(tt => tt.CategoryId == tc.CategoryId && tt.IsActive)
                })
                .ToList();
            return Json(data);
        }

        [HttpGet]
        public IActionResult CategoryReportJson(DateTime from, DateTime to)
        {
            var guard = RequireManager(); if (guard != null) return Json(Array.Empty<object>());
            if (from.Date > to.Date) return BadRequest("Choose a valid date range.");

            var fromDate = from.Date;
            var toDateExclusive = to.Date.AddDays(1);

            var data = _context.RequestTestTypes
                .Where(r => r.CompletedAt >= fromDate
                    && r.CompletedAt < toDateExclusive
                    && r.Status == "Verified")
                .GroupBy(r => r.TestType!.Category!.CategoryName)
                .Select(g => new { category = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total)
                .ToList();
            return Json(data);
        }

        [HttpGet]
        public IActionResult StockSummaryJson()
        {
            var guard = RequireManager(); if (guard != null) return Json(Array.Empty<object>());

            var data = _context.Consumables
                .Where(c => c.IsActive)
                .Include(c => c.Supplier)
                .OrderBy(c => c.QuantityOnHand)
                .Select(c => new
                {
                    name = c.ConsumableName,
                    onHand = c.QuantityOnHand,
                    reorderLevel = c.ReorderLevel,
                    supplier = c.Supplier!.SupplierName
                })
                .ToList();
            return Json(data);
        }

        [HttpGet]
        public IActionResult StaffSummaryJson()
        {
            var guard = RequireManager(); if (guard != null) return Json(Array.Empty<object>());

            var doctorCount = _context.Doctors.Count(d => !d.IsDeleted);
            var techCount = _context.Technicians.Count(t => !t.IsDeleted);
            var patientCount = _context.Patients.Count();

            var data = new[]
            {
                new { role = "Doctors", count = doctorCount, link = Url.Action(nameof(Doctors)) },
                new { role = "Technicians", count = techCount, link = Url.Action(nameof(Technicians)) },
                new { role = "Patients", count = patientCount, link = (string?)null }
            };
            return Json(data);
        }

        [HttpGet]
        public IActionResult RecentOrdersJson()
        {
            var guard = RequireManager(); if (guard != null) return Json(Array.Empty<object>());

            var data = _context.StockOrders
                .Include(o => o.Supplier)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderNumber,
                    supplierName = o.Supplier!.SupplierName,
                    o.OrderDate,
                    o.Status
                })
                .ToList();
            return Json(data);
        }

        // ═══════════════════════════════════════════════════════════
        // TEST TYPES
        // ═══════════════════════════════════════════════════════════
        public IActionResult TestTypes()
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var list = _context.TestTypes
                .Include(tt => tt.Category)
                .Include(tt => tt.SampleType)
                .OrderByDescending(tt => tt.IsActive)
                .ThenBy(tt => tt.Category!.CategoryName)
                .ThenBy(tt => tt.TestName)
                .ToList();

            ViewBag.AssignedTechnicians = _context.TechnicianTestTypes
                .Include(link => link.Technician)
                .AsNoTracking().ToList()
                .ToLookup(link => link.TestTypeId);
            return View(list);
        }

        [HttpGet]
        public IActionResult AssignTechnicians(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var test = _context.TestTypes.Find(id);
            if (test == null) return NotFound();

            return View(new TestTypeTechniciansViewModel
            {
                TestTypeId = test.TestTypeId,
                TestName = test.TestName,
                IsActive = test.IsActive,
                SelectedTechnicianIds = _context.TechnicianTestTypes
                    .Where(x => x.TestTypeId == id)
                    .Select(x => x.TechnicianId)
                    .ToList(),
                Technicians = _context.Technicians
                    .AsNoTracking()
                    .OrderBy(t => t.Surname).ThenBy(t => t.Name)
                    .ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AssignTechnicians(TestTypeTechniciansViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var test = _context.TestTypes.Find(model.TestTypeId);
            if (test == null) return NotFound();

            model.TestName = test.TestName;
            model.IsActive = test.IsActive;
            model.Technicians = _context.Technicians
                .AsNoTracking()
                .OrderBy(t => t.Surname).ThenBy(t => t.Name)
                .ToList();

            var selected = (model.SelectedTechnicianIds ?? new List<int>()).Distinct().ToHashSet();
            if (!test.IsActive)
                ModelState.AddModelError("", "Restore this test type before changing its assignments.");
            if (selected.Except(model.Technicians.Where(t => !t.IsDeleted).Select(t => t.TechnicianId)).Any())
                ModelState.AddModelError("", "A selected technician is no longer available. Review the selection and try again.");
            if (!ModelState.IsValid) return View(model);

            var existing = _context.TechnicianTestTypes
                .Where(x => x.TestTypeId == test.TestTypeId)
                .ToList();

            _context.TechnicianTestTypes.RemoveRange(
                existing.Where(x => model.Technicians.Any(t => t.TechnicianId == x.TechnicianId && !t.IsDeleted)
                                    && !selected.Contains(x.TechnicianId)));

            var assigned = existing.Select(x => x.TechnicianId).ToHashSet();
            foreach (var technicianId in selected.Except(assigned))
                _context.TechnicianTestTypes.Add(new TechnicianTestType { TestTypeId = test.TestTypeId, TechnicianId = technicianId });

            _context.SaveChanges();
            TempData["Success"] = "Technician assignments updated.";
            return RedirectToAction(nameof(TestTypes));
        }

        [HttpGet]
        public IActionResult CreateTestType()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            return View(new TestTypeCreateViewModel
            {
                Categories = GetCategories(),
                SampleTypes = GetSampleTypes(),
                AvailableConsumables = GetConsumables()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateTestType(TestTypeCreateViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            model.Categories = GetCategories();
            model.SampleTypes = GetSampleTypes();
            model.AvailableConsumables = GetConsumables();

            if (!_context.SampleTypes.Any(s => s.SampleTypeId == model.SampleTypeId && !s.IsDeleted))
                ModelState.AddModelError("SampleTypeId", "Choose a sample type that has not been archived.");

            if (!ModelState.IsValid) return View(model);

            if (_context.TestTypes.Any(tt => tt.TestName == model.TestName))
            {
                ModelState.AddModelError("TestName", "Test name already exists.");
                return View(model);
            }

            var testType = new TestType
            {
                CategoryId = model.CategoryId,
                TestName = model.TestName,
                SampleTypeId = model.SampleTypeId,
                Units = model.Units,
                NormalRangeMin = model.NormalRangeMin,
                NormalRangeMax = model.NormalRangeMax,
                TurnaroundMinutes = model.TurnaroundMinutes,
                IsActive = true
            };

            _context.TestTypes.Add(testType);
            _context.SaveChanges();

            foreach (var cId in model.SelectedConsumableIds ?? new List<int>())
            {
                _context.TestTypeConsumables.Add(new TestTypeConsumable
                {
                    TestTypeId = testType.TestTypeId,
                    ConsumableId = cId
                });
            }
            _context.SaveChanges();

            TempData["Success"] = "Test type created.";
            return RedirectToAction(nameof(TestTypes));
        }

        [HttpGet]
        public IActionResult EditTestType(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var tt = _context.TestTypes
                .Include(t => t.TestTypeConsumables)
                .FirstOrDefault(t => t.TestTypeId == id);
            if (tt == null) return NotFound();

            var model = new TestTypeCreateViewModel
            {
                TestName = tt.TestName,
                CategoryId = tt.CategoryId,
                SampleTypeId = tt.SampleTypeId,
                Units = tt.Units,
                NormalRangeMin = tt.NormalRangeMin,
                NormalRangeMax = tt.NormalRangeMax,
                TurnaroundMinutes = tt.TurnaroundMinutes,
                SelectedConsumableIds = tt.TestTypeConsumables.Select(tc => tc.ConsumableId).ToList()
            };
            model.Categories = GetCategories();
            model.SampleTypes = GetSampleTypes();
            model.AvailableConsumables = GetConsumables();

            ViewBag.TestTypeId = id;
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditTestType(TestTypeCreateViewModel model, int testTypeId)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            model.Categories = GetCategories();
            model.SampleTypes = GetSampleTypes();
            model.AvailableConsumables = GetConsumables();

            if (!_context.SampleTypes.Any(s => s.SampleTypeId == model.SampleTypeId && !s.IsDeleted))
                ModelState.AddModelError("SampleTypeId", "Choose a sample type that has not been archived.");

            if (!ModelState.IsValid)
            {
                ViewBag.TestTypeId = testTypeId;
                return View(model);
            }

            var tt = _context.TestTypes.Find(testTypeId);
            if (tt == null) return NotFound();

            tt.CategoryId = model.CategoryId;
            tt.TestName = model.TestName;
            tt.SampleTypeId = model.SampleTypeId;
            tt.Units = model.Units;
            tt.NormalRangeMin = model.NormalRangeMin;
            tt.NormalRangeMax = model.NormalRangeMax;
            tt.TurnaroundMinutes = model.TurnaroundMinutes;

            var existing = _context.TestTypeConsumables.Where(tc => tc.TestTypeId == testTypeId);
            _context.TestTypeConsumables.RemoveRange(existing);

            foreach (var cId in model.SelectedConsumableIds ?? new List<int>())
            {
                _context.TestTypeConsumables.Add(new TestTypeConsumable
                {
                    TestTypeId = testTypeId,
                    ConsumableId = cId
                });
            }
            _context.SaveChanges();

            TempData["Success"] = "Test type updated.";
            return RedirectToAction(nameof(TestTypes));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult SoftDeleteTestType(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var tt = _context.TestTypes.Find(id);
            if (tt != null) { tt.IsActive = false; _context.SaveChanges(); }
            TempData["Success"] = "Test type deactivated.";
            return RedirectToAction(nameof(TestTypes));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreTestType(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var tt = _context.TestTypes.Find(id);
            if (tt != null) { tt.IsActive = true; _context.SaveChanges(); }
            TempData["Success"] = "Test type restored.";
            return RedirectToAction(nameof(TestTypes));
        }

        // ═══════════════════════════════════════════════════════════
        // CONSUMABLES
        // ═══════════════════════════════════════════════════════════
        public IActionResult Consumables()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var list = _context.Consumables
                .Include(c => c.Supplier)
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.ConsumableName)
                .ToList();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateConsumable()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            ViewBag.Suppliers = GetSuppliers();
            return View(new Consumable());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateConsumable(Consumable model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            ViewBag.Suppliers = GetSuppliers();

            if (!_context.Suppliers.Any(s => s.SupplierId == model.SupplierId && !s.IsDeleted))
                ModelState.AddModelError("SupplierId", "Choose a supplier that has not been archived.");

            if (!ModelState.IsValid) return View(model);

            model.IsActive = true;
            _context.Consumables.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Consumable added.";
            return RedirectToAction(nameof(Consumables));
        }

        [HttpGet]
        public IActionResult EditConsumable(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var c = _context.Consumables.Find(id);
            if (c == null) return NotFound();
            ViewBag.Suppliers = GetSuppliers();
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditConsumable(Consumable model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            ViewBag.Suppliers = GetSuppliers();

            if (!_context.Suppliers.Any(s => s.SupplierId == model.SupplierId && !s.IsDeleted))
                ModelState.AddModelError("SupplierId", "Choose a supplier that has not been archived.");

            if (!ModelState.IsValid) return View(model);

            _context.Consumables.Update(model);
            _context.SaveChanges();
            TempData["Success"] = "Consumable updated.";
            return RedirectToAction(nameof(Consumables));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteConsumable(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var c = _context.Consumables.Find(id);
            if (c != null) { c.IsActive = false; _context.SaveChanges(); }
            TempData["Success"] = "Consumable deactivated.";
            return RedirectToAction(nameof(Consumables));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreConsumable(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var c = _context.Consumables.Find(id);
            if (c == null) return NotFound();
            c.IsActive = true;
            _context.SaveChanges();
            TempData["Success"] = "Consumable restored.";
            return RedirectToAction(nameof(Consumables));
        }

        [HttpGet]
        public IActionResult AdjustStock(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var c = _context.Consumables.Find(id);
            if (c == null) return NotFound();

            return View(new AdjustStockViewModel
            {
                ConsumableId = c.ConsumableId,
                ConsumableName = c.ConsumableName,
                CurrentQuantity = c.QuantityOnHand
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AdjustStock(AdjustStockViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            var c = _context.Consumables.Find(model.ConsumableId);
            if (c == null) return NotFound();

            switch (model.AdjustmentType)
            {
                case "Increase": c.QuantityOnHand += model.Amount; break;
                case "Decrease": c.QuantityOnHand = Math.Max(0, c.QuantityOnHand - model.Amount); break;
                default: c.QuantityOnHand = model.Amount; break;
            }
            _context.SaveChanges();
            TempData["Success"] = "Stock adjusted.";
            return RedirectToAction(nameof(Consumables));
        }

        // ═══════════════════════════════════════════════════════════
        // SUPPLIERS
        // ═══════════════════════════════════════════════════════════
        public IActionResult Suppliers()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            return View(_context.Suppliers.OrderBy(s => s.SupplierName).ToList());
        }

        [HttpGet]
        public IActionResult CreateSupplier()
        {
            var g = RequireManager(); if (g != null) return g;
            return View(new Supplier());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateSupplier(Supplier model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            model.IsDeleted = false;
            _context.Suppliers.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Supplier created.";
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpGet]
        public IActionResult EditSupplier(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var s = _context.Suppliers.Find(id);
            if (s == null) return NotFound();
            return View(s);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditSupplier(Supplier model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            var existing = _context.Suppliers.Find(model.SupplierId);
            if (existing == null) return NotFound();

            existing.SupplierName = model.SupplierName;
            existing.ContactPerson = model.ContactPerson;
            existing.Email = model.Email;
            _context.SaveChanges();
            TempData["Success"] = "Supplier updated.";
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteSupplier(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var s = _context.Suppliers.Find(id);
            if (s != null) { s.IsDeleted = true; _context.SaveChanges(); }
            TempData["Success"] = "Supplier archived. Existing records have been preserved.";
            return RedirectToAction(nameof(Suppliers));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreSupplier(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var item = _context.Suppliers.Find(id);
            if (item == null) return NotFound();
            item.IsDeleted = false;
            _context.SaveChanges();
            TempData["Success"] = "Supplier restored.";
            return RedirectToAction(nameof(Suppliers));
        }

        // ═══════════════════════════════════════════════════════════
        // STOCK ORDERS
        // ═══════════════════════════════════════════════════════════
        public IActionResult Orders()
        {
            var guard = RequireManager(); if (guard != null) return guard;

            // ═══ FIXED — no orphaned navigation in Select ═══
            var list = _context.StockOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            ViewBag.LowStockCount = _context.Consumables
                .Count(c => c.IsActive && c.QuantityOnHand <= c.ReorderLevel * 1.1m);

            return View(list);
        }

        [HttpGet]
        public IActionResult CreateOrder()
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var availableConsumables = _context.Consumables
                .Include(c => c.Supplier)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ConsumableName)
                .ToList();

            return View(new CreateOrderViewModel
            {
                Suppliers = GetSuppliers(),
                LowStockConsumables = availableConsumables,
                OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}"
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateOrder(CreateOrderViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            model.Suppliers = GetSuppliers();
            model.LowStockConsumables = _context.Consumables.Where(c => c.IsActive).ToList();

            if (!_context.Suppliers.Any(s => s.SupplierId == model.SupplierId && !s.IsDeleted))
                ModelState.AddModelError("SupplierId", "Choose a supplier that has not been archived.");

            var lines = model.Lines?.Where(l => l.QuantityOrdered > 0).ToList() ?? new List<OrderLineViewModel>();
            if (lines.Any(l => !_context.Consumables.Any(c => c.ConsumableId == l.ConsumableId && c.IsActive)))
                ModelState.AddModelError("", "Choose active consumables only.");
            if (!lines.Any())
                ModelState.AddModelError("", "Add at least one item.");

            if (!ModelState.IsValid) return View(model);

            var order = new StockOrder
            {
                OrderNumber = model.OrderNumber,
                SupplierId = model.SupplierId,
                OrderDate = DateTime.Now,
                Status = "Ordered",
                Items = lines.Select(l => new StockOrderItem
                {
                    ConsumableId = l.ConsumableId,
                    QuantityOrdered = l.QuantityOrdered,
                    Status = "Ordered"
                }).ToList()
            };

            _context.StockOrders.Add(order);
            _context.SaveChanges();

            var supplier = _context.Suppliers.Find(model.SupplierId);
            if (supplier != null && !string.IsNullOrEmpty(supplier.Email))
            {
                var body = $"<h3>New Stock Order: {model.OrderNumber}</h3><ul>" +
                    string.Join("", lines.Select(l => $"<li>{l.ConsumableName}: {l.QuantityOrdered} units</li>")) + "</ul>";
                try { _email.Send(supplier.Email, $"Stock Order {model.OrderNumber}", body); } catch { }
            }

            TempData["Success"] = "Order placed and supplier notified.";
            return RedirectToAction(nameof(OrderDetails), new { id = order.OrderId });
        }

        public IActionResult OrderDetails(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var order = _context.StockOrders
                .Include(o => o.Supplier)
                .Include(o => o.Items).ThenInclude(i => i.Consumable)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ReceiveOrderItem(int orderItemId)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var item = _context.StockOrderItems
                .Include(i => i.StockOrder)
                .FirstOrDefault(i => i.OrderItemId == orderItemId);
            if (item == null) return NotFound();

            item.Status = "Received";
            item.DateReceived = DateTime.Now;

            var consumable = _context.Consumables.Find(item.ConsumableId);
            if (consumable != null)
                consumable.QuantityOnHand += item.QuantityOrdered;

            _context.SaveChanges();
            UpdateOrderStatus(item.StockOrder!.OrderId);

            TempData["Success"] = "Item received – stock updated.";
            return RedirectToAction(nameof(OrderDetails), new { id = item.StockOrder.OrderId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelOrderItem(int orderItemId, string reason)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var item = _context.StockOrderItems
                .Include(i => i.StockOrder)
                .FirstOrDefault(i => i.OrderItemId == orderItemId);
            if (item == null) return NotFound();

            item.Status = "Cancelled";
            item.CancelReason = reason;
            item.DateCancelled = DateTime.Now;
            _context.SaveChanges();
            UpdateOrderStatus(item.StockOrder!.OrderId);

            TempData["Success"] = "Item cancelled.";
            return RedirectToAction(nameof(OrderDetails), new { id = item.StockOrder.OrderId });
        }

        // ═══════════════════════════════════════════════════════════
        // SUPPLY REQUEST APPROVALS (Phase 5 — new)
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult SupplyRequests(string? status)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var q = _context.ConsumableRequests
                .AsNoTracking()
                .Include(r => r.Consumable)
                .Include(r => r.Technician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                q = q.Where(r => r.Status == status);

            var list = q.OrderByDescending(r => r.RequestedAt).ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = _context.ConsumableRequests.Count(r => r.Status == "Pending");
            ViewBag.ApprovedCount = _context.ConsumableRequests.Count(r => r.Status == "Approved" || r.Status == "PartiallyApproved" || r.Status == "Fulfilled");
            ViewBag.RejectedCount = _context.ConsumableRequests.Count(r => r.Status == "Rejected");
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ApproveSupplyRequest(int requestId, string? decisionNote)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var req = _context.ConsumableRequests
                .Include(r => r.Consumable)
                .Include(r => r.Technician)
                .FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return NotFound();
            if (req.Status != "Pending")
            {
                TempData["Error"] = "This request is no longer pending.";
                return RedirectToAction(nameof(SupplyRequests));
            }

            req.Status = "Approved";
            req.QuantityApproved = req.QuantityRequested;
            req.DecisionAt = DateTime.Now;
            req.ManagerDecisionNote = decisionNote?.Trim();
            _context.SaveChanges();

            // Notify the requesting technician (does NOT touch stock — spec rule)
            if (req.Technician != null)
            {
                _notif.Notify(
                    req.Technician.UserId,
                    "Supply",
                    $"Supply Request Approved: {req.RequestNumber}",
                    $"Your supply request {req.RequestNumber} for {req.Consumable?.ConsumableName} was approved. " +
                    $"Stock will be issued via Adjust Stock.",
                    Url.Action("MySupplyRequests", "Technician"));
            }

            TempData["Success"] = $"Request {req.RequestNumber} approved.";
            return RedirectToAction(nameof(SupplyRequests));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult PartiallyApproveSupplyRequest(int requestId, int quantityApproved, string? decisionNote)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var req = _context.ConsumableRequests
                .Include(r => r.Consumable)
                .Include(r => r.Technician)
                .FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return NotFound();
            if (req.Status != "Pending")
            {
                TempData["Error"] = "This request is no longer pending.";
                return RedirectToAction(nameof(SupplyRequests));
            }
            if (quantityApproved <= 0 || quantityApproved > req.QuantityRequested)
            {
                TempData["Error"] = $"Quantity approved must be between 1 and {req.QuantityRequested}.";
                return RedirectToAction(nameof(SupplyRequests));
            }

            req.Status = "PartiallyApproved";
            req.QuantityApproved = quantityApproved;
            req.DecisionAt = DateTime.Now;
            req.ManagerDecisionNote = decisionNote?.Trim();
            _context.SaveChanges();

            if (req.Technician != null)
            {
                _notif.Notify(
                    req.Technician.UserId,
                    "Supply",
                    $"Supply Request Partially Approved: {req.RequestNumber}",
                    $"Your request {req.RequestNumber} was partially approved: {quantityApproved} of {req.QuantityRequested} unit(s) of {req.Consumable?.ConsumableName}.",
                    Url.Action("MySupplyRequests", "Technician"));
            }

            TempData["Success"] = $"Request {req.RequestNumber} partially approved ({quantityApproved}/{req.QuantityRequested}).";
            return RedirectToAction(nameof(SupplyRequests));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RejectSupplyRequest(int requestId, string reason)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "A reason is required to reject a supply request.";
                return RedirectToAction(nameof(SupplyRequests));
            }

            var req = _context.ConsumableRequests
                .Include(r => r.Consumable)
                .Include(r => r.Technician)
                .FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return NotFound();
            if (req.Status != "Pending")
            {
                TempData["Error"] = "This request is no longer pending.";
                return RedirectToAction(nameof(SupplyRequests));
            }

            req.Status = "Rejected";
            req.DecisionAt = DateTime.Now;
            req.ManagerDecisionNote = reason.Trim();
            _context.SaveChanges();

            if (req.Technician != null)
            {
                _notif.Notify(
                    req.Technician.UserId,
                    "Supply",
                    $"Supply Request Rejected: {req.RequestNumber}",
                    $"Your request {req.RequestNumber} was rejected. Reason: {reason.Trim()}",
                    Url.Action("MySupplyRequests", "Technician"));
            }

            TempData["Success"] = $"Request {req.RequestNumber} rejected.";
            return RedirectToAction(nameof(SupplyRequests));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult FulfillSupplyRequest(int requestId)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var req = _context.ConsumableRequests
                .Include(r => r.Technician)
                .FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return NotFound();

            if (req.Status != "Approved" && req.Status != "PartiallyApproved")
            {
                TempData["Error"] = "Only approved requests can be marked as fulfilled.";
                return RedirectToAction(nameof(SupplyRequests));
            }

            req.Status = "Fulfilled";
            req.QuantityFulfilled = req.QuantityApproved ?? req.QuantityRequested;
            req.DecisionAt = DateTime.Now;
            _context.SaveChanges();

            if (req.Technician != null)
            {
                _notif.Notify(
                    req.Technician.UserId,
                    "Supply",
                    $"Supply Request Fulfilled: {req.RequestNumber}",
                    $"Your request {req.RequestNumber} has been marked fulfilled.",
                    Url.Action("MySupplyRequests", "Technician"));
            }

            TempData["Success"] = $"Request {req.RequestNumber} marked fulfilled.";
            return RedirectToAction(nameof(SupplyRequests));
        }

        // ═══════════════════════════════════════════════════════════
        // STOCK PROBLEM REPORTS (Phase 5 — new)
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult StockProblems(string? status)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var q = _context.StockProblemReports
                .AsNoTracking()
                .Include(r => r.Consumable)
                .Include(r => r.Technician)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
                q = q.Where(r => r.Status == status);

            var list = q.OrderByDescending(r => r.ReportedAt).ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.OpenCount = _context.StockProblemReports.Count(r => r.Status == "Open");
            ViewBag.InvestigatingCount = _context.StockProblemReports.Count(r => r.Status == "Investigating");
            ViewBag.ResolvedCount = _context.StockProblemReports.Count(r => r.Status == "Resolved");
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UpdateStockProblemStatus(int reportId, string newStatus, string? note)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            if (newStatus != "Open" && newStatus != "Investigating" && newStatus != "Resolved")
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(StockProblems));
            }

            var report = _context.StockProblemReports
                .Include(r => r.Consumable)
                .Include(r => r.Technician)
                .FirstOrDefault(r => r.ReportId == reportId);
            if (report == null) return NotFound();

            report.Status = newStatus;
            // Persist manager note inside Description if provided (no schema column for notes)
            if (!string.IsNullOrWhiteSpace(note))
            {
                report.Description += $"\n[Manager · {DateTime.Now:dd MMM yyyy HH:mm} · {newStatus}] {note.Trim()}";
            }
            _context.SaveChanges();

            if (report.Technician != null)
            {
                _notif.Notify(
                    report.Technician.UserId,
                    "Stock",
                    $"Stock Problem {newStatus}",
                    $"The stock problem you reported for {report.Consumable?.ConsumableName} is now {newStatus}.",
                    Url.Action("ReportStockProblem", "Technician"));
            }

            TempData["Success"] = $"Stock problem #{report.ReportId} set to {newStatus}.";
            return RedirectToAction(nameof(StockProblems));
        }

        // ═══════════════════════════════════════════════════════════
        // DOCTORS
        // ═══════════════════════════════════════════════════════════
        public IActionResult Doctors()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var list = _context.Doctors.Include(d => d.User).OrderBy(d => d.Surname).ToList();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ArchiveDoctor(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var doctor = _context.Doctors.Find(id);
            if (doctor == null) return NotFound();
            if (doctor.IsDeleted)
            {
                TempData["Success"] = "Doctor is already archived.";
                return RedirectToAction(nameof(Doctors));
            }

            doctor.IsDeleted = true;
            _context.SaveChanges();
            TempData["Success"] = "Doctor archived. History has been preserved and sign-in is disabled.";
            return RedirectToAction(nameof(Doctors));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreDoctor(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var doctor = _context.Doctors.Find(id);
            if (doctor == null) return NotFound();
            doctor.IsDeleted = false;
            _context.SaveChanges();
            TempData["Success"] = "Doctor restored. They can sign in again.";
            return RedirectToAction(nameof(Doctors));
        }

        [HttpGet]
        public IActionResult CreateDoctor()
        {
            var g = RequireManager(); if (g != null) return g;
            return View(new DoctorCreateViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateDoctor(DoctorCreateViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            var tempPw = _auth.GenerateTemporaryPassword();
            var pwHash = _auth.HashPassword(tempPw);

            var user = new User
            {
                Email = model.Email,
                PasswordHash = pwHash,
                Role = "Doctor",
                IsFirstLogin = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var doctor = new Doctor
            {
                UserId = user.UserId,
                Name = model.Name,
                Surname = model.Surname,
                HPCSANumber = model.HPCSANumber,
                ContactNumber = model.ContactNumber
            };
            _context.Doctors.Add(doctor);
            _context.SaveChanges();

            _email.Send(model.Email, "NMB-HLabSys – Your Account",
                $"<p>Welcome Dr {model.Surname}. Temporary password: <strong>{tempPw}</strong>. Please change on first login.</p>");

            TempData["Success"] = $"Doctor created. Password e-mailed to {model.Email}.";
            return RedirectToAction(nameof(Doctors));
        }

        [HttpGet]
        public IActionResult EditDoctor(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;

            var doctor = _context.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorId == id);
            if (doctor == null) return NotFound();

            var vm = new DoctorCreateViewModel
            {
                Name = doctor.Name,
                Surname = doctor.Surname,
                HPCSANumber = doctor.HPCSANumber,
                Email = doctor.User!.Email,
                ContactNumber = doctor.ContactNumber
            };
            ViewBag.DoctorId = id;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditDoctor(DoctorCreateViewModel model, int doctorId)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) { ViewBag.DoctorId = doctorId; return View(model); }

            var doctor = _context.Doctors.Include(d => d.User).FirstOrDefault(d => d.DoctorId == doctorId);
            if (doctor == null) return NotFound();

            doctor.Name = model.Name;
            doctor.Surname = model.Surname;
            doctor.HPCSANumber = model.HPCSANumber;
            doctor.ContactNumber = model.ContactNumber;
            doctor.User!.Email = model.Email;

            _context.SaveChanges();
            TempData["Success"] = "Doctor updated.";
            return RedirectToAction(nameof(Doctors));
        }

        // ═══════════════════════════════════════════════════════════
        // TECHNICIANS
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult AssignTestTypes(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var technician = _context.Technicians.Find(id);
            if (technician == null) return NotFound();

            return View(new TechnicianAssignmentsViewModel
            {
                TechnicianId = id,
                IsArchived = technician.IsDeleted,
                TechnicianName = technician.Name + " " + technician.Surname,
                EmployeeNumber = technician.EmployeeNumber,
                SelectedTestTypeIds = _context.TechnicianTestTypes
                    .Where(x => x.TechnicianId == id)
                    .Select(x => x.TestTypeId)
                    .ToList(),
                TestTypes = _context.TestTypes
                    .AsNoTracking()
                    .OrderByDescending(t => t.IsActive).ThenBy(t => t.TestName)
                    .ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AssignTestTypes(TechnicianAssignmentsViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var technician = _context.Technicians.Find(model.TechnicianId);
            if (technician == null) return NotFound();

            model.IsArchived = technician.IsDeleted;
            if (technician.IsDeleted)
                ModelState.AddModelError("", "Restore this technician before changing assignments.");

            model.TechnicianName = technician.Name + " " + technician.Surname;
            model.EmployeeNumber = technician.EmployeeNumber;
            model.TestTypes = _context.TestTypes
                .AsNoTracking()
                .OrderByDescending(t => t.IsActive).ThenBy(t => t.TestName)
                .ToList();

            var selected = (model.SelectedTestTypeIds ?? new List<int>()).Distinct().ToHashSet();
            var active = model.TestTypes.Where(t => t.IsActive).Select(t => t.TestTypeId).ToHashSet();
            var existing = _context.TechnicianTestTypes
                .Where(x => x.TechnicianId == model.TechnicianId)
                .ToList();

            if (selected.Except(active).Any())
                ModelState.AddModelError("", "Select only active test types. An item may have been deactivated; review your selection.");

            if (!ModelState.IsValid) return View(model);

            _context.TechnicianTestTypes.RemoveRange(
                existing.Where(x => active.Contains(x.TestTypeId) && !selected.Contains(x.TestTypeId)));

            var assigned = existing.Select(x => x.TestTypeId).ToHashSet();
            foreach (var testTypeId in selected.Except(assigned))
                _context.TechnicianTestTypes.Add(new TechnicianTestType
                {
                    TechnicianId = model.TechnicianId,
                    TestTypeId = testTypeId
                });

            _context.SaveChanges();
            TempData["Success"] = "Technician test-type assignments updated.";
            return RedirectToAction(nameof(AssignTestTypes), new { id = model.TechnicianId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ArchiveTechnician(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var technician = _context.Technicians.Find(id);
            if (technician == null) return NotFound();
            technician.IsDeleted = true;
            _context.SaveChanges();
            TempData["Success"] = "Technician archived. History and assignments have been preserved.";
            return RedirectToAction(nameof(Technicians));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreTechnician(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var technician = _context.Technicians.Find(id);
            if (technician == null) return NotFound();
            technician.IsDeleted = false;
            _context.SaveChanges();
            TempData["Success"] = "Technician restored. They can sign in again.";
            return RedirectToAction(nameof(Technicians));
        }

        public IActionResult Technicians()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var list = _context.Technicians
                .Include(t => t.User)
                .Include(t => t.TechnicianTestTypes)
                .OrderBy(t => t.Surname)
                .ToList();
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateTechnician()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            return View(new TechnicianCreateViewModel { AvailableTestTypes = GetTestTypes() });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateTechnician(TechnicianCreateViewModel model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            model.AvailableTestTypes = GetTestTypes();
            if (!ModelState.IsValid) return View(model);

            var tempPw = _auth.GenerateTemporaryPassword();
            var pwHash = _auth.HashPassword(tempPw);

            var user = new User
            {
                Email = model.Email,
                PasswordHash = pwHash,
                Role = "Technician",
                IsFirstLogin = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var tech = new Technician
            {
                UserId = user.UserId,
                Name = model.Name,
                Surname = model.Surname,
                SAIDNumber = model.SAIDNumber,
                EmployeeNumber = model.EmployeeNumber,
                ContactNumber = model.ContactNumber,
                TechnicianTestTypes = model.SelectedTestTypeIds
                    .Select(ttId => new TechnicianTestType { TestTypeId = ttId })
                    .ToList()
            };
            _context.Technicians.Add(tech);
            _context.SaveChanges();

            _email.Send(model.Email, "NMB-HLabSys – Your Technician Account",
                $"<p>Welcome {model.Name}. Temporary password: <strong>{tempPw}</strong>. Change on first login.</p>");

            TempData["Success"] = "Technician created.";
            return RedirectToAction(nameof(Technicians));
        }

        [HttpGet]
        public IActionResult EditTechnician(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var tech = _context.Technicians
                .Include(t => t.User)
                .Include(t => t.TechnicianTestTypes)
                .FirstOrDefault(t => t.TechnicianId == id);

            if (tech == null) return NotFound();
            if (tech.IsDeleted)
            {
                TempData["Error"] = "Restore the technician before editing their details.";
                return RedirectToAction(nameof(Technicians));
            }

            var vm = new TechnicianCreateViewModel
            {
                Name = tech.Name,
                Surname = tech.Surname,
                SAIDNumber = tech.SAIDNumber,
                EmployeeNumber = tech.EmployeeNumber,
                Email = tech.User!.Email,
                ContactNumber = tech.ContactNumber,
                SelectedTestTypeIds = tech.TechnicianTestTypes.Select(tt => tt.TestTypeId).ToList(),
                AvailableTestTypes = GetTestTypes()
            };
            ViewBag.TechnicianId = id;
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditTechnician(TechnicianCreateViewModel model, int technicianId)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            model.AvailableTestTypes = GetTestTypes();
            if (!ModelState.IsValid) { ViewBag.TechnicianId = technicianId; return View(model); }

            var tech = _context.Technicians
                .Include(t => t.User)
                .Include(t => t.TechnicianTestTypes)
                .FirstOrDefault(t => t.TechnicianId == technicianId);

            if (tech == null) return NotFound();
            if (tech.IsDeleted)
            {
                TempData["Error"] = "Restore the technician before editing their details.";
                return RedirectToAction(nameof(Technicians));
            }

            tech.Name = model.Name;
            tech.Surname = model.Surname;
            tech.EmployeeNumber = model.EmployeeNumber;
            tech.ContactNumber = model.ContactNumber;
            tech.User!.Email = model.Email;

            tech.TechnicianTestTypes.Clear();
            foreach (var ttId in model.SelectedTestTypeIds ?? new List<int>())
                tech.TechnicianTestTypes.Add(new TechnicianTestType { TestTypeId = ttId });

            _context.SaveChanges();
            TempData["Success"] = "Technician updated.";
            return RedirectToAction(nameof(Technicians));
        }

        // ═══════════════════════════════════════════════════════════
        // SAMPLE TYPES
        // ═══════════════════════════════════════════════════════════
        public IActionResult SampleTypes()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            return View(_context.SampleTypes.OrderBy(s => s.TypeName).ToList());
        }

        [HttpGet]
        public IActionResult EditSampleType(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var sample = _context.SampleTypes.Find(id);
            if (sample == null) return NotFound();
            if (sample.IsDeleted)
            {
                TempData["Error"] = "Restore this sample type before editing it.";
                return RedirectToAction(nameof(SampleTypes));
            }
            return View(sample);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditSampleType([Bind("SampleTypeId,TypeName")] SampleType model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var sample = _context.SampleTypes.Find(model.SampleTypeId);
            if (sample == null) return NotFound();
            if (sample.IsDeleted)
            {
                TempData["Error"] = "Restore this sample type before editing it.";
                return RedirectToAction(nameof(SampleTypes));
            }

            model.TypeName = (model.TypeName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(model.TypeName))
                ModelState.AddModelError(nameof(model.TypeName), "Enter a sample type name.");
            else if (_context.SampleTypes.Any(s => s.SampleTypeId != model.SampleTypeId && s.TypeName.Trim().ToUpper() == model.TypeName.ToUpper()))
                ModelState.AddModelError(nameof(model.TypeName), "That sample type name already exists, including archived records.");

            if (!ModelState.IsValid) return View(model);

            sample.TypeName = model.TypeName;
            _context.SaveChanges();
            TempData["Success"] = "Sample type updated.";
            return RedirectToAction(nameof(SampleTypes));
        }

        [HttpGet]
        public IActionResult CreateSampleType()
        {
            var guard = RequireManager(); if (guard != null) return guard;
            return View(new SampleType());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateSampleType(SampleType model)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid) return View(model);

            if (_context.SampleTypes.Any(s => s.TypeName == model.TypeName))
            {
                ModelState.AddModelError("TypeName", "That sample type already exists.");
                return View(model);
            }

            model.IsDeleted = false;
            _context.SampleTypes.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Sample type added.";
            return RedirectToAction(nameof(SampleTypes));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DeleteSampleType(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var st = _context.SampleTypes.Find(id);
            if (st != null) { st.IsDeleted = true; _context.SaveChanges(); }
            TempData["Success"] = "Sample type archived. Existing records have been preserved.";
            return RedirectToAction(nameof(SampleTypes));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreSampleType(int id)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            var item = _context.SampleTypes.Find(id);
            if (item == null) return NotFound();
            item.IsDeleted = false;
            _context.SaveChanges();
            TempData["Success"] = "Sample type restored.";
            return RedirectToAction(nameof(SampleTypes));
        }

        // ═══════════════════════════════════════════════════════════
        // REPORTS
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Reports()
        {
            var g = RequireManager(); if (g != null) return g;
            return View(new ReportFilterViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TestsByCategory(ReportFilterViewModel filter)
        {
            var guard = RequireManager(); if (guard != null) return guard;
            if (!ModelState.IsValid || filter.FromDate.Date > filter.ToDate.Date)
            {
                TempData["Error"] = "Choose a valid date range.";
                return RedirectToAction(nameof(Reports));
            }

            var pdfBytes = _pdf.GenerateTestsByCategoryReport(filter.FromDate, filter.ToDate);
            return File(pdfBytes, "application/pdf",
                $"NMB_TestsByCategory_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
        }

        // ═══════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════
        private List<TestCategory> GetCategories()
            => _context.TestCategories.OrderBy(c => c.CategoryName).ToList();

        private List<SampleType> GetSampleTypes()
            => _context.SampleTypes.Where(s => !s.IsDeleted).OrderBy(s => s.TypeName).ToList();

        private List<Consumable> GetConsumables()
            => _context.Consumables.Where(c => c.IsActive).OrderBy(c => c.ConsumableName).ToList();

        private List<Supplier> GetSuppliers()
            => _context.Suppliers.Where(s => !s.IsDeleted).OrderBy(s => s.SupplierName).ToList();

        private List<TestType> GetTestTypes()
            => _context.TestTypes.Where(tt => tt.IsActive).OrderBy(tt => tt.TestName).ToList();

        private void UpdateOrderStatus(int orderId)
        {
            var order = _context.StockOrders.Include(o => o.Items).FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return;

            var total = order.Items.Count;
            var received = order.Items.Count(i => i.Status == "Received");
            var cancelled = order.Items.Count(i => i.Status == "Cancelled");

            string newStatus;
            if (received + cancelled == total)
                newStatus = (received == total) ? "Complete" : "PartiallyComplete";
            else
                newStatus = (received > 0) ? "PartiallyComplete" : "Ordered";

            order.Status = newStatus;
            if (newStatus == "Complete")
                order.DateCompleted = DateTime.Now;

            _context.SaveChanges();
        }
    }
}