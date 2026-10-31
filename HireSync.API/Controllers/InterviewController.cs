using HireSync.API.Data;
using HireSync.API.Models;
using HireSync.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace HireSync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly AppDbContext _db;

        public InterviewController(IGeminiService geminiService, AppDbContext db)
        {
            _geminiService = geminiService;
            _db = db;
        }

        [HttpPost("next-turn")]
        public async Task<IActionResult> NextTurn([FromBody] InterviewTurnRequestDto request)
        {
            var response = await _geminiService.ProcessInterviewTurnAsync(request);

            if (response.IsCompleted)
            {
                var allScores = new List<int>(request.PreviousScores ?? new List<int>()) { response.Score };
                double avg = Math.Round(allScores.Average(), 1);
                string verdict = avg >= 7.5 ? "Strong Hire" : avg >= 5.5 ? "Promising Candidate" : "Needs More Practice";

                _db.InterviewLogs.Add(new InterviewLog
                {
                    Role = request.Role,
                    AverageScore = avg,
                    Verdict = verdict,
                    CompletedAt = DateTime.Now
                });
                await _db.SaveChangesAsync();
            }

            return Ok(response);
        }
    }
}