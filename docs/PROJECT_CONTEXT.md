# TaskFlow - Project Context

## 1. Project Overview

TaskFlow là hệ thống quản lý công việc (Task Management System) được phát triển theo mô hình SaaS, lấy cảm hứng từ Trello, Jira và ClickUp.

Mục tiêu của dự án là xây dựng một nền tảng quản lý công việc hiện đại hỗ trợ:

- Quản lý Workspace
- Quản lý Project
- Quản lý Board
- Quản lý Task
- Theo dõi Sprint
- Ghi nhận Time Log
- Phân quyền người dùng
- Thông báo
- Chia sẻ tài liệu
- Quản lý thành viên

Đây là dự án Portfolio chính của tác giả.

---

# 2. Technology Stack

## Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- BCrypt Password Hash
- Swagger / OpenAPI

## Database

- PostgreSQL

## ORM

- Entity Framework Core

## Authentication

- JWT Bearer Token
- BCrypt Password Hash

## IDE

- Visual Studio 2022

---

# 3. Architecture

Dự án sử dụng Clean Architecture.

```
Presentation (API)

↓

Application

↓

Domain

↓

Infrastructure

↓

PostgreSQL
```

### Solution Structure

```
TaskFlow.API

TaskFlow.Application

TaskFlow.Domain

TaskFlow.Infrastructure
```

---

# 4. Design Principles

Dự án phải tuân thủ:

- Clean Architecture
- SOLID
- DRY
- KISS
- Repository Pattern
- Dependency Injection
- Async/Await
- Nullable Reference Types

Không được:

- Viết Business Logic trong Controller
- Truy cập DbContext từ Controller
- Hard-code dữ liệu
- Duplicate code

---

# 5. Current Project Status

Đã hoàn thành:

- Clean Architecture
- PostgreSQL
- DbContext
- Entity Framework Core
- Fluent API Configuration
- 28 Domain Entities
- BaseEntity
- Dependency Injection
- UserRepository
- IUserRepository
- PasswordHasher
- IPasswordHasher
- JwtProvider
- IJwtProvider
- JWT Authentication
- EF Core Migration

Đang thực hiện:

- Authentication Module

Chưa thực hiện:

- Workspace Module
- Project Module
- Board Module
- Task Module
- Notification Module
- Sprint Module
- Time Tracking Module
- Permission Module

---

# 6. Database

Database Name

```
taskflow
```

Database Engine

```
PostgreSQL
```

---

# 7. Domain Entities

Hiện tại hệ thống có 28 Domain Entities.

- User
- Workspace
- WorkspaceMember
- Invitation
- Project
- ProjectMember
- Board
- BoardColumn
- Task
- TaskAssignee
- TaskWatcher
- TaskDependency
- Label
- TaskLabel
- Checklist
- ChecklistItem
- Comment
- File
- TaskAttachment
- Activity
- Notification
- Sprint
- TimeLog
- Role
- Permission
- UserRole
- RolePermission
- RefreshToken

Tất cả đều kế thừa BaseEntity.

---

# 8. Coding Convention

Sử dụng PascalCase.

Ví dụ:

```
UserRepository

AuthService

WorkspaceController

RegisterRequest
```

Interface

```
IUserRepository

IAuthService

IJwtProvider
```

Private Field

```
private readonly IUserRepository _userRepository;
```

Method

```
RegisterAsync()

LoginAsync()

CreateWorkspaceAsync()
```

---

# 9. Development Rules

Controller

Chỉ nhận Request và trả Response.

Không viết Business Logic.

Service

Chứa toàn bộ Business Logic.

Repository

Chỉ thao tác Database.

Domain

Chỉ chứa Entity.

Infrastructure

Triển khai Repository, Security, Database.

---

# 10. AI Coding Rules

Khi AI sinh code phải tuân thủ:

- Không tạo file trùng.
- Không sửa code đang hoạt động nếu không được yêu cầu.
- Chỉ tạo module được yêu cầu.
- Không thay đổi kiến trúc dự án.
- Không đổi namespace nếu không cần.
- Không đổi tên Entity.
- Không đổi tên Database.
- Không tạo thêm Framework mới.
- Không sử dụng thư viện ngoài nếu chưa được yêu cầu.

Nếu cần sửa file cũ phải giải thích lý do.

---

# 11. Development Workflow

Mỗi module phải thực hiện theo quy trình:

1. DTO
2. Interface
3. Repository
4. Service
5. Controller
6. Dependency Injection
7. Swagger Test
8. Build
9. Review
10. Commit Git

Không được bỏ qua bước Build.

---

# 12. Current Module

Authentication

Các chức năng cần hoàn thành:

- Register
- Login
- JWT
- BCrypt Verify
- Swagger Test

Sau khi hoàn thành mới chuyển sang Workspace Module.

---

# 13. Important Notes

Đây là dự án production-ready.

Mọi code phải:

- Compile thành công
- Không Warning
- Không Error
- Có thể mở rộng
- Có thể deploy
- Đúng Clean Architecture

Không viết code demo.

Không viết code học tập.

Code phải đủ chất lượng để đưa lên GitHub Portfolio.
