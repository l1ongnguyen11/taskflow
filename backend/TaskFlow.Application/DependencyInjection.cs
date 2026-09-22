using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Services;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IChecklistService, ChecklistService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<ITimeLogService, TimeLogService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
