using System.ComponentModel.DataAnnotations;

namespace HireSync.API.Models
{
    public class AtsScanLog
    {
        [Key]
        public int Id { get; set; }
        public string JobSnippet { get; set; } = string.Empty;
        public int MatchPercentage { get; set; }
        public string MatchingKeywords { get; set; } = string.Empty;
        public string MissingKeywords { get; set; } = string.Empty;
        public string ImprovementTips { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; } = DateTime.Now;
    }

    public class InterviewLog
    {
        [Key]
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public double AverageScore { get; set; }
        public string Verdict { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }
}