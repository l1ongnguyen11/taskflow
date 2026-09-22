using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using Task = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Configurations;

public static class ConfigurationExtensions
{
    public static ModelBuilder ApplyTaskFlowConfigurations(this ModelBuilder builder)
    {
        // Apply all configurations in the current assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ConfigurationExtensions).Assembly);

        // Centrally configure BaseEntity fields to avoid duplication and ignore non-existent db columns
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var type = entityType.ClrType;

                builder.Entity(type, entity =>
                {
                    // Map Id to 'id'
                    entity.Property("Id")
                          .HasColumnName("id")
                          .HasColumnType("uuid");

                    // Map CreatedAt to 'created_at'
                    entity.Property("CreatedAt")
                          .HasColumnName("created_at")
                          .HasColumnType("timestamptz")
                          .HasDefaultValueSql("now()");

                    // Conditionally map/ignore UpdatedAt
                    if (EntitiesWithoutUpdatedAt.Contains(type))
                    {
                        entity.Ignore("UpdatedAt");
                    }
                    else
                    {
                        entity.Property("UpdatedAt")
                              .HasColumnName("updated_at")
                              .HasColumnType("timestamptz")
                              .HasDefaultValueSql("now()");
                    }

                    // Conditionally map/ignore DeletedAt
                    if (EntitiesWithoutDeletedAt.Contains(type))
                    {
                        entity.Ignore("DeletedAt");
                    }
                    else
                    {
                        entity.Property("DeletedAt")
                              .HasColumnName("deleted_at")
                              .HasColumnType("timestamptz");
                    }
                });
            }
        }

        return builder;
    }

    private static readonly HashSet<Type> EntitiesWithoutUpdatedAt = new()
    {
        typeof(RolePermission),
        typeof(WorkspaceMember),
        typeof(UserRole),
        typeof(RefreshToken),
        typeof(ProjectMember),
        typeof(TaskAssignee),
        typeof(TaskWatcher),
        typeof(TaskDependency),
        typeof(TaskLabel),
        typeof(TaskAttachment),
        typeof(Activity)
    };

    private static readonly HashSet<Type> EntitiesWithoutDeletedAt = new()
    {
        typeof(Role),
        typeof(Permission),
        typeof(RolePermission),
        typeof(WorkspaceMember),
        typeof(UserRole),
        typeof(RefreshToken),
        typeof(Invitation),
        typeof(ProjectMember),
        typeof(TaskAssignee),
        typeof(TaskWatcher),
        typeof(TaskDependency),
        typeof(TaskLabel),
        typeof(Checklist),
        typeof(ChecklistItem),
        typeof(TaskAttachment),
        typeof(Activity),
        typeof(Notification),
        typeof(TimeLog)
    };
}
