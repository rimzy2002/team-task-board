using Microsoft.EntityFrameworkCore;

namespace TeamTaskBoard.Api.Data;

public sealed class TeamTaskBoardContext(DbContextOptions<TeamTaskBoardContext> options)
    : DbContext(options)
{
    private const string ExactTextCollation = "utf8mb4_0900_bin";

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<BoardTask> Tasks => Set<BoardTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<User>();
        users.ToTable("users", table =>
            table.HasCheckConstraint("ck_users_firebase_uid", "CHAR_LENGTH(TRIM(firebase_uid)) > 0"));
        users.Property(user => user.Id).HasColumnName("id");
        users.Property(user => user.FirebaseUid).HasColumnName("firebase_uid")
            .HasMaxLength(128).UseCollation(ExactTextCollation);
        users.Property(user => user.Email).HasColumnName("email").HasMaxLength(254);
        users.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)");
        users.HasIndex(user => user.FirebaseUid).IsUnique().HasDatabaseName("ux_users_firebase_uid");

        var projects = modelBuilder.Entity<Project>();
        projects.ToTable("projects", table =>
            table.HasCheckConstraint("ck_projects_name", "CHAR_LENGTH(TRIM(name)) > 0"));
        projects.Property(project => project.Id).HasColumnName("id");
        projects.Property(project => project.OwnerId).HasColumnName("owner_id");
        projects.Property(project => project.Name).HasColumnName("name").HasMaxLength(200);
        projects.Property(project => project.Description).HasColumnName("description").HasColumnType("text");
        projects.Property(project => project.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)");
        projects.Property(project => project.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime(6)");
        projects.HasOne(project => project.Owner).WithMany().HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        projects.HasIndex(project => project.OwnerId).HasDatabaseName("ix_projects_owner_id");

        var tasks = modelBuilder.Entity<BoardTask>();
        tasks.ToTable("tasks", table =>
        {
            table.HasCheckConstraint("ck_tasks_title", "CHAR_LENGTH(TRIM(title)) > 0");
            table.HasCheckConstraint("ck_tasks_status", "status IN ('ToDo', 'InProgress', 'Done')");
            table.HasCheckConstraint("ck_tasks_priority", "priority IN ('Low', 'Medium', 'High')");
        });
        tasks.Property(task => task.Id).HasColumnName("id");
        tasks.Property(task => task.ProjectId).HasColumnName("project_id");
        tasks.Property(task => task.Title).HasColumnName("title").HasMaxLength(200);
        tasks.Property(task => task.Description).HasColumnName("description").HasColumnType("text");
        tasks.Property(task => task.Status).HasColumnName("status").HasConversion<string>()
            .HasMaxLength(16).UseCollation(ExactTextCollation);
        tasks.Property(task => task.Priority).HasColumnName("priority").HasConversion<string>()
            .HasMaxLength(8).UseCollation(ExactTextCollation);
        tasks.Property(task => task.DueDate).HasColumnName("due_date").HasColumnType("date");
        tasks.Property(task => task.Assignee).HasColumnName("assignee").HasMaxLength(200);
        tasks.Property(task => task.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("datetime(6)");
        tasks.Property(task => task.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("datetime(6)");
        tasks.HasOne(task => task.Project).WithMany().HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        tasks.HasIndex(task => new { task.ProjectId, task.Status, task.Priority })
            .HasDatabaseName("ix_tasks_project_status_priority");
    }
}
