// src/API_TechCompass/Program.cs
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
using Hangfire; // ĐÃ THÊM

namespace API_TechCompass
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. DATABASE CONTEXT
            builder.Services.AddDbContext<Swp391CareerRoadmapContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // 2. ĐĂNG KÝ REPOSITORY
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
            builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IContentRepository, ContentRepository>();
            builder.Services.AddScoped<IPracticeWorkspaceRepository, PracticeWorkspaceRepository>();
            builder.Services.AddScoped<IMarketPulseRepository, MarketPulseRepository>();

            // 3. ĐĂNG KÝ SEMANTIC KERNEL (TÍCH HỢP AI)
            var geminiConfig = builder.Configuration.GetSection("GeminiApiConfig");
            var geminiApiKeys = geminiConfig.GetSection("ApiKeys").Get<string[]>();
            var geminiModelId = geminiConfig["ModelId"] ?? "gemini-2.5-flash";

            var openAiConfig = builder.Configuration.GetSection("OpenAiApiConfig");
            var openAiApiKey = openAiConfig["ApiKey"];
            var openAiModelId = openAiConfig["ModelId"] ?? "gpt-4o-mini";

            if (geminiApiKeys == null || geminiApiKeys.Length == 0 || string.IsNullOrEmpty(openAiApiKey))
            {
                throw new InvalidOperationException("[LỖI CẤU HÌNH NGHIÊM TRỌNG]: API Key của Gemini hoặc OpenAI bị rỗng trong appsettings.json!");
            }

            builder.Services.AddTransient<Kernel>(sp =>
            {
                // Lấy ngẫu nhiên 1 API Key từ danh sách để tránh bị hit limit
                var randomGeminiKey = geminiApiKeys[Random.Shared.Next(geminiApiKeys.Length)];

                return Kernel.CreateBuilder()
                    .AddGoogleAIGeminiChatCompletion(
                        modelId: geminiModelId,
                        apiKey: randomGeminiKey,
                        serviceId: "GeminiChat")
                    .AddOpenAIChatCompletion(
                        modelId: openAiModelId,
                        apiKey: openAiApiKey,
                        serviceId: "OpenAiCodeAnalyzer")
                    .Build();
            });

            // 4. ĐĂNG KÝ SERVICE
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
            builder.Services.AddScoped<IProfileService, ProfileService>();

            // 5. ĐĂNG KÝ HTTP CLIENT SERVICES
            builder.Services.AddHttpClient<IQuizSyncService, QuizSyncService>();
            builder.Services.AddHttpClient<ICareerService, CareerService>();
            builder.Services.AddHttpClient<IMarketPulseService, MarketPulseService>();

            // 6. CÁC DỊCH VỤ NỀN & SIGNALR
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundTaskQueue(1000));
            builder.Services.AddScoped<ITelemetryService, TelemetryService>();
            builder.Services.AddHostedService<TelemetryWorker>();

            // ---------------------------------------------------------
            // ĐĂNG KÝ HANGFIRE (XỬ LÝ LỖI "Not Initialized")
            // ---------------------------------------------------------
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHangfireServer();
            // ---------------------------------------------------------

            // 7. CẤU HÌNH CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173")
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
            });

            // 8. CẤU HÌNH AUTHENTICATION & JWT
            var jwtConfig = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtConfig["Key"];

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TechCompass API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Nhập token theo cú pháp: Bearer {token của bạn}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            // ---------------------------------------------------------
            // KÍCH HOẠT HANGFIRE DASHBOARD & ĐĂNG KÝ JOB ĐỊNH KỲ
            // ---------------------------------------------------------
            app.UseHangfireDashboard("/hangfire");

            // Kích hoạt cào dữ liệu lúc 17h UTC (0h đêm VN)
            RecurringJob.AddOrUpdate<IMarketPulseService>(
                "daily-job-scraper",
                service => service.RunScraperAndTrendAnalysisAsync(),
                Cron.Daily(17));
            // ---------------------------------------------------------

            app.MapControllers();

            // MAP CÁC HUB SIGNALR
            app.MapHub<Service_TechCompass.Hubs.RoadmapNotificationHub>("/hubs/roadmap");
            app.MapHub<Service_TechCompass.Hubs.VirtualMentorChatHub>("/hubs/virtualMentor");
            app.MapHub<Service_TechCompass.Hubs.PortfolioHub>("/portfolioHub");
            app.Run();
        }
    }
}