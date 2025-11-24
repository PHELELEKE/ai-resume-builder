namespace AIResumeBuilder.Services
{
    public interface IAIService
    {
        Task<string> GenerateContentAsync(string prompt);
    }

    public class AIService : IAIService
    {
        public async Task<string> GenerateContentAsync(string prompt)
        {
            // For now, return mock AI content
            await Task.Delay(500); // Simulate API call
            return $"AI Generated content for: {prompt}. This is mock AI response. Integrate with real AI API later.";
        }
    }
}