# TaskFlow

TaskFlow is a modern, full-stack collaborative project management platform designed to streamline workspace organization, project planning, Kanban board tracking, task dependencies, sprint management, time tracking, file attachments, real-time activity logging, and team notifications.

---

## 🚀 Features

- **Authentication & User Profiles**: User registration, JWT access token authentication, refresh token rotation, and profile management.
- **Workspace Management**: Workspace creation, custom slugs, role-based member management, and workspace invitations.
- **Project Management**: Multi-project support per workspace, project key prefixes, project lead assignments, and archiving/restoration.
- **Kanban Boards & Columns**: Project-based Kanban boards, customizable columns, Work-In-Progress (WIP) limits, drag-and-drop task reordering, and status synchronization.
- **Comprehensive Task Management**: Task creation, descriptions, priority levels, task types (Task, Bug, Story, Epic), start/due dates, assignees, and watchers.
- **Task Labels & Dependencies**: Workspace-scoped label tags with color pills, predecessor/successor task dependencies, and circular dependency validation.
- **Task Checklists**: Task checklists with progress tracking and item completion toggles.
- **Comments & Collaboration**: Nested/threaded comment discussions, author edit/delete permissions, and user notifications on comment updates.
- **File Attachments**: File uploads up to 10 MB with extension whitelisting, task attachment linking, and secure file downloads.
- **Sprint Management**: Agile sprint lifecycle (Planning, Active, Complete), task sprint assignment, velocity tracking, and single active sprint enforcement per project.
- **Time Tracking**: Live running timer, active timer retrieval, manual time log entries, and project time statistics.
- **Activity Timeline**: Automatic activity logging for workspace, project, task, board, sprint, comment, and member mutations.
- **Notification System**: User notifications for invitations, task assignments, project lead assignments, member changes, and comments.
- **Dashboard Statistics**: Workspace & project level KPI metric cards, priority/type breakdown charts, active sprint status, and recent activity feed.

---

## 🛠 Tech Stack

| Layer | Technology | Description |
| :--- | :--- | :--- |
| **Frontend UI** | React 19 | UI framework for interactive components |
| **Frontend Language**| TypeScript 6 | Static typing and type-safe frontend application logic |
| **Build Tool** | Vite 8 | Fast frontend development server and production bundler |
| **Routing** | React Router 7 | Client-side routing and protected route handling |
| **HTTP Client** | Axios 1 | Centralized API client with JWT interceptors & error handlers |
| **Drag & Drop** | `@dnd-kit` | Accessible drag-and-drop primitives for Kanban boards |
| **Icons** | Lucide React | Modern icon set for task UI elements |
| **Backend Framework**| .NET 8 (ASP.NET Core) | High-performance Web API architecture |
| **Backend Language** | C# 12 | Enterprise object-oriented backend programming language |
| **ORM** | Entity Framework Core 8 | Object-Relational Mapper for database queries and migrations |
| **Database** | PostgreSQL | Relational database storage |
| **Database Driver** | Npgsql 8 | Entity Framework Core provider for PostgreSQL |
| **Authentication** | JWT & BCrypt | Token-based authentication and secure password hashing |
| **Validation** | FluentValidation 11 | Strongly-typed request DTO validation |

---

## 🏗 Architecture

TaskFlow backend adheres to **Clean Architecture** principles, enforcing clear separation of concerns across four projects:

```
TaskFlow/
├── backend/
│   ├── TaskFlow.API/             # Presentation Layer (Controllers, Middlewares, Program.cs)
│   ├── TaskFlow.Application/     # Application Layer (DTOs, Interfaces, Services, Validators)
│   ├── TaskFlow.Domain/          # Domain Layer (Entities, Enums, Base Classes)
│   └── TaskFlow.Infrastructure/  # Infrastructure Layer (DbContext, EF Configurations, Repositories)
└── frontend/                     # React Single Page Application (SPA)
    ├── src/
    │   ├── api/                  # Axios HTTP client and API services
    │   ├── components/           # Reusable UI components & modals
    │   ├── contexts/             # React Contexts (Auth, Workspace, Language)
    │   ├── pages/                # Screen views (Dashboard, Board, Projects, Sprints, etc.)
    │   └── types/                # TypeScript interface contracts
```

### Data Flow Pattern:
`React Frontend UI` ➔ `dashboardApi / taskApi / etc.` ➔ `Axios Client (JWT Interceptor)` ➔ `ASP.NET Core Web API Controllers` ➔ `Application Services` ➔ `EF Core Repositories` ➔ `PostgreSQL Database`

---

## 🔒 Authentication & Security

- **JWT Bearer Authentication**: Requests require a valid `Bearer <access_token>` in the `Authorization` header.
- **Refresh Token Rotation**: Refresh tokens are stored securely in PostgreSQL and rotated via `/api/auth/refresh-token`.
- **Password Security**: Password strings are hashed using `BCrypt.Net` before storage.
- **Resource Authorization**: All endpoints enforce workspace and project membership checks to prevent Unauthorized Access / IDOR.
- **CORS Protection**: Access restricted to authorized origins (`http://localhost:5173`, `http://localhost:3000`).

---

## 📋 Requirements & Prerequisites

Ensure the following tools are installed on your system before running the application:

- **Node.js**: `v18.0.0` or higher
- **npm**: `v9.0.0` or higher
- **.NET SDK**: `.NET 8.0 SDK`
- **PostgreSQL**: `v14.0` or higher (running on port `5432`)
- **Git**: Latest release

---

## 🌐 Environment Variables

### Frontend Configuration (`frontend/.env`):

```env
VITE_API_BASE_URL=http://localhost:5013
```

### Backend Configuration (`backend/TaskFlow.API/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskflow;Username=postgres;Password=your_postgres_password"
  },
  "Jwt": {
    "Issuer": "TaskFlow",
    "Audience": "TaskFlowClient",
    "SecretKey": "your_strong_jwt_secret_key_here",
    "ExpireMinutes": 60,
    "RefreshTokenExpireDays": 7
  },
  "FileStorage": {
    "MaxSizeBytes": 10485760,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".zip", ".txt"]
  }
}
```

### Production Environment Variable Overrides:

When deploying to production, sensitive credentials and domain configurations MUST be supplied via environment variables using standard ASP.NET Core double-underscore (`__`) syntax:

```env
# PostgreSQL Database Connection String
ConnectionStrings__DefaultConnection=Host=<prod-db-host>;Port=5432;Database=taskflow;Username=<prod-db-user>;Password=<prod-db-password>

# Cryptographically Strong JWT Secret Key
Jwt__SecretKey=<strong-random-production-secret-key-at-least-256-bits>

# Production Frontend CORS Allowed Origins (array syntax or single domain)
Cors__AllowedOrigins__0=https://your-frontend-domain.com
Cors__AllowedOrigins__1=https://app.yourdomain.com
```

> ⚠️ **Security Notice**: Never commit actual database passwords or production JWT secret keys to version control.

---

## ⚡ Getting Started

### 1. Database Setup

Ensure PostgreSQL server is running and create the target database:

```sql
CREATE DATABASE taskflow;
```

### 2. Backend Setup & Startup

Navigate to the `backend` directory, restore dependencies, and start the .NET API server:

```bash
cd backend
dotnet restore
dotnet run --project TaskFlow.API
```

The Web API server will start at `http://localhost:5013`.

### 3. Frontend Setup & Startup

Open a new terminal window, navigate to the `frontend` directory, install dependencies, and start the Vite development server:

```bash
cd frontend
npm install
npm run dev
```

The React SPA will launch at `http://localhost:5173`.

---

## 📦 Production Build

### Frontend Production Build:
```bash
cd frontend
npm run build
```
Generates minified static bundle output in `frontend/dist/`.

### Backend Build:
```bash
cd backend
dotnet build -c Release
```
Compiles the backend solution into production-ready binaries.

---

## 📁 File Attachment Rules

- **Maximum File Size**: 10 MB (`10,485,760` bytes)
- **Allowed Extensions**: `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.zip`, `.txt`
- **File Storage**: Uploaded files are assigned server-side GUID filenames to prevent path traversal vulnerabilities.

---

## 📌 Current Limitations

- **Workspace Aggregate Time Log Metric**: Logged time totals are calculated per project; workspace-wide time aggregation across all projects is not exposed as a single dedicated endpoint.
- **Checklist Item Advanced Fields**: The backend model supports item `assigneeId` and `position`, while the frontend UI currently focuses on title management, completion toggling, and deletion.
- **Activity & Notification Trigger Architecture**: Business mutation events dispatch activities and notifications through explicit service-level triggers within application domain services rather than a global event bus.

---

## ✅ Testing & Build Status

- **Frontend TypeScript Check**: `npx tsc --noEmit` — **0 errors**
- **Frontend Production Build**: `npm run build` — **PASS**
- **Backend Solution Build**: `dotnet build` — **0 warnings, 0 errors**
- **Manual E2E Functional Verification**: **PASS** across all integrated modules

---

## 📄 License & Status

**Status**: Portfolio & Demonstration Project / Active Development  
**License**: MIT License
