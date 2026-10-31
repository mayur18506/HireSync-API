using HireSync.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HireSync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoryController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HistoryController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            var atsLogs = await _db.AtsScanLogs
                .OrderByDescending(x => x.ScannedAt)
                .Take(20)
                .ToListAsync();

            var interviewLogs = await _db.InterviewLogs
                .OrderByDescending(x => x.CompletedAt)
                .Take(20)
                .ToListAsync();

            return Ok(new
            {
                totalScans = atsLogs.Count,
                avgAtsScore = atsLogs.Count > 0 ? (int)Math.Round(atsLogs.Average(x => x.MatchPercentage)) : 0,
                totalInterviews = interviewLogs.Count,
                avgInterviewScore = interviewLogs.Count > 0 ? Math.Round(interviewLogs.Average(x => x.AverageScore), 1) : 0,
                atsLogs,
                interviewLogs
            });
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearHistory()
        {
            _db.AtsScanLogs.RemoveRange(_db.AtsScanLogs);
            _db.InterviewLogs.RemoveRange(_db.InterviewLogs);
            await _db.SaveChangesAsync();
            return Ok(new { message = "History cleared." });
        }
    }
}