using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IProjectRepository
{
    System.Threading.Tasks.Task<Project?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<Project?> GetByIdWithLeadAndMembersAsync(Guid id);
    System.Threading.Tasks.Task<Project?> GetByKeyAndWorkspaceAsync(string key, Guid workspaceId);
    System.Threading.Tasks.Task AddAsync(Project project);
    System.Threading.Tasks.Task AddMemberAsync(ProjectMember member);
    System.Threading.Tasks.Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId);
    void RemoveMember(ProjectMember member);
    System.Threading.Tasks.Task<(List<ProjectMember> Members, int TotalCount)> GetProjectMembersAsync(Guid projectId, int limit, int offset);
    System.Threading.Tasks.Task<(List<Project> Projects, int TotalCount)> GetWorkspaceProjectsAsync(Guid workspaceId, bool includeArchived, bool includeDeleted, int limit, int offset);
    System.Threading.Tasks.Task SaveChangesAsync();
}
