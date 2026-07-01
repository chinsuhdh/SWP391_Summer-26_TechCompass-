using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repository_TechCompass.Models;
using System.IO;

namespace Repository_TechCompass;

public partial class Swp391CareerRoadmapContext : DbContext
{
    public Swp391CareerRoadmapContext()
    {
    }

    public Swp391CareerRoadmapContext(DbContextOptions<Swp391CareerRoadmapContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiChatSession> AiChatSessions { get; set; }
    public virtual DbSet<AiRecommendation> AiRecommendations { get; set; }
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }
    public virtual DbSet<EPortfolio> EPortfolios { get; set; }
    public virtual DbSet<GithubRepository> GithubRepositories { get; set; }
    public virtual DbSet<JobPosting> JobPostings { get; set; }
    public virtual DbSet<LearningHistory> LearningHistories { get; set; }
    public virtual DbSet<LearningResource> LearningResources { get; set; }
    public virtual DbSet<Mentor> Mentors { get; set; }
    public virtual DbSet<MentorSession> MentorSessions { get; set; }
    public virtual DbSet<RoadmapProgress> RoadmapProgresses { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<SkillAssessment> SkillAssessments { get; set; }
    public virtual DbSet<SkillGapReport> SkillGapReports { get; set; }
    public virtual DbSet<SkillNode> SkillNodes { get; set; }
    public virtual DbSet<Student> Students { get; set; }
    public virtual DbSet<TargetCareerRole> TargetCareerRoles { get; set; }
    public virtual DbSet<TechPath> TechPaths { get; set; }
    public virtual DbSet<TrendAnalysis> TrendAnalyses { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
    public virtual DbSet<CodingExercise> CodingExercises { get; set; }

    // NEW DB SETS FOR ASSESSMENT SESSION
    public virtual DbSet<AssessmentSession> AssessmentSessions { get; set; }
    public virtual DbSet<AssessmentQuizDetail> AssessmentQuizDetails { get; set; }
    public virtual DbSet<AssessmentCodeDetail> AssessmentCodeDetails { get; set; }


    public virtual DbSet<Counselor> Counselors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiChatSession>(entity =>
        {
            entity.HasKey(e => e.AiSessionId).HasName("PK__ai_chat___1332BF331050D9A4");
            entity.ToTable("ai_chat_sessions");
            entity.Property(e => e.AiSessionId)
                .ValueGeneratedNever()
                .HasColumnName("ai_session_id");
            entity.Property(e => e.ContextType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("context_type");
            entity.Property(e => e.StartedAt)
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithMany(p => p.AiChatSessions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_aichat_student");
        });

        modelBuilder.Entity<AiRecommendation>(entity =>
        {
            entity.HasKey(e => e.RecommendationId).HasName("PK__ai_recom__BCB11F4F25DF62E3");
            entity.ToTable("ai_recommendations");
            entity.Property(e => e.RecommendationId)
                .ValueGeneratedNever()
                .HasColumnName("recommendation_id");
            entity.Property(e => e.ContentJson).HasColumnName("content_json");
            entity.Property(e => e.GeneratedAt)
                .HasColumnType("datetime")
                .HasColumnName("generated_at");
            entity.Property(e => e.RecommendationType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("recommendation_type");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithMany(p => p.AiRecommendations)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_airec_student");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__chat_mes__0BBF6EE6F0E5B61A");
            entity.ToTable("chat_messages");
            entity.Property(e => e.MessageId)
                .ValueGeneratedNever()
                .HasColumnName("message_id");
            entity.Property(e => e.AiSessionId).HasColumnName("ai_session_id");
            entity.Property(e => e.MentorSessionId).HasColumnName("mentor_session_id");
            entity.Property(e => e.MessageText).HasColumnName("message_text");
            entity.Property(e => e.SenderType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sender_type");
            entity.Property(e => e.SentAt)
                .HasColumnType("datetime")
                .HasColumnName("sent_at");

            entity.HasOne(d => d.AiSession).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.AiSessionId)
                .HasConstraintName("fk_chat_aisession");

            entity.HasOne(d => d.MentorSession).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.MentorSessionId)
                .HasConstraintName("fk_chat_mentorsession");
        });

        modelBuilder.Entity<EPortfolio>(entity =>
        {
            entity.HasKey(e => e.PortfolioId).HasName("PK__e_portfo__42EE526F1E68FEB5");
            entity.ToTable("e_portfolios");
            entity.HasIndex(e => e.StudentId, "UQ__e_portfo__2A33069BE8F9E036").IsUnique();
            entity.HasIndex(e => e.ShareableUrl, "UQ__e_portfo__6E3379896B463C22").IsUnique();
            entity.Property(e => e.PortfolioId)
                .ValueGeneratedNever()
                .HasColumnName("portfolio_id");
            entity.Property(e => e.AiProfileSummary).HasColumnName("ai_profile_summary");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ShareableUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("shareable_url");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithOne(p => p.EPortfolio)
                .HasForeignKey<EPortfolio>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_portfolio_student");
        });

        modelBuilder.Entity<GithubRepository>(entity =>
        {
            entity.HasKey(e => e.RepoId).HasName("PK__github_r__E2D3BC807F7B5A6B");
            entity.ToTable("github_repositories");
            entity.Property(e => e.RepoId)
                .ValueGeneratedNever()
                .HasColumnName("repo_id");
            entity.Property(e => e.AiProjectSummary).HasColumnName("ai_project_summary");
            entity.Property(e => e.ExtractedTechStack)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("extracted_tech_stack");
            entity.Property(e => e.GithubUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("github_url");
            entity.Property(e => e.PortfolioId).HasColumnName("portfolio_id");
            entity.Property(e => e.ReadmeContent).HasColumnName("readme_content");
            entity.Property(e => e.RepoName)
                .HasMaxLength(150)
                .HasColumnName("repo_name");
            entity.Property(e => e.SyncedAt)
                .HasColumnType("datetime")
                .HasColumnName("synced_at");

            entity.HasOne(d => d.Portfolio).WithMany(p => p.GithubRepositories)
                .HasForeignKey(d => d.PortfolioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_repo_portfolio");
        });

        modelBuilder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(e => e.PostingId).HasName("PK__job_post__945363DE2DAA7746");
            entity.ToTable("job_postings");
            entity.Property(e => e.PostingId)
                .ValueGeneratedNever()
                .HasColumnName("posting_id");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(150)
                .HasColumnName("company_name");
            entity.Property(e => e.JobDescriptionRaw).HasColumnName("job_description_raw");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(255)
                .HasColumnName("job_title");
            entity.Property(e => e.ScrapedAt)
                .HasColumnType("datetime")
                .HasColumnName("scraped_at");
            entity.Property(e => e.SourcePlatform)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("source_platform");

            entity.HasMany(d => d.SkillNodes).WithMany(p => p.Postings)
                .UsingEntity<Dictionary<string, object>>(
                    "JobPostingSkill",
                    r => r.HasOne<SkillNode>().WithMany()
                        .HasForeignKey("SkillNodeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_jobskills_skillnode"),
                    l => l.HasOne<JobPosting>().WithMany()
                        .HasForeignKey("PostingId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_jobskills_posting"),
                    j =>
                    {
                        j.HasKey("PostingId", "SkillNodeId").HasName("PK__job_post__4B09DF958248A712");
                        j.ToTable("job_posting_skills");
                        j.IndexerProperty<Guid>("PostingId").HasColumnName("posting_id");
                        j.IndexerProperty<int>("SkillNodeId").HasColumnName("skill_node_id");
                    });
        });

        modelBuilder.Entity<LearningHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__learning__096AA2E96A280403");
            entity.ToTable("learning_histories");
            entity.Property(e => e.HistoryId)
                .ValueGeneratedNever()
                .HasColumnName("history_id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("action_type");
            entity.Property(e => e.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(e => e.ProgressId).HasColumnName("progress_id");
            entity.Property(e => e.RecordedAt)
                .HasColumnType("datetime")
                .HasColumnName("recorded_at");

            entity.HasOne(d => d.Progress).WithMany(p => p.LearningHistories)
                .HasForeignKey(d => d.ProgressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_history_progress");
        });

        modelBuilder.Entity<LearningResource>(entity =>
        {
            entity.HasKey(e => e.ResourceId).HasName("PK__learning__4985FC734E1996CD");
            entity.ToTable("learning_resources");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("difficulty_level");
            entity.Property(e => e.Provider)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("provider");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("resource_type");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Url)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("url");

            entity.HasOne(d => d.SkillNode).WithMany(p => p.LearningResources)
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_learningres_skillnode");
        });

        modelBuilder.Entity<Mentor>(entity =>
        {
            entity.HasKey(e => e.MentorId).HasName("PK__mentors__E5D27EF3317BF9C8");
            entity.ToTable("mentors");
            entity.HasIndex(e => e.UserId, "UQ__mentors__B9BE370E28E8D3C1").IsUnique();
            entity.Property(e => e.MentorId)
                .ValueGeneratedNever()
                .HasColumnName("mentor_id");
            entity.Property(e => e.CurrentCompany)
                .HasMaxLength(150)
                .HasColumnName("current_company");
            entity.Property(e => e.ExpertiseTags)
                .HasMaxLength(255)
                .HasColumnName("expertise_tags");
            entity.Property(e => e.LinkedinUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("linkedin_url");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Mentor)
                .HasForeignKey<Mentor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_mentor_user");
        });

        modelBuilder.Entity<MentorSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__mentor_s__69B13FDC497F876A");
            entity.ToTable("mentor_sessions");
            entity.Property(e => e.SessionId)
                .ValueGeneratedNever()
                .HasColumnName("session_id");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.MeetingLink)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("meeting_link");
            entity.Property(e => e.MentorId).HasColumnName("mentor_id");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("payment_status");
            entity.Property(e => e.ReviewNotes).HasColumnName("review_notes");
            entity.Property(e => e.ScheduledAt)
                .HasColumnType("datetime")
                .HasColumnName("scheduled_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Mentor).WithMany(p => p.MentorSessions)
                .HasForeignKey(d => d.MentorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_session_mentor");

            entity.HasOne(d => d.Student).WithMany(p => p.MentorSessions)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_session_student");
        });

        modelBuilder.Entity<RoadmapProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PK__roadmap___49B3D8C176124995");
            entity.ToTable("roadmap_progress");
            entity.Property(e => e.ProgressId)
                .ValueGeneratedNever()
                .HasColumnName("progress_id");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.CompletionPercent).HasColumnName("completion_percent");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.StartedAt)
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.SkillNode).WithMany(p => p.RoadmapProgresses)
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_progress_skillnode");

            entity.HasOne(d => d.Student).WithMany(p => p.RoadmapProgresses)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_progress_student");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__roles__760965CC60832C83");
            entity.ToTable("roles");
            entity.HasIndex(e => e.RoleName, "UQ__roles__783254B1EEECB4DB").IsUnique();
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<SkillAssessment>(entity =>
        {
            entity.HasKey(e => e.AssessmentId).HasName("PK__skill_as__00B98C26CD379950");
            entity.ToTable("skill_assessments");
            entity.Property(e => e.AssessmentId)
                .ValueGeneratedNever()
                .HasColumnName("assessment_id");
            entity.Property(e => e.AiFeedback).HasColumnName("ai_feedback");
            entity.Property(e => e.CodingPatternSnapshot).HasColumnName("coding_pattern_snapshot");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TakenAt)
                .HasColumnType("datetime")
                .HasColumnName("taken_at");
            entity.Property(e => e.TestScore)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("test_score");

            entity.HasOne(d => d.SkillNode).WithMany(p => p.SkillAssessments)
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assessment_skillnode");

            entity.HasOne(d => d.Student).WithMany(p => p.SkillAssessments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assessment_student");
        });

        modelBuilder.Entity<SkillGapReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__skill_ga__779B7C58F7397CFC");
            entity.ToTable("skill_gap_reports");
            entity.Property(e => e.ReportId)
                .ValueGeneratedNever()
                .HasColumnName("report_id");
            entity.Property(e => e.GeneratedAt)
                .HasColumnType("datetime")
                .HasColumnName("generated_at");
            entity.Property(e => e.PdfUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("pdf_url");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Summary).HasColumnName("summary");

            entity.HasOne(d => d.Student).WithMany(p => p.SkillGapReports)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gapreport_student");
        });

        modelBuilder.Entity<SkillNode>(entity =>
        {
            entity.HasKey(e => e.SkillNodeId).HasName("PK__skill_no__F5ABC4BC572DCBB8");
            entity.ToTable("skill_nodes");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.NodeName)
                .HasMaxLength(100)
                .HasColumnName("node_name");
            entity.Property(e => e.ParentNodeId).HasColumnName("parent_node_id");
            entity.Property(e => e.PriorityLevel).HasColumnName("priority_level");
            entity.Property(e => e.TechPathId).HasColumnName("tech_path_id");
            entity.Property(e => e.IsCodingRequired)
                .HasDefaultValueSql("((1))")
                .HasColumnName("is_coding_required");

            entity.HasOne(d => d.ParentNode).WithMany(p => p.InverseParentNode)
                .HasForeignKey(d => d.ParentNodeId)
                .HasConstraintName("fk_skillnode_parent");

            entity.HasOne(d => d.TechPath).WithMany(p => p.SkillNodes)
                .HasForeignKey(d => d.TechPathId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_skillnode_techpath");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__students__2A33069ACD851B21");
            entity.ToTable("students");
            entity.HasIndex(e => e.StudentCode, "UQ__students__6DF33C45403B5C49").IsUnique();
            entity.HasIndex(e => e.UserId, "UQ__students__B9BE370EC1022F7C").IsUnique();
            entity.Property(e => e.StudentId)
                .ValueGeneratedNever()
                .HasColumnName("student_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.LatentTalentSummary).HasColumnName("latent_talent_summary");
            entity.Property(e => e.StudentCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("student_code");
            entity.Property(e => e.TargetRoleId).HasColumnName("target_role_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.TargetRole).WithMany(p => p.Students)
                .HasForeignKey(d => d.TargetRoleId)
                .HasConstraintName("fk_student_target_role");

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_student_user");
        });

        modelBuilder.Entity<TargetCareerRole>(entity =>
        {
            entity.HasKey(e => e.TargetRoleId).HasName("PK__target_c__D04E000A67124F81");
            entity.ToTable("target_career_roles");
            entity.HasIndex(e => e.RoleName, "UQ__target_c__783254B16E7E4447").IsUnique();
            entity.Property(e => e.TargetRoleId).HasColumnName("target_role_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MarketDemandIndex)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("market_demand_index");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<TechPath>(entity =>
        {
            entity.HasKey(e => e.TechPathId).HasName("PK__tech_pat__E0447470A2DFFE3E");
            entity.ToTable("tech_paths");
            entity.Property(e => e.TechPathId).HasColumnName("tech_path_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.PathName)
                .HasMaxLength(150)
                .HasColumnName("path_name");
            entity.Property(e => e.TargetRoleId).HasColumnName("target_role_id");
            entity.Property(e => e.TotalNodes).HasColumnName("total_nodes");

            entity.HasOne(d => d.TargetRole).WithMany(p => p.TechPaths)
                .HasForeignKey(d => d.TargetRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_techpath_target_role");
        });

        modelBuilder.Entity<TrendAnalysis>(entity =>
        {
            entity.HasKey(e => e.AnalysisId).HasName("PK__trend_an__5B14DE5A814E62F5");
            entity.ToTable("trend_analysis");
            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
            entity.Property(e => e.AnalyzedDate).HasColumnName("analyzed_date");
            entity.Property(e => e.DemandPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("demand_percent");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.TrendScore)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("trend_score");

            entity.HasOne(d => d.SkillNode).WithMany(p => p.TrendAnalyses)
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_trend_skillnode");
        });

        modelBuilder.Entity<AssessmentQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__assessment_questions");
            entity.ToTable("assessment_questions");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.OptionA).HasColumnName("option_a");
            entity.Property(e => e.OptionB).HasColumnName("option_b");
            entity.Property(e => e.OptionC).HasColumnName("option_c");
            entity.Property(e => e.OptionD).HasColumnName("option_d");
            entity.Property(e => e.CorrectAnswer)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("correct_answer");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("difficulty_level");

            entity.HasOne(d => d.SkillNode).WithMany(p => p.AssessmentQuestions)
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_question_skillnode");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F280C6F01");
            entity.ToTable("users");
            entity.HasIndex(e => e.Email, "UQ__users__AB6E61641520CA06").IsUnique();
            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("provider");
            entity.Property(e => e.ProviderId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("provider_id");
            entity.Property(e => e.RoleId)
                .HasColumnName("role_id");
            entity.Property(e => e.OtpCode)
                .HasMaxLength(6)
                .IsUnicode(false)
                .HasColumnName("OtpCode");
            entity.Property(e => e.OtpExpiry)
                .HasColumnType("datetime")
                .HasColumnName("OtpExpiry");

            entity.HasOne(d => d.Role)
                .WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_users_role");
        });

        modelBuilder.Entity<CodingExercise>(entity =>
        {
            entity.HasKey(e => e.ExerciseId).HasName("PK__coding_exercises");
            entity.ToTable("coding_exercises");
            entity.Property(e => e.ExerciseId).HasColumnName("exercise_id");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.ProblemDescription).HasColumnName("problem_description");
            entity.Property(e => e.DefaultCodeTemplate).HasColumnName("default_code_template");
            entity.Property(e => e.TestStdin).HasColumnName("test_stdin");
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("difficulty_level");

            entity.HasOne(d => d.SkillNode).WithMany(p => p.CodingExercises)
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_exercise_skillnode");
        });

        // ==========================================
        // CẤU HÌNH CHO CÁC BẢNG MASTER-DETAIL MỚI
        // ==========================================
        modelBuilder.Entity<AssessmentSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.ToTable("assessment_sessions");

            entity.Property(e => e.SessionId)
                .ValueGeneratedNever()
                .HasColumnName("session_id");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SkillNodeId).HasColumnName("skill_node_id");

            entity.Property(e => e.TotalQuizScore)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("total_quiz_score");

            entity.Property(e => e.TotalCodeScore)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("total_code_score");

            entity.Property(e => e.AssessmentType)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false) 
                .HasColumnName("assessment_type")
                .HasDefaultValueSql("('TESTED')");

            entity.Property(e => e.TakenAt)
                .HasColumnType("datetime")
                .HasColumnName("taken_at")
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Student)
                .WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assessment_session_student");

            entity.HasOne(d => d.SkillNode)
                .WithMany()
                .HasForeignKey(d => d.SkillNodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_assessment_session_skillnode");
        });

        modelBuilder.Entity<AssessmentQuizDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("assessment_quiz_details");
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.SelectedOption)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("selected_option");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");

            // Khóa ngoại trỏ về bảng Cha (Session)
            entity.HasOne(d => d.Session)
                .WithMany(p => p.QuizDetails)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_quizdetail_session");

            // Khóa ngoại trỏ về bảng Câu hỏi
            entity.HasOne(d => d.Question)
                .WithMany()
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_quizdetail_question");
        });

        modelBuilder.Entity<AssessmentCodeDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("assessment_code_details");
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.SourceCode).HasColumnName("source_code");
            entity.Property(e => e.AiFeedback).HasColumnName("ai_feedback");

            // Khóa ngoại trỏ về bảng Cha (Session - 1:1)
            entity.HasOne(d => d.Session)
                .WithOne(p => p.CodeDetail)
                .HasForeignKey<AssessmentCodeDetail>(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_codedetail_session");
        });

        modelBuilder.Entity<Counselor>(entity =>
        {
            entity.HasKey(e => e.CounselorId).HasName("PK__counselors");
            entity.ToTable("counselors");

            // Đảm bảo quan hệ 1-1 với User
            entity.HasIndex(e => e.UserId, "UQ__counselors__user_id").IsUnique();

            entity.Property(e => e.CounselorId)
                .ValueGeneratedNever()
                .HasColumnName("counselor_id");

            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");

            entity.Property(e => e.Department)
                .HasMaxLength(100)
                .HasColumnName("department");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.User).WithOne() 
                .HasForeignKey<Counselor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_counselor_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}