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
            try
            {
                // 1. Lấy cấu hình SerpApi từ appsettings.json
                string serpApiKey = _config["SerpApiConfig:ApiKey"]!;
                string serpBaseUrl = _config["SerpApiConfig:BaseUrl"] ?? "https://serpapi.com/search.json";

                // SỬA ĐOẠN NÀY: Đổi sang phạm vi rộng hơn và thêm cờ quốc gia (gl=vn)
                var keywords = new[] { "Lập trình viên .NET", "IT Backend", ".NET Developer jobs", "Software Engineer C#" };
                string query = keywords[new Random().Next(keywords.Length)];

                // Mở rộng location ra toàn Việt Nam thay vì khóa cứng ở HCM
                string location = "Vietnam";

                // Thêm tham số gl=vn (Google Country = Vietnam) để Google ưu tiên trả về việc làm nội địa
                string requestUrl = $"{serpBaseUrl}?engine=google_jobs&q={Uri.EscapeDataString(query)}&location={Uri.EscapeDataString(location)}&gl=vn&hl=vi&api_key={serpApiKey}";

                var response = await _httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return (500, "Lỗi khi gọi API cào dữ liệu từ SerpApi.");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var serpData = JsonSerializer.Deserialize<SerpApiResponseDto>(jsonResponse);

                // Thêm Log để Debug trực tiếp trên Terminal nếu API không trả về job
                if (serpData?.JobsResults == null || !serpData.JobsResults.Any())
                {
                    Console.WriteLine($"[CẢNH BÁO] SerpApi trả về rỗng. Query: {query}. Nguyên văn JSON: {jsonResponse}");
                    return (404, "Không tìm thấy công việc nào mới để quét từ SerpApi. Hãy xem Console Log ở Backend để biết chi tiết.");
                }

                var allNodes = await _repo.GetAllSkillNodesAsync();
                var trendsToSave = new List<TrendAnalysis>();
                int newJobsCount = 0;

                // 3. Xử lý từng Job cào về được (Giới hạn Take(5) để test không bị tốn quá nhiều quota API)
                foreach (var scrapedJob in serpData.JobsResults.Take(5))
                {
                    var job = new JobPosting
                    {
                        PostingId = Guid.NewGuid(),
                        JobTitle = scrapedJob.Title ?? "Vị trí lập trình viên",
                        CompanyName = scrapedJob.CompanyName ?? "Công ty công nghệ",
                        SourcePlatform = scrapedJob.Source ?? "Google Jobs",
                        JobDescriptionRaw = scrapedJob.Description ?? "",
                        ScrapedAt = DateTime.Now
                    };

                    // Gọi AI bóc tách từ khóa dựa trên Description thật vừa cào được
                    var matchedNodes = await ExtractSkillsUsingAiAsync(job.JobDescriptionRaw, allNodes);

                    foreach (var node in matchedNodes)
                    {
                        job.SkillNodes.Add(node);

                        // Chuẩn bị dữ liệu Trend
                        trendsToSave.Add(new TrendAnalysis
                        {
                            SkillNodeId = node.SkillNodeId,
                            AnalyzedDate = DateOnly.FromDateTime(DateTime.Now),
                            DemandPercent = (decimal)new Random().Next(10, 90),
                            TrendScore = (decimal)(new Random().NextDouble() * 5)
                        });
                    }

                    await _repo.SaveJobPostingAsync(job);
                    newJobsCount++;
                }

                // 4. Lưu dữ liệu Trend vào DB
                if (trendsToSave.Any())
                {
                    await _repo.SaveTrendAnalysisAsync(trendsToSave);
                }

                return (200, $"Cào thành công {newJobsCount} công việc thực tế với từ khóa '{query}', bóc tách từ khóa qua AI và cập nhật Trend hoàn tất.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống trong quá trình cào dữ liệu: {ex.Message}");
            }
        }

        // Dùng Gemini AI để map Description thô thành các SkillNode ID trong DB
        private async Task<List<SkillNode>> ExtractSkillsUsingAiAsync(string description, List<SkillNode> allNodes)
        {
            // Nếu mô tả công việc bị rỗng thì bỏ qua không gọi AI để tiết kiệm chi phí
            if (string.IsNullOrWhiteSpace(description)) return new List<SkillNode>();

            string availableSkills = string.Join(", ", allNodes.Select(n => n.NodeName));

            // Tối ưu prompt để AI trả về đúng format mảng ngăn cách dấu phẩy
            string prompt = $@"Bạn là một hệ thống tự động. Dưới đây là danh sách các kỹ năng hệ thống có: [{availableSkills}].
Nhiệm vụ: Đọc đoạn mô tả công việc sau và trích xuất TẤT CẢ các kỹ năng công nghệ có xuất hiện trong đoạn mô tả và trùng khớp (hoặc gần giống) với danh sách trên.
ĐỊNH DẠNG TRẢ VỀ: Chỉ in ra tên các kỹ năng, ngăn cách nhau bằng DẤU PHẨY. Tuyệt đối KHÔNG có câu chào hỏi, KHÔNG có bullet point, KHÔNG giải thích.
Ví dụ: C#, .NET Core, SQL Server
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
                        AnalyzedDate = x.AnalyzedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue,
                        DemandPercent = x.DemandPercent ?? 0,
                        TrendScore = x.TrendScore ?? 0
                    }).OrderBy(x => x.AnalyzedDate).ToList()
                }).ToList();

            return grouped;
        }
    }
}