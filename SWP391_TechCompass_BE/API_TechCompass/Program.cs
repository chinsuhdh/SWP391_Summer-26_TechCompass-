using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repository_TechCompass;
using Repository_TechCompass.Repositories;
using Service_TechCompass.Interfaces;
using Service_TechCompass.Services;
using Service_TechCompass.Services.BackgroundJobs;
using System.Text;
using Repository_TechCompass.Interfaces;

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
            builder.Services.AddScoped<IMentorRepository, MentorRepository>();
            builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            builder.Services.AddScoped<IMentorBookingRepository, MentorBookingRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IContentRepository, ContentRepository>();

            // THÊM DÒNG NÀY VÀO ĐỂ FIX LỖI:
            builder.Services.AddScoped<IPracticeWorkspaceRepository, PracticeWorkspaceRepository>();



            // 3. ĐĂNG KÝ SERVICE
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
            builder.Services.AddScoped<IRoadmapEngineService, RoadmapEngineService>();
            builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
            builder.Services.AddScoped<IAdminMentorService, AdminMentorService>();
            builder.Services.AddScoped<IAssessmentService, AssessmentService>();
            builder.Services.AddScoped<IAiTalentService, AiTalentService>();
            builder.Services.AddScoped<IMentorBookingService, MentorBookingService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IAdminUserService, AdminUserService>();
            builder.Services.AddScoped<IRoadmapService, RoadmapService>();
            builder.Services.AddScoped<ILearningHubService, LearningHubService>();
            builder.Services.AddScoped<IAdminContentService, AdminContentService>();
            builder.Services.AddScoped<IAdminMonitorService, AdminMonitorService>();

            // Đăng ký Service cho luồng thực hành Code (Fix lỗi Unable to resolve service)
            builder.Services.AddScoped<IPracticeWorkspaceService, PracticeWorkspaceService>();

            // 4. ĐĂNG KÝ HTTP CLIENT SERVICES (Dành cho các Service gọi API bên ngoài)
            builder.Services.AddHttpClient<IQuizSyncService, QuizSyncService>();
            builder.Services.AddScoped<IQuizSyncService, QuizSyncService>();

            builder.Services.AddHttpClient<ICareerService, CareerService>();
            builder.Services.AddScoped<ICareerService, CareerService>();

            builder.Services.AddHttpClient<IPortfolioService, PortfolioService>();
            builder.Services.AddScoped<IPortfolioService, PortfolioService>();

            // 5. CÁC DỊCH VỤ NỀN & SIGNALR
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IBackgroundTaskQueue>(ctx => new BackgroundTaskQueue(1000));
            builder.Services.AddScoped<ITelemetryService, TelemetryService>();
            builder.Services.AddHostedService<TelemetryWorker>();

            // 6. CẤU HÌNH CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
            });

            // 7. CẤU HÌNH AUTHENTICATION & JWT
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
            });

            // 8. CẤU HÌNH CONTROLLER & SWAGGER
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

            // 9. PIPELINE MIDDLEWARE
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<Service_TechCompass.Hubs.MentorChatHub>("/mentorChatHub");

            app.Run();
        }
    }
}