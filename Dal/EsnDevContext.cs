using Bo.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal;

public partial class EsnDevContext : DbContext
{
    public EsnDevContext()
    {
    }

    public EsnDevContext(DbContextOptions<EsnDevContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminBo> Admins { get; set; }
    public virtual DbSet<EventBo> Events { get; set; }
    public virtual DbSet<EventRegistrationBo> EventRegistrations { get; set; }
    public virtual DbSet<EventTemplateBo> EventTemplates { get; set; }
    public virtual DbSet<UserBo> Users { get; set; }
    public virtual DbSet<RoleBo> Roles { get; set; }
    public virtual DbSet<PropositionBo> Propositions { get; set; }
    public virtual DbSet<PropositionVoteBo> PropositionVotes { get; set; }
    public virtual DbSet<CalendarBo> Calendars { get; set; }
    public virtual DbSet<CalendarSubOrganizerBo> CalendarSubOrganizers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Admins__3214EC07C94A3654");
            entity.HasIndex(e => e.Email, "UQ__Admins__A9D105348FC7749F").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.LastName).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.LastLoginAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<EventBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Events__3214EC079399E819");

            entity.Property(e => e.Title).HasMaxLength(255).IsUnicode(false).IsRequired();
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Location).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.StartDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.EventfrogLink).IsUnicode(false);
            entity.Property(e => e.SurveyJsData).IsUnicode(false);
            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(d => d.User)
                .WithMany(p => p.Events)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Events_Users");
        });

        modelBuilder.Entity<EventRegistrationBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventReg__3214EC07FA5EB91B");

            entity.HasIndex(e => new { e.UserId, e.EventId }, "UQ_Registration").IsUnique();

            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SurveyJsData).IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("registered");

            entity.HasOne(d => d.Event)
                .WithMany(p => p.EventRegistrations)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Registrations_Events");

            entity.HasOne(d => d.User)
                .WithMany(p => p.EventRegistrations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Registrations_Users");
        });

        modelBuilder.Entity<UserBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC076FF3256C");
            entity.HasIndex(e => e.Email, "UQ__Users__A9D105340768CBC8").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.LastName).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.BirthDate).HasColumnType("datetime");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.EsnCardNumber).HasMaxLength(50).IsUnicode(false).HasColumnName("ESNCardNumber");
            entity.Property(e => e.UniversityName).HasMaxLength(255).IsUnicode(false);
            entity.Property(e => e.StudentType).HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.TransportPass).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.LastLoginAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<RoleBo>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(50).IsUnicode(false);

            entity.Property(r => r.CanCreateEvents).IsRequired();
            entity.Property(r => r.CanModifyEvents).IsRequired();
            entity.Property(r => r.CanDeleteEvents).IsRequired();
            entity.Property(r => r.CanCreateUsers).IsRequired();
            entity.Property(r => r.CanModifyUsers).IsRequired();
            entity.Property(r => r.CanDeleteUsers).IsRequired();
        });

        modelBuilder.Entity<UserBo>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PropositionBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Propositions__3214EC07");

            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.ModifiedAt)
                .HasColumnType("datetime");

            entity.Property(e => e.VotesUp)
                .HasDefaultValue(0);

            entity.Property(e => e.VotesDown)
                .HasDefaultValue(0);

            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Propositions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Propositions_Users");
        });

        modelBuilder.Entity<PropositionVoteBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PropositionVotes__3214EC07");

            // Unique constraint: one user can vote only once per proposition
            entity.HasIndex(e => new { e.UserId, e.PropositionId }, "UQ_PropositionVotes_User_Proposition")
                .IsUnique();

            entity.Property(e => e.VoteType)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime");

            entity.HasOne(d => d.Proposition)
                .WithMany(p => p.Votes)
                .HasForeignKey(d => d.PropositionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PropositionVotes_Propositions");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_PropositionVotes_Users");
        });

        modelBuilder.Entity<CalendarBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Calendars__3214EC07");

            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.EventDate)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())")
                .IsRequired();

            entity.HasOne(d => d.Event)
                .WithMany(p => p.Calendars)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Calendars_Events");

            entity.HasOne(d => d.MainOrganizer)
                .WithMany()
                .HasForeignKey(d => d.MainOrganizerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Calendars_MainOrganizer");

            entity.HasOne(d => d.EventManager)
                .WithMany()
                .HasForeignKey(d => d.EventManagerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Calendars_EventManager");

            entity.HasOne(d => d.ResponsableCom)
                .WithMany()
                .HasForeignKey(d => d.ResponsableComId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Calendars_ResponsableCom");
        });

        modelBuilder.Entity<CalendarSubOrganizerBo>(entity =>
        {
            entity.HasKey(e => new { e.CalendarId, e.UserId });

            entity.HasOne(d => d.Calendar)
                .WithMany(p => p.CalendarSubOrganizers)
                .HasForeignKey(d => d.CalendarId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CalendarSubOrganizers_Calendars");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CalendarSubOrganizers_Users");
        });

        modelBuilder.Entity<EventTemplateBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventTemplate__3214EC07");

            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.Property(e => e.SurveyJsData)
                .IsUnicode(false)
                .IsRequired();
        });

        // Note: Initial data seeding is handled by Dal/Seeds/DatabaseSeeder.cs
        // The seeder is called automatically at application startup in Program.cs

        base.OnModelCreating(modelBuilder);
        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}