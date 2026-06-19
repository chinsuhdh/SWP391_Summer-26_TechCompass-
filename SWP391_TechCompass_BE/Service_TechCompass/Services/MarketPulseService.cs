using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Service_TechCompass.Services
{
    public class MarketPulseService : IMarketPulseService
    {
        private readonly IMarketPulseRepository _repo;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly Swp391CareerRoadmapContext _context; // Inject DB Context để query bảng mới

        public MarketPulseService(IMarketPulseRepository repo, HttpClient httpClient, IConfiguration config, Swp391CareerRoadmapContext context)
        {
            _repo = repo;
            _httpClient = httpClient;
            _config = config;
            _context = context;
        }

        // ==========================================
        // 1. AI JOB MATCHING
        // ==========================================
        public async Task<List<JobMatchDto>> GetMatchingJobsAsync(Guid studentId, JobFilterDto filter)
        {
            var student = await _repo.GetStudentWithPassedSkillsAsync(studentId);
            if (student == null) throw new Exception("Không tìm thấy sinh viên.");

            var mySkills = await _context.RoadmapProgresses
                .Include(p => p.SkillNode)
                .Where(p => p.StudentId == studentId && p.Status == "Completed")
                .Select(p => p.SkillNode.NodeName)
                .ToListAsync();

            int skip = (filter.Page - 1) * filter.PageSize;
            var jobs = await _repo.GetJobPostingsAsync(filter.Keyword, filter.SourcePlatform, skip, filter.PageSize);

            var result = new List<JobMatchDto>();

            foreach (var job in jobs)
            {
                var jobSkills = job.SkillNodes.Select(s => s.NodeName).ToList();

                // ---------------------------------------------------------
                // THÊM ĐOẠN NÀY: LỌC THEO KỸ NĂNG USER CHỌN TỪ GIAO DIỆN
                // ---------------------------------------------------------
                if (filter.Skills != null && filter.Skills.Any())
                {
                    // Nếu Job này không chứa BẤT KỲ kỹ năng nào user đang filter -> Bỏ qua
                    if (!jobSkills.Intersect(filter.Skills, StringComparer.OrdinalIgnoreCase).Any())
                    {
                        continue;
                    }
                }

                var matched = jobSkills.Intersect(mySkills, StringComparer.OrdinalIgnoreCase).ToList();
                var missing = jobSkills.Except(mySkills, StringComparer.OrdinalIgnoreCase).ToList();

                decimal matchPercent = jobSkills.Count > 0 ? (decimal)matched.Count / jobSkills.Count * 100 : 0;

                // Áp dụng bộ lọc MinMatch từ giao diện (Thanh gạt %)
                if (filter.MinMatch > 0 && matchPercent < filter.MinMatch) continue;

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

            return filter.SortBy == "match"
                ? result.OrderByDescending(x => x.MatchPercentage).ToList()
                : result.OrderByDescending(x => x.PostingId).ToList();
        }

        // ==========================================
        // 2. DYNAMIC SCRAPING (Cào dữ liệu đa nền tảng)
        // ==========================================
        public async Task<(int StatusCode, string Message)> RunScraperAndTrendAnalysisAsync()
        {
            try
            {
                string serpApiKey = _config["SerpApiConfig:ApiKey"]!;
                string serpBaseUrl = _config["SerpApiConfig:BaseUrl"] ?? "https://serpapi.com/search.json";

                var allRoles = await _context.TargetCareerRoles.Select(r => r.RoleName).ToListAsync();
                if (!allRoles.Any()) allRoles = new List<string> { "Software Engineer" };

                // [FIX LỖI CÀO DỮ LIỆU RỖNG Ở VN]: Thay đổi cách sinh từ khóa phù hợp với Google VN
                var dynamicKeywords = new List<string>();
                foreach (var role in allRoles)
                {
                    // Lọc bỏ cụm từ dài dòng nếu có, ví dụ "Backend Developer Java..." -> "Backend Developer"
                    string shortRole = role.Split('(')[0].Split('-')[0].Trim();

                    dynamicKeywords.Add(shortRole); // Vd: "Frontend Developer"
                    dynamicKeywords.Add($"Tuyển dụng {shortRole}"); // Vd: "Tuyển dụng Frontend Developer"
                    dynamicKeywords.Add($"Việc làm {shortRole}"); // Vd: "Việc làm Frontend Developer"
                }

                // Chọn ngẫu nhiên 1 từ khóa
                string query = dynamicKeywords[new Random().Next(dynamicKeywords.Count)];

                // [FIX LỖI LOCATION]: Trỏ đích danh vào các IT Hub của VN để Google Jobs dễ bắt kết quả hơn
                var locations = new[] { "Ho Chi Minh City, Vietnam", "Hanoi, Vietnam", "Da Nang, Vietnam", "Vietnam" };
                string location = locations[new Random().Next(locations.Length)];

                // Tạm bỏ tham số hl=vi để tránh Google lọc mất các Job tiếng Anh (Rất phổ biến ở VN)
                string requestUrl = $"{serpBaseUrl}?engine=google_jobs&q={Uri.EscapeDataString(query)}&location={Uri.EscapeDataString(location)}&gl=vn&api_key={serpApiKey}";

                var response = await _httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return (500, "Lỗi khi gọi API cào dữ liệu từ SerpApi.");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var serpData = JsonSerializer.Deserialize<SerpApiResponseDto>(jsonResponse);

                if (serpData?.JobsResults == null || !serpData.JobsResults.Any())
                {
                    Console.WriteLine($"[CẢNH BÁO] SerpApi trả về rỗng. Query: '{query}', Location: '{location}'.");
                    return (404, $"Không tìm thấy việc làm mới cho từ khóa '{query}' tại '{location}'. Vui lòng thử lại lần nữa!");
                }

                var allNodes = await _repo.GetAllSkillNodesAsync();
                var trendsToSave = new List<TrendAnalysis>();
                int newJobsCount = 0;

                // Tăng số lượng cào lên 10 để bạn dễ test hơn
                foreach (var scrapedJob in serpData.JobsResults.Take(10))
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

                    var matchedNodes = await ExtractSkillsUsingAiAsync(job.JobDescriptionRaw, allNodes);

                    foreach (var node in matchedNodes)
                    {
                        job.SkillNodes.Add(node);

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

                if (trendsToSave.Any())
                {
                    await _repo.SaveTrendAnalysisAsync(trendsToSave);
                }

                return (200, $"Cào thành công {newJobsCount} công việc cho nhóm ngành '{query}' tại '{location}'.");
            }
            catch (Exception ex)
            {
                return (500, $"Lỗi hệ thống trong quá trình cào dữ liệu: {ex.Message}");
            }
        }

        // ==========================================
        // CÁC HÀM CÒN LẠI GIỮ NGUYÊN BÊN DƯỚI ...
        // ==========================================

        private async Task<List<SkillNode>> ExtractSkillsUsingAiAsync(string description, List<SkillNode> allNodes)
        {
            if (string.IsNullOrWhiteSpace(description)) return new List<SkillNode>();

            string availableSkills = string.Join(", ", allNodes.Select(n => n.NodeName));

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