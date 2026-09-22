using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly TaskFlowDbContext _context;

    public ProjectRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Project?> GetByIdAsync(Guid id)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Project?> GetByIdWithLeadAndMembersAsync(Guid id)
    {
        return await _context.Projects
            .Include(p => p.Workspace)
            .Include(p => p.Lead)
            .Include(p => p.Members)
                .ThenInclude(pm => pm.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async System.Threading.Tasks.Task<Project?> GetByKeyAndWorkspaceAsync(string key, Guid workspaceId)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Key.ToLower() == key.ToLower() && 
                                     p.WorkspaceId == workspaceId && 
                                     p.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(Project project)
    {
        await _context.Projects.AddAsync(project);
    }

    public async System.Threading.Tasks.Task AddMemberAsync(ProjectMember member)
    {
        await _context.ProjectMembers.AddAsync(member);
    }

    public async System.Threading.Tasks.Task<ProjectMember?> GetMemberAsync(Guid projectId, Guid userId)
    {
        return await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId && pm.DeletedAt == null);
    }

    public void RemoveMember(ProjectMember member)
    {
        _context.ProjectMembers.Remove(member);
    }

    public async System.Threading.Tasks.Task<(List<ProjectMember> Members, int TotalCount)> GetProjectMembersAsync(Guid projectId, int limit, int offset)
    {
        var query = _context.ProjectMembers
            .Include(pm => pm.User)
            .Where(pm => pm.ProjectId == projectId && pm.DeletedAt == null);

        var totalCount = await query.CountAsync();
        var members = await query
            .OrderBy(pm => pm.User.DisplayName)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (members, totalCount);
    }

    public async System.Threading.Tasks.Task<(List<Project> Projects, int TotalCount)> GetWorkspaceProjectsAsync(
        Guid workspaceId, bool includeArchived, bool includeDeleted, int limit, int offset)
    {
        IQueryable<Project> query = _context.Projects
            .Include(p => p.Lead)
            .Where(p => p.WorkspaceId == workspaceId);

        if (!includeDeleted)
        {
            query = query.Where(p => p.DeletedAt == null);
        }

        if (!includeArchived)
        {
            query = query.Where(p => p.IsArchived == false);
        }

        var totalCount = await query.CountAsync();
        var projects = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (projects, totalCount);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
