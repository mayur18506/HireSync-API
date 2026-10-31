namespace HireSync.API.Models
{
    public class AtsResultDto
    {
        public int MatchPercentage { get; set; }
        public List<string> MissingKeywords { get; set; }
        public List<string> MatchingKeywords { get; set; }
        public string ImprovementTips { get; set; }
    }
}