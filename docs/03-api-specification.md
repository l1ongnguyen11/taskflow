# TaskFlow — Production-Ready REST API Specification

> **Version**: 1.0.0
> **Date**: 2026-07-04
> **Status**: Approved for Implementation
> **Author**: Principal Backend Architect, Microsoft Core Services Group

---

## 1. Architectural Review & Audit Notes

Prior to detailing the endpoint specifications, this section highlights critical API design reviews, compliance audits against Microsoft REST API Guidelines, security reviews, and scalability considerations.

### 1.1 REST Compliance & URL Design
*   **Plural Naming**: Standardized all collection resources to use plural nouns (e.g., `/users`, `/workspaces`, `/sprints`, `/checklist-items`).
*   **Avoid Verbs in URIs**:
    *   *Violation*: `/api/v1/invitations/accept` -> *Resolved*: `POST /api/v1/invitations/{token}/accept` (treating the acceptance action as an execution sub-resource).
    *   *Violation*: `/api/v1/sprints/{sprintId}/start` -> *Resolved*: `PATCH /api/v1/sprints/{sprintId}` with body `{"status": "active"}`.
    *   *Violation*: `/api/v1/notifications/read` -> *Resolved*: `PATCH /api/v1/notifications` with body `{"notification_ids": [...], "is_read": true}`.
    *   *Violation*: `/api/v1/columns/{columnId}/position` -> *Resolved*: `PATCH /api/v1/columns/{columnId}` with body `{"position": 1500}`.
*   **Depth Scoping Constraint**: Nested collections are restricted to 2 levels (e.g., `/projects/{id}/boards`). Deep hierarchies are flattened using query parameters to simplify routing tables and avoid URL length limits.

### 1.2 Missing Endpoints Identified and Added
To make the API production-ready, the following CRUD and session endpoints were added:
*   **Auth**: `POST /api/v1/auth/logout` (session revocation).
*   **Workspace**: `PATCH /api/v1/workspaces/{workspaceId}` and `DELETE /api/v1/workspaces/{workspaceId}`.
*   **Project**: `PATCH /api/v1/projects/{projectId}` and `DELETE /api/v1/projects/{projectId}`.
*   **Board Columns**: `PUT /api/v1/columns/{columnId}` and `DELETE /api/v1/columns/{columnId}`.
*   **Tasks**: `DELETE /api/v1/tasks/{taskId}`.
*   **Comments**: `PATCH /api/v1/comments/{commentId}` and `DELETE /api/v1/comments/{commentId}`.
*   **Sprints**: `PATCH /api/v1/sprints/{sprintId}`, `DELETE /api/v1/sprints/{sprintId}`, and `GET /api/v1/projects/{projectId}/sprints`.
*   **Time Logs**: `GET /api/v1/tasks/{taskId}/time-logs` and `DELETE /api/v1/time-logs/{timeLogId}`.

### 1.3 Security & Boundary Audits
*   **CSRF Mitigation**: Refresh Tokens stored in HTTP-Only, SameSite=Strict cookies. Access tokens remain in memory on the client.
*   **Workspace Isolation Enforcer**: Middleware intercepts all `/projects/`, `/boards/`, `/columns/`, `/tasks/`, `/comments/` endpoints, cross-checking the caller's `user_id` against `workspace_member` of the associated workspace.
*   **Rate Limiting**: Public endpoints (`/auth/login`, `/auth/register`, `/auth/refresh`) must be rate-limited at the Gateway layer to 5 requests/minute per IP address. Authenticated endpoints rate-limited to 1000 requests/minute per user.

### 1.4 Scalability & Concurrency Considerations
*   **Task Counter Concurrency**: To safely increment `task_number` without a direct `project_id` on the `task` table, the API uses a transaction block with a row-level write lock on the associated `project` row:
    ```sql
    SELECT id FROM project WHERE id = (
        SELECT project_id FROM board WHERE id = (
            SELECT board_id FROM board_column WHERE id = $1
        )
    ) FOR UPDATE;
    ```
    This sequentializes task number generation for that project, preventing duplicates under high concurrent create loads.
*   **Database Scans Prevention**: Realtime activity feeds and notification queues enforce cursor-based keyset pagination. Standard offset-based pagination is prohibited for tables exceeding 10,000 records.

---

## 2. Standardized Response Formats

### 2.1 Single Resource Response
```json
{
  "data": {
    "id": "e44d3209-645b-413c-8b83-a9d0263c9a01",
    "email": "user@example.com",
    "created_at": "2026-07-04T12:00:00Z"
  }
}
```

### 2.2 Paginated Collection Response (Offset-Based)
Used for lookup metadata (e.g. Workspaces, Projects, Boards).
```json
{
  "data": [
    {
      "id": "c1f7c10b-608b-4a57-be8e-17fa97918a55",
      "name": "Acme Workspace"
    }
  ],
  "pagination": {
    "total_count": 12,
    "limit": 20,
    "offset": 0
  }
}
```

### 2.3 Paginated Collection Response (Cursor-Based / Keyset)
Used for high-volume operational feeds (e.g. Tasks, Comments, Activities, Notifications).
```json
{
  "data": [
    {
      "id": "f2f22222-3333-4444-5555-666666666666",
      "title": "Fix REST endpoints"
    }
  ],
  "pagination": {
    "next_cursor": "eyJpZCI6ImYyZjIyMjIyLTMzMzMtNDQ0NC01NTU1LTY2NjY2NjY2NjY2NiIsImNyZWF0ZWRfYXQiOiIyMDI2LTA3LTA0VDE0OjMwOjAwWiJ9",
    "has_more": true
  }
}
```

### 2.4 Error Response Payload
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "The request entity failed validation rules.",
    "details": [
      {
        "field": "email",
        "issue": "Must be a valid email address format."
      }
    ]
  }
}
```

---

## 3. Complete Endpoint Contract Specification

---

### 3.1 Authentication

#### POST /api/v1/auth/register
*   **Description**: Registers a new user account.
*   **Auth Required**: No.
*   **Database Tables**: `user`
*   **Business Logic**: Checks if email is already in use. Hashes the password.
*   **Request Body**:
    ```json
    {
      "email": "user@example.com",
      "password": "StrongPassword123!",
      "display_name": "John Doe"
    }
    ```
*   **Response Body (201 Created)**:
    ```json
    {
      "data": {
        "id": "e44d3209-645b-413c-8b83-a9d0263c9a01",
        "email": "user@example.com",
        "display_name": "John Doe",
        "created_at": "2026-07-04T12:00:00Z"
      }
    }
    ```
*   **Status Codes**: `201 Created`, `400 Bad Request` (validation), `409 Conflict` (duplicate email).

#### POST /api/v1/auth/login
*   **Description**: Verifies credentials and sets JWT tokens.
*   **Auth Required**: No.
*   **Database Tables**: `user`, `refresh_token`
*   **Request Body**:
    ```json
    {
      "email": "user@example.com",
      "password": "StrongPassword123!"
    }
    ```
*   **Response Body (200 OK)**:
    ```json
    {
      "data": {
        "access_token": "eyJhbGciOiJIUzI1NiIsIn...",
        "token_type": "Bearer",
        "expires_in": 900
      }
    }
    ```
*   **Cookie Set**: `refresh_token=<token_hash>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Max-Age=604800`
*   **Status Codes**: `200 OK`, `401 Unauthorized` (bad credentials or inactive account).

#### POST /api/v1/auth/refresh
*   **Description**: Validates refresh token and issues a new token pair.
*   **Auth Required**: No (reads `refresh_token` cookie).
*   **Database Tables**: `refresh_token`, `user`
*   **Business Logic**: Verifies cookie token hash, deletes old token, generates new rotated tokens.
*   **Response Body (200 OK)**: Same as `/auth/login`.
*   **Status Codes**: `200 OK`, `401 Unauthorized`.

#### POST /api/v1/auth/logout
*   **Description**: Invalidates the user session.
*   **Auth Required**: Yes.
*   **Database Tables**: `refresh_token`
*   **Business Logic**: Extracts token, updates `revoked_at` in DB, clears cookie.
*   **Response Body (204 No Content)**: Empty.
*   **Status Codes**: `204 No Content`, `401 Unauthorized`.

---

### 3.2 User Profile

#### GET /api/v1/users/me
*   **Description**: Retrieves current user profile details.
*   **Auth Required**: Yes.
*   **Database Tables**: `user`
*   **Response Body (200 OK)**:
    ```json
    {
      "data": {
        "id": "e44d3209-645b-413c-8b83-a9d0263c9a01",
        "email": "user@example.com",
        "display_name": "John Doe",
        "avatar_url": "https://example.com/avatar.png",
        "created_at": "2026-07-04T12:00:00Z"
      }
    }
    ```

#### PATCH /api/v1/users/me
*   **Description**: Partially updates current user profile fields.
*   **Auth Required**: Yes.
*   **Database Tables**: `user`
*   **Request Body**:
    ```json
    {
      "display_name": "Johnny Doe",
      "avatar_url": "https://example.com/avatar2.png"
    }
    ```
*   **Response Body (200 OK)**: Updated resource entity.

---

### 3.3 Workspace

#### POST /api/v1/workspaces
*   **Description**: Creates a new workspace. Automatically creates membership and owner role record.
*   **Auth Required**: Yes.
*   **Database Tables**: `workspace`, `workspace_member`, `user_role`
*   **Request Body**:
    ```json
    {
      "name": "Acme Corp",
      "slug": "acme-corp",
      "description": "Acme global workspace"
    }
    ```
*   **Response Body (201 Created)**:
    ```json
    {
      "data": {
        "id": "w-uuid-1",
        "name": "Acme Corp",
        "slug": "acme-corp",
        "description": "Acme global workspace",
        "created_at": "2026-07-04T12:00:00Z"
      }
    }
    ```
*   **Status Codes**: `201 Created`, `409 Conflict` (slug exists).

#### GET /api/v1/workspaces
*   **Description**: Lists workspaces the authenticated user belongs to.
*   **Database Tables**: `workspace`, `workspace_member`
*   **Pagination**: Limit-Offset.
*   **Response Body (200 OK)**: Paginated collection layout.

#### PATCH /api/v1/workspaces/{workspaceId}
*   **Description**: Partially updates workspace settings.
*   **Required Permissions**: `workspace:manage`
*   **Database Tables**: `workspace`
*   **Request Body**:
    ```json
    {
      "name": "Acme Global Solutions",
      "description": "Updated global workspace description"
    }
    ```
*   **Response Body (200 OK)**: Updated workspace object.

#### DELETE /api/v1/workspaces/{workspaceId}
*   **Description**: Soft-deletes a workspace.
*   **Required Permissions**: `workspace:manage` (Owner role only)
*   **Database Tables**: `workspace`
*   **Business Logic**: Sets `deleted_at = now()`.
*   **Response Body (204 No Content)**: Empty.

---

### 3.4 Workspace Members

#### GET /api/v1/workspaces/{workspaceId}/members
*   **Description**: Lists members in a workspace.
*   **Required Permissions**: `workspace:read`
*   **Database Tables**: `workspace_member`, `user`, `user_role`
*   **Pagination**: Limit-Offset.
*   **Response Body (200 OK)**:
    ```json
    {
      "data": [
        {
          "user_id": "e44d3209-645b-413c-8b83-a9d0263c9a01",
          "display_name": "John Doe",
          "email": "user@example.com",
          "joined_at": "2026-07-04T12:00:00Z",
          "roles": ["owner"]
        }
      ]
    }
    ```

#### DELETE /api/v1/workspaces/{workspaceId}/members/{userId}
*   **Description**: Removes a member from the workspace.
*   **Required Permissions**: `member:manage`
*   **Database Tables**: `workspace_member`, `user_role`, `project_member`, `task_assignee`
*   **Business Logic**: Removes membership record, roles, and project-specific links.
*   **Response Body (204 No Content)**: Empty.

---

### 3.5 Invitations

#### POST /api/v1/workspaces/{workspaceId}/invitations
*   **Description**: Invites a user via email.
*   **Required Permissions**: `workspace:invite`
*   **Database Tables**: `invitation`
*   **Request Body**:
    ```json
    {
      "email": "invitee@example.com"
    }
    ```
*   **Response Body (201 Created)**: Created invitation metadata.

#### POST /api/v1/invitations/{token}/accept
*   **Description**: Accepts a workspace invitation.
*   **Auth Required**: Yes.
*   **Database Tables**: `invitation`, `workspace_member`, `user_role`
*   **Business Logic**: Matches user session email with invitation token, records accepted state, inserts membership.
*   **Response Body (200 OK)**:
    ```json
    {
      "data": {
        "workspace_id": "w-uuid-1",
        "status": "accepted"
      }
    }
    ```

---

### 3.6 Projects

#### POST /api/v1/workspaces/{workspaceId}/projects
*   **Description**: Creates a project.
*   **Required Permissions**: `project:create`
*   **Database Tables**: `project`, `project_member`
*   **Request Body**:
    ```json
    {
      "name": "Frontend Portal",
      "key": "PORT",
      "description": "Customer portal development",
      "lead_id": "e44d3209-645b-413c-8b83-a9d0263c9a01"
    }
    ```
*   **Response Body (201 Created)**: Created project metadata.

#### GET /api/v1/workspaces/{workspaceId}/projects
*   **Description**: Lists projects in a workspace.
*   **Required Permissions**: `project:read`
*   **Database Tables**: `project`
*   **Pagination**: Limit-Offset.

#### PATCH /api/v1/projects/{projectId}
*   **Description**: Updates project info or project lead.
*   **Required Permissions**: `project:update`
*   **Database Tables**: `project`
*   **Request Body**:
    ```json
    {
      "name": "New Portal Name",
      "lead_id": "lead-uuid-here"
    }
    ```
*   **Response Body (200 OK)**: Updated project object.

#### DELETE /api/v1/projects/{projectId}
*   **Description**: Soft-deletes a project.
*   **Required Permissions**: `project:delete` (Owner or Admin role only)
*   **Database Tables**: `project`
*   **Response Body (204 No Content)**: Empty.

---

### 3.7 Project Members

#### POST /api/v1/projects/{projectId}/members
*   **Description**: Adds an existing workspace member to a project.
*   **Required Permissions**: `project:update`
*   **Database Tables**: `project_member`, `workspace_member`
*   **Request Body**:
    ```json
    {
      "user_id": "e44d3209-645b-413c-8b83-a9d0263c9a01"
    }
    ```
*   **Response Body (200 OK)**: Project member link metadata.

#### DELETE /api/v1/projects/{projectId}/members/{userId}
*   **Description**: Removes a member from a project.
*   **Required Permissions**: `project:update`
*   **Database Tables**: `project_member`
*   **Response Body (204 No Content)**: Empty.

---

### 3.8 Boards

#### POST /api/v1/projects/{projectId}/boards
*   **Description**: Creates a Kanban board.
*   **Required Permissions**: `board:create`
*   **Database Tables**: `board`, `board_column`
*   **Request Body**:
    ```json
    {
      "name": "Design Sprint",
      "description": "UX boards"
    }
    ```
*   **Response Body (201 Created)**: Board object.

#### GET /api/v1/projects/{projectId}/boards
*   **Description**: Lists boards in a project.
*   **Required Permissions**: `board:read`
*   **Database Tables**: `board`
*   **Pagination**: Limit-Offset.

---

### 3.9 Board Columns

#### POST /api/v1/boards/{boardId}/columns
*   **Description**: Appends a column to a board.
*   **Required Permissions**: `board:update`
*   **Database Tables**: `board_column`
*   **Request Body**:
    ```json
    {
      "name": "Code Review",
      "wip_limit": 3
    }
    ```
*   **Response Body (201 Created)**: Column details.

#### PATCH /api/v1/columns/{columnId}
*   **Description**: Updates details or reorders a column.
*   **Required Permissions**: `board:update`
*   **Database Tables**: `board_column`
*   **Request Body**:
    ```json
    {
      "name": "Review Pending",
      "position": 1500,
      "wip_limit": 5
    }
    ```
*   **Response Body (200 OK)**: Updated column resource.

#### DELETE /api/v1/columns/{columnId}
*   **Description**: Soft-deletes a column.
*   **Required Permissions**: `board:update`
*   **Database Tables**: `board_column`
*   **Business Logic**: Tasks in deleted column are set to `board_column_id = NULL`.
*   **Response Body (204 No Content)**: Empty.

---

### 3.10 Tasks

#### POST /api/v1/columns/{columnId}/tasks
*   **Description**: Creates a task.
*   **Required Permissions**: `task:create`
*   **Database Tables**: `task`, `board_column`, `board`, `project`
*   **Business Logic**: Locks the target project row, increments task counter, checks WIP limits of the column, sets position.
*   **Request Body**:
    ```json
    {
      "title": "Write unit tests",
      "description": "Write database integration tests.",
      "type": "task",
      "priority": "medium",
      "story_points": 2,
      "due_date": "2026-07-15"
    }
    ```
*   **Response Body (201 Created)**: Task object with generated `task_number` and `completed_at: null`.

#### GET /api/v1/boards/{boardId}/tasks
*   **Description**: Gets tasks in a board.
*   **Required Permissions**: `task:read`
*   **Database Tables**: `task`
*   **Pagination**: Cursor-based.
*   **Filtering**: `status`, `assignee_id`, `q` (text search).

#### PATCH /api/v1/tasks/{taskId}
*   **Description**: Updates task attributes. Triggers auto-complete timestamp if moved to a column marked `is_done_column = true`.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `task`, `board_column`
*   **Request Body**:
    ```json
    {
      "title": "Updated Title",
      "board_column_id": "column-done-uuid",
      "position": 3500,
      "story_points": 5
    }
    ```
*   **Response Body (200 OK)**: Updated task object (with updated `completed_at` if set).

#### DELETE /api/v1/tasks/{taskId}
*   **Description**: Soft-deletes a task.
*   **Required Permissions**: `task:delete`
*   **Database Tables**: `task`
*   **Response Body (204 No Content)**: Empty.

---

### 3.11 Task Assignees

#### POST /api/v1/tasks/{taskId}/assignees
*   **Description**: Assigns a user to a task.
*   **Required Permissions**: `task:assign`
*   **Database Tables**: `task_assignee`, `project_member`
*   **Request Body**:
    ```json
    {
      "user_id": "e44d3209-645b-413c-8b83-a9d0263c9a01"
    }
    ```
*   **Response Body (200 OK)**: Assignment record.

#### DELETE /api/v1/tasks/{taskId}/assignees/{userId}
*   **Description**: Unassigns a user from a task.
*   **Required Permissions**: `task:assign`
*   **Database Tables**: `task_assignee`
*   **Response Body (204 No Content)**: Empty.

---

### 3.12 Task Watchers

#### POST /api/v1/tasks/{taskId}/watchers
*   **Description**: Adds a watcher to a task.
*   **Database Tables**: `task_watcher`
*   **Request Body**:
    ```json
    {
      "user_id": "e44d3209-645b-413c-8b83-a9d0263c9a01"
    }
    ```
*   **Response Body (200 OK)**: Watcher link info.

#### DELETE /api/v1/tasks/{taskId}/watchers/{userId}
*   **Description**: Removes a watcher from a task.
*   **Database Tables**: `task_watcher`
*   **Response Body (204 No Content)**: Empty.

---

### 3.13 Task Dependencies

#### POST /api/v1/tasks/{taskId}/dependencies
*   **Description**: Adds a dependency block mapping.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `task_dependency`
*   **Business Logic**: Graph validation traversal to verify no circular reference loops.
*   **Request Body**:
    ```json
    {
      "depends_on_id": "dependency-task-uuid",
      "type": "finish_to_start"
    }
    ```
*   **Response Body (201 Created)**: Dependency entry details.

#### DELETE /api/v1/tasks/{taskId}/dependencies/{dependsOnId}
*   **Description**: Removes a dependency mapping link.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `task_dependency`
*   **Response Body (204 No Content)**: Empty.

---

### 3.14 Labels

#### POST /api/v1/workspaces/{workspaceId}/labels
*   **Description**: Creates a label.
*   **Database Tables**: `label`
*   **Request Body**:
    ```json
    {
      "name": "bugfix",
      "color": "#FF00FF"
    }
    ```
*   **Response Body (201 Created)**: Label object.

#### POST /api/v1/tasks/{taskId}/labels
*   **Description**: Links a label to a task.
*   **Database Tables**: `task_label`
*   **Request Body**:
    ```json
    {
      "label_id": "label-uuid"
    }
    ```
*   **Response Body (200 OK)**: Label link mapping.

#### DELETE /api/v1/tasks/{taskId}/labels/{labelId}
*   **Description**: Unlinks a label from a task.
*   **Database Tables**: `task_label`
*   **Response Body (204 No Content)**: Empty.

---

### 3.15 Checklists

#### POST /api/v1/tasks/{taskId}/checklists
*   **Description**: Creates a checklist.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `checklist`
*   **Request Body**:
    ```json
    {
      "title": "QA steps"
    }
    ```
*   **Response Body (201 Created)**: Checklist container metadata.

#### POST /api/v1/checklists/{checklistId}/items
*   **Description**: Creates a checklist item.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `checklist_item`
*   **Request Body**:
    ```json
    {
      "content": "Verify headers match spec"
    }
    ```
*   **Response Body (201 Created)**: Checklist item details.

#### PATCH /api/v1/checklist-items/{itemId}
*   **Description**: Updates content or completion status.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `checklist_item`
*   **Request Body**:
    ```json
    {
      "content": "Updated item steps",
      "is_completed": true,
      "assignee_id": "user-uuid"
    }
    ```
*   **Response Body (200 OK)**: Updated checklist item object.

---

### 3.16 Files

#### POST /api/v1/files/upload
*   **Description**: Uploads a raw binary. Returns file UUID.
*   **Required Permissions**: `file:upload`
*   **Database Tables**: `file`
*   **Response Body (201 Created)**: Uploaded file info.

#### POST /api/v1/tasks/{taskId}/attachments
*   **Description**: Attaches a file to a task.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `task_attachment`
*   **Request Body**:
    ```json
    {
      "file_id": "file-uuid"
    }
    ```
*   **Response Body (200 OK)**: Attachment record.

#### DELETE /api/v1/tasks/{taskId}/attachments/{attachmentId}
*   **Description**: Detaches file from a task.
*   **Required Permissions**: `task:update`
*   **Database Tables**: `task_attachment`
*   **Response Body (204 No Content)**: Empty.

---

### 3.17 Comments

#### POST /api/v1/tasks/{taskId}/comments
*   **Description**: Adds a comment.
*   **Required Permissions**: `comment:create`
*   **Database Tables**: `comment`
*   **Request Body**:
    ```json
    {
      "body": "Comment text content",
      "parent_comment_id": "optional-parent-uuid"
    }
    ```
*   **Response Body (201 Created)**: Comment details.

#### PATCH /api/v1/comments/{commentId}
*   **Description**: Edits comment text.
*   **Required Permissions**: `comment:update` (Author only)
*   **Database Tables**: `comment`
*   **Request Body**:
    ```json
    {
      "body": "Edited comment text content"
    }
    ```
*   **Response Body (200 OK)**: Updated comment metadata.

#### DELETE /api/v1/comments/{commentId}
*   **Description**: Soft-deletes a comment.
*   **Required Permissions**: `comment:delete` (Author, Admin, or Owner only)
*   **Database Tables**: `comment`
*   **Response Body (204 No Content)**: Empty.

---

### 3.18 Activities

#### GET /api/v1/workspaces/{workspaceId}/activities
*   **Description**: Retrieves audit activity log feed for a workspace.
*   **Required Permissions**: `workspace:read`
*   **Database Tables**: `activity`
*   **Pagination**: Cursor-based.
*   **Filtering**: `entity_type`, `actor_id`.

---

### 3.19 Notifications

#### GET /api/v1/notifications
*   **Description**: Gets alert queue for user.
*   **Database Tables**: `notification`
*   **Pagination**: Cursor-based.
*   **Filtering**: `is_read`.

#### PATCH /api/v1/notifications
*   **Description**: Bulk marks notifications as read.
*   **Database Tables**: `notification`
*   **Request Body**:
    ```json
    {
      "notification_ids": [
        "notif-uuid-1",
        "notif-uuid-2"
      ],
      "is_read": true
    }
    ```
*   **Response Body (200 OK)**: Batch updates response details.

---

### 3.20 Time Logs

#### POST /api/v1/tasks/{taskId}/time-logs
*   **Description**: Creates a time tracking entry.
*   **Required Permissions**: `time_log:manage`
*   **Database Tables**: `time_log`
*   **Request Body**:
    ```json
    {
      "description": "Development coding",
      "started_at": "2026-07-04T12:00:00Z",
      "duration_minutes": 90
    }
    ```
*   **Response Body (201 Created)**: Time log details.

#### GET /api/v1/tasks/{taskId}/time-logs
*   **Description**: Retrieves logged timesheet records for a task.
*   **Required Permissions**: `task:read`
*   **Database Tables**: `time_log`
*   **Pagination**: Cursor-based.

#### DELETE /api/v1/time-logs/{timeLogId}
*   **Description**: Deletes a time tracking log.
*   **Required Permissions**: `time_log:manage`
*   **Database Tables**: `time_log`
*   **Response Body (204 No Content)**: Empty.

---

### 3.21 Sprints

#### POST /api/v1/projects/{projectId}/sprints
*   **Description**: Creates a sprint.
*   **Required Permissions**: `sprint:manage`
*   **Database Tables**: `sprint`
*   **Request Body**:
    ```json
    {
      "name": "Sprint 3",
      "goal": "API compliance",
      "start_date": "2026-07-10",
      "end_date": "2026-07-24"
    }
    ```
*   **Response Body (201 Created)**: Sprint metadata details.

#### GET /api/v1/projects/{projectId}/sprints
*   **Description**: Lists sprints in a project.
*   **Required Permissions**: `project:read`
*   **Database Tables**: `sprint`
*   **Pagination**: Limit-Offset.

#### PATCH /api/v1/sprints/{sprintId}
*   **Description**: Updates sprint status, dates, or settings.
*   **Required Permissions**: `sprint:manage`
*   **Database Tables**: `sprint`
*   **Business Logic**: Ensures no other active sprints exist for the project if changing `status` to `'active'`.
*   **Request Body**:
    ```json
    {
      "name": "Sprint 3 - Extended",
      "status": "active"
    }
    ```
*   **Response Body (200 OK)**: Updated sprint metadata.

#### DELETE /api/v1/sprints/{sprintId}
*   **Description**: Soft-deletes a sprint.
*   **Required Permissions**: `sprint:manage`
*   **Database Tables**: `sprint`
*   **Response Body (204 No Content)**: Empty.
