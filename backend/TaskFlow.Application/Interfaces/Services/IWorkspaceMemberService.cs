using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Workspaces;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceMemberService
{
    Task<ApiResponse<InviteMemberResponse>> InviteMemberAsync(Guid userId, Guid workspaceId, InviteMemberRequest request);
    Task<ApiResponse<object>> RemoveMemberAsync(Guid userId, Guid workspaceId, Guid targetUserId);
    Task<ApiResponse<object>> ChangeMemberRoleAsync(Guid userId, Guid workspaceId, Guid targetUserId, ChangeRoleRequest request);
    Task<PaginatedResponse<WorkspaceMemberResponse>> GetMembersAsync(Guid userId, Guid workspaceId, string? search, int limit, int offset);
    Task<ApiResponse<AcceptInvitationResponse>> AcceptInvitationAsync(Guid userId, string token);
    Task<ApiResponse<object>> RejectInvitationAsync(Guid userId, string token);
    Task<ApiResponse<object>> CancelInvitationAsync(Guid userId, Guid workspaceId, Guid invitationId);
    Task<PaginatedResponse<InvitationResponse>> GetWorkspaceInvitationsAsync(Guid userId, Guid workspaceId, string? status, int limit, int offset);
    Task<ApiResponse<List<InvitationResponse>>> GetMyInvitationsAsync(Guid userId);
}
