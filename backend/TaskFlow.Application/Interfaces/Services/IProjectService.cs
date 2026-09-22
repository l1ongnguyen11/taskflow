using System;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Interfaces.Services;

public interface IProjectService
{
    Task<ApiResponse<ProjectResponse>> CreateProjectAsync(Guid userId, Guid workspaceId, CreateProjectRequest request);
    Task<ApiResponse<ProjectResponse>> GetProjectByIdAsync(Guid userId, Guid projectId);
    Task<ApiResponse<ProjectResponse>> UpdateProjectAsync(Guid userId, Guid projectId, UpdateProjectRequest request);
    Task<ApiResponse<object>> DeleteProjectAsync(Guid userId, Guid projectId);
    Task<ApiResponse<object>> ArchiveProjectAsync(Guid userId, Guid projectId);
    Task<ApiResponse<object>> RestoreProjectAsync(Guid userId, Guid projectId);
    Task<PaginatedResponse<ProjectResponse>> GetWorkspaceProjectsAsync(Guid userId, Guid workspaceId, bool includeArchived, bool includeDeleted, int limit, int offset);

    // Project Member Methods
    Task<ApiResponse<ProjectMemberResponse>> AddMemberAsync(Guid userId, Guid projectId, AddProjectMemberRequest request);
    Task<ApiResponse<object>> RemoveMemberAsync(Guid userId, Guid projectId, Guid targetUserId);
    Task<ApiResponse<ProjectMemberResponse>> UpdateMemberRoleAsync(Guid userId, Guid projectId, Guid targetUserId, UpdateProjectMemberRoleRequest request);
    Task<PaginatedResponse<ProjectMemberResponse>> GetProjectMembersAsync(Guid userId, Guid projectId, int limit, int offset);
}
