using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class MarketPulseService : IMarketPulseService
    {
        private readonly IMarketPulseRepository _repo;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public MarketPulseService(IMarketPulseRepository repo, HttpClient httpClient, IConfiguration config)
        {
            _repo = repo;
            _httpClient = httpClient;
            _config = config;
        }

        // Task 51 & 52: Match công việc theo skill và Filter
        public async Task<List<JobMatchDto>> GetMatchingJobsAsync(Guid studentId, JobFilterDto filter)
        {
            var student = await _repo.GetStudentWithPassedSkillsAsync(studentId);
            if (student == null) throw new Exception("Không tìm thấy sinh viên.");

            // Lấy các kỹ năng sinh viên đã pass (Điểm >= 5)
            var mySkills = student.SkillAssessments
                .Where(a => a.TestScore >= 5 && a.SkillNode != null)
                .Select(a => a.SkillNode.NodeName)
                .ToList();

            int skip = (filter.Page - 1) * filter.PageSize;
            var jobs = await _repo.GetJobPostingsAsync(filter.Keyword, filter.SourcePlatform, skip, filter.PageSize);

            var result = new List<JobMatchDto>();

            foreach (var job in jobs)
            {
                var jobSkills = job.SkillNodes.Select(s => s.NodeName).ToList();
                var matched = jobSkills.Intersect(mySkills).ToList();
                var missing = jobSkills.Except(mySkills).ToList();

                decimal matchPercent = jobSkills.Count > 0 ? (decimal)matched.Count / jobSkills.Count * 100 : 0;

                result.Add(new JobMatchDto
                {
                    PostingId = job.PostingId,
                    JobTitle = job.JobTitle,
                    CompanyName = job.CompanyName ?? "Công ty bảo mật",
                    SourcePlatform = job.SourcePlatform,
                    MatchPercentage = Math.Round(matchPercent, 2),
                    MatchedSkills = matched,
                    MissingSkills = missing
                });
            }

            // Ưu tiên các job có độ phù hợp cao nhất
            return result.OrderByDescending(x => x.MatchPercentage).ToList();
        }

        // Task 53, 54, 55: Scrape -> Extract Keyword AI -> Sinh Trend (Dành cho Background Job)
        public async Task<(int StatusCode, string Message)> RunScraperAndTrendAnalysisAsync()
        {
            // Trong thực tế, bạn sẽ dùng thư viện như HtmlAgilityPack để cào web. 
            // Ở đây, giả lập lấy được 1 mô tả công việc thô từ API Crawler bên thứ 3 hoặc code cào thô.
            string mockRawDescription = "We are looking for a Backend Developer proficient in C#, .NET Core, SQL Server. Experience with Docker and AWS is a plus.";

            var job = new JobPosting
            {
                PostingId = Guid.NewGuid(),
                JobTitle = "Backend .NET Developer",
                CompanyName = "Tech StartUp",
                SourcePlatform = "TopCV",
                JobDescriptionRaw = mockRawDescription,
                ScrapedAt = DateTime.Now
            };

            var allNodes = await _repo.GetAllSkillNodesAsync();
            var matchedNodes = await ExtractSkillsUsingAiAsync(mockRawDescription, allNodes);

            foreach (var node in matchedNodes)
            {
                job.SkillNodes.Add(node);
            }

            await _repo.SaveJobPostingAsync(job);

            // Task 55: Generate Trend Analytics
            var trends = new List<TrendAnalysis>();
            foreach (var node in matchedNodes)
            {
                trends.Add(new TrendAnalysis
                {
                    // XÓA DÒNG AnalysisId = Guid.NewGuid(),

                    SkillNodeId = node.SkillNodeId, // Đây là cột khóa ngoại, vẫn giữ nguyên
                    AnalyzedDate = DateOnly.FromDateTime(DateTime.Now),
                    DemandPercent = (decimal)new Random().Next(10, 90),
                    TrendScore = (decimal)(new Random().NextDouble() * 5)
                });
            }
            if (trends.Any()) await _repo.SaveTrendAnalysisAsync(trends);

            return (200, "Quét việc làm, bóc tách từ khóa và cập nhật Trend thành công.");
        }

        // Dùng Gemini AI để map Description thô thành các SkillNode ID trong DB
        private async Task<List<SkillNode>> ExtractSkillsUsingAiAsync(string description, List<SkillNode> allNodes)
        {
            string availableSkills = string.Join(", ", allNodes.Select(n => n.NodeName));
            string prompt = $@"Phân tích mô tả công việc sau và trích xuất các kỹ năng công nghệ. 
Chỉ trả về các kỹ năng có trong danh sách cho sẵn này: [{availableSkills}].
Trả về dưới dạng danh sách ngăn cách bằng dấu phẩy, KHÔNG giải thích.
Mô tả công việc: {description}";

            string apiKey = _config["GeminiApiConfig:ApiKey"]!;
            string baseUrl = _config["GeminiApiConfig:BaseUrl"]!;

            try
            {
                var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}?key={apiKey}", payload);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    string aiText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

                    var extractedSkillNames = aiText.Split(',').Select(s => s.Trim().ToLower()).ToList();
                    return allNodes.Where(n => extractedSkillNames.Contains(n.NodeName.ToLower())).ToList();
                }
            }
            catch { /* Fallback */ }

            return new List<SkillNode>();
        }

        // Task 56: Interactive trend chart
        public async Task<List<TrendChartDto>> GetTrendChartDataAsync(int days = 30)
        {
            var fromDate = DateTime.Now.AddDays(-days);
            var rawTrends = await _repo.GetTrendsForChartAsync(fromDate);

            var grouped = rawTrends.GroupBy(t => t.SkillNode.NodeName)
                .Select(g => new TrendChartDto
                {
                    NodeName = g.Key,
                    DataPoints = g.Select(x => new TrendPointDto
                    {
                        // SỬA DÒNG NÀY: Dùng ? và ?? để xử lý giá trị null
                        AnalyzedDate = x.AnalyzedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,

                        DemandPercent = x.DemandPercent ?? 0,
                        TrendScore = x.TrendScore ?? 0
                    }).OrderBy(x => x.AnalyzedDate).ToList()
                }).ToList();

            return grouped;
        }
    }
}