using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using AIResumeBuilder.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
namespace AIResumeBuilder.Controllers
{
    [Authorize]
    public class AIAnalysisController : Controller
    {
        private readonly string? _openRouterKey;
        private readonly ILogger<AIAnalysisController> _logger;

        public AIAnalysisController(IConfiguration configuration, ILogger<AIAnalysisController> logger)
        {
            _openRouterKey = configuration["OpenRouter:ApiKey"];
            _logger = logger;
            
            if (string.IsNullOrEmpty(_openRouterKey))
            {
                _logger.LogWarning("OpenRouter API key is not configured");
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeResume(IFormFile resumeFile, string jobDescription)
        {
            if (resumeFile == null || resumeFile.Length == 0)
            {
                TempData["Error"] = "Please select a resume file to analyze.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrEmpty(jobDescription))
            {
                TempData["Error"] = "Please provide a job description for targeted analysis.";
                return RedirectToAction("Index");
            }

            try
            {
                // Check file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
                var fileExtension = Path.GetExtension(resumeFile.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Please upload PDF, Word, or Text files only.";
                    return RedirectToAction("Index");
                }

                // Get the text from the hidden field (converted in browser)
                string resumeText = Request.Form["resumeText"].ToString() ?? string.Empty;

                // If no text from hidden field, fallback to original file reading for .txt files
                if (string.IsNullOrEmpty(resumeText))
                {
                    using (var stream = new StreamReader(resumeFile.OpenReadStream()))
                    {
                        resumeText = await stream.ReadToEndAsync() ?? string.Empty;
                    }
                }

                // Clean and normalize the resume text
                resumeText = CleanResumeText(resumeText);

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    TempData["Error"] = "Resume file appears to be empty or too short. Please try a different file.";
                    return RedirectToAction("Index");
                }

                // Use real AI APIs with your keys
                ResumeAnalysisResult analysisResult;
                
                // Try OpenRouter first if API key is available
                if (!string.IsNullOrEmpty(_openRouterKey))
                {
                    try
                    {
                        analysisResult = await AnalyzeWithOpenRouter(resumeText, jobDescription);
                        analysisResult.IsAIEnhanced = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "OpenRouter analysis failed, using fallback");
                        // Fallback to enhanced analysis
                        analysisResult = await AnalyzeWithStrictLocalAI(resumeText, jobDescription);
                        analysisResult.IsAIEnhanced = false;
                        analysisResult.AnalysisSource = "Strict Local Analysis (API Failed)";
                    }
                }
                else
                {
                    // Use local analysis if no API key
                    analysisResult = await AnalyzeWithStrictLocalAI(resumeText, jobDescription);
                    analysisResult.IsAIEnhanced = false;
                    analysisResult.AnalysisSource = "Strict Local Analysis";
                }

                analysisResult.FileName = resumeFile.FileName;
                analysisResult.JobDescription = jobDescription;

                return View("AnalysisResult", analysisResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing resume");
                TempData["Error"] = $"Error analyzing resume: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private string CleanResumeText(string resumeText)
        {
            if (string.IsNullOrEmpty(resumeText)) return resumeText;
            
            // Remove excessive whitespace
            resumeText = Regex.Replace(resumeText, @"\s+", " ");
            
            // Remove common PDF extraction artifacts
            resumeText = Regex.Replace(resumeText, @"\b\d+\b", " "); // Remove standalone numbers
            resumeText = Regex.Replace(resumeText, @"[^\w\s\.@-]", " "); // Keep only words, spaces, dots, @, -
            
            return resumeText.Trim();
        }

        private async Task<ResumeAnalysisResult> AnalyzeWithOpenRouter(string resumeText, string jobDescription)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(45);

                var apiUrl = "https://openrouter.ai/api/v1/chat/completions";
                
                var prompt = $@"ANALYZE THIS RESUME AGAINST JOB DESCRIPTION. BE VERY CRITICAL AND REALISTIC.

JOB DESCRIPTION:
{jobDescription.Substring(0, Math.Min(1500, jobDescription.Length))}

RESUME CONTENT:
{resumeText.Substring(0, Math.Min(2000, resumeText.Length))}

IMPORTANT: Be extremely critical. If skills don't match, give LOW score. 
Score based on ACTUAL match, not potential.

RESPOND WITH JSON ONLY:
{{
    ""score"": 0,
    ""matchPercentage"": 0,
    ""feedback"": [""specific feedback here""],
    ""missingKeywords"": [""keyword1""],
    ""strengths"": [""strength1""],
    ""improvements"": [""improvement1""],
    ""assessment"": ""brief assessment""
}}";

                var requestData = new
                {
                    model = "google/gemini-pro-1.5",
                    messages = new[]
                    {
                        new { 
                            role = "user", 
                            content = prompt 
                        }
                    },
                    max_tokens = 1500,
                    temperature = 0.1
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Content = content;
                request.Headers.Add("Authorization", $"Bearer {_openRouterKey}");
                request.Headers.Add("HTTP-Referer", Request.Scheme + "://" + Request.Host);
                request.Headers.Add("X-Title", "AI Resume Analyzer");

                var response = await httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = ParseOpenRouterResponse(responseContent);
                    result.AnalysisSource = "OpenRouter AI (Critical Analysis)";
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenRouter API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"OpenRouter analysis failed: {ex.Message}");
            }
        }

        // ADD THIS MISSING METHOD
        private ResumeAnalysisResult ParseOpenRouterResponse(string responseContent)
        {
            try
            {
                using var document = JsonDocument.Parse(responseContent);
                var choices = document.RootElement.GetProperty("choices");
                var firstChoice = choices.EnumerateArray().First();
                var messageContent = firstChoice.GetProperty("message").GetProperty("content").GetString();

                if (string.IsNullOrEmpty(messageContent))
                {
                    throw new Exception("Empty response from AI");
                }

                // Extract JSON from the response
                var jsonStart = messageContent.IndexOf('{');
                var jsonEnd = messageContent.LastIndexOf('}') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = messageContent.Substring(jsonStart, jsonEnd - jsonStart);
                    var analysisData = JsonSerializer.Deserialize<OpenRouterAnalysis>(jsonContent);

                    return new ResumeAnalysisResult
                    {
                        Score = analysisData?.score ?? 50,
                        MatchPercentage = analysisData?.matchPercentage ?? 50,
                        Feedback = analysisData?.feedback ?? new List<string> { "AI analysis completed" },
                        KeywordMatches = analysisData?.missingKeywords ?? new List<string>(),
                        Assessment = analysisData?.assessment ?? "Analysis completed",
                        Strengths = analysisData?.strengths ?? new List<string>(),
                        Improvements = analysisData?.improvements ?? new List<string>()
                    };
                }

                throw new Exception("Could not parse AI response");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse OpenRouter response, using fallback");
                return new ResumeAnalysisResult
                {
                    Score = 45,
                    MatchPercentage = 45,
                    Feedback = new List<string>
                    {
                        "🤖 **AI Analysis Completed**",
                        "⚠️ **API Response Parse Issue**",
                        "💡 Using fallback analysis method"
                    },
                    Assessment = "Analysis completed with fallback method",
                    AnalysisSource = "OpenRouter (Fallback)"
                };
            }
        }

        private async Task<ResumeAnalysisResult> AnalyzeWithStrictLocalAI(string resumeText, string jobDescription)
        {
            await Task.Delay(300);

            var score = CalculateStrictScore(resumeText, jobDescription);
            var feedback = GenerateStrictFeedback(resumeText, jobDescription, score);
            var missingKeywords = FindCriticalMissingKeywords(resumeText, jobDescription);

            return new ResumeAnalysisResult
            {
                Score = score,
                MatchPercentage = score,
                Feedback = feedback,
                KeywordMatches = missingKeywords,
                Assessment = GetStrictAssessment(score),
                AnalysisSource = "Strict Local Analysis"
            };
        }

        private int CalculateStrictScore(string resumeText, string jobDescription)
        {
            int score = 30; // Start very low - be strict!

            var resumeLower = resumeText.ToLower();
            var jobLower = jobDescription.ToLower();

            // 1. Job Role Match (20 points max)
            score += CalculateRoleMatch(resumeLower, jobLower);

            // 2. Hard Skills Match (30 points max)
            score += CalculateHardSkillsMatch(resumeLower, jobLower);

            // 3. Experience Match (20 points max)
            score += CalculateExperienceMatch(resumeLower, jobLower);

            // 4. Education Match (10 points max)
            score += CalculateEducationMatch(resumeLower, jobLower);

            // 5. Keyword Match (20 points max)
            score += CalculateKeywordMatch(resumeLower, jobLower);

            return Math.Min(Math.Max(score, 10), 95);
        }

        private int CalculateRoleMatch(string resumeLower, string jobLower)
        {
            var jobRoles = ExtractJobRoles(jobLower);
            var resumeRoles = ExtractResumeRoles(resumeLower);

            if (!jobRoles.Any()) return 5;

            int matches = jobRoles.Count(role => resumeRoles.Contains(role));
            return (int)((double)matches / jobRoles.Count * 20);
        }

        private int CalculateHardSkillsMatch(string resumeLower, string jobLower)
        {
            var requiredSkills = ExtractHardSkills(jobLower);
            if (!requiredSkills.Any()) return 10;

            int found = requiredSkills.Count(skill => resumeLower.Contains(skill));
            return (int)((double)found / requiredSkills.Count * 30);
        }

        private int CalculateExperienceMatch(string resumeLower, string jobLower)
        {
            var jobYears = ExtractYearsFromText(jobLower);
            var resumeYears = ExtractYearsFromText(resumeLower);

            if (jobYears == 0) return 5;

            if (resumeYears >= jobYears) return 20;
            if (resumeYears >= jobYears - 1) return 15;
            if (resumeYears >= jobYears - 2) return 10;
            if (resumeYears >= jobYears - 3) return 5;
            return 0;
        }

        private int CalculateEducationMatch(string resumeLower, string jobLower)
        {
            var jobEducation = ExtractEducationRequirements(jobLower);
            var resumeEducation = ExtractEducationFromResume(resumeLower);

            if (!jobEducation.Any()) return 5;

            int matches = jobEducation.Count(edu => resumeEducation.Contains(edu));
            return matches * 5;
        }

        private int CalculateKeywordMatch(string resumeLower, string jobLower)
        {
            var jobKeywords = jobLower.Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(w => w.Length > 4)
                                    .Distinct()
                                    .ToArray();

            if (!jobKeywords.Any()) return 5;

            int matches = jobKeywords.Count(keyword => resumeLower.Contains(keyword));
            return (int)((double)matches / jobKeywords.Length * 20);
        }

        private List<string> GenerateStrictFeedback(string resumeText, string jobDescription, int score)
        {
            var feedback = new List<string>
            {
                "🎯 **STRICT Resume Analysis**",
                $"📊 **Match Score: {score}%**",
                ""
            };

            var resumeLower = resumeText.ToLower();
            var jobLower = jobDescription.ToLower();

            // Score-based assessment
            if (score >= 75)
            {
                feedback.Add("✅ **Good Match** - Meets key requirements");
            }
            else if (score >= 60)
            {
                feedback.Add("⚠️ **Moderate Match** - Some gaps need addressing");
            }
            else if (score >= 40)
            {
                feedback.Add("🔶 **Weak Match** - Significant improvements needed");
            }
            else
            {
                feedback.Add("🚨 **Poor Match** - Major skill/experience gaps");
            }

            feedback.Add("");

            // Specific gaps
            var missingHardSkills = ExtractHardSkills(jobLower)
                .Where(skill => !resumeLower.Contains(skill))
                .Take(5)
                .ToList();

            if (missingHardSkills.Any())
            {
                feedback.Add("🔧 **Missing Hard Skills:**");
                foreach (var skill in missingHardSkills)
                {
                    feedback.Add($"   • {skill}");
                }
                feedback.Add("");
            }

            // Experience gap
            var jobYears = ExtractYearsFromText(jobLower);
            var resumeYears = ExtractYearsFromText(resumeLower);
            if (jobYears > 0 && resumeYears < jobYears)
            {
                feedback.Add($"⏰ **Experience Gap:** Requires {jobYears}+ years, you have ~{resumeYears}");
                feedback.Add("");
            }

            // Role mismatch
            var jobRoles = ExtractJobRoles(jobLower);
            var resumeRoles = ExtractResumeRoles(resumeLower);
            var roleGaps = jobRoles.Except(resumeRoles).Take(3).ToList();
            if (roleGaps.Any())
            {
                feedback.Add("🎭 **Role Alignment:**");
                feedback.Add($"   • Target roles: {string.Join(", ", jobRoles)}");
                feedback.Add($"   • Your roles: {string.Join(", ", resumeRoles.Take(3))}");
                feedback.Add("");
            }

            feedback.Add("💡 **Recommendation:** Focus on the specific missing skills and requirements above");

            return feedback;
        }

        // Helper methods
        private List<string> ExtractJobRoles(string jobLower)
        {
            var roles = new List<string>();
            var commonRoles = new[] { "engineer", "developer", "manager", "analyst", "architect", "specialist", "consultant" };
            
            foreach (var role in commonRoles)
            {
                if (jobLower.Contains(role))
                    roles.Add(role);
            }
            return roles.Distinct().ToList();
        }

        private List<string> ExtractResumeRoles(string resumeLower)
        {
            var roles = new List<string>();
            var commonRoles = new[] { "engineer", "developer", "manager", "analyst", "architect", "specialist", "consultant" };
            
            foreach (var role in commonRoles)
            {
                if (resumeLower.Contains(role))
                    roles.Add(role);
            }
            return roles.Distinct().ToList();
        }

        private List<string> ExtractHardSkills(string jobLower)
        {
            var skills = new List<string>();
            var techSkills = new[]
            {
                "python", "java", "javascript", "c#", "c++", "sql", "react", "angular", "vue",
                "aws", "azure", "docker", "kubernetes", "jenkins", "git", "rest", "api",
                "machine learning", "ai", "data analysis", "cloud", "devops", "agile", "scrum",
                "typescript", "node", "express", "mongodb", "mysql", "postgresql", "linux"
            };

            foreach (var skill in techSkills)
            {
                if (jobLower.Contains(skill))
                    skills.Add(skill);
            }
            return skills.Distinct().ToList();
        }

        private List<string> ExtractEducationRequirements(string jobLower)
        {
            var education = new List<string>();
            if (jobLower.Contains("bachelor") || jobLower.Contains("degree")) education.Add("bachelor");
            if (jobLower.Contains("master")) education.Add("master");
            if (jobLower.Contains("phd") || jobLower.Contains("doctorate")) education.Add("phd");
            return education;
        }

        private List<string> ExtractEducationFromResume(string resumeLower)
        {
            var education = new List<string>();
            if (resumeLower.Contains("bachelor")) education.Add("bachelor");
            if (resumeLower.Contains("master")) education.Add("master");
            if (resumeLower.Contains("phd") || resumeLower.Contains("doctorate")) education.Add("phd");
            return education;
        }

        private int ExtractYearsFromText(string text)
        {
            var patterns = new[]
            {
                @"(\d+)\+ years",
                @"(\d+)-(\d+) years",
                @"(\d+) years",
                @"minimum (\d+) years",
                @"at least (\d+) years"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int years))
                {
                    return years;
                }
            }
            return 0;
        }

        private List<string> FindCriticalMissingKeywords(string resumeText, string jobDescription)
        {
            var resumeLower = resumeText.ToLower();
            var jobLower = jobDescription.ToLower();
            
            var missing = new List<string>();
            var importantTerms = new[]
            {
                "python", "java", "javascript", "c#", "c++", "sql", "react", "angular", "vue",
                "aws", "azure", "docker", "kubernetes", "jenkins", "git", "rest", "api",
                "machine learning", "ai", "data analysis", "cloud", "devops", "agile", "scrum"
            };

            foreach (var term in importantTerms)
            {
                if (jobLower.Contains(term) && !resumeLower.Contains(term))
                    missing.Add(term);
            }

            return missing.Distinct().Take(8).ToList();
        }

        private string GetStrictAssessment(int score)
        {
            return score switch
            {
                >= 75 => "Good alignment with most key requirements",
                >= 60 => "Moderate match with some important skills present",
                >= 40 => "Weak match requiring substantial improvements",
                _ => "Poor alignment - consider different roles or major resume overhaul"
            };
        }
    }

}
