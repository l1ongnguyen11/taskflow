using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Security;
using TaskFlow.Domain.Entities;
using TaskEntity = TaskFlow.Domain.Entities.Task;

namespace TaskFlow.Infrastructure.Data;

public static class DbSeeder
{
    public static async System.Threading.Tasks.Task SeedAsync(TaskFlowDbContext context, IPasswordHasher passwordHasher)
    {
        // 1. Check if test accounts already exist
        var leadEmail = "lead@taskflow.dev";
        var memberEmail = "member@taskflow.dev";

        if (await context.Users.AnyAsync(u => u.Email == leadEmail))
        {
            return; // Seed data already present
        }

        // 2. Ensure Default Roles exist
        var ownerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "owner");
        if (ownerRole == null)
        {
            ownerRole = new Role { Id = Guid.NewGuid(), Name = "owner", Description = "Full control over the workspace", CreatedAt = DateTime.UtcNow };
            await context.Roles.AddAsync(ownerRole);
        }

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
        if (adminRole == null)
        {
            adminRole = new Role { Id = Guid.NewGuid(), Name = "admin", Description = "Administrative access", CreatedAt = DateTime.UtcNow };
            await context.Roles.AddAsync(adminRole);
        }

        var memberRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "member");
        if (memberRole == null)
        {
            memberRole = new Role { Id = Guid.NewGuid(), Name = "member", Description = "Standard member", CreatedAt = DateTime.UtcNow };
            await context.Roles.AddAsync(memberRole);
        }

        var viewerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "viewer");
        if (viewerRole == null)
        {
            viewerRole = new Role { Id = Guid.NewGuid(), Name = "viewer", Description = "Read-only access", CreatedAt = DateTime.UtcNow };
            await context.Roles.AddAsync(viewerRole);
        }

        await context.SaveChangesAsync();

        // 3. Create Test Accounts
        var passwordHash = passwordHasher.Hash("Password123!");

        var leadUser = new User
        {
            Id = Guid.NewGuid(),
            Email = leadEmail,
            DisplayName = "Nguyen Van Lead (Trưởng Nhóm)",
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var memberUser = new User
        {
            Id = Guid.NewGuid(),
            Email = memberEmail,
            DisplayName = "Tran Thi Member (Thành Viên)",
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(leadUser, memberUser);
        await context.SaveChangesAsync();

        // 4. Create Demo Workspace
        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = "TaskFlow Global Core",
            Slug = "taskflow-global-core",
            Description = "Workspace chính dành cho dự án demo TaskFlow với đầy đủ các dự án, bảng Kanban và tác vụ mẫu.",
            CreatedBy = leadUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        await context.Workspaces.AddAsync(workspace);
        await context.SaveChangesAsync();

        // 5. Add Workspace Members & Roles
        await context.WorkspaceMembers.AddRangeAsync(
            new WorkspaceMember { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = leadUser.Id, JoinedAt = DateTime.UtcNow },
            new WorkspaceMember { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = memberUser.Id, JoinedAt = DateTime.UtcNow }
        );

        await context.UserRoles.AddRangeAsync(
            new UserRole { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = leadUser.Id, RoleId = ownerRole.Id, CreatedAt = DateTime.UtcNow },
            new UserRole { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = memberUser.Id, RoleId = memberRole.Id, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // 6. Create Task Labels
        var backendLabel = new Label { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, Name = "Backend", Color = "#3B82F6", CreatedAt = DateTime.UtcNow };
        var frontendLabel = new Label { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, Name = "Frontend", Color = "#10B981", CreatedAt = DateTime.UtcNow };
        var dbLabel = new Label { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, Name = "Database", Color = "#F59E0B", CreatedAt = DateTime.UtcNow };
        var securityLabel = new Label { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, Name = "Security", Color = "#EF4444", CreatedAt = DateTime.UtcNow };
        var uiLabel = new Label { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, Name = "UI/UX", Color = "#8B5CF6", CreatedAt = DateTime.UtcNow };

        await context.Labels.AddRangeAsync(backendLabel, frontendLabel, dbLabel, securityLabel, uiLabel);
        await context.SaveChangesAsync();

        // 7. Create Project 1: Core System Architecture (CSA)
        var projectCsa = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Name = "Core System Architecture",
            Key = "CSA",
            Description = "Dự án hạ tầng hệ thống backend .NET 8, cơ sở dữ liệu PostgreSQL và kiến trúc Clean Architecture.",
            LeadId = leadUser.Id,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };
        await context.Projects.AddAsync(projectCsa);
        await context.SaveChangesAsync();

        await context.ProjectMembers.AddRangeAsync(
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = projectCsa.Id, UserId = leadUser.Id, Role = "lead" },
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = projectCsa.Id, UserId = memberUser.Id, Role = "member" }
        );

        // 8. Create Kanban Board & Columns for CSA
        var boardCsa = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectCsa.Id,
            Name = "CSA Sprint Board",
            Description = "Bản đồ theo dõi tiến độ sprint dự án Core System Architecture",
            CreatedAt = DateTime.UtcNow
        };
        await context.Boards.AddAsync(boardCsa);
        await context.SaveChangesAsync();

        var colToDo = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardCsa.Id, Name = "To Do", Position = 0, Color = "#6B7280", IsDoneColumn = false };
        var colInProgress = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardCsa.Id, Name = "In Progress", Position = 1, Color = "#3B82F6", WipLimit = 5, IsDoneColumn = false };
        var colReview = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardCsa.Id, Name = "Code Review", Position = 2, Color = "#F59E0B", WipLimit = 3, IsDoneColumn = false };
        var colDone = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardCsa.Id, Name = "Done", Position = 3, Color = "#10B981", IsDoneColumn = true };

        await context.BoardColumns.AddRangeAsync(colToDo, colInProgress, colReview, colDone);
        await context.SaveChangesAsync();

        // 9. Create Sprint for CSA
        var activeSprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = projectCsa.Id,
            Name = "Sprint 1 - Core & Auth Integration",
            Goal = "Hoàn thiện hạ tầng xác thực JWT, Refresh Token và giao diện Kanban Board core.",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9)),
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        await context.Sprints.AddAsync(activeSprint);
        await context.SaveChangesAsync();

        // 10. Populate Tasks in CSA (Across all columns: To Do, In Progress, Code Review, Done)
        int taskNumber = 1;

        // TO DO Tasks
        var task1 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colToDo.Id, SprintId = activeSprint.Id, ReporterId = leadUser.Id,
            Title = "Thiết kế RESTful API cho Module Notification",
            Description = "Cần định nghĩa đầy đủ DTO và endpoint cho việc truy vấn danh sách thông báo và đánh dấu đã đọc.",
            Type = "story", Priority = "high", TaskNumber = taskNumber++, Position = 0, StoryPoints = 5,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            CreatedAt = DateTime.UtcNow
        };

        var task2 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colToDo.Id, SprintId = activeSprint.Id, ReporterId = leadUser.Id,
            Title = "Tối ưu hóa query PostgreSQL cho Dashboard KPI",
            Description = "Viết query tổng hợp số lượng công việc theo trạng thái và độ ưu tiên để hiển thị trên Dashboard mà không gây nghẽn.",
            Type = "task", Priority = "medium", TaskNumber = taskNumber++, Position = 1, StoryPoints = 3,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
            CreatedAt = DateTime.UtcNow
        };

        // IN PROGRESS Tasks
        var task3 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colInProgress.Id, SprintId = activeSprint.Id, ReporterId = leadUser.Id,
            Title = "Tích hợp JWT Refresh Token rotation trên Frontend React",
            Description = "Cấu hình Axios Interceptor tự động gửi request /refresh-token khi gặp mã lỗi 401 Unauthorized.",
            Type = "task", Priority = "highest", TaskNumber = taskNumber++, Position = 0, StoryPoints = 8,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var task4 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colInProgress.Id, SprintId = activeSprint.Id, ReporterId = memberUser.Id,
            Title = "Xây dựng giao diện Drag-and-Drop cho Kanban Board",
            Description = "Sử dụng thư viện @dnd-kit để cho phép người dùng kéo thả thẻ công việc giữa các cột To Do, In Progress, Review, Done.",
            Type = "story", Priority = "high", TaskNumber = taskNumber++, Position = 1, StoryPoints = 5,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        // CODE REVIEW Tasks
        var task5 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colReview.Id, SprintId = activeSprint.Id, ReporterId = leadUser.Id,
            Title = "Cấu hình Docker Compose cho PostgreSQL & Backend .NET 8",
            Description = "Đã viết Dockerfile và docker-compose.yml khởi chạy nhanh PostgreSQL database và Web API.",
            Type = "task", Priority = "high", TaskNumber = taskNumber++, Position = 0, StoryPoints = 3,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var task6 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colReview.Id, SprintId = activeSprint.Id, ReporterId = memberUser.Id,
            Title = "Thêm validation giới hạn dung lượng file đính kèm 10MB",
            Description = "Bổ sung FluentValidation cho endpoint upload file, đảm bảo từ chối các file lớn hơn 10MB hoặc sai định dạng.",
            Type = "bug", Priority = "medium", TaskNumber = taskNumber++, Position = 1, StoryPoints = 2,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        // DONE Tasks
        var task7 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colDone.Id, SprintId = activeSprint.Id, ReporterId = leadUser.Id,
            Title = "Khởi tạo Clean Architecture Solution cho .NET 8",
            Description = "Phân chia đầy đủ 4 tầng API, Application, Domain, Infrastructure theo chuẩn Clean Architecture.",
            Type = "epic", Priority = "highest", TaskNumber = taskNumber++, Position = 0, StoryPoints = 13,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            CompletedAt = DateTime.UtcNow.AddDays(-5), CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var task8 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colDone.Id, SprintId = activeSprint.Id, ReporterId = leadUser.Id,
            Title = "Thiết lập cơ sở dữ liệu PostgreSQL schema & Enum types",
            Description = "Tạo 28 bảng dữ liệu chuẩn hóa 3NF và thiết lập các trigger auto-update TIMESTAMPTZ.",
            Type = "task", Priority = "high", TaskNumber = taskNumber++, Position = 1, StoryPoints = 8,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)),
            CompletedAt = DateTime.UtcNow.AddDays(-4), CreatedAt = DateTime.UtcNow.AddDays(-6)
        };

        var task9 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colDone.Id, SprintId = activeSprint.Id, ReporterId = memberUser.Id,
            Title = "Tạo trang Đăng nhập & Đăng ký với i18n đa ngôn ngữ",
            Description = "Giao diện React hiện đại hỗ trợ tiếng Việt và tiếng Anh linh hoạt.",
            Type = "story", Priority = "high", TaskNumber = taskNumber++, Position = 2, StoryPoints = 5,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            CompletedAt = DateTime.UtcNow.AddDays(-3), CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        await context.Tasks.AddRangeAsync(task1, task2, task3, task4, task5, task6, task7, task8, task9);
        await context.SaveChangesAsync();

        // 11. Assignees & Labels
        await context.TaskAssignees.AddRangeAsync(
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task1.Id, UserId = memberUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task2.Id, UserId = leadUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task3.Id, UserId = memberUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task4.Id, UserId = leadUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task5.Id, UserId = leadUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task6.Id, UserId = memberUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task7.Id, UserId = leadUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task8.Id, UserId = leadUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = task9.Id, UserId = memberUser.Id, CreatedAt = DateTime.UtcNow }
        );

        await context.TaskLabels.AddRangeAsync(
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task1.Id, LabelId = backendLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task2.Id, LabelId = dbLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task3.Id, LabelId = securityLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task3.Id, LabelId = frontendLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task4.Id, LabelId = frontendLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task4.Id, LabelId = uiLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task5.Id, LabelId = backendLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task6.Id, LabelId = backendLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task7.Id, LabelId = backendLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task8.Id, LabelId = dbLabel.Id },
            new TaskLabel { Id = Guid.NewGuid(), TaskId = task9.Id, LabelId = frontendLabel.Id }
        );

        // 12. Checklists
        var checklist3 = new Checklist { Id = Guid.NewGuid(), TaskId = task3.Id, Title = "Các bước tích hợp Refresh Token", Position = 0 };
        await context.Checklists.AddAsync(checklist3);
        await context.SaveChangesAsync();

        await context.ChecklistItems.AddRangeAsync(
            new ChecklistItem { Id = Guid.NewGuid(), ChecklistId = checklist3.Id, Content = "Tạo RefreshToken entity và DbContext configuration", IsCompleted = true, Position = 0, AssigneeId = memberUser.Id },
            new ChecklistItem { Id = Guid.NewGuid(), ChecklistId = checklist3.Id, Content = "Viết endpoint AuthController /refresh-token", IsCompleted = true, Position = 1, AssigneeId = memberUser.Id },
            new ChecklistItem { Id = Guid.NewGuid(), ChecklistId = checklist3.Id, Content = "Cấu hình Axios Interceptor tự động refresh khi 401", IsCompleted = true, Position = 2, AssigneeId = memberUser.Id },
            new ChecklistItem { Id = Guid.NewGuid(), ChecklistId = checklist3.Id, Content = "Kiểm thử kịch bản token hết hạn trên trình duyệt", IsCompleted = false, Position = 3, AssigneeId = memberUser.Id }
        );

        // 13. Comments
        await context.Comments.AddRangeAsync(
            new Comment { Id = Guid.NewGuid(), TaskId = task4.Id, UserId = leadUser.Id, Body = "Tôi đã hoàn thành component KanbanColumn với @dnd-kit, nhờ bạn test giúp trên mobile nhé.", CreatedAt = DateTime.UtcNow.AddHours(-12) },
            new Comment { Id = Guid.NewGuid(), TaskId = task4.Id, UserId = memberUser.Id, Body = "Đã test thử trên Safari và Chrome Mobile, giao diện tương tác rất mượt mà!", CreatedAt = DateTime.UtcNow.AddHours(-6) },
            new Comment { Id = Guid.NewGuid(), TaskId = task5.Id, UserId = memberUser.Id, Body = "Em đã xem PR Docker Compose, các container Postgres và API khởi chạy đúng thứ tự. Approved!", CreatedAt = DateTime.UtcNow.AddHours(-3) }
        );

        // 14. Time Logs
        await context.TimeLogs.AddRangeAsync(
            new TimeLog { Id = Guid.NewGuid(), TaskId = task7.Id, UserId = leadUser.Id, Description = "Khởi tạo cấu trúc Clean Architecture và thiết lập Web API", StartedAt = DateTime.UtcNow.AddDays(-6), EndedAt = DateTime.UtcNow.AddDays(-6).AddHours(4), DurationMinutes = 240, CreatedAt = DateTime.UtcNow.AddDays(-6) },
            new TimeLog { Id = Guid.NewGuid(), TaskId = task9.Id, UserId = memberUser.Id, Description = "Phát triển giao diện React Login/Register và tích hợp i18n", StartedAt = DateTime.UtcNow.AddDays(-4), EndedAt = DateTime.UtcNow.AddDays(-4).AddHours(3), DurationMinutes = 180, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new TimeLog { Id = Guid.NewGuid(), TaskId = task3.Id, UserId = memberUser.Id, Description = "Tích hợp Axios Interceptors tự động refresh token", StartedAt = DateTime.UtcNow.AddDays(-1), EndedAt = DateTime.UtcNow.AddDays(-1).AddHours(2), DurationMinutes = 120, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );

        // 15. Project 2: Mobile App & UI Revamp (MAU)
        var projectMau = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Name = "Mobile App & UI Revamp",
            Key = "MAU",
            Description = "Dự án thiết kế lại giao diện người dùng theo xu hướng Modern UI và tối ưu ứng dụng di động.",
            LeadId = leadUser.Id,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };
        await context.Projects.AddAsync(projectMau);
        await context.SaveChangesAsync();

        await context.ProjectMembers.AddRangeAsync(
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = projectMau.Id, UserId = leadUser.Id, Role = "lead" },
            new ProjectMember { Id = Guid.NewGuid(), ProjectId = projectMau.Id, UserId = memberUser.Id, Role = "member" }
        );

        var boardMau = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectMau.Id,
            Name = "MAU Agile Board",
            Description = "Bảng quản lý tiến độ UI/UX",
            CreatedAt = DateTime.UtcNow
        };
        await context.Boards.AddAsync(boardMau);
        await context.SaveChangesAsync();

        var colMauToDo = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardMau.Id, Name = "To Do", Position = 0, Color = "#6B7280", IsDoneColumn = false };
        var colMauInProg = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardMau.Id, Name = "In Progress", Position = 1, Color = "#3B82F6", IsDoneColumn = false };
        var colMauReview = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardMau.Id, Name = "Code Review", Position = 2, Color = "#F59E0B", IsDoneColumn = false };
        var colMauDone = new BoardColumn { Id = Guid.NewGuid(), BoardId = boardMau.Id, Name = "Done", Position = 3, Color = "#10B981", IsDoneColumn = true };

        await context.BoardColumns.AddRangeAsync(colMauToDo, colMauInProg, colMauReview, colMauDone);
        await context.SaveChangesAsync();

        var taskMau1 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colMauToDo.Id, ReporterId = leadUser.Id,
            Title = "Nghiên cứu Push Notification trên di động",
            Description = "Đánh giá tích hợp Firebase Cloud Messaging (FCM) cho thông báo đẩy.",
            Type = "story", Priority = "medium", TaskNumber = 1, Position = 0, StoryPoints = 3,
            CreatedAt = DateTime.UtcNow
        };

        var taskMau2 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colMauInProg.Id, ReporterId = memberUser.Id,
            Title = "Cải thiện trải nghiệm thao tác vuốt thẻ trên cảm ứng",
            Description = "Tối ưu cảm ứng cho việc kéo thả các thẻ Kanban trên màn hình điện thoại.",
            Type = "bug", Priority = "high", TaskNumber = 2, Position = 0, StoryPoints = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var taskMau3 = new TaskEntity
        {
            Id = Guid.NewGuid(), BoardColumnId = colMauDone.Id, ReporterId = leadUser.Id,
            Title = "Thiết kế giao diện Profile & Đổi mật khẩu",
            Description = "Hoàn thành trang cài đặt tài khoản người dùng.",
            Type = "story", Priority = "high", TaskNumber = 3, Position = 0, StoryPoints = 3,
            CompletedAt = DateTime.UtcNow.AddDays(-2), CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        await context.Tasks.AddRangeAsync(taskMau1, taskMau2, taskMau3);
        await context.TaskAssignees.AddRangeAsync(
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = taskMau1.Id, UserId = memberUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = taskMau2.Id, UserId = memberUser.Id, CreatedAt = DateTime.UtcNow },
            new TaskAssignee { Id = Guid.NewGuid(), TaskId = taskMau3.Id, UserId = leadUser.Id, CreatedAt = DateTime.UtcNow }
        );

        await context.SaveChangesAsync();
    }
}
