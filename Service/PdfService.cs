using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Events;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using System;
using System.IO;
using System.Linq;

namespace NMB_HLabSys.Services
{
    /// <summary>
    /// Generates all PDF reports using iText 7 Community Edition.
    /// Phase 4 adds strict professional reports with headers, footers, and page numbers.
    /// </summary>
    public class PdfService
    {
        private readonly ApplicationDbContext _context;

        // ─── Existing palette (unchanged) ───
        private static readonly DeviceRgb HeaderColor = new DeviceRgb(13, 59, 102);
        private static readonly DeviceRgb AbnormalColor = new DeviceRgb(220, 53, 69);
        private static readonly DeviceRgb SubHeaderColor = new DeviceRgb(230, 236, 243);

        // ─── Phase 4 palette additions ───
        private static readonly DeviceRgb NavyBlue = new DeviceRgb(7, 59, 130);
        private static readonly DeviceRgb BlueAccent = new DeviceRgb(13, 110, 253);
        private static readonly DeviceRgb LightGrey = new DeviceRgb(243, 244, 246);
        private static readonly DeviceRgb MidGrey = new DeviceRgb(107, 114, 128);
        private static readonly DeviceRgb DangerRed = new DeviceRgb(220, 38, 38);

        public PdfService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // EXISTING SHARED HELPERS (unchanged)
        // =================================================================

        private static Cell HeaderCell(string text)
        {
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            return new Cell()
                .SetBackgroundColor(HeaderColor)
                .SetFontColor(ColorConstants.WHITE)
                .SetFont(font).SetFontSize(9)
                .Add(new Paragraph(text));
        }

        private static Cell DataCell(string text, bool abnormal = false)
        {
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var cell = new Cell()
                .SetFont(font).SetFontSize(8)
                .Add(new Paragraph(text));
            if (abnormal)
                cell.SetFontColor(AbnormalColor).SetBold();
            return cell;
        }

        // =================================================================
        // SHARED REPORTS
        // =================================================================

        public byte[] GenerateTestsByCategoryReport(DateTime from, DateTime to)
        {
            var fromDate = from.Date;
            var toDateExclusive = to.Date.AddDays(1);
            var rows = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.CompletedAt >= fromDate
                    && rtt.CompletedAt < toDateExclusive
                    && rtt.Status == "Verified")
                .Include(rtt => rtt.TestType!).ThenInclude(tt => tt.Category)
                .GroupBy(rtt => rtt.TestType!.Category!.CategoryName)
                .Select(g => new { Category = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToList();

            using var ms = new MemoryStream();
            using var pdf = new PdfDocument(new PdfWriter(ms));
            using var doc = new Document(pdf);

            doc.SetMargins(60, 40, 60, 55);
            AttachHeaderAndFooter(pdf, "Tests Performed by Category", "NMB-HLabSys");
            AddTitleBlock(doc, "Tests Performed by Category",
                $"Date range: {fromDate:dd MMM yyyy} – {to.Date:dd MMM yyyy}");

            var totalTests = rows.Sum(row => row.Total);
            var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(16);
            summary.AddCell(SummaryBox("Categories", rows.Count.ToString()));
            summary.AddCell(SummaryBox("Verified tests", totalTests.ToString()));
            doc.Add(summary);

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth();
            table.AddHeaderCell(HeaderCell("Test Category"));
            table.AddHeaderCell(HeaderCell("Total Verified"));

            int grandTotal = 0;
            foreach (var row in rows)
            {
                table.AddCell(DataCell(row.Category));
                table.AddCell(DataCell(row.Total.ToString()));
                grandTotal += row.Total;
            }

            if (!rows.Any())
            {
                doc.Add(new Paragraph("No verified results were found for the selected date range.")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                    .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(24));
            }
            else
            {
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                table.AddCell(new Cell().SetBackgroundColor(SubHeaderColor).SetFont(boldFont).SetFontSize(9)
                    .Add(new Paragraph("TOTAL")));
                table.AddCell(new Cell().SetBackgroundColor(SubHeaderColor).SetFont(boldFont).SetFontSize(9)
                    .Add(new Paragraph(grandTotal.ToString())));
                doc.Add(table);
            }
            doc.Close();
            return ms.ToArray();
        }

        public byte[] GenerateDoctorRequestsReport(int doctorId, DateTime from, DateTime to)
        {
            var doctor = _context.Doctors
                .AsNoTracking()
                .Where(d => d.DoctorId == doctorId)
                .Select(d => new { d.Name, d.Surname })
                .FirstOrDefault();

            var fromDate = from.Date;
            var toDateExclusive = to.Date.AddDays(1);
            var rows = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.DoctorId == doctorId
                    && tr.RequestDate >= fromDate
                    && tr.RequestDate < toDateExclusive)
                .Include(tr => tr.Patient)
                .Select(tr => new
                {
                    tr.RequestNumber,
                    Patient = tr.Patient!.Name + " " + tr.Patient.Surname,
                    tr.RequestDate,
                    tr.Urgency,
                    tr.Status,
                    TestCount = tr.RequestTestTypes.Count
                })
                .OrderByDescending(tr => tr.RequestDate)
                .ToList();

            using var ms = new MemoryStream();
            using var pdf = new PdfDocument(new PdfWriter(ms));
            using var doc = new Document(pdf);

            doc.SetMargins(60, 40, 60, 55);
            AttachHeaderAndFooter(pdf, "Test Requests Report", "NMB-HLabSys");
            AddTitleBlock(doc, "Test Requests Report",
                $"Date range: {fromDate:dd MMM yyyy} – {to.Date:dd MMM yyyy}",
                $"Prepared for: Dr {doctor?.Name} {doctor?.Surname}");
            var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(16);
            summary.AddCell(SummaryBox("Requests", rows.Count.ToString()));
            summary.AddCell(SummaryBox("Tests requested", rows.Sum(row => row.TestCount).ToString()));
            doc.Add(summary);

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 22, 28, 15, 12, 15, 8 }))
                .UseAllAvailableWidth();
            table.AddHeaderCell(HeaderCell("Request #"));
            table.AddHeaderCell(HeaderCell("Patient"));
            table.AddHeaderCell(HeaderCell("Date"));
            table.AddHeaderCell(HeaderCell("Urgency"));
            table.AddHeaderCell(HeaderCell("Status"));
            table.AddHeaderCell(HeaderCell("Tests"));

            if (!rows.Any())
            {
                doc.Add(new Paragraph("No test requests were found for the selected date range.")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                    .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(24));
            }
            else foreach (var row in rows)
            {
                table.AddCell(DataCell(row.RequestNumber));
                table.AddCell(DataCell(row.Patient));
                table.AddCell(DataCell(row.RequestDate.ToString("dd MMM yyyy")));
                table.AddCell(DataCell(row.Urgency));
                table.AddCell(DataCell(row.Status));
                table.AddCell(DataCell(row.TestCount.ToString()));
            }

            if (rows.Any()) doc.Add(table);
            doc.Close();
            return ms.ToArray();
        }

        public byte[] GenerateTechnicianReport(int technicianId, DateTime from, DateTime to)
        {
            var tech = _context.Technicians
                .AsNoTracking()
                .Where(t => t.TechnicianId == technicianId)
                .Select(t => new { t.Name, t.Surname })
                .FirstOrDefault();

            var fromDate = from.Date;
            var toDateExclusive = to.Date.AddDays(1);
            var rows = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.TechnicianId == technicianId
                    && rtt.CompletedAt >= fromDate && rtt.CompletedAt < toDateExclusive
                    && (rtt.Status == "Completed" || rtt.Status == "Verified"))
                .Include(rtt => rtt.TestType!).ThenInclude(tt => tt.Category)
                .GroupBy(rtt => rtt.TestType!.Category!.CategoryName)
                .Select(g => new { Category = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .ToList();

            using var ms = new MemoryStream();
            using var pdf = new PdfDocument(new PdfWriter(ms));
            using var doc = new Document(pdf);

            doc.SetMargins(60, 40, 60, 55);
            AttachHeaderAndFooter(pdf, "Tests Completed by Category", "NMB-HLabSys",
                $"{tech?.Name} {tech?.Surname}".Trim());
            AddTitleBlock(doc, "Tests Completed by Category",
                $"Date range: {fromDate:dd MMM yyyy} – {to.Date:dd MMM yyyy}",
                $"Technician: {tech?.Name} {tech?.Surname}");
            var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(16);
            summary.AddCell(SummaryBox("Categories", rows.Count.ToString()));
            summary.AddCell(SummaryBox("Tests completed", rows.Sum(row => row.Total).ToString()));
            doc.Add(summary);

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth();
            table.AddHeaderCell(HeaderCell("Test Category"));
            table.AddHeaderCell(HeaderCell("Tests Completed"));

            if (!rows.Any())
            {
                doc.Add(new Paragraph("No completed tests were found for the selected date range.")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                    .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(24));
            }
            else
            {
                foreach (var row in rows)
                {
                    table.AddCell(DataCell(row.Category));
                    table.AddCell(DataCell(row.Total.ToString()));
                }
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                table.AddCell(new Cell().SetBackgroundColor(SubHeaderColor).SetFont(boldFont).SetFontSize(9).Add(new Paragraph("TOTAL")));
                table.AddCell(new Cell().SetBackgroundColor(SubHeaderColor).SetFont(boldFont).SetFontSize(9).Add(new Paragraph(rows.Sum(row => row.Total).ToString())));
                doc.Add(table);
            }
            doc.Close();
            return ms.ToArray();
        }

        public byte[] GeneratePatientResultsReport(int patientId, DateTime from, DateTime to)
        {
            var fromDate = from.Date;
            var toDateExclusive = to.Date.AddDays(1);
            var patient = _context.Patients
                .AsNoTracking()
                .Where(p => p.PatientId == patientId)
                .Select(p => new { p.Name, p.Surname })
                .FirstOrDefault();

            var rows = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.Request!.PatientId == patientId
                    && rtt.Request.Status == "ReleasedByDoctor"
                    && rtt.Request.RequestDate >= fromDate
                    && rtt.Request.RequestDate < toDateExclusive
                    && rtt.ResultValue.HasValue)
                .Include(rtt => rtt.TestType!).ThenInclude(tt => tt.Category)
                .Include(rtt => rtt.Request)
                .OrderBy(rtt => rtt.TestType!.Category!.CategoryName)
                .ThenByDescending(rtt => rtt.Request!.RequestDate)
                .Select(rtt => new
                {
                    Category = rtt.TestType!.Category!.CategoryName,
                    TestName = rtt.TestType.TestName,
                    rtt.ResultValue,
                    rtt.TestType.Units,
                    rtt.IsAbnormal,
                    rtt.TestType.NormalRangeMin,
                    rtt.TestType.NormalRangeMax,
                    RequestDate = rtt.Request!.RequestDate,
                    rtt.ResultNotes
                })
                .ToList();

            using var ms = new MemoryStream();
            using var pdf = new PdfDocument(new PdfWriter(ms));
            using var doc = new Document(pdf);

            doc.SetMargins(60, 40, 60, 55);
            AttachHeaderAndFooter(pdf, "Patient Lab Results Report", "NMB-HLabSys");
            AddTitleBlock(doc, "Patient Lab Results Report",
                $"Date range: {from.Date:dd MMM yyyy} – {to.Date:dd MMM yyyy}",
                $"Patient: {patient?.Name} {patient?.Surname}");

            var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(8);
            summary.AddCell(SummaryBox("Released results", rows.Count.ToString()));
            summary.AddCell(SummaryBox("Abnormal results", rows.Count(row => row.IsAbnormal).ToString()));
            doc.Add(summary);

            string? lastCat = null;
            Table? table = null;

            void FlushTable() { if (table != null) { doc.Add(table); doc.Add(new Paragraph("\n")); } }

            foreach (var row in rows)
            {
                if (row.Category != lastCat)
                {
                    FlushTable();
                    lastCat = row.Category;
                    var boldFont2 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    doc.Add(new Paragraph(row.Category)
                        .SetFont(boldFont2).SetFontSize(10)
                        .SetFontColor(new DeviceRgb(13, 59, 102))
                        .SetMarginTop(8));

                    table = new Table(UnitValue.CreatePercentArray(new float[] { 25, 14, 10, 20, 10, 21 }))
                        .UseAllAvailableWidth();
                    table.AddHeaderCell(HeaderCell("Test"));
                    table.AddHeaderCell(HeaderCell("Result"));
                    table.AddHeaderCell(HeaderCell("Units"));
                    table.AddHeaderCell(HeaderCell("Normal Range"));
                    table.AddHeaderCell(HeaderCell("Flag"));
                    table.AddHeaderCell(HeaderCell("Date"));
                }

                var range = (row.NormalRangeMin.HasValue && row.NormalRangeMax.HasValue)
                    ? $"{row.NormalRangeMin:F2} – {row.NormalRangeMax:F2}" : "—";

                table!.AddCell(DataCell(row.TestName));
                table.AddCell(DataCell(row.ResultValue?.ToString("F2") ?? "—", row.IsAbnormal));
                table.AddCell(DataCell(row.Units ?? "—"));
                table.AddCell(DataCell(range));
                table.AddCell(DataCell(row.IsAbnormal ? "ABNORMAL" : "Normal", row.IsAbnormal));
                table.AddCell(DataCell(row.RequestDate.ToString("dd MMM yyyy")));
            }
            FlushTable();

            if (!rows.Any())
                doc.Add(new Paragraph("No released results were found for this period.")
                    .SetFontColor(ColorConstants.GRAY).SetFontSize(9));

            doc.Close();
            return ms.ToArray();
        }

        public byte[] GeneratePatientRequestHistoryReport(int patientId, DateTime from, DateTime to)
        {
            var patient = _context.Patients
                .AsNoTracking()
                .Where(p => p.PatientId == patientId)
                .Select(p => new { p.Name, p.Surname })
                .FirstOrDefault();

            var requests = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.PatientId == patientId &&
                    tr.RequestDate >= from.Date &&
                    tr.RequestDate < to.Date.AddDays(1))
                .Include(tr => tr.Doctor)
                .Include(tr => tr.RequestTestTypes)
                    .ThenInclude(rtt => rtt.TestType)
                .OrderByDescending(tr => tr.RequestDate)
                .ToList();

            using var ms = new MemoryStream();
            using var pdf = new PdfDocument(new PdfWriter(ms));
            using var doc = new Document(pdf);

            doc.SetMargins(60, 40, 60, 55);
            AttachHeaderAndFooter(pdf, "Patient Test Request History", "NMB-HLabSys");
            AddTitleBlock(doc, "Patient Test Request History",
                $"Date range: {from.Date:dd MMM yyyy} – {to.Date:dd MMM yyyy}",
                $"Patient: {patient?.Name} {patient?.Surname}");

            doc.Add(new Paragraph($"Total requests: {requests.Count}")
                .SetFontSize(9).SetMarginBottom(8));

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 18, 14, 20, 11, 17, 20 }))
                .UseAllAvailableWidth();
            table.AddHeaderCell(HeaderCell("Request #"));
            table.AddHeaderCell(HeaderCell("Date"));
            table.AddHeaderCell(HeaderCell("Doctor"));
            table.AddHeaderCell(HeaderCell("Urgency"));
            table.AddHeaderCell(HeaderCell("Request Status"));
            table.AddHeaderCell(HeaderCell("Tests / Status"));

            foreach (var request in requests)
            {
                var doctorName = request.Doctor == null
                    ? "—"
                    : $"Dr {request.Doctor.Name} {request.Doctor.Surname}";
                var tests = request.RequestTestTypes.Any()
                    ? string.Join("\n", request.RequestTestTypes
                        .OrderBy(test => test.TestType?.TestName)
                        .Select(test => $"{test.TestType?.TestName ?? "Test"}: {test.Status}"))
                    : "—";

                table.AddCell(DataCell(request.RequestNumber));
                table.AddCell(DataCell(request.RequestDate.ToString("dd MMM yyyy")));
                table.AddCell(DataCell(doctorName));
                table.AddCell(DataCell(request.Urgency));
                table.AddCell(DataCell(request.Status));
                table.AddCell(DataCell(tests));
            }

            if (requests.Count == 0)
            {
                doc.Add(new Paragraph("No test requests found for this period.")
                    .SetFontColor(ColorConstants.GRAY).SetFontSize(9));
            }
            else
            {
                doc.Add(table);
            }

            doc.Close();
            return ms.ToArray();
        }

        public byte[] GenerateRequestResultsPdf(int requestId)
        {
            var req = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.RequestId == requestId)
                .Include(tr => tr.Patient)
                .Include(tr => tr.Doctor)
                .Select(tr => new
                {
                    tr.RequestNumber,
                    Patient = tr.Patient!.Name + " " + tr.Patient.Surname,
                    tr.RequestDate,
                    Doctor = tr.Doctor!.Name + " " + tr.Doctor.Surname
                })
                .FirstOrDefault();

            var rows = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.RequestId == requestId && (rtt.Status == "Verified" || rtt.Status == "Completed"))
                .Include(rtt => rtt.TestType)
                .Select(rtt => new
                {
                    TestName = rtt.TestType!.TestName,
                    rtt.ResultValue,
                    rtt.TestType.Units,
                    rtt.TestType.NormalRangeMin,
                    rtt.TestType.NormalRangeMax,
                    rtt.IsAbnormal,
                    rtt.ResultNotes
                })
                .OrderBy(row => row.TestName)
                .ToList();

            using var ms = new MemoryStream();
            using var pdf = new PdfDocument(new PdfWriter(ms));
            using var doc = new Document(pdf);

            doc.SetMargins(60, 40, 60, 55);
            AttachHeaderAndFooter(pdf, "Laboratory Results Report", "NMB-HLabSys");
            AddTitleBlock(doc, "Laboratory Results Report",
                $"Request date: {req?.RequestDate:dd MMM yyyy}",
                $"Patient: {req?.Patient} | Request: {req?.RequestNumber} | Doctor: {req?.Doctor}");

            var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth().SetMarginBottom(16);
            summary.AddCell(SummaryBox("Reported tests", rows.Count.ToString()));
            summary.AddCell(SummaryBox("Abnormal results", rows.Count(row => row.IsAbnormal).ToString()));
            doc.Add(summary);

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 26, 13, 8, 18, 12, 23 }))
                .UseAllAvailableWidth();
            table.AddHeaderCell(HeaderCell("Test"));
            table.AddHeaderCell(HeaderCell("Result"));
            table.AddHeaderCell(HeaderCell("Units"));
            table.AddHeaderCell(HeaderCell("Normal Range"));
            table.AddHeaderCell(HeaderCell("Flag"));
            table.AddHeaderCell(HeaderCell("Notes"));

            if (!rows.Any())
            {
                doc.Add(new Paragraph("No completed test results are available for this request.")
                    .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                    .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(24));
            }
            else foreach (var row in rows)
            {
                var range = (row.NormalRangeMin.HasValue && row.NormalRangeMax.HasValue)
                    ? $"{row.NormalRangeMin:F2} – {row.NormalRangeMax:F2}" : "—";
                table.AddCell(DataCell(row.TestName));
                table.AddCell(DataCell(row.ResultValue?.ToString("F2") ?? "—", row.IsAbnormal));
                table.AddCell(DataCell(row.Units ?? "—"));
                table.AddCell(DataCell(range));
                table.AddCell(DataCell(row.IsAbnormal ? "ABNORMAL" : "Normal", row.IsAbnormal));
                table.AddCell(DataCell(row.ResultNotes ?? "—"));
            }

            if (rows.Any()) doc.Add(table);
            doc.Add(new Paragraph("This is an official laboratory report generated by NMB-HLabSys.")
                .SetFontSize(7).SetFontColor(ColorConstants.GRAY));
            doc.Close();
            return ms.ToArray();
        }

        // =================================================================
        // PHASE 4 — NEW SHARED HELPERS
        // =================================================================

        /// <summary>
        /// Draws a repeating top header (pages 2+) and page-numbered footer (all pages).
        /// </summary>
        private static void AttachHeaderAndFooter(
            PdfDocument pdf,
            string documentTitle,
            string systemName,
            string? techName = null)
        {
            pdf.AddEventHandler(PdfDocumentEvent.END_PAGE,
                new HeaderFooterHandler(documentTitle, systemName, techName));
        }

        private sealed class HeaderFooterHandler : IEventHandler
        {
            private readonly string _title;
            private readonly string _system;
            private readonly string? _techName;

            public HeaderFooterHandler(string title, string system, string? techName)
            {
                _title = title;
                _system = system;
                _techName = techName;
            }

            public void HandleEvent(Event @event)
            {
                var docEvent = (PdfDocumentEvent)@event;
                var pdfDoc = docEvent.GetDocument();
                var page = docEvent.GetPage();
                var pageSize = pdfDoc.GetDefaultPageSize();
                var pageNumber = pdfDoc.GetPageNumber(page);
                var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDoc);
                var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // ─── TOP HEADER (pages 2+) ───────────────────────
                if (pageNumber > 1)
                {
                    canvas.SaveState();
                    canvas.SetStrokeColor(BlueAccent).SetLineWidth(1.2f)
                          .MoveTo(40, pageSize.GetTop() - 48)
                          .LineTo(pageSize.GetRight() - 40, pageSize.GetTop() - 48)
                          .Stroke();

                    canvas.BeginText()
                          .SetFontAndSize(fontBold, 9)
                          .SetColor(NavyBlue, true)
                          .MoveText(40, pageSize.GetTop() - 40)
                          .ShowText(_title.ToUpperInvariant())
                          .EndText();

                    canvas.BeginText()
                          .SetFontAndSize(fontRegular, 9)
                          .SetColor(MidGrey, true)
                          .MoveText(pageSize.GetRight() - 40 - (fontRegular.GetWidth(_system, 9)),
                                    pageSize.GetTop() - 40)
                          .ShowText(_system)
                          .EndText();

                    canvas.RestoreState();
                }

                // ─── BOTTOM FOOTER (all pages) ───────────────────
                canvas.SaveState();
                canvas.SetStrokeColor(new DeviceRgb(229, 231, 235)).SetLineWidth(0.8f)
                      .MoveTo(40, 42)
                      .LineTo(pageSize.GetRight() - 40, 42)
                      .Stroke();

                var leftText = $"{_system} · Generated {DateTime.Now:dd MMM yyyy HH:mm}";
                if (!string.IsNullOrWhiteSpace(_techName))
                    leftText += $" · Technician: {_techName}";

                canvas.BeginText()
                      .SetFontAndSize(fontRegular, 8)
                      .SetColor(MidGrey, true)
                      .MoveText(40, 28)
                      .ShowText(leftText)
                      .EndText();

                var pageText = $"Page {pageNumber}";
                canvas.BeginText()
                      .SetFontAndSize(fontBold, 8)
                      .SetColor(NavyBlue, true)
                      .MoveText(pageSize.GetRight() - 40 - (fontBold.GetWidth(pageText, 8)), 28)
                      .ShowText(pageText)
                      .EndText();

                canvas.RestoreState();
            }
        }

        /// <summary>
        /// Full title block shown on page 1.
        /// </summary>
        private static void AddTitleBlock(Document doc, string title, string subtitle, string? filterLine = null)
        {
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontReg = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Navy banner
            var bar = new Table(UnitValue.CreatePercentArray(1)).UseAllAvailableWidth();
            bar.SetMarginBottom(14);
            var cell = new Cell().SetBorder(Border.NO_BORDER)
                                  .SetPadding(14)
                                  .SetBackgroundColor(NavyBlue);
            cell.Add(new Paragraph("NMB-HLabSys")
                .SetFont(fontBold).SetFontSize(16).SetFontColor(ColorConstants.WHITE));
            cell.Add(new Paragraph("Haematology Laboratory Management System")
                .SetFont(fontReg).SetFontSize(9).SetFontColor(new DeviceRgb(191, 219, 254)));
            bar.AddCell(cell);
            doc.Add(bar);

            // Title
            doc.Add(new Paragraph(title)
                .SetFont(fontBold).SetFontSize(20).SetFontColor(NavyBlue)
                .SetMarginTop(6).SetMarginBottom(2));

            // Subtitle (date range)
            doc.Add(new Paragraph(subtitle)
                .SetFont(fontReg).SetFontSize(11).SetFontColor(MidGrey)
                .SetMarginBottom(2));

            if (!string.IsNullOrWhiteSpace(filterLine))
            {
                doc.Add(new Paragraph(filterLine)
                    .SetFont(fontReg).SetFontSize(10).SetFontColor(MidGrey)
                    .SetMarginBottom(6));
            }

            doc.Add(new Paragraph($"Report generated on {DateTime.Now:dd MMMM yyyy 'at' HH:mm}")
                .SetFont(fontReg).SetFontSize(9).SetFontColor(MidGrey)
                .SetMarginBottom(16));
        }

        private static Table CreateStyledTable(string[] headers, float[] widths)
        {
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            var table = new Table(UnitValue.CreatePercentArray(widths)).UseAllAvailableWidth();
            table.SetMarginBottom(16);

            foreach (var h in headers)
            {
                var hc = new Cell()
                    .SetBackgroundColor(NavyBlue)
                    .SetBorder(Border.NO_BORDER)
                    .SetPadding(8);
                hc.Add(new Paragraph(h.ToUpperInvariant())
                    .SetFont(fontBold).SetFontSize(8).SetFontColor(ColorConstants.WHITE));
                table.AddHeaderCell(hc);
            }

            return table;
        }

        private static Cell BodyCell(string? text, bool bold = false, bool center = false, bool right = false, bool danger = false)
        {
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontReg = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var cell = new Cell().SetBorder(new SolidBorder(new DeviceRgb(229, 231, 235), 0.5f))
                                  .SetPadding(7);

            if (center) cell.SetTextAlignment(TextAlignment.CENTER);
            else if (right) cell.SetTextAlignment(TextAlignment.RIGHT);

            var p = new Paragraph(string.IsNullOrEmpty(text) ? "—" : text)
                .SetFont(bold ? fontBold : fontReg).SetFontSize(9);

            if (danger) p.SetFontColor(DangerRed);

            cell.Add(p);
            return cell;
        }

        private static Cell SummaryBox(string label, string value)
        {
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontReg = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var cell = new Cell().SetBorder(Border.NO_BORDER)
                                  .SetPadding(10)
                                  .SetBackgroundColor(LightGrey);

            cell.Add(new Paragraph(value)
                .SetFont(fontBold).SetFontSize(16).SetFontColor(NavyBlue).SetMarginBottom(0));
            cell.Add(new Paragraph(label)
                .SetFont(fontReg).SetFontSize(8).SetFontColor(MidGrey).SetMarginTop(2));

            return cell;
        }

        // =================================================================
        // PHASE 4 — REPORT 1: COMPLETED TESTS
        // =================================================================

        public byte[] GenerateCompletedTestsReport(
            int technicianId, DateTime fromDate, DateTime toDate, string? categoryFilter = null)
        {
            var tech = _context.Technicians.AsNoTracking()
                .Where(t => t.TechnicianId == technicianId)
                .Select(t => new { t.Name, t.Surname }).FirstOrDefault();

            var techName = tech == null ? "Technician" : $"{tech.Name} {tech.Surname}";

            var query = _context.RequestTestTypes.AsNoTracking()
                .Where(rtt => rtt.TechnicianId == technicianId
                           && rtt.CompletedAt != null
                           && rtt.CompletedAt >= fromDate.Date
                           && rtt.CompletedAt < toDate.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                query = query.Where(rtt => rtt.TestType != null
                                        && rtt.TestType.Category != null
                                        && rtt.TestType.Category.CategoryName == categoryFilter);
            }

            var rows = query.Select(rtt => new
            {
                RequestNumber = rtt.Request != null ? rtt.Request.RequestNumber : "—",
                TestName = rtt.TestType != null ? rtt.TestType.TestName : "—",
                CategoryName = rtt.TestType != null && rtt.TestType.Category != null
                                    ? rtt.TestType.Category.CategoryName : "—",
                PatientName = rtt.Request != null && rtt.Request.Patient != null
                                    ? rtt.Request.Patient.Name + " " + rtt.Request.Patient.Surname : "—",
                rtt.ResultValue,
                rtt.IsAbnormal,
                Units = rtt.TestType != null ? rtt.TestType.Units : null,
                rtt.CompletedAt,
                Urgency = rtt.Request != null ? rtt.Request.Urgency : "—"
            })
            .OrderByDescending(x => x.CompletedAt)
            .ToList();

            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(60, 40, 60, 40);

                AttachHeaderAndFooter(pdf, "Completed Tests Report", "NMB-HLabSys", techName);

                AddTitleBlock(doc,
                    "Completed Tests Report",
                    $"Date range: {fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}",
                    string.IsNullOrWhiteSpace(categoryFilter) ? "All categories" : $"Category filter: {categoryFilter}");

                var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1 }))
                    .UseAllAvailableWidth().SetMarginBottom(16);

                summary.AddCell(SummaryBox("Total tests", rows.Count.ToString()));
                summary.AddCell(SummaryBox("Abnormal results", rows.Count(x => x.IsAbnormal).ToString()));
                summary.AddCell(SummaryBox("Urgent / Stat", rows.Count(x => x.Urgency == "Urgent" || x.Urgency == "Stat").ToString()));
                doc.Add(summary);

                if (!rows.Any())
                {
                    doc.Add(new Paragraph("No completed tests were found in this period.")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(40));
                }
                else
                {
                    var table = CreateStyledTable(
                        new[] { "Request #", "Test", "Category", "Patient", "Result", "Completed", "Urgency" },
                        new float[] { 12, 20, 14, 20, 12, 12, 10 });

                    foreach (var r in rows)
                    {
                        table.AddCell(BodyCell(r.RequestNumber, bold: true));
                        table.AddCell(BodyCell(r.TestName));
                        table.AddCell(BodyCell(r.CategoryName));
                        table.AddCell(BodyCell(r.PatientName));
                        table.AddCell(BodyCell(
                            $"{r.ResultValue?.ToString("0.##") ?? "—"} {r.Units}".Trim(),
                            danger: r.IsAbnormal));
                        table.AddCell(BodyCell(r.CompletedAt?.ToString("dd MMM HH:mm") ?? "—"));
                        table.AddCell(BodyCell(r.Urgency, center: true));
                    }

                    doc.Add(table);
                }

                doc.Close();
            }

            return ms.ToArray();
        }

        // =================================================================
        // PHASE 4 — REPORT 2: TURNAROUND TIME
        // =================================================================

        public byte[] GenerateTurnaroundReport(int technicianId, DateTime fromDate, DateTime toDate)
        {
            var tech = _context.Technicians.AsNoTracking()
                .Where(t => t.TechnicianId == technicianId)
                .Select(t => new { t.Name, t.Surname }).FirstOrDefault();

            var techName = tech == null ? "Technician" : $"{tech.Name} {tech.Surname}";

            var raw = _context.RequestTestTypes.AsNoTracking()
                .Where(rtt => rtt.TechnicianId == technicianId
                           && rtt.StartedAt != null
                           && rtt.CompletedAt != null
                           && rtt.CompletedAt >= fromDate.Date
                           && rtt.CompletedAt < toDate.Date.AddDays(1))
                .Select(rtt => new
                {
                    RequestNumber = rtt.Request != null ? rtt.Request.RequestNumber : "—",
                    TestName = rtt.TestType != null ? rtt.TestType.TestName : "—",
                    ExpectedMinutes = rtt.TestType != null ? rtt.TestType.TurnaroundMinutes : 0,
                    StartedAt = rtt.StartedAt!.Value,
                    CompletedAt = rtt.CompletedAt!.Value
                })
                .OrderByDescending(x => x.CompletedAt)
                .ToList();

            var rows = raw.Select(r => new
            {
                r.RequestNumber,
                r.TestName,
                r.ExpectedMinutes,
                r.StartedAt,
                r.CompletedAt,
                ActualMinutes = (r.CompletedAt - r.StartedAt).TotalMinutes
            }).ToList();

            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(60, 40, 60, 40);

                AttachHeaderAndFooter(pdf, "Turnaround-Time Report", "NMB-HLabSys", techName);

                AddTitleBlock(doc,
                    "Turnaround-Time Report",
                    $"Date range: {fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}",
                    "How long each test took vs. its expected turnaround time");

                if (rows.Any())
                {
                    var avgActual = rows.Average(r => r.ActualMinutes);
                    var avgExpected = rows.Average(r => r.ExpectedMinutes);
                    var onTimeCount = rows.Count(r => r.ActualMinutes <= r.ExpectedMinutes);
                    var lateCount = rows.Count - onTimeCount;

                    var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }))
                        .UseAllAvailableWidth().SetMarginBottom(16);
                    summary.AddCell(SummaryBox("Tests measured", rows.Count.ToString()));
                    summary.AddCell(SummaryBox("Avg actual (min)", avgActual.ToString("0.0")));
                    summary.AddCell(SummaryBox("Avg expected (min)", avgExpected.ToString("0.0")));
                    summary.AddCell(SummaryBox("On-time / late", $"{onTimeCount} / {lateCount}"));
                    doc.Add(summary);

                    var table = CreateStyledTable(
                        new[] { "Request #", "Test", "Started", "Completed", "Actual (min)", "Expected (min)", "Variance" },
                        new float[] { 12, 22, 12, 12, 11, 12, 10 });

                    foreach (var r in rows)
                    {
                        var variance = r.ActualMinutes - r.ExpectedMinutes;
                        var varianceText = variance >= 0 ? $"+{variance:0.0}" : variance.ToString("0.0");

                        table.AddCell(BodyCell(r.RequestNumber, bold: true));
                        table.AddCell(BodyCell(r.TestName));
                        table.AddCell(BodyCell(r.StartedAt.ToString("dd MMM HH:mm")));
                        table.AddCell(BodyCell(r.CompletedAt.ToString("dd MMM HH:mm")));
                        table.AddCell(BodyCell(r.ActualMinutes.ToString("0.0"), right: true));
                        table.AddCell(BodyCell(r.ExpectedMinutes.ToString("0"), right: true));
                        table.AddCell(BodyCell(varianceText, right: true, danger: variance > 0));
                    }

                    doc.Add(table);
                }
                else
                {
                    doc.Add(new Paragraph("No tests with recorded start & completion times in this period.")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(40));
                }

                doc.Close();
            }

            return ms.ToArray();
        }

        // =================================================================
        // PHASE 4 — REPORT 3: MY CONSUMABLE USAGE
        // =================================================================

        public byte[] GenerateConsumableUsageReport(int technicianId, DateTime fromDate, DateTime toDate)
        {
            var tech = _context.Technicians.AsNoTracking()
                .Where(t => t.TechnicianId == technicianId)
                .Select(t => new { t.Name, t.Surname }).FirstOrDefault();

            var techName = tech == null ? "Technician" : $"{tech.Name} {tech.Surname}";

            var rows = _context.TestConsumableUsages.AsNoTracking()
                .Where(u => u.UsedByTechnicianId == technicianId
                         && u.UsedAt >= fromDate.Date
                         && u.UsedAt < toDate.Date.AddDays(1))
                .Select(u => new
                {
                    RequestNumber = u.RequestTest != null && u.RequestTest.Request != null
                                        ? u.RequestTest.Request.RequestNumber : "—",
                    TestName = u.RequestTest != null && u.RequestTest.TestType != null
                                        ? u.RequestTest.TestType.TestName : "—",
                    ConsumableName = u.Consumable != null ? u.Consumable.ConsumableName : "—",
                    u.QuantityUsed,
                    u.UsedAt
                })
                .OrderByDescending(u => u.UsedAt)
                .ToList();

            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(60, 40, 60, 40);

                AttachHeaderAndFooter(pdf, "My Consumable Usage Report", "NMB-HLabSys", techName);

                AddTitleBlock(doc,
                    "My Consumable Usage Report",
                    $"Date range: {fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}",
                    "Consumables you have consumed while performing tests");

                var byConsumable = rows
                    .GroupBy(r => r.ConsumableName)
                    .Select(g => new { Name = g.Key, Quantity = g.Sum(x => x.QuantityUsed), Events = g.Count() })
                    .OrderByDescending(g => g.Quantity)
                    .ToList();

                var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1 }))
                    .UseAllAvailableWidth().SetMarginBottom(16);
                summary.AddCell(SummaryBox("Usage events", rows.Count.ToString()));
                summary.AddCell(SummaryBox("Total units", rows.Sum(r => r.QuantityUsed).ToString()));
                summary.AddCell(SummaryBox("Distinct consumables", byConsumable.Count.ToString()));
                doc.Add(summary);

                if (byConsumable.Any())
                {
                    doc.Add(new Paragraph("Summary by consumable")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(12).SetFontColor(NavyBlue).SetMarginTop(4).SetMarginBottom(8));

                    var ct = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1 }))
                        .UseAllAvailableWidth().SetMarginBottom(18);

                    foreach (var h in new[] { "Consumable", "Units", "Events" })
                    {
                        var hc = new Cell().SetBackgroundColor(LightGrey).SetBorder(Border.NO_BORDER).SetPadding(6);
                        hc.Add(new Paragraph(h.ToUpperInvariant())
                            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                            .SetFontSize(8).SetFontColor(NavyBlue));
                        ct.AddHeaderCell(hc);
                    }

                    foreach (var g in byConsumable)
                    {
                        ct.AddCell(BodyCell(g.Name, bold: true));
                        ct.AddCell(BodyCell(g.Quantity.ToString(), right: true));
                        ct.AddCell(BodyCell(g.Events.ToString(), right: true));
                    }

                    doc.Add(ct);
                }

                if (rows.Any())
                {
                    doc.Add(new Paragraph("Detailed usage log")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                        .SetFontSize(12).SetFontColor(NavyBlue).SetMarginBottom(8));

                    var table = CreateStyledTable(
                        new[] { "When", "Request #", "Test", "Consumable", "Qty" },
                        new float[] { 14, 14, 30, 30, 8 });

                    foreach (var r in rows)
                    {
                        table.AddCell(BodyCell(r.UsedAt.ToString("dd MMM yyyy HH:mm")));
                        table.AddCell(BodyCell(r.RequestNumber, bold: true));
                        table.AddCell(BodyCell(r.TestName));
                        table.AddCell(BodyCell(r.ConsumableName));
                        table.AddCell(BodyCell(r.QuantityUsed.ToString(), right: true));
                    }

                    doc.Add(table);
                }
                else
                {
                    doc.Add(new Paragraph("No consumable usage recorded in this period.")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(40));
                }

                doc.Close();
            }

            return ms.ToArray();
        }

        // =================================================================
        // PHASE 4 — REPORT 4: SUPPLY REQUESTS
        // =================================================================

        public byte[] GenerateSupplyRequestsReport(int technicianId, DateTime fromDate, DateTime toDate)
        {
            var tech = _context.Technicians.AsNoTracking()
                .Where(t => t.TechnicianId == technicianId)
                .Select(t => new { t.Name, t.Surname }).FirstOrDefault();

            var techName = tech == null ? "Technician" : $"{tech.Name} {tech.Surname}";

            var rows = _context.ConsumableRequests.AsNoTracking()
                .Where(r => r.TechnicianId == technicianId
                         && r.RequestedAt >= fromDate.Date
                         && r.RequestedAt < toDate.Date.AddDays(1))
                .Select(r => new
                {
                    r.RequestNumber,
                    ConsumableName = r.Consumable != null ? r.Consumable.ConsumableName : "—",
                    r.QuantityRequested,
                    r.QuantityApproved,
                    r.QuantityFulfilled,
                    r.Urgency,
                    r.Status,
                    r.Reason,
                    r.RequestedAt,
                    r.DecisionAt,
                    r.ManagerDecisionNote
                })
                .OrderByDescending(r => r.RequestedAt)
                .ToList();

            using var ms = new MemoryStream();
            using (var writer = new PdfWriter(ms))
            using (var pdf = new PdfDocument(writer))
            using (var doc = new Document(pdf, PageSize.A4))
            {
                doc.SetMargins(60, 40, 60, 40);

                AttachHeaderAndFooter(pdf, "Supply Requests Report", "NMB-HLabSys", techName);

                AddTitleBlock(doc,
                    "Supply Requests Report",
                    $"Date range: {fromDate:dd MMM yyyy} – {toDate:dd MMM yyyy}",
                    "Status of stock requests you submitted to the Lab Manager");

                var summary = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1, 1 }))
                    .UseAllAvailableWidth().SetMarginBottom(16);
                summary.AddCell(SummaryBox("Total", rows.Count.ToString()));
                summary.AddCell(SummaryBox("Pending", rows.Count(r => r.Status == "Pending").ToString()));
                summary.AddCell(SummaryBox("Approved", rows.Count(r => r.Status == "Approved" || r.Status == "PartiallyApproved").ToString()));
                summary.AddCell(SummaryBox("Fulfilled", rows.Count(r => r.Status == "Fulfilled" || r.Status == "PartiallyFulfilled").ToString()));
                summary.AddCell(SummaryBox("Rejected", rows.Count(r => r.Status == "Rejected").ToString()));
                doc.Add(summary);

                if (!rows.Any())
                {
                    doc.Add(new Paragraph("No supply requests submitted in this period.")
                        .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                        .SetFontSize(11).SetFontColor(MidGrey).SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(40));
                }
                else
                {
                    var table = CreateStyledTable(
                        new[] { "Request #", "Consumable", "Qty", "Approved", "Status", "Urgency", "Requested", "Decision Note" },
                        new float[] { 11, 16, 6, 8, 12, 9, 11, 27 });

                    foreach (var r in rows)
                    {
                        table.AddCell(BodyCell(r.RequestNumber, bold: true));
                        table.AddCell(BodyCell(r.ConsumableName));
                        table.AddCell(BodyCell(r.QuantityRequested.ToString(), right: true));
                        table.AddCell(BodyCell(r.QuantityApproved?.ToString() ?? "—", right: true));
                        table.AddCell(BodyCell(r.Status, danger: r.Status == "Rejected"));
                        table.AddCell(BodyCell(r.Urgency, center: true));
                        table.AddCell(BodyCell(r.RequestedAt.ToString("dd MMM HH:mm")));
                        table.AddCell(BodyCell(r.ManagerDecisionNote ?? "—"));
                    }

                    doc.Add(table);
                }

                doc.Close();
            }

            return ms.ToArray();
        }
    }
}