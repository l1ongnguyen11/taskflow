using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Files;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;
using FileEntity = TaskFlow.Domain.Entities.File;

namespace TaskFlow.Application.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly string _uploadFolder;
    private readonly long _maxSizeBytes;
    private readonly HashSet<string> _allowedExtensions;
    private readonly HashSet<string> _allowedMimeTypes;

    public FileService(
        IFileRepository fileRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository,
        IConfiguration configuration)
    {
        _fileRepository = fileRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;

        _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files");
        _maxSizeBytes = configuration.GetValue<long>("FileStorage:MaxSizeBytes", 10 * 1024 * 1024);
        _allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions")
            .Get<string[]>()?.Select(e => e.ToLowerInvariant()).ToHashSet()
            ?? new HashSet<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".zip", ".txt" };
        _allowedMimeTypes = configuration.GetSection("FileStorage:AllowedMimeTypes")
            .Get<string[]>()?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ApiResponse<FileResponse>> UploadAsync(
        Guid userId, Stream stream, string fileName, string contentType, long sizeBytes)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
        {
            return ApiResponse<FileResponse>.FailResponse("Invalid file type.");
        }

        if (_allowedMimeTypes.Count > 0 && !_allowedMimeTypes.Contains(contentType))
        {
            return ApiResponse<FileResponse>.FailResponse("Invalid MIME type.");
        }

        if (sizeBytes <= 0 || sizeBytes > _maxSizeBytes)
        {
            return ApiResponse<FileResponse>.FailResponse($"File size must be between 1 byte and {_maxSizeBytes} bytes.");
        }

        Directory.CreateDirectory(_uploadFolder);

        var storedName = $"{Guid.NewGuid()}{extension}";
        var physicalPath = Path.Combine(_uploadFolder, storedName);
        var storedPath = $"/uploads/files/{storedName}";

        await using (var fileStream = new FileStream(physicalPath, FileMode.Create))
        {
            await stream.CopyToAsync(fileStream);
        }

        var file = new FileEntity
        {
            Id = Guid.NewGuid(),
            OriginalName = fileName,
            StoredPath = storedPath,
            MimeType = contentType,
            SizeBytes = sizeBytes,
            UploadedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fileRepository.AddAsync(file);
        await _fileRepository.SaveChangesAsync();

        var reloaded = await _fileRepository.GetByIdAsync(file.Id);
        return ApiResponse<FileResponse>.SuccessResponse(MapToFileResponse(reloaded!), "File uploaded successfully.");
    }

    public async Task<(ApiResponse<FileResponse>? Error, Stream? Stream, string? ContentType, string? FileName)> DownloadAsync(
        Guid userId, Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            return (ApiResponse<FileResponse>.FailResponse("File not found."), null, null, null);
        }

        if (!await CanAccessFileAsync(userId, file))
        {
            return (ApiResponse<FileResponse>.FailResponse("You do not have access to this file."), null, null, null);
        }

        var physicalPath = GetPhysicalPath(file.StoredPath);
        if (!System.IO.File.Exists(physicalPath))
        {
            return (ApiResponse<FileResponse>.FailResponse("Physical file not found."), null, null, null);
        }

        var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (null, stream, file.MimeType, file.OriginalName);
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            return ApiResponse<object>.FailResponse("File not found.");
        }

        if (file.UploadedBy != userId)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to delete this file.");
        }

        if (await _fileRepository.HasActiveAttachmentsAsync(fileId))
        {
            return ApiResponse<object>.FailResponse("Cannot delete file while it is attached to tasks.");
        }

        file.DeletedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;
        await _fileRepository.SaveChangesAsync();

        var physicalPath = GetPhysicalPath(file.StoredPath);
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        return ApiResponse<object>.SuccessResponse(new { }, "File deleted successfully.");
    }

    public async Task<ApiResponse<TaskAttachmentResponse>> AttachToTaskAsync(
        Guid userId, Guid taskId, AttachFileToTaskRequest request)
    {
        var (task, _, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<TaskAttachmentResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskAttachmentResponse>.FailResponse("You do not have access to this task.");
        }

        var file = await _fileRepository.GetByIdAsync(request.FileId);
        if (file == null)
        {
            return ApiResponse<TaskAttachmentResponse>.FailResponse("File not found.");
        }

        var existing = await _fileRepository.GetTaskAttachmentAsync(taskId, request.FileId);
        if (existing != null)
        {
            return ApiResponse<TaskAttachmentResponse>.FailResponse("File is already attached to this task.");
        }

        var attachment = new TaskAttachment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            FileId = request.FileId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _fileRepository.AddTaskAttachmentAsync(attachment);
        await _fileRepository.SaveChangesAsync();

        var reloaded = await _fileRepository.GetTaskAttachmentByIdAsync(attachment.Id);
        return ApiResponse<TaskAttachmentResponse>.SuccessResponse(
            MapToAttachmentResponse(reloaded!), "File attached successfully.");
    }

    public async Task<ApiResponse<List<TaskAttachmentResponse>>> GetTaskAttachmentsAsync(Guid userId, Guid taskId)
    {
        var (task, _, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<List<TaskAttachmentResponse>>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<List<TaskAttachmentResponse>>.FailResponse("You do not have access to this task.");
        }

        var attachments = await _fileRepository.GetTaskAttachmentsByTaskIdAsync(taskId);
        var responses = attachments.Select(MapToAttachmentResponse).ToList();
        return ApiResponse<List<TaskAttachmentResponse>>.SuccessResponse(responses, "Attachments retrieved successfully.");
    }

    public async Task<ApiResponse<object>> DetachFromTaskAsync(Guid userId, Guid taskId, Guid attachmentId)
    {
        var (task, _, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this task.");
        }

        var attachment = await _fileRepository.GetTaskAttachmentByIdAsync(attachmentId);
        if (attachment == null || attachment.TaskId != taskId)
        {
            return ApiResponse<object>.FailResponse("Attachment not found.");
        }

        _fileRepository.RemoveTaskAttachment(attachment);
        await _fileRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Attachment removed successfully.");
    }

    private async Task<bool> CanAccessFileAsync(Guid userId, FileEntity file)
    {
        if (file.UploadedBy == userId)
        {
            return true;
        }

        var taskIds = await _fileRepository.GetTaskIdsByFileIdAsync(file.Id);
        foreach (var taskId in taskIds)
        {
            var (_, _, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
            if (isMember)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<(TaskFlow.Domain.Entities.Task? Task, Guid WorkspaceId, bool IsMember)> GetTaskWorkspaceInfoAsync(
        Guid taskId, Guid userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return (null, Guid.Empty, false);

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId ?? Guid.Empty);
        if (column == null) return (task, Guid.Empty, false);

        var project = await _projectRepository.GetByIdAsync(column.Board.ProjectId);
        if (project == null || project.DeletedAt != null) return (task, Guid.Empty, false);

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        return (task, project.WorkspaceId, isMember);
    }

    private static string GetPhysicalPath(string storedPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storedPath.TrimStart('/'));
    }

    private static FileResponse MapToFileResponse(FileEntity file)
    {
        return new FileResponse
        {
            Id = file.Id,
            OriginalName = file.OriginalName,
            MimeType = file.MimeType,
            SizeBytes = file.SizeBytes,
            UploadedBy = file.UploadedBy,
            DownloadUrl = $"/api/files/{file.Id}/download",
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };
    }

    private static TaskAttachmentResponse MapToAttachmentResponse(TaskAttachment attachment)
    {
        return new TaskAttachmentResponse
        {
            Id = attachment.Id,
            TaskId = attachment.TaskId,
            FileId = attachment.FileId,
            File = MapToFileResponse(attachment.File),
            CreatedAt = attachment.CreatedAt
        };
    }
}
