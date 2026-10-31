namespace HireSync.API.Models
{
    public class InterviewTurnRequestDto
    {
        public string Role { get; set; } = ".NET Developer";
        public int QuestionNumber { get; set; } = 1;
        public string? PreviousQuestion { get; set; }
        public string? UserAnswer { get; set; }
        public List<int> PreviousScores { get; set; } = new();
    }

    public class InterviewTurnResponseDto
    {
        public int Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string NextQuestion { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }
}