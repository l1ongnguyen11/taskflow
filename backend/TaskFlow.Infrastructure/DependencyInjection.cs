using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Security;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<TaskFlowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ITimeLogRepository, TimeLogRepository>();
        services.AddScoped<IAuthorizationRepository, AuthorizationRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        services.Configure<TaskFlow.Application.DTOs.Common.SmtpSettings>(options =>
            configuration.GetSection("SmtpSettings").Bind(options));
        services.AddScoped<TaskFlow.Application.Interfaces.Services.IEmailService, TaskFlow.Infrastructure.Services.EmailService>();

        return services;
    }
}