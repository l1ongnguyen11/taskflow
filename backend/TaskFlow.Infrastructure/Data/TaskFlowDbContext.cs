using Microsoft.EntityFrameworkCore;
// Alias để tránh trùng với System.Task và System.IO.File
using TaskEntity = TaskFlow.Domain.Entities.Task;
using FileEntity = TaskFlow.Domain.Entities.File;

using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Data;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
        string[] sqlStatements = new[]
        {
            "ALTER TABLE project ADD COLUMN IF NOT EXISTS is_archived BOOLEAN NOT NULL DEFAULT FALSE;",
            "ALTER TABLE project_member ADD COLUMN IF NOT EXISTS role VARCHAR(50) NOT NULL DEFAULT 'member';",
            "ALTER TABLE invitation ADD COLUMN IF NOT EXISTS role VARCHAR(50) NOT NULL DEFAULT 'member';",
            "ALTER TABLE role_permission ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE workspace_member ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE user_role ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE refresh_token ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE project_member ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE task_assignee ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE task_watcher ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE task_dependency ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE task_label ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE task_attachment ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE activity ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now();",
            "ALTER TABLE role ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE permission ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE role_permission ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE workspace_member ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE user_role ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE refresh_token ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE invitation ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE project_member ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE task_assignee ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE task_watcher ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE task_dependency ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE task_label ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE checklist ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE checklist_item ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE task_attachment ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE activity ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE notification ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "ALTER TABLE time_log ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ;",
            "DROP INDEX IF EXISTS uq_sprint_active_per_project;",
            "ALTER TABLE sprint ALTER COLUMN status DROP DEFAULT;",
            "ALTER TABLE sprint ALTER COLUMN status TYPE VARCHAR(50) USING status::text;",
            "ALTER TABLE task ALTER COLUMN priority DROP DEFAULT;",
            "ALTER TABLE task ALTER COLUMN priority TYPE VARCHAR(50) USING priority::varchar;",
            "ALTER TABLE task ALTER COLUMN type DROP DEFAULT;",
            "ALTER TABLE task ALTER COLUMN type TYPE VARCHAR(50) USING type::varchar;",
            "ALTER TABLE task_dependency ALTER COLUMN type DROP DEFAULT;",
            "ALTER TABLE task_dependency ALTER COLUMN type TYPE VARCHAR(50) USING type::varchar;",
            "ALTER TABLE invitation ALTER COLUMN status DROP DEFAULT;",
            "ALTER TABLE invitation ALTER COLUMN status TYPE VARCHAR(50) USING status::varchar;",
            "ALTER TABLE notification ALTER COLUMN type DROP DEFAULT;",
            "ALTER TABLE notification ALTER COLUMN type TYPE VARCHAR(50) USING type::varchar;",
            "ALTER TABLE activity ALTER COLUMN action DROP DEFAULT;",
            "ALTER TABLE activity ALTER COLUMN action TYPE VARCHAR(50) USING action::varchar;"
        };

        foreach (var stmt in sqlStatements)
        {
            try
            {
                Database.ExecuteSqlRaw(stmt);
            }
            catch
            {
                // Ignore individual schema alter failures
            }
        }
    }

    // Authentication
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Workspace
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    // Project
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    // Board
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();

    // Task
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
    public DbSet<TaskWatcher> TaskWatchers => Set<TaskWatcher>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    // File
    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    // Collaboration
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // Tracking
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<TimeLog> TimeLogs => Set<TimeLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}