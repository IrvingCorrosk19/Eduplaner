using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=dpg-d0pkk1ruibrs73fu6qfg-a/schoolmanager_1226a_nmph;Database=schoolmanager_1226a_nmph;Username=admin;Password=RzyER2uM8Dqc51zVBhvC35n9QHXmxvUn");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activities_pkey");

            entity.ToTable("activities");

            entity.HasIndex(e => e.GroupId, "idx_activities_group");

            entity.HasIndex(e => e.TeacherId, "idx_activities_teacher");

            entity.HasIndex(e => e.Trimester, "idx_activities_trimester");

            entity.HasIndex(e => new { e.Name, e.Type, e.SubjectId, e.GroupId, e.TeacherId, e.Trimester }, "idx_activities_unique_lookup");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
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
                .HasColumnType("timestamp without time zone")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Activity).WithMany(p => p.ActivityAttachments)
                .HasForeignKey(d => d.ActivityId)
                .HasConstraintName("activity_attachments_activity_id_fkey");
        });

        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("activity_types_pkey");

            entity.ToTable("activity_types");

            entity.HasIndex(e => e.Name, "activity_types_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("area_pkey");

            entity.ToTable("area");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attendance_pkey");

            entity.ToTable("attendance");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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
                .HasColumnType("timestamp without time zone")
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GradeLevelId).HasColumnName("grade_level_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Hora).HasColumnName("hora");
            entity.Property(e => e.ReportType)
                .HasMaxLength(100)
                .HasColumnName("report_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pendiente'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

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
                .HasColumnType("timestamp without time zone")
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<SecuritySetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("security_settings_pkey");

            entity.ToTable("security_settings");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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
                .HasColumnType("timestamp without time zone")
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Score)
                .HasPrecision(4, 2)
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SchoolId).HasColumnName("school_id");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");

            entity.HasOne(d => d.School).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("subjects_school_id_fkey");
        });

        modelBuilder.Entity<SubjectAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subject_assignments_pkey");

            entity.ToTable("subject_assignments");

            entity.HasIndex(e => new { e.SpecialtyId, e.AreaId, e.SubjectId, e.GradeLevelId, e.GroupId }, "subject_assignments_specialty_id_area_id_subject_id_grade_l_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AreaId).HasColumnName("area_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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

            entity.HasIndex(e => new { e.TeacherId, e.SubjectAssignmentId }, "teacher_assignments_teacher_id_subject_assignment_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
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
            entity.HasKey(e => e.Id).HasName("trimesters_pkey");

            entity.ToTable("trimesters");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.DocumentId, "users_document_id_key").IsUnique();

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("date_of_birth");
            entity.Property(e => e.DocumentId)
                .HasMaxLength(50)
                .HasColumnName("document_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.LastLogin)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_login");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
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

            entity.HasOne(d => d.School).WithMany(p => p.Users)
                .HasForeignKey(d => d.SchoolId)
                .OnDelete(DeleteBehavior.Cascade)
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
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("SubjectId").HasColumnName("subject_id");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
