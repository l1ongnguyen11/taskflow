using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Workspaces;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceService
{
    Task<ApiResponse<WorkspaceResponse>> CreateWorkspaceAsync(Guid userId, CreateWorkspaceRequest request);
    Task<ApiResponse<WorkspaceResponse>> UpdateWorkspaceAsync(Guid userId, Guid workspaceId, UpdateWorkspaceRequest request);
    Task<ApiResponse<object>> DeleteWorkspaceAsync(Guid userId, Guid workspaceId);
    Task<ApiResponse<WorkspaceResponse>> GetWorkspaceAsync(Guid userId, Guid workspaceId);
    Task<PaginatedResponse<WorkspaceResponse>> GetMyWorkspacesAsync(Guid userId, int limit, int offset);
}
