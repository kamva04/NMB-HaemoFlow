using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;           // your DbContext
using NMB_HLabSys.Models.Domain;
using NMB_HLabSys.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NMB_HLabSys.Controllers
{
	public class AdminController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly AuthService _auth;

		public AdminController(ApplicationDbContext context, AuthService auth)
		{
			_context = context;
			_auth = auth;
		}

		private IActionResult RequireAdmin()
		{
			if (!_auth.IsLoggedIn()) return RedirectToAction("Login", "Account");
			if (!_auth.IsInRole("Admin")) return RedirectToAction("AccessDenied", "Account");
			return null!;
		}

		// ── DASHBOARD ─────────────────────────────────────────────
		public IActionResult Index()
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			return View();
		}

		// ═══════════════════════════════════════════════════════════
		// MEDICAL CONDITION CATEGORIES
		// ═══════════════════════════════════════════════════════════
		public IActionResult ConditionCategories()
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var list = _context.MedicalConditionCategories
							   .OrderBy(c => c.CategoryName)
							   .ToList();
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateConditionCategory()
		{
			var g = RequireAdmin(); if (g != null) return g;
			return View(new MedicalConditionCategory());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateConditionCategory(MedicalConditionCategory model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			model.CategoryName = model.CategoryName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.CategoryName))
				ModelState.AddModelError(nameof(model.CategoryName), "Category name is required.");
			if (_context.MedicalConditionCategories.Any(c => c.CategoryName.Trim().ToLower() == model.CategoryName.ToLower()))
				ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
			if (!ModelState.IsValid) return View(model);

			model.IsActive = true;
			_context.MedicalConditionCategories.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Category created.";
			return RedirectToAction(nameof(ConditionCategories));
		}

		[HttpGet]
		public IActionResult EditConditionCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var cat = _context.MedicalConditionCategories
							  .FirstOrDefault(c => c.CategoryId == id);
			if (cat == null) return NotFound();
			return View(cat);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult EditConditionCategory(MedicalConditionCategory model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			model.CategoryName = model.CategoryName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.CategoryName))
				ModelState.AddModelError(nameof(model.CategoryName), "Category name is required.");
			if (_context.MedicalConditionCategories.Any(c => c.CategoryId != model.CategoryId && c.CategoryName.Trim().ToLower() == model.CategoryName.ToLower()))
				ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
			if (!ModelState.IsValid) return View(model);

			var category = _context.MedicalConditionCategories.Find(model.CategoryId);
			if (category == null) return NotFound();
			category.CategoryName = model.CategoryName;
			_context.SaveChanges();
			TempData["Success"] = "Category updated.";
			return RedirectToAction(nameof(ConditionCategories));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteConditionCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var cat = _context.MedicalConditionCategories
							  .FirstOrDefault(c => c.CategoryId == id);
			if (cat != null)
			{
				cat.IsActive = false;
				_context.SaveChanges();
			}
			TempData["Success"] = "Category archived.";
			return RedirectToAction(nameof(ConditionCategories));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult RestoreConditionCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var category = _context.MedicalConditionCategories.Find(id);
			if (category == null) return NotFound();
			category.IsActive = true;
			_context.SaveChanges();
			TempData["Success"] = "Category restored.";
			return RedirectToAction(nameof(ConditionCategories));
		}

		// ═══════════════════════════════════════════════════════════
		// MEDICAL CONDITIONS
		// ═══════════════════════════════════════════════════════════
		public IActionResult Conditions(string direction = "asc")
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			direction = direction == "desc" ? "desc" : "asc";
			var query = _context.MedicalConditions.Include(mc => mc.Category);
			var list = direction == "desc"
				? query.OrderByDescending(mc => mc.ConditionName).ToList()
				: query.OrderBy(mc => mc.ConditionName).ToList();
			ViewData["SortDirection"] = direction;
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateCondition()
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetConditionCategories();
			return View(new MedicalCondition());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateCondition(MedicalCondition model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetConditionCategories();
			model.ConditionName = model.ConditionName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.ConditionName))
				ModelState.AddModelError(nameof(model.ConditionName), "Condition name is required.");
			if (_context.MedicalConditions.Any(c => c.ConditionName.Trim().ToLower() == model.ConditionName.ToLower()))
				ModelState.AddModelError(nameof(model.ConditionName), "A condition with this name already exists.");
			if (!_context.MedicalConditionCategories.Any(c => c.CategoryId == model.CategoryId && c.IsActive))
				ModelState.AddModelError(nameof(model.CategoryId), "Select an active category.");
			if (!ModelState.IsValid) return View(model);

			model.IsActive = true;
			_context.MedicalConditions.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Condition added.";
			return RedirectToAction(nameof(Conditions));
		}

		[HttpGet]
		public IActionResult EditCondition(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetConditionCategories();
			var cond = _context.MedicalConditions
							   .FirstOrDefault(mc => mc.ConditionId == id);
			if (cond == null) return NotFound();
			return View(cond);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult EditCondition(MedicalCondition model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetConditionCategories();
			model.ConditionName = model.ConditionName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.ConditionName))
				ModelState.AddModelError(nameof(model.ConditionName), "Condition name is required.");
			if (_context.MedicalConditions.Any(c => c.ConditionId != model.ConditionId && c.ConditionName.Trim().ToLower() == model.ConditionName.ToLower()))
				ModelState.AddModelError(nameof(model.ConditionName), "A condition with this name already exists.");
			if (!_context.MedicalConditionCategories.Any(c => c.CategoryId == model.CategoryId && c.IsActive))
				ModelState.AddModelError(nameof(model.CategoryId), "Select an active category.");
			if (!ModelState.IsValid) return View(model);

			var condition = _context.MedicalConditions.Find(model.ConditionId);
			if (condition == null) return NotFound();
			condition.ConditionName = model.ConditionName;
			condition.CategoryId = model.CategoryId;
			_context.SaveChanges();
			TempData["Success"] = "Condition updated.";
			return RedirectToAction(nameof(Conditions));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteCondition(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var cond = _context.MedicalConditions
							   .FirstOrDefault(mc => mc.ConditionId == id);
			if (cond != null)
			{
				cond.IsActive = false;
				_context.SaveChanges();
			}
			TempData["Success"] = "Condition archived.";
			return RedirectToAction(nameof(Conditions));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult RestoreCondition(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var condition = _context.MedicalConditions.Find(id);
			if (condition == null) return NotFound();
			condition.IsActive = true;
			_context.SaveChanges();
			TempData["Success"] = "Condition restored.";
			return RedirectToAction(nameof(Conditions));
		}

		// ═══════════════════════════════════════════════════════════
		// ALLERGY CATEGORIES
		// ═══════════════════════════════════════════════════════════
		public IActionResult AllergyCategories()
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var list = _context.AllergyCategories
							   .OrderBy(c => c.CategoryName)
							   .ToList();
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateAllergyCategory()
		{
			var g = RequireAdmin(); if (g != null) return g;
			return View(new AllergyCategory());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateAllergyCategory(AllergyCategory model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			model.CategoryName = model.CategoryName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.CategoryName))
				ModelState.AddModelError(nameof(model.CategoryName), "Category name is required.");
			if (_context.AllergyCategories.Any(c => c.CategoryName.Trim().ToLower() == model.CategoryName.ToLower()))
				ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
			if (!ModelState.IsValid) return View(model);

			model.IsActive = true;
			_context.AllergyCategories.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Allergy category created.";
			return RedirectToAction(nameof(AllergyCategories));
		}

		[HttpGet]
		public IActionResult EditAllergyCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var cat = _context.AllergyCategories
							  .FirstOrDefault(c => c.CategoryId == id);
			if (cat == null) return NotFound();
			return View(cat);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult EditAllergyCategory(AllergyCategory model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			model.CategoryName = model.CategoryName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.CategoryName))
				ModelState.AddModelError(nameof(model.CategoryName), "Category name is required.");
			if (_context.AllergyCategories.Any(c => c.CategoryId != model.CategoryId && c.CategoryName.Trim().ToLower() == model.CategoryName.ToLower()))
				ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
			if (!ModelState.IsValid) return View(model);

			var category = _context.AllergyCategories.Find(model.CategoryId);
			if (category == null) return NotFound();
			category.CategoryName = model.CategoryName;
			_context.SaveChanges();
			TempData["Success"] = "Allergy category updated.";
			return RedirectToAction(nameof(AllergyCategories));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteAllergyCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var category = _context.AllergyCategories.Find(id);
			if (category != null)
			{
				category.IsActive = false;
				_context.SaveChanges();
			}
			TempData["Success"] = "Allergy category archived.";
			return RedirectToAction(nameof(AllergyCategories));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult RestoreAllergyCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var category = _context.AllergyCategories.Find(id);
			if (category == null) return NotFound();
			category.IsActive = true;
			_context.SaveChanges();
			TempData["Success"] = "Allergy category restored.";
			return RedirectToAction(nameof(AllergyCategories));
		}

		// ═══════════════════════════════════════════════════════════
		// ALLERGIES
		// ═══════════════════════════════════════════════════════════
		public IActionResult Allergies(string direction = "asc")
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			direction = direction == "desc" ? "desc" : "asc";
			var query = _context.Allergies.Include(a => a.Category);
			var list = direction == "desc"
				? query.OrderByDescending(a => a.AllergyName).ToList()
				: query.OrderBy(a => a.AllergyName).ToList();
			ViewData["SortDirection"] = direction;
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateAllergy()
		{
			var g = RequireAdmin(); if (g != null) return g;
			ViewBag.Categories = GetAllergyCategories();
			return View(new Allergy());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateAllergy(Allergy model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetAllergyCategories();
			model.AllergyName = model.AllergyName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.AllergyName))
				ModelState.AddModelError(nameof(model.AllergyName), "Allergy name is required.");
			if (_context.Allergies.Any(a => a.AllergyName.Trim().ToLower() == model.AllergyName.ToLower()))
				ModelState.AddModelError(nameof(model.AllergyName), "An allergy with this name already exists.");
			if (!_context.AllergyCategories.Any(c => c.CategoryId == model.CategoryId && c.IsActive))
				ModelState.AddModelError(nameof(model.CategoryId), "Select an active category.");
			if (!ModelState.IsValid) return View(model);

			model.IsActive = true;
			_context.Allergies.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Allergy added.";
			return RedirectToAction(nameof(Allergies));
		}

		[HttpGet]
		public IActionResult EditAllergy(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetAllergyCategories();
			var allergy = _context.Allergies
								  .FirstOrDefault(a => a.AllergyId == id);
			if (allergy == null) return NotFound();
			return View(allergy);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult EditAllergy(Allergy model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetAllergyCategories();
			model.AllergyName = model.AllergyName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.AllergyName))
				ModelState.AddModelError(nameof(model.AllergyName), "Allergy name is required.");
			if (_context.Allergies.Any(a => a.AllergyId != model.AllergyId && a.AllergyName.Trim().ToLower() == model.AllergyName.ToLower()))
				ModelState.AddModelError(nameof(model.AllergyName), "An allergy with this name already exists.");
			if (!_context.AllergyCategories.Any(c => c.CategoryId == model.CategoryId && c.IsActive))
				ModelState.AddModelError(nameof(model.CategoryId), "Select an active category.");
			if (!ModelState.IsValid) return View(model);

			var allergy = _context.Allergies.Find(model.AllergyId);
			if (allergy == null) return NotFound();
			allergy.AllergyName = model.AllergyName;
			allergy.CategoryId = model.CategoryId;
			_context.SaveChanges();
			TempData["Success"] = "Allergy updated.";
			return RedirectToAction(nameof(Allergies));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteAllergy(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var allergy = _context.Allergies
								  .FirstOrDefault(a => a.AllergyId == id);
			if (allergy != null)
			{
				allergy.IsActive = false;
				_context.SaveChanges();
			}
			TempData["Success"] = "Allergy archived.";
			return RedirectToAction(nameof(Allergies));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult RestoreAllergy(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var allergy = _context.Allergies.Find(id);
			if (allergy == null) return NotFound();
			allergy.IsActive = true;
			_context.SaveChanges();
			TempData["Success"] = "Allergy restored.";
			return RedirectToAction(nameof(Allergies));
		}

		// ═══════════════════════════════════════════════════════════
		// MEDICATION CATEGORIES
		// ═══════════════════════════════════════════════════════════
		public IActionResult MedicationCategories()
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var list = _context.MedicationCategories
							   .OrderBy(c => c.CategoryName)
							   .ToList();
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateMedicationCategory()
		{
			var g = RequireAdmin(); if (g != null) return g;
			return View(new MedicationCategory());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateMedicationCategory(MedicationCategory model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			model.CategoryName = model.CategoryName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.CategoryName))
				ModelState.AddModelError(nameof(model.CategoryName), "Category name is required.");
			if (_context.MedicationCategories.Any(c => c.CategoryName.Trim().ToLower() == model.CategoryName.ToLower()))
				ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
			if (!ModelState.IsValid) return View(model);

			model.IsActive = true;
			_context.MedicationCategories.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Medication category created.";
			return RedirectToAction(nameof(MedicationCategories));
		}

		[HttpGet]
		public IActionResult EditMedicationCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var cat = _context.MedicationCategories
							  .FirstOrDefault(c => c.CategoryId == id);
			if (cat == null) return NotFound();
			return View(cat);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult EditMedicationCategory(MedicationCategory model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			model.CategoryName = model.CategoryName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.CategoryName))
				ModelState.AddModelError(nameof(model.CategoryName), "Category name is required.");
			if (_context.MedicationCategories.Any(c => c.CategoryId != model.CategoryId && c.CategoryName.Trim().ToLower() == model.CategoryName.ToLower()))
				ModelState.AddModelError(nameof(model.CategoryName), "A category with this name already exists.");
			if (!ModelState.IsValid) return View(model);

			var category = _context.MedicationCategories.Find(model.CategoryId);
			if (category == null) return NotFound();
			category.CategoryName = model.CategoryName;
			_context.SaveChanges();
			TempData["Success"] = "Medication category updated.";
			return RedirectToAction(nameof(MedicationCategories));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteMedicationCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var category = _context.MedicationCategories.Find(id);
			if (category != null)
			{
				category.IsActive = false;
				_context.SaveChanges();
			}
			TempData["Success"] = "Medication category archived.";
			return RedirectToAction(nameof(MedicationCategories));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult RestoreMedicationCategory(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var category = _context.MedicationCategories.Find(id);
			if (category == null) return NotFound();
			category.IsActive = true;
			_context.SaveChanges();
			TempData["Success"] = "Medication category restored.";
			return RedirectToAction(nameof(MedicationCategories));
		}

		// ═══════════════════════════════════════════════════════════
		// MEDICATIONS
		// ═══════════════════════════════════════════════════════════
		public IActionResult Medications(string direction = "asc")
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			direction = direction == "desc" ? "desc" : "asc";
			var query = _context.Medications.Include(m => m.Category);
			var list = direction == "desc"
				? query.OrderByDescending(m => m.MedicationName).ToList()
				: query.OrderBy(m => m.MedicationName).ToList();
			ViewData["SortDirection"] = direction;
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateMedication()
		{
			var g = RequireAdmin(); if (g != null) return g;
			ViewBag.Categories = GetMedicationCategories();
			return View(new Medication());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateMedication(Medication model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetMedicationCategories();
			model.MedicationName = model.MedicationName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.MedicationName))
				ModelState.AddModelError(nameof(model.MedicationName), "Medication name is required.");
			if (_context.Medications.Any(m => m.MedicationName.Trim().ToLower() == model.MedicationName.ToLower()))
				ModelState.AddModelError(nameof(model.MedicationName), "A medication with this name already exists.");
			if (!_context.MedicationCategories.Any(c => c.CategoryId == model.CategoryId && c.IsActive))
				ModelState.AddModelError(nameof(model.CategoryId), "Select an active category.");
			if (!ModelState.IsValid) return View(model);

			model.IsActive = true;
			_context.Medications.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Medication added.";
			return RedirectToAction(nameof(Medications));
		}

		[HttpGet]
		public IActionResult EditMedication(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetMedicationCategories();
			var med = _context.Medications
							  .FirstOrDefault(m => m.MedicationId == id);
			if (med == null) return NotFound();
			return View(med);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult EditMedication(Medication model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			ViewBag.Categories = GetMedicationCategories();
			model.MedicationName = model.MedicationName?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(model.MedicationName))
				ModelState.AddModelError(nameof(model.MedicationName), "Medication name is required.");
			if (_context.Medications.Any(m => m.MedicationId != model.MedicationId && m.MedicationName.Trim().ToLower() == model.MedicationName.ToLower()))
				ModelState.AddModelError(nameof(model.MedicationName), "A medication with this name already exists.");
			if (!_context.MedicationCategories.Any(c => c.CategoryId == model.CategoryId && c.IsActive))
				ModelState.AddModelError(nameof(model.CategoryId), "Select an active category.");
			if (!ModelState.IsValid) return View(model);

			var medication = _context.Medications.Find(model.MedicationId);
			if (medication == null) return NotFound();
			medication.MedicationName = model.MedicationName;
			medication.CategoryId = model.CategoryId;
			_context.SaveChanges();
			TempData["Success"] = "Medication updated.";
			return RedirectToAction(nameof(Medications));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteMedication(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var med = _context.Medications
							  .FirstOrDefault(m => m.MedicationId == id);
			if (med != null)
			{
				med.IsActive = false;
				_context.SaveChanges();
			}
			TempData["Success"] = "Medication archived.";
			return RedirectToAction(nameof(Medications));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult RestoreMedication(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var medication = _context.Medications.Find(id);
			if (medication == null) return NotFound();
			medication.IsActive = true;
			_context.SaveChanges();
			TempData["Success"] = "Medication restored.";
			return RedirectToAction(nameof(Medications));
		}

		// ═══════════════════════════════════════════════════════════
		// SAMPLE TYPES
		// ═══════════════════════════════════════════════════════════
		public IActionResult SampleTypes()
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var list = _context.SampleTypes
							   .OrderBy(s => s.TypeName)
							   .ToList();
			return View(list);
		}

		[HttpGet]
		public IActionResult CreateSampleType()
		{
			var g = RequireAdmin(); if (g != null) return g;
			return View(new SampleType());
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult CreateSampleType(SampleType model)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			if (!ModelState.IsValid) return View(model);

			model.IsDeleted = false;
			_context.SampleTypes.Add(model);
			_context.SaveChanges();
			TempData["Success"] = "Sample type added.";
			return RedirectToAction(nameof(SampleTypes));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public IActionResult DeleteSampleType(int id)
		{
			var guard = RequireAdmin(); if (guard != null) return guard;
			var sample = _context.SampleTypes.Find(id);
			if (sample != null)
			{
				sample.IsDeleted = true;
				_context.SaveChanges();
			}
			TempData["Success"] = "Sample type archived. Existing records have been preserved.";
			return RedirectToAction(nameof(SampleTypes));
		}

		// ── PRIVATE HELPERS ───────────────────────────────────────
		        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult RestoreSampleType(int id)
        {
            var guard = RequireAdmin(); if (guard != null) return guard;
            var item = _context.SampleTypes.Find(id);
            if (item == null) return NotFound();
            item.IsDeleted = false;
            _context.SaveChanges();
            TempData["Success"] = "Sample type restored.";
            return RedirectToAction(nameof(SampleTypes));
        }

		private List<MedicalConditionCategory> GetConditionCategories()
			=> _context.MedicalConditionCategories
					   .Where(c => c.IsActive)
					   .OrderBy(c => c.CategoryName)
					   .ToList();

		private List<AllergyCategory> GetAllergyCategories()
			=> _context.AllergyCategories
					   .Where(c => c.IsActive)
					   .OrderBy(c => c.CategoryName)
					   .ToList();

		private List<MedicationCategory> GetMedicationCategories()
			=> _context.MedicationCategories
					   .Where(c => c.IsActive)
					   .OrderBy(c => c.CategoryName)
					   .ToList();
	}
}
