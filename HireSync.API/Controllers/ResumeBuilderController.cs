using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.API.Controllers
{
    public class EnhanceSummaryRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string TargetRole { get; set; } = ".NET Developer";
        public string Skills { get; set; } = string.Empty;
        public string CurrentSummary { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ResumeBuilderController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ResumeBuilderController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("enhance-summary")]
        public async Task<IActionResult> EnhanceSummary([FromBody] EnhanceSummaryRequest request)
        {
            try
            {
                string? apiKey = _configuration["Gemini:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "YAHAN_APNI_API_KEY_DAALO")
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                    string prompt = $@"
                        Write a strong, ATS-optimized 3-sentence professional resume summary for a '{request.TargetRole}'.
                        Key Technical Skills: {request.Skills}.
                        Draft Summary: {request.CurrentSummary}.
                        Return ONLY plain text (no markdown, no quotes, no headings).";

                    var body = new
                    {
                        contents = new[] { new { parts = new[] { new { text = prompt } } } }
                    };

                    var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var aiSummary = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text").GetString();

                        if (!string.IsNullOrWhiteSpace(aiSummary))
                            return Ok(new { summary = aiSummary.Trim() });
                    }
                }
            }
            catch
            {
                // Fallback niche handle karega
            }

            // Smart C# ATS Summary Generator (Instant Fallback)
            var topSkills = string.IsNullOrWhiteSpace(request.Skills)
                ? "C#, ASP.NET Core, Entity Framework Core, SQL Server, and React.js"
                : request.Skills;

            string polishedSummary =
                $"Results-driven {request.TargetRole} skilled in building scalable web applications and secure RESTful APIs using {topSkills}. " +
                $"Experienced in clean architecture, database optimization, and responsive frontend integration to deliver high-performance enterprise solutions. " +
                $"Passionate about writing maintainable code and solving real-world engineering challenges.";

            return Ok(new { summary = polishedSummary });
        }
    }
}