using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SchoolManager.Models;

public partial class SchoolDbContext : DbContext
{
    public SchoolDbContext()
    {
    }

    public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activity> Activities { get; set; }

    public virtual DbSet<ActivityAttachment> ActivityAttachments { get; set; }

    public virtual DbSet<ActivityType> ActivityTypes { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<DisciplineReport> DisciplineReports { get; set; }

    public virtual DbSet<GradeLevel> GradeLevels { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<SecuritySetting> SecuritySettings { get; set; }

    public virtual DbSet<Specialty> Specialties { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentActivityScore> StudentActivityScores { get; set; }

    public virtual DbSet<StudentAssignment> StudentAssignments { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<SubjectAssignment> SubjectAssignments { get; set; }

    public virtual DbSet<TeacherAssignment> TeacherAssignments { get; set; }

    public virtual DbSet<Trimester> Trimesters { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configurar interceptor global para DateTime
        optionsBuilder.AddInterceptors(new DateTimeInterceptor());
        
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=SchoolManagement;Username=postgres;Password=Panama2020$");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurar DateTime globalmente para PostgreSQL
        ConfigureDateTimeHandling(modelBuilder);
        
        modelBuilder
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activities_pkey");

            entity.ToTable("activities");

            entity.HasIndex(e => e.ActivityTypeId, "IX_activities_ActivityTypeId");

            entity.HasIndex(e => e.TrimesterId, "IX_activities_TrimesterId");

            entity.HasIndex(e => e.SchoolId, "IX_activities_school_id");

            entity.HasIndex(e => e.SubjectId, "IX_activities_subject_id");

            entity.HasIndex(e => e.GroupId, "idx_activities_group");

            entity.HasIndex(e => e.TeacherId, "idx_activities_teacher");

            entity.HasIndex(e => e.Trimester, "idx_activities_trimester");

            entity.HasIndex(e => new { e.Name, e.Type, e.SubjectId, e.GroupId, e.TeacherId, e.Trimester }, "idx_activities_unique_lookup");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DueDate)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("due_date");
            entity.Property(e => e.GradeLevelId).HasColumnName("grade_level_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PdfUrl).HasColumnName("pdf_url");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.Trimester)
                .HasMaxLength(5)
                .HasColumnName("trimester");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");

            entity.HasOne(d => d.ActivityType).WithMany(p => p.Activities).HasForeignKey(d => d.ActivityTypeId);

            entity.HasOne(d => d.Group).WithMany(p => p.Activities)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("activities_group_id_fkey");

            entity.HasOne(d => d.School).WithMany(p => p.Activities)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("activities_school_id_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.Activities)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("activities_subject_id_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Activities)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("activities_teacher_id_fkey");

            entity.HasOne(d => d.TrimesterNavigation).WithMany(p => p.Activities).HasForeignKey(d => d.TrimesterId);
        });

        modelBuilder.Entity<ActivityAttachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_attachments_pkey");

            entity.ToTable("activity_attachments");

            entity.HasIndex(e => e.ActivityId, "idx_attach_activity");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActivityId).HasColumnName("activity_id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.MimeType)
                .HasMaxLength(50)
                .HasColumnName("mime_type");
            entity.Property(e => e.StoragePath)
                .HasMaxLength(500)
                .HasColumnName("storage_path");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Activity).WithMany(p => p.ActivityAttachments)
                .HasForeignKey(d => d.ActivityId)
                .HasConstraintName("activity_attachments_activity_id_fkey");
        });

        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_types_pkey");

            entity.ToTable("activity_types");

            entity.HasIndex(e => e.SchoolId, "IX_activity_types_school_id");

            entity.HasIndex(e => new { e.Name, e.SchoolId }, "activity_types_name_school_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.Icon)
                .HasMaxLength(50)
                .HasColumnName("icon");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsGlobal)
                .HasDefaultValue(false)
                .HasColumnName("is_global");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.School).WithMany(p => p.ActivityTypes)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("activity_types_school_id_fkey");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("area_pkey");

            entity.ToTable("area");

            entity.HasIndex(e => e.SchoolId, "IX_area_school_id");

            entity.HasIndex(e => new { e.Name, e.SchoolId }, "area_name_school_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsGlobal)
                .HasDefaultValue(false)
                .HasColumnName("is_global");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.School).WithMany(p => p.Areas)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("area_school_id_fkey");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attendance_pkey");

            entity.ToTable("attendance");

            entity.HasIndex(e => e.GradeId, "IX_attendance_grade_id");

            entity.HasIndex(e => e.GroupId, "IX_attendance_group_id");

            entity.HasIndex(e => e.StudentId, "IX_attendance_student_id");

            entity.HasIndex(e => e.TeacherId, "IX_attendance_teacher_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

            entity.HasOne(d => d.Grade).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.GradeId)
                .HasConstraintName("attendance_grade_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("attendance_group_id_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.AttendanceStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("attendance_student_id_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.AttendanceTeachers)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("attendance_teacher_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.SchoolId, "IX_audit_logs_school_id");

            entity.HasIndex(e => e.UserId, "IX_audit_logs_user_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(30)
                .HasColumnName("action");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .HasColumnName("ip_address");
            entity.Property(e => e.Resource)
                .HasMaxLength(50)
                .HasColumnName("resource");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserName)
                .HasMaxLength(100)
                .HasColumnName("user_name");
            entity.Property(e => e.UserRole)
                .HasMaxLength(20)
                .HasColumnName("user_role");

            entity.HasOne(d => d.School).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.SchoolId)
                .HasConstraintName("audit_logs_school_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<DisciplineReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("discipline_reports_pkey");

            entity.ToTable("discipline_reports");

            entity.HasIndex(e => e.GradeLevelId, "IX_discipline_reports_grade_level_id");

            entity.HasIndex(e => e.GroupId, "IX_discipline_reports_group_id");

            entity.HasIndex(e => e.StudentId, "IX_discipline_reports_student_id");

            entity.HasIndex(e => e.SubjectId, "IX_discipline_reports_subject_id");

            entity.HasIndex(e => e.TeacherId, "IX_discipline_reports_teacher_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Date)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GradeLevelId).HasColumnName("grade_level_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.ReportType)
                .HasMaxLength(50)
                .HasColumnName("report_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.GradeLevel).WithMany(p => p.DisciplineReports)
                .HasForeignKey(d => d.GradeLevelId)
                .HasConstraintName("discipline_reports_grade_level_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.DisciplineReports)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("discipline_reports_group_id_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.DisciplineReportStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("discipline_reports_student_id_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.DisciplineReports)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("discipline_reports_subject_id_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.DisciplineReportTeachers)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("discipline_reports_teacher_id_fkey");
        });

        modelBuilder.Entity<GradeLevel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("grade_levels_pkey");

            entity.ToTable("grade_levels");

            entity.HasIndex(e => e.Name, "grade_levels_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("groups_pkey");

            entity.ToTable("groups");

            entity.HasIndex(e => e.SchoolId, "IX_groups_school_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Grade)
                .HasMaxLength(20)
                .HasColumnName("grade");
            entity.Property(e => e.Name)
                .HasMaxLength(20)
                .HasColumnName("name");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");

            entity.HasOne(d => d.School).WithMany(p => p.Groups)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("groups_school_id_fkey");
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("schools_pkey");

            entity.ToTable("schools");

            entity.HasIndex(e => e.AdminId, "IX_schools_admin_id").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("address");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.LogoUrl)
                .HasMaxLength(500)
                .HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("phone");

            entity.HasOne(d => d.Admin).WithOne(p => p.School)
                .HasForeignKey<School>(d => d.AdminId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SecuritySetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("security_settings_pkey");

            entity.ToTable("security_settings");

            entity.HasIndex(e => e.SchoolId, "IX_security_settings_school_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryDays)
                .HasDefaultValue(90)
                .HasColumnName("expiry_days");
            entity.Property(e => e.MaxLoginAttempts)
                .HasDefaultValue(5)
                .HasColumnName("max_login_attempts");
            entity.Property(e => e.PasswordMinLength)
                .HasDefaultValue(8)
                .HasColumnName("password_min_length");
            entity.Property(e => e.PreventReuse)
                .HasDefaultValue(5)
                .HasColumnName("prevent_reuse");
            entity.Property(e => e.RequireLowercase)
                .HasDefaultValue(true)
                .HasColumnName("require_lowercase");
            entity.Property(e => e.RequireNumbers)
                .HasDefaultValue(true)
                .HasColumnName("require_numbers");
            entity.Property(e => e.RequireSpecial)
                .HasDefaultValue(true)
                .HasColumnName("require_special");
            entity.Property(e => e.RequireUppercase)
                .HasDefaultValue(true)
                .HasColumnName("require_uppercase");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.SessionTimeoutMinutes)
                .HasDefaultValue(30)
                .HasColumnName("session_timeout_minutes");

            entity.HasOne(d => d.School).WithMany(p => p.SecuritySettings)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("security_settings_school_id_fkey");
        });

        modelBuilder.Entity<Specialty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("specialties_pkey");

            entity.ToTable("specialties");

            entity.HasIndex(e => e.Name, "specialties_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_pkey");

            entity.ToTable("students");

            entity.HasIndex(e => e.ParentId, "IX_students_parent_id");

            entity.HasIndex(e => e.SchoolId, "IX_students_school_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Grade)
                .HasMaxLength(20)
                .HasColumnName("grade");
            entity.Property(e => e.GroupName)
                .HasMaxLength(20)
                .HasColumnName("group_name");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.Students)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("students_parent_id_fkey");

            entity.HasOne(d => d.School).WithMany(p => p.Students)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("students_school_id_fkey");
        });

        modelBuilder.Entity<StudentActivityScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("student_activity_scores_pkey");

            entity.ToTable("student_activity_scores");

            entity.HasIndex(e => e.ActivityId, "idx_scores_activity");

            entity.HasIndex(e => e.StudentId, "idx_scores_student");

            entity.HasIndex(e => new { e.StudentId, e.ActivityId }, "uq_scores").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActivityId).HasColumnName("activity_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Score)
                .HasPrecision(2, 1)
                .HasColumnName("score");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Activity).WithMany(p => p.StudentActivityScores)
                .HasForeignKey(d => d.ActivityId)
                .HasConstraintName("student_activity_scores_activity_id_fkey");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentActivityScores)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("student_activity_scores_student_id_fkey");
        });

        modelBuilder.Entity<StudentAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("student_assignments_pkey");

            entity.ToTable("student_assignments");

            entity.HasIndex(e => e.GradeId, "IX_student_assignments_grade_id");

            entity.HasIndex(e => e.GroupId, "IX_student_assignments_group_id");

            entity.HasIndex(e => e.StudentId, "IX_student_assignments_student_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GradeId).HasColumnName("grade_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Grade).WithMany(p => p.StudentAssignments)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_grade");

            entity.HasOne(d => d.Group).WithMany(p => p.StudentAssignments)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_group");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentAssignments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_student");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subjects_pkey");

            entity.ToTable("subjects");

            entity.HasIndex(e => e.AreaId, "IX_subjects_AreaId");

            entity.HasIndex(e => e.SchoolId, "IX_subjects_school_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");

            entity.HasOne(d => d.Area).WithMany(p => p.Subjects).HasForeignKey(d => d.AreaId);

            entity.HasOne(d => d.School).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("subjects_school_id_fkey");
        });

        modelBuilder.Entity<SubjectAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subject_assignments_pkey");

            entity.ToTable("subject_assignments");

            entity.HasIndex(e => e.SchoolId, "IX_subject_assignments_SchoolId");

            entity.HasIndex(e => e.AreaId, "IX_subject_assignments_area_id");

            entity.HasIndex(e => e.GradeLevelId, "IX_subject_assignments_grade_level_id");

            entity.HasIndex(e => e.GroupId, "IX_subject_assignments_group_id");

            entity.HasIndex(e => e.SubjectId, "IX_subject_assignments_subject_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AreaId).HasColumnName("area_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GradeLevelId).HasColumnName("grade_level_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.SpecialtyId).HasColumnName("specialty_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasColumnName("status");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.HasOne(d => d.Area).WithMany(p => p.SubjectAssignments)
                .HasForeignKey(d => d.AreaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subject_assignments_area_id_fkey");

            entity.HasOne(d => d.GradeLevel).WithMany(p => p.SubjectAssignments)
                .HasForeignKey(d => d.GradeLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subject_assignments_grade_level_id_fkey");

            entity.HasOne(d => d.Group).WithMany(p => p.SubjectAssignments)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subject_assignments_group_id_fkey");

            entity.HasOne(d => d.School).WithMany(p => p.SubjectAssignments).HasForeignKey(d => d.SchoolId);

            entity.HasOne(d => d.Specialty).WithMany(p => p.SubjectAssignments)
                .HasForeignKey(d => d.SpecialtyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subject_assignments_specialty_id_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.SubjectAssignments)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("subject_assignments_subject_id_fkey");
        });

        modelBuilder.Entity<TeacherAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("teacher_assignments_pkey");

            entity.ToTable("teacher_assignments");

            entity.HasIndex(e => e.SubjectAssignmentId, "IX_teacher_assignments_subject_assignment_id");

            entity.HasIndex(e => new { e.TeacherId, e.SubjectAssignmentId }, "teacher_assignments_teacher_id_subject_assignment_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.SubjectAssignmentId).HasColumnName("subject_assignment_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

            entity.HasOne(d => d.SubjectAssignment).WithMany(p => p.TeacherAssignments)
                .HasForeignKey(d => d.SubjectAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teacher_assignments_subject_assignment_id_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherAssignments)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teacher_assignments_teacher_id_fkey");
        });

        modelBuilder.Entity<Trimester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trimester_pkey");

            entity.ToTable("trimester");

            entity.HasIndex(e => e.SchoolId, "IX_trimester_school_id");

            entity.HasIndex(e => new { e.Name, e.SchoolId }, "trimester_name_school_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Order).HasColumnName("order");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.School).WithMany(p => p.Trimesters)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("trimester_school_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.SchoolId, "IX_users_school_id");

            entity.HasIndex(e => e.DocumentId, "users_document_id_key").IsUnique();

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp with time zone")
                .HasColumnName("date_of_birth");
            entity.Property(e => e.DocumentId)
                .HasMaxLength(50)
                .HasColumnName("document_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.LastLogin)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("last_name");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(100)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TwoFactorEnabled)
                .HasDefaultValue(false)
                .HasColumnName("two_factor_enabled");

            entity.HasOne(d => d.SchoolNavigation).WithMany(p => p.Users)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_school_id_fkey");

            entity.HasMany(d => d.Grades).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserGrade",
                    r => r.HasOne<GradeLevel>().WithMany()
                        .HasForeignKey("GradeId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_grades_grade"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_grades_user"),
                    j =>
                    {
                        j.HasKey("UserId", "GradeId").HasName("user_grades_pkey");
                        j.ToTable("user_grades");
                        j.HasIndex(new[] { "GradeId" }, "IX_user_grades_grade_id");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("GradeId").HasColumnName("grade_id");
                    });

            entity.HasMany(d => d.Groups).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserGroup",
                    r => r.HasOne<Group>().WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_groups_group"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_groups_user"),
                    j =>
                    {
                        j.HasKey("UserId", "GroupId").HasName("user_groups_pkey");
                        j.ToTable("user_groups");
                        j.HasIndex(new[] { "GroupId" }, "IX_user_groups_group_id");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("GroupId").HasColumnName("group_id");
                    });

            entity.HasMany(d => d.Subjects).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserSubject",
                    r => r.HasOne<Subject>().WithMany()
                        .HasForeignKey("SubjectId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_subjects_subject"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_user_subjects_user"),
                    j =>
                    {
                        j.HasKey("UserId", "SubjectId").HasName("user_subjects_pkey");
                        j.ToTable("user_subjects");
                        j.HasIndex(new[] { "SubjectId" }, "IX_user_subjects_subject_id");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("SubjectId").HasColumnName("subject_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    /// <summary>
    /// Configura el manejo global de DateTime para PostgreSQL
    /// </summary>
    private void ConfigureDateTimeHandling(ModelBuilder modelBuilder)
    {
        // Configurar todas las propiedades DateTime para usar UTC con timezone
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    // Configurar para usar timestamp with time zone
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasColumnType("timestamp with time zone");
                    
                    // Configurar el valor por defecto para DateTime no nullable
                    if (property.ClrType == typeof(DateTime))
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property(property.Name)
                            .HasDefaultValueSql("CURRENT_TIMESTAMP");
                    }
                }
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
