using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;
using FileEntity = TaskFlow.Domain.Entities.File;

namespace TaskFlow.Infrastructure.Repositories;

public class FileRepository : IFileRepository
{
    private readonly TaskFlowDbContext _context;

    public FileRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<FileEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Files
            .Include(f => f.Uploader)
            .FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(FileEntity file)
    {
        await _context.Files.AddAsync(file);
    }

    public async System.Threading.Tasks.Task AddTaskAttachmentAsync(TaskAttachment attachment)
    {
        await _context.TaskAttachments.AddAsync(attachment);
    }

    public void RemoveTaskAttachment(TaskAttachment attachment)
    {
        _context.TaskAttachments.Remove(attachment);
    }

    public async System.Threading.Tasks.Task<TaskAttachment?> GetTaskAttachmentAsync(Guid taskId, Guid fileId)
    {
        return await _context.TaskAttachments
            .FirstOrDefaultAsync(ta => ta.TaskId == taskId && ta.FileId == fileId);
    }

    public async System.Threading.Tasks.Task<TaskAttachment?> GetTaskAttachmentByIdAsync(Guid attachmentId)
    {
        return await _context.TaskAttachments
            .Include(ta => ta.File)
            .FirstOrDefaultAsync(ta => ta.Id == attachmentId);
    }

    public async System.Threading.Tasks.Task<List<TaskAttachment>> GetTaskAttachmentsByTaskIdAsync(Guid taskId)
    {
        return await _context.TaskAttachments
            .Include(ta => ta.File)
                .ThenInclude(f => f.Uploader)
            .Where(ta => ta.TaskId == taskId && ta.File.DeletedAt == null)
            .OrderBy(ta => ta.CreatedAt)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<bool> HasActiveAttachmentsAsync(Guid fileId)
    {
        return await _context.TaskAttachments
            .AnyAsync(ta => ta.FileId == fileId && ta.File.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<Guid>> GetTaskIdsByFileIdAsync(Guid fileId)
    {
        return await _context.TaskAttachments
            .Where(ta => ta.FileId == fileId && ta.File.DeletedAt == null)
            .Select(ta => ta.TaskId)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
