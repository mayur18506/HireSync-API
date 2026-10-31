using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HireSync.API.Models;

namespace HireSync.API.Services
{
    public interface IGeminiService
    {
        Task<AtsResultDto> AnalyzeResumeAsync(string resume, string jd);
        Task<InterviewTurnResponseDto> ProcessInterviewTurnAsync(InterviewTurnRequestDto request);
    }

    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<AtsResultDto> AnalyzeResumeAsync(string resume, string jd)
        {
            try
            {
                string? apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YAHAN_APNI_API_KEY_DAALO")
                    return RunSmartFallbackEngine(resume, jd);

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                string prompt = $@"
                    You are an expert technical ATS (Applicant Tracking System).
                    Compare the Resume with the Job Description carefully.
                    Resume: {resume}
                    Job Description: {jd}

                    Return ONLY valid JSON matching this exact structure:
                    {{
                        ""matchPercentage"": 78,
                        ""matchingKeywords"": [""C#"", ""ASP.NET Core""],
                        ""missingKeywords"": [""Docker"", ""Azure""],
                        ""improvementTips"": ""2-3 clear, actionable lines of advice to improve this resume for the JD.""
                    }}";

                var aiText = await CallGeminiJsonAsync(url, prompt);
                if (string.IsNullOrWhiteSpace(aiText))
                    return RunSmartFallbackEngine(resume, jd);

                var result = JsonSerializer.Deserialize<AtsResultDto>(
                    aiText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return result ?? RunSmartFallbackEngine(resume, jd);
            }
            catch
            {
                return RunSmartFallbackEngine(resume, jd);
            }
        }

        public async Task<InterviewTurnResponseDto> ProcessInterviewTurnAsync(InterviewTurnRequestDto request)
        {
            bool isFinalTurn = request.QuestionNumber > 5;

            try
            {
                string? apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YAHAN_APNI_API_KEY_DAALO")
                    return RunInterviewFallbackEngine(request);

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                string prompt = request.QuestionNumber == 1
                    ? $@"You are a friendly technical interviewer hiring for a '{request.Role}' role.
                         Ask Question #1 (concise, spoken-style, max 2 sentences).
                         Return ONLY valid JSON:
                         {{
                             ""score"": 0,
                             ""feedback"": """",
                             ""nextQuestion"": ""Your first interview question here?"",
                             ""isCompleted"": false
                         }}"
                    : $@"You are a technical interviewer for a '{request.Role}' role.
                         Previous Question: {request.PreviousQuestion}
                         Candidate's Answer: {request.UserAnswer}

                         1. Score the candidate's answer from 1 to 10.
                         2. Give short, encouraging, constructive feedback (max 2 sentences).
                         3. {(isFinalTurn ? "Set nextQuestion to 'Thank you! Your mock interview is complete. Check your final scorecard below.' and isCompleted to true." : $"Ask Question #{request.QuestionNumber} for the '{request.Role}' role (concise, different topic, max 2 sentences) and set isCompleted to false.")}

                         Return ONLY valid JSON:
                         {{
                             ""score"": 8,
                             ""feedback"": ""Brief feedback on previous answer."",
                             ""nextQuestion"": ""Next question or closing message."",
                             ""isCompleted"": {(isFinalTurn ? "true" : "false")}
                         }}";

                var aiText = await CallGeminiJsonAsync(url, prompt);
                if (string.IsNullOrWhiteSpace(aiText))
                    return RunInterviewFallbackEngine(request);

                var result = JsonSerializer.Deserialize<InterviewTurnResponseDto>(
                    aiText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (result == null)
                    return RunInterviewFallbackEngine(request);

                result.IsCompleted = isFinalTurn;
                return result;
            }
            catch
            {
                return RunInterviewFallbackEngine(request);
            }
        }

        private async Task<string?> CallGeminiJsonAsync(string url, string prompt)
        {
            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { responseMimeType = "application/json" }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode) return null;

            string responseString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseString);

            var parts = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            string aiTextResponse = "";
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                    aiTextResponse = textProp.GetString() ?? "";
            }

            return aiTextResponse.Replace("```json", "").Replace("```", "").Trim();
        }

        private InterviewTurnResponseDto RunInterviewFallbackEngine(InterviewTurnRequestDto request)
        {
            var questionBanks = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [".NET Developer"] = new[]
                {
                    "Can you explain Dependency Injection in ASP.NET Core and its main service lifetimes?",
                    "What is the difference between IEnumerable and IQueryable when querying data with Entity Framework Core?",
                    "How does middleware work in the ASP.NET Core request pipeline?",
                    "How do you secure an ASP.NET Core Web API using JWT authentication?",
                    "How does async and await improve scalability in a C# backend application?"
                },
                ["Full Stack (React + .NET)"] = new[]
                {
                    "How do you handle communication and CORS between a React frontend and an ASP.NET Core Web API?",
                    "What is the difference between useState and useEffect in React, and how do you avoid infinite re-renders?",
                    "How do you manage authentication tokens securely between React and a .NET backend?",
                    "Explain how Entity Framework Core tracks changes and optimizes SQL queries.",
                    "How would you implement real-time notifications in a React and ASP.NET Core app?"
                },
                ["SQL & Database Engineer"] = new[]
                {
                    "What is the difference between Clustered and Non-Clustered indexes in SQL Server?",
                    "Can you explain Primary Key, Foreign Key, and Unique constraints with a quick example?",
                    "What is the difference between INNER JOIN and LEFT JOIN?",
                    "How would you identify and optimize a slow-running SQL query?",
                    "What are ACID properties in database transactions?"
                }
            };

            if (!questionBanks.TryGetValue(request.Role, out var questions))
                questions = questionBanks[".NET Developer"];

            if (request.QuestionNumber == 1)
            {
                return new InterviewTurnResponseDto
                {
                    Score = 0,
                    Feedback = "",
                    NextQuestion = questions[0],
                    IsCompleted = false
                };
            }

            string answer = request.UserAnswer ?? "";
            int wordCount = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

            int score = wordCount switch
            {
                < 5 => 3,
                < 15 => 6,
                < 30 => 8,
                _ => 9
            };

            string feedback = score >= 8
                ? "Solid explanation! You covered the core concept clearly with good technical depth."
                : score >= 6
                    ? "Good start! Adding a quick real-world project example will make your answer even stronger."
                    : "Your answer was quite brief. Try elaborating on how it works under the hood with an example.";

            bool isCompleted = request.QuestionNumber > 5;
            string nextQ = isCompleted
                ? "Thank you! Your technical mock interview is complete. Review your scorecard below."
                : questions[Math.Min(request.QuestionNumber - 1, questions.Length - 1)];

            return new InterviewTurnResponseDto
            {
                Score = score,
                Feedback = feedback,
                NextQuestion = nextQ,
                IsCompleted = isCompleted
            };
        }

        private AtsResultDto RunSmartFallbackEngine(string resume, string jd)
        {
            string[] techDictionary = new[]
            {
                "C#", "ASP.NET Core", ".NET", "MVC", "Entity Framework", "EF Core", "LINQ", "ADO.NET",
                "SQL Server", "MySQL", "MongoDB", "PostgreSQL", "React", "Angular", "Vue", "JavaScript",
                "TypeScript", "HTML", "CSS", "Tailwind", "Bootstrap", "Node.js", "Docker", "Kubernetes",
                "AWS", "Azure", "CI/CD", "Microservices", "REST API", "GraphQL", "Git", "SignalR", "JWT"
            };

            var requiredInJd = techDictionary
                .Where(skill => jd.Contains(skill, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requiredInJd.Count == 0)
            {
                requiredInJd = Regex.Matches(jd, @"\b[A-Z][A-Za-z0-9+#.]+\b")
                    .Select(m => m.Value)
                    .Where(w => w.Length > 2 && !new[] { "The", "We", "Are", "Must", "With", "And", "For", "Looking" }.Contains(w))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToList();
            }

            var matching = requiredInJd
                .Where(skill => resume.Contains(skill, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var missing = requiredInJd
                .Except(matching, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int score = requiredInJd.Count > 0
                ? (int)Math.Round((double)matching.Count / requiredInJd.Count * 100)
                : 50;

            string advice = missing.Count == 0
                ? "Zabardast match! Aapke resume mein JD ke saare core technical skills maujood hain."
                : $"Score badhane ke liye apne projects mein {string.Join(", ", missing.Take(3))} ka practical use mention karein.";

            return new AtsResultDto
            {
                MatchPercentage = score,
                MatchingKeywords = matching,
                MissingKeywords = missing,
                ImprovementTips = advice
            };
        }
    }
}