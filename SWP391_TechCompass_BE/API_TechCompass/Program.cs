using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repository_TechCompass;
using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Repositories;
using Service_TechCompass.Interfaces;
using Service_TechCompass.Services;
using Service_TechCompass.Services.BackgroundJobs;
using System.Text;
using Microsoft.SemanticKernel;
using Hangfire;

namespace API_TechCompass
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. CẤU HÌNH CORS LỚN (CHO PHÉP TẤT CẢ PORT LOCALHOST)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            builder.Services.AddMemoryCache();

            // 2. DATABASE CONTEXT
            builder.Services.AddDbContext<Swp391CareerRoadmapContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 3. ĐĂNG KÝ REPOSITORY
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IContentRepository, ContentRepository>();
            builder.Services.AddScoped<IPracticeWorkspaceRepository, PracticeWorkspaceRepository>();
            builder.Services.AddScoped<IMarketPulseRepository, MarketPulseRepository>();

            // 4. ĐĂNG KÝ AI & CÁC DỊCH VỤ KHÁC
            var geminiConfig = builder.Configuration.GetSection("GeminiApiConfig");
            var geminiApiKeys = geminiConfig.GetSection("ApiKeys").Get<string[]>();
            var geminiModelId = geminiConfig["ModelId"] ?? "gemini-2.5-flash";
            var openAiConfig = builder.Configuration.GetSection("OpenAiApiConfig");
            var openAiApiKey = openAiConfig["ApiKey"];
            var openAiModelId = openAiConfig["ModelId"] ?? "gpt-4o-mini";

            if (geminiApiKeys == null || geminiApiKeys.Length == 0 || string.IsNullOrEmpty(openAiApiKey))
            {
                throw new InvalidOperationException("[LỖI CẤU HÌNH]: API Key bị rỗng!");
            }

            builder.Services.AddTransient<Kernel>(sp =>
            {
                var randomGeminiKey = geminiApiKeys[Random.Shared.Next(geminiApiKeys.Length)];
                return Kernel.CreateBuilder()
                    .AddGoogleAIGeminiChatCompletion(modelId: geminiModelId, apiKey: randomGeminiKey, serviceId: "GeminiChat")
                    .AddOpenAIChatCompletion(modelId: openAiModelId, apiKey: openAiApiKey, serviceId: "OpenAiCodeAnalyzer")
                    .Build();
            });

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
            builder.Services.AddScoped<IRoadmapEngineService, RoadmapEngineService>();
            builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
            builder.Services.AddScoped<IAssessmentService, AssessmentService>();
            builder.Services.AddScoped<IAiTalentService, AiTalentService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<IRoadmapService, RoadmapService>();
            builder.Services.AddScoped<ILearningHubService, LearningHubService>();
            builder.Services.AddScoped<IAdminContentService, AdminContentService>();
            builder.Services.AddScoped<IAdminMonitorService, AdminMonitorService>();
            builder.Services.AddScoped<IPracticeWorkspaceService, PracticeWorkspaceService>();
            builder.Services.AddScoped<ISkillGapReportService, SkillGapReportService>();
            builder.Services.AddScoped<IPortfolioService, PortfolioService>();
            builder.Services.AddScoped<IVirtualMentorService, VirtualMentorService>();
            builder.Services.AddScoped<ICounselorService, CounselorService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();

            builder.Services.AddHttpClient<IQuizSyncService, QuizSyncService>();
            builder.Services.AddHttpClient<ICareerService, CareerService>();
            builder.Services.AddHttpClient<IMarketPulseService, MarketPulseService>();

            // Thêm dòng này vào Program.cs (trong phần AddServices)
            builder.Services.AddScoped<ICounselorService, CounselorService>();
            builder.Services.AddScoped<IMentorService, MentorService>();

            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundTaskQueue(1000));
            builder.Services.AddScoped<ITelemetryService, TelemetryService>();
            builder.Services.AddHostedService<TelemetryWorker>();

            // HANGFIRE
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddHangfireServer();

            // 5. AUTHENTICATION & JWT
            var jwtConfig = builder.Configuration.GetSection("Jwt");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!))
        };

        // THÊM ĐOẠN NÀY ĐỂ SIGNALR ĐỌC ĐƯỢC TOKEN
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Nếu request có token và đang gọi vào endpoint của SignalR
                if (!string.IsNullOrEmpty(accessToken) &&
                   (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/portfolioHub")))
                {
                    // Cấp token cho context
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // KÍCH HOẠT CORS ĐÚNG THỨ TỰ (Trước Authentication/Authorization)
            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHangfireDashboard("/hangfire");
            RecurringJob.AddOrUpdate<IMarketPulseService>(
                "daily-job-scraper",
                service => service.RunScraperAndTrendAnalysisAsync(),
                Cron.Daily(17));

            app.MapControllers();
            app.MapHub<Service_TechCompass.Hubs.RoadmapNotificationHub>("/hubs/roadmap");
            app.MapHub<Service_TechCompass.Hubs.VirtualMentorChatHub>("/hubs/virtualMentor");
            app.MapHub<Service_TechCompass.Hubs.PortfolioHub>("/portfolioHub");

            app.Run();
        }
    }
}