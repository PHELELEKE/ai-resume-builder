namespace AIResumeBuilder.Models
{
    public class ResumeAnalysisResult
    {
        public int Score { get; set; }
        public int MatchPercentage { get; set; }
        public List<string> Feedback { get; set; } = new List<string>();
        public List<string> KeywordMatches { get; set; } = new List<string>();
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Improvements { get; set; } = new List<string>();
        public string Assessment { get; set; } = string.Empty;
        public bool IsAIEnhanced { get; set; }
        public string FileName { get; set; } = "Resume";
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
        public string JobDescription { get; set; } = string.Empty;
        public string AnalysisSource { get; set; } = "AI Analysis";
    }

    // Add this class for JSON parsing from OpenRouter
    public class OpenRouterAnalysis
    {
        public int score { get; set; }
        public int matchPercentage { get; set; }
        public List<string> feedback { get; set; } = new List<string>();
        public List<string> missingKeywords { get; set; } = new List<string>();
        public List<string> strengths { get; set; } = new List<string>();
        public List<string> improvements { get; set; } = new List<string>();
        public string assessment { get; set; } = string.Empty;
    }
}