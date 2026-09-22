using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;
using FileEntity = TaskFlow.Domain.Entities.File;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IFileRepository
{
    System.Threading.Tasks.Task<FileEntity?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task AddAsync(FileEntity file);

    // Task Attachments
    System.Threading.Tasks.Task AddTaskAttachmentAsync(TaskAttachment attachment);
    void RemoveTaskAttachment(TaskAttachment attachment);
    System.Threading.Tasks.Task<TaskAttachment?> GetTaskAttachmentAsync(Guid taskId, Guid fileId);
    System.Threading.Tasks.Task<TaskAttachment?> GetTaskAttachmentByIdAsync(Guid attachmentId);
    System.Threading.Tasks.Task<List<TaskAttachment>> GetTaskAttachmentsByTaskIdAsync(Guid taskId);
    System.Threading.Tasks.Task<bool> HasActiveAttachmentsAsync(Guid fileId);
    System.Threading.Tasks.Task<List<Guid>> GetTaskIdsByFileIdAsync(Guid fileId);

    System.Threading.Tasks.Task SaveChangesAsync();
}
