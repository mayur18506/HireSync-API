using System.Text;
using HireSync.API.Data;
using HireSync.API.Models;
using HireSync.API.Services;
using Microsoft.AspNetCore.Mvc;
using UglyToad.PdfPig;

namespace HireSync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtsAnalyzerController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly AppDbContext _db;

        public AtsAnalyzerController(IGeminiService geminiService, AppDbContext db)
        {
            _geminiService = geminiService;
            _db = db;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeResume([FromBody] AtsRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.ResumeText) || string.IsNullOrWhiteSpace(request.JobDescription))
                return BadRequest("Resume aur Job Description dono zaruri hain.");

            var result = await _geminiService.AnalyzeResumeAsync(request.ResumeText, request.JobDescription);

            // Save scan result to SQL Server
            var snippet = request.JobDescription.Length > 85
                ? request.JobDescription.Substring(0, 85) + "..."
                : request.JobDescription;

            _db.AtsScanLogs.Add(new AtsScanLog
            {
                JobSnippet = snippet,
                MatchPercentage = result.MatchPercentage,
                MatchingKeywords = result.MatchingKeywords != null ? string.Join(", ", result.MatchingKeywords) : "",
                MissingKeywords = result.MissingKeywords != null ? string.Join(", ", result.MissingKeywords) : "",
                ImprovementTips = result.ImprovementTips ?? "",
                ScannedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            return Ok(result);
        }

        [HttpPost("upload-resume")]
        public async Task<IActionResult> UploadResume(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please ek valid PDF file select karein.");

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Sirf PDF (.pdf) resume hi upload karein.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            var extractedText = new StringBuilder();
            using (var document = PdfDocument.Open(stream))
            {
                foreach (var page in document.GetPages())
                {
                    var words = page.GetWords().Select(w => w.Text);
                    extractedText.AppendLine(string.Join(" ", words));
                }
            }

            return Ok(new { resumeText = extractedText.ToString().Trim() });
        }
    }
}