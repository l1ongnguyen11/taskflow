-- ============================================================================
-- TaskFlow — PostgreSQL Database Schema
-- Version: 1.0
-- Date: 2026-07-04
-- Description: Production-ready schema for the TaskFlow project management
--              platform. 28 tables across 8 modules.
-- ============================================================================

-- ============================================================================
-- EXTENSIONS
-- ============================================================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()

-- ============================================================================
-- ENUM TYPES
-- ============================================================================
CREATE TYPE invitation_status AS ENUM (
    'pending',
    'accepted',
    'declined',
    'expired'
);

CREATE TYPE task_priority AS ENUM (
    'lowest',
    'low',
    'medium',
    'high',
    'highest'
);

CREATE TYPE task_type AS ENUM (
    'task',
    'bug',
    'story',
    'epic',
    'subtask'
);

CREATE TYPE sprint_status AS ENUM (
    'planning',
    'active',
    'completed',
    'cancelled'
);

CREATE TYPE notification_type AS ENUM (
    'task_assigned',
    'task_commented',
    'task_updated',
    'mention',
    'invitation',
    'sprint_started',
    'sprint_completed',
    'due_date_reminder'
);

CREATE TYPE activity_action AS ENUM (
    'created',
    'updated',
    'deleted',
    'moved',
    'assigned',
    'unassigned',
    'commented',
    'attached',
    'status_changed'
);

CREATE TYPE dependency_type AS ENUM (
    'finish_to_start',
    'start_to_start',
    'finish_to_finish',
    'start_to_finish'
);


-- ============================================================================
-- MODULE 1: AUTHENTICATION
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: user
-- Purpose: Core identity for every person in the system.
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE "user" (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email           VARCHAR(255) NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    display_name    VARCHAR(100) NOT NULL,
    avatar_url      TEXT,
    is_active       BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT uq_user_email UNIQUE (email)
);

-- --------------------------------------------------------------------------
-- Table: role
-- Purpose: Named authorization roles (owner, admin, member, viewer).
-- Soft Delete: No
-- --------------------------------------------------------------------------
CREATE TABLE role (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(50) NOT NULL,
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT uq_role_name UNIQUE (name)
);

-- --------------------------------------------------------------------------
-- Table: permission
-- Purpose: Granular permission keys (e.g., 'task:create', 'project:delete').
-- Soft Delete: No
-- --------------------------------------------------------------------------
CREATE TABLE permission (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    key             VARCHAR(100) NOT NULL,
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT uq_permission_key UNIQUE (key)
);

-- --------------------------------------------------------------------------
-- Table: role_permission
-- Purpose: Bridge — which permissions a role grants.
-- Soft Delete: No (hard delete = revoke)
-- --------------------------------------------------------------------------
CREATE TABLE role_permission (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id         UUID        NOT NULL,
    permission_id   UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_role_permission_role
        FOREIGN KEY (role_id)       REFERENCES role (id)        ON DELETE CASCADE,
    CONSTRAINT fk_role_permission_permission
        FOREIGN KEY (permission_id) REFERENCES permission (id)  ON DELETE CASCADE,

    CONSTRAINT uq_role_permission UNIQUE (role_id, permission_id)
);


-- ============================================================================
-- MODULE 2: WORKSPACE
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: workspace
-- Purpose: Top-level organizational container (team/org).
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE workspace (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL,
    slug            VARCHAR(100) NOT NULL,
    description     TEXT,
    logo_url        TEXT,
    created_by      UUID,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_workspace_created_by
        FOREIGN KEY (created_by) REFERENCES "user" (id) ON DELETE SET NULL,

    CONSTRAINT uq_workspace_slug UNIQUE (slug)
);

-- --------------------------------------------------------------------------
-- Table: workspace_member
-- Purpose: Bridge — which users belong to which workspace.
-- Soft Delete: No (hard delete = user leaves)
-- --------------------------------------------------------------------------
CREATE TABLE workspace_member (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id    UUID        NOT NULL,
    user_id         UUID        NOT NULL,
    joined_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_workspace_member_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id) ON DELETE CASCADE,
    CONSTRAINT fk_workspace_member_user
        FOREIGN KEY (user_id)      REFERENCES "user" (id)    ON DELETE CASCADE,

    CONSTRAINT uq_workspace_member UNIQUE (workspace_id, user_id)
);

-- --------------------------------------------------------------------------
-- Table: user_role  (placed here because it depends on workspace)
-- Purpose: Bridge — assigns a role to a user within a specific workspace.
-- Soft Delete: No (hard delete = revoke role)
-- --------------------------------------------------------------------------
CREATE TABLE user_role (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL,
    role_id         UUID        NOT NULL,
    workspace_id    UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_role_user
        FOREIGN KEY (user_id)      REFERENCES "user" (id)     ON DELETE CASCADE,
    CONSTRAINT fk_user_role_role
        FOREIGN KEY (role_id)      REFERENCES role (id)        ON DELETE CASCADE,
    CONSTRAINT fk_user_role_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id)   ON DELETE CASCADE,

    CONSTRAINT uq_user_role UNIQUE (user_id, role_id, workspace_id)
);

-- --------------------------------------------------------------------------
-- Table: refresh_token  (placed here because it depends on user only)
-- Purpose: JWT refresh tokens for session management.
-- Soft Delete: No (hard delete on logout/expiry)
-- --------------------------------------------------------------------------
CREATE TABLE refresh_token (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID        NOT NULL,
    token_hash      VARCHAR(255) NOT NULL,
    device_info     VARCHAR(255),
    ip_address      VARCHAR(45),
    expires_at      TIMESTAMPTZ NOT NULL,
    revoked_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_refresh_token_user
        FOREIGN KEY (user_id) REFERENCES "user" (id) ON DELETE CASCADE,

    CONSTRAINT uq_refresh_token_hash UNIQUE (token_hash)
);

-- --------------------------------------------------------------------------
-- Table: invitation
-- Purpose: Pending workspace invitations sent via email.
-- Soft Delete: No (has status lifecycle)
-- --------------------------------------------------------------------------
CREATE TABLE invitation (
    id              UUID              PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id    UUID              NOT NULL,
    email           VARCHAR(255)      NOT NULL,
    invited_by      UUID,
    token           VARCHAR(255)      NOT NULL,
    status          invitation_status NOT NULL DEFAULT 'pending',
    expires_at      TIMESTAMPTZ       NOT NULL,
    created_at      TIMESTAMPTZ       NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ       NOT NULL DEFAULT now(),

    CONSTRAINT fk_invitation_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id) ON DELETE CASCADE,
    CONSTRAINT fk_invitation_invited_by
        FOREIGN KEY (invited_by)   REFERENCES "user" (id)    ON DELETE SET NULL,

    CONSTRAINT uq_invitation_token UNIQUE (token)
);


-- ============================================================================
-- MODULE 3: PROJECT
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: project
-- Purpose: A project within a workspace. Has a short key (e.g., "TF").
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE project (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id    UUID        NOT NULL,
    name            VARCHAR(100) NOT NULL,
    key             VARCHAR(10) NOT NULL,
    description     TEXT,
    lead_id         UUID,
    is_archived     BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_project_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id) ON DELETE CASCADE,
    CONSTRAINT fk_project_lead
        FOREIGN KEY (lead_id)      REFERENCES "user" (id)    ON DELETE SET NULL,

    CONSTRAINT uq_project_key UNIQUE (workspace_id, key)
);

-- --------------------------------------------------------------------------
-- Table: project_member
-- Purpose: Bridge — which users participate in a project.
-- Soft Delete: No (hard delete = removed from project)
-- --------------------------------------------------------------------------
CREATE TABLE project_member (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id      UUID        NOT NULL,
    user_id         UUID        NOT NULL,
    role            VARCHAR(50) NOT NULL DEFAULT 'member',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_project_member_project
        FOREIGN KEY (project_id) REFERENCES project (id) ON DELETE CASCADE,
    CONSTRAINT fk_project_member_user
        FOREIGN KEY (user_id)    REFERENCES "user" (id)   ON DELETE CASCADE,

    CONSTRAINT uq_project_member UNIQUE (project_id, user_id)
);


-- ============================================================================
-- MODULE 4: BOARD
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: board
-- Purpose: A Kanban board within a project.
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE board (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id      UUID        NOT NULL,
    name            VARCHAR(100) NOT NULL,
    description     TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_board_project
        FOREIGN KEY (project_id) REFERENCES project (id) ON DELETE CASCADE
);

-- --------------------------------------------------------------------------
-- Table: board_column
-- Purpose: A column within a board (e.g., "To Do", "In Progress", "Done").
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE board_column (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id        UUID        NOT NULL,
    name            VARCHAR(100) NOT NULL,
    position        INTEGER     NOT NULL,
    color           VARCHAR(7),
    wip_limit       INTEGER,
    is_done_column  BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_board_column_board
        FOREIGN KEY (board_id) REFERENCES board (id) ON DELETE CASCADE,

    CONSTRAINT uq_board_column_position UNIQUE (board_id, position)
);


-- ============================================================================
-- MODULE 5: TRACKING (Sprint defined here because Task depends on it)
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: sprint
-- Purpose: Time-boxed iteration within a project.
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE sprint (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    project_id      UUID          NOT NULL,
    name            VARCHAR(100)  NOT NULL,
    goal            TEXT,
    start_date      DATE,
    end_date        DATE,
    status          sprint_status NOT NULL DEFAULT 'planning',
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_sprint_project
        FOREIGN KEY (project_id) REFERENCES project (id) ON DELETE CASCADE
);

-- Enforce: only one active sprint per project
CREATE UNIQUE INDEX uq_sprint_active_per_project
    ON sprint (project_id)
    WHERE status = 'active' AND deleted_at IS NULL;


-- ============================================================================
-- MODULE 6: TASK
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: task
-- Purpose: The core work item — title, description, priority, type, etc.
-- Soft Delete: Yes
-- Note: project is derived via board_column → board → project (no direct FK).
--       task_number uniqueness per project is enforced at the application layer.
-- --------------------------------------------------------------------------
CREATE TABLE task (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    board_column_id UUID,
    parent_task_id  UUID,
    sprint_id       UUID,
    reporter_id     UUID,
    title           VARCHAR(500)  NOT NULL,
    description     TEXT,
    type            task_type     NOT NULL DEFAULT 'task',
    priority        task_priority NOT NULL DEFAULT 'medium',
    task_number     INTEGER       NOT NULL,
    position        INTEGER       NOT NULL DEFAULT 0,
    story_points    SMALLINT,
    start_date      DATE,
    due_date        DATE,
    completed_at    TIMESTAMPTZ,
    created_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ   NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_task_board_column
        FOREIGN KEY (board_column_id) REFERENCES board_column (id) ON DELETE SET NULL,
    CONSTRAINT fk_task_parent
        FOREIGN KEY (parent_task_id)  REFERENCES task (id)         ON DELETE CASCADE,
    CONSTRAINT fk_task_sprint
        FOREIGN KEY (sprint_id)       REFERENCES sprint (id)       ON DELETE SET NULL,
    CONSTRAINT fk_task_reporter
        FOREIGN KEY (reporter_id)     REFERENCES "user" (id)       ON DELETE SET NULL
);

-- --------------------------------------------------------------------------
-- Table: task_assignee
-- Purpose: Bridge — M:N between tasks and users (multiple assignees).
-- Soft Delete: No (hard delete = unassign)
-- --------------------------------------------------------------------------
CREATE TABLE task_assignee (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id         UUID        NOT NULL,
    user_id         UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_task_assignee_task
        FOREIGN KEY (task_id) REFERENCES task (id)   ON DELETE CASCADE,
    CONSTRAINT fk_task_assignee_user
        FOREIGN KEY (user_id) REFERENCES "user" (id) ON DELETE CASCADE,

    CONSTRAINT uq_task_assignee UNIQUE (task_id, user_id)
);

-- --------------------------------------------------------------------------
-- Table: task_watcher
-- Purpose: Users watching a task for updates without being assigned.
-- Soft Delete: No (hard delete = unwatch)
-- --------------------------------------------------------------------------
CREATE TABLE task_watcher (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id         UUID        NOT NULL,
    user_id         UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_task_watcher_task
        FOREIGN KEY (task_id) REFERENCES task (id)   ON DELETE CASCADE,
    CONSTRAINT fk_task_watcher_user
        FOREIGN KEY (user_id) REFERENCES "user" (id) ON DELETE CASCADE,

    CONSTRAINT uq_task_watcher UNIQUE (task_id, user_id)
);

-- --------------------------------------------------------------------------
-- Table: task_dependency
-- Purpose: Task-to-task dependencies for Gantt chart and workflow blocking.
-- Soft Delete: No
-- --------------------------------------------------------------------------
CREATE TABLE task_dependency (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id         UUID            NOT NULL,
    depends_on_id   UUID            NOT NULL,
    type            dependency_type NOT NULL DEFAULT 'finish_to_start',
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT fk_task_dependency_task
        FOREIGN KEY (task_id)       REFERENCES task (id) ON DELETE CASCADE,
    CONSTRAINT fk_task_dependency_depends_on
        FOREIGN KEY (depends_on_id) REFERENCES task (id) ON DELETE CASCADE,

    CONSTRAINT uq_task_dependency UNIQUE (task_id, depends_on_id),
    CONSTRAINT chk_no_self_dependency CHECK (task_id != depends_on_id)
);

-- --------------------------------------------------------------------------
-- Table: label
-- Purpose: Workspace-scoped color-coded labels for task categorization.
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE label (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id    UUID        NOT NULL,
    name            VARCHAR(50) NOT NULL,
    color           VARCHAR(7)  NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_label_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id) ON DELETE CASCADE,

    CONSTRAINT uq_label_name UNIQUE (workspace_id, name)
);

-- --------------------------------------------------------------------------
-- Table: task_label
-- Purpose: Bridge — M:N between tasks and labels.
-- Soft Delete: No (hard delete = remove label from task)
-- --------------------------------------------------------------------------
CREATE TABLE task_label (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id         UUID        NOT NULL,
    label_id        UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_task_label_task
        FOREIGN KEY (task_id)  REFERENCES task (id)  ON DELETE CASCADE,
    CONSTRAINT fk_task_label_label
        FOREIGN KEY (label_id) REFERENCES label (id) ON DELETE CASCADE,

    CONSTRAINT uq_task_label UNIQUE (task_id, label_id)
);

-- --------------------------------------------------------------------------
-- Table: checklist
-- Purpose: A named checklist container within a task.
-- Soft Delete: No (cascades from task)
-- --------------------------------------------------------------------------
CREATE TABLE checklist (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id         UUID         NOT NULL,
    title           VARCHAR(255) NOT NULL,
    position        INTEGER      NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT fk_checklist_task
        FOREIGN KEY (task_id) REFERENCES task (id) ON DELETE CASCADE
);

-- --------------------------------------------------------------------------
-- Table: checklist_item
-- Purpose: Individual items within a checklist.
-- Soft Delete: No (cascades from checklist)
-- --------------------------------------------------------------------------
CREATE TABLE checklist_item (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    checklist_id    UUID         NOT NULL,
    content         VARCHAR(500) NOT NULL,
    is_completed    BOOLEAN      NOT NULL DEFAULT FALSE,
    position        INTEGER      NOT NULL DEFAULT 0,
    assignee_id     UUID,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT fk_checklist_item_checklist
        FOREIGN KEY (checklist_id) REFERENCES checklist (id) ON DELETE CASCADE,
    CONSTRAINT fk_checklist_item_assignee
        FOREIGN KEY (assignee_id)  REFERENCES "user" (id)    ON DELETE SET NULL
);


-- ============================================================================
-- MODULE 7: FILE
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: file
-- Purpose: File storage metadata (references cloud storage like S3).
-- Soft Delete: Yes (grace period before permanent deletion from storage)
-- --------------------------------------------------------------------------
CREATE TABLE file (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    original_name   VARCHAR(255) NOT NULL,
    stored_path     TEXT         NOT NULL,
    mime_type       VARCHAR(100) NOT NULL,
    size_bytes      BIGINT       NOT NULL,
    uploaded_by     UUID,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ,

    CONSTRAINT fk_file_uploaded_by
        FOREIGN KEY (uploaded_by) REFERENCES "user" (id) ON DELETE SET NULL
);

-- --------------------------------------------------------------------------
-- Table: task_attachment
-- Purpose: Bridge — which files are attached to which tasks.
-- Soft Delete: No (hard delete = detach file from task)
-- --------------------------------------------------------------------------
CREATE TABLE task_attachment (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id         UUID        NOT NULL,
    file_id         UUID        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_task_attachment_task
        FOREIGN KEY (task_id) REFERENCES task (id) ON DELETE CASCADE,
    CONSTRAINT fk_task_attachment_file
        FOREIGN KEY (file_id) REFERENCES file (id) ON DELETE RESTRICT,

    CONSTRAINT uq_task_attachment UNIQUE (task_id, file_id)
);


-- ============================================================================
-- MODULE 8: COMMENT
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: comment
-- Purpose: Task comments with threaded reply support.
-- Soft Delete: Yes
-- --------------------------------------------------------------------------
CREATE TABLE comment (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id             UUID        NOT NULL,
    user_id             UUID,
    parent_comment_id   UUID,
    body                TEXT        NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ,

    CONSTRAINT fk_comment_task
        FOREIGN KEY (task_id)           REFERENCES task (id)    ON DELETE CASCADE,
    CONSTRAINT fk_comment_user
        FOREIGN KEY (user_id)           REFERENCES "user" (id)  ON DELETE SET NULL,
    CONSTRAINT fk_comment_parent
        FOREIGN KEY (parent_comment_id) REFERENCES comment (id) ON DELETE CASCADE
);


-- ============================================================================
-- MODULE 9: ACTIVITY
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: activity
-- Purpose: Polymorphic audit log for all entity changes across a workspace.
-- Soft Delete: No (immutable audit log — never deleted, never modified)
-- --------------------------------------------------------------------------
CREATE TABLE activity (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id    UUID            NOT NULL,
    actor_id        UUID,
    entity_type     VARCHAR(50)     NOT NULL,
    entity_id       UUID            NOT NULL,
    action          activity_action NOT NULL,
    old_value       JSONB,
    new_value       JSONB,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT fk_activity_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id) ON DELETE CASCADE,
    CONSTRAINT fk_activity_actor
        FOREIGN KEY (actor_id)     REFERENCES "user" (id)    ON DELETE SET NULL
);


-- ============================================================================
-- MODULE 10: NOTIFICATION
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: notification
-- Purpose: User-facing notification alerts.
-- Soft Delete: No (hard delete after read or TTL expiry)
-- --------------------------------------------------------------------------
CREATE TABLE notification (
    id              UUID              PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID              NOT NULL,
    workspace_id    UUID,
    type            notification_type NOT NULL,
    title           VARCHAR(255)      NOT NULL,
    body            TEXT,
    entity_type     VARCHAR(50),
    entity_id       UUID,
    is_read         BOOLEAN           NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ       NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ       NOT NULL DEFAULT now(),

    CONSTRAINT fk_notification_user
        FOREIGN KEY (user_id)      REFERENCES "user" (id)     ON DELETE CASCADE,
    CONSTRAINT fk_notification_workspace
        FOREIGN KEY (workspace_id) REFERENCES workspace (id)   ON DELETE SET NULL
);


-- ============================================================================
-- MODULE 5 (continued): TIME LOG
-- ============================================================================

-- --------------------------------------------------------------------------
-- Table: time_log
-- Purpose: Time tracking entries linked to tasks.
-- Soft Delete: No (immutable tracking data)
-- --------------------------------------------------------------------------
CREATE TABLE time_log (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id          UUID        NOT NULL,
    user_id          UUID,
    description      TEXT,
    started_at       TIMESTAMPTZ NOT NULL,
    ended_at         TIMESTAMPTZ,
    duration_minutes INTEGER     NOT NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_time_log_task
        FOREIGN KEY (task_id) REFERENCES task (id)   ON DELETE CASCADE,
    CONSTRAINT fk_time_log_user
        FOREIGN KEY (user_id) REFERENCES "user" (id) ON DELETE SET NULL,

    CONSTRAINT chk_duration_positive CHECK (duration_minutes > 0)
);


-- ============================================================================
-- INDEXES
-- ============================================================================

-- =============================================
-- Tier 1: Critical (Query Hot Paths)
-- =============================================

-- "List all tasks in a column, ordered by position" — Kanban board rendering
CREATE INDEX idx_task_board_column_position
    ON task (board_column_id, position)
    WHERE deleted_at IS NULL;

-- "My tasks" — dashboard for current user
CREATE INDEX idx_task_assignee_user
    ON task_assignee (user_id);

-- "Assignees for a task" — task detail view
CREATE INDEX idx_task_assignee_task
    ON task_assignee (task_id);

-- "Comments for a task, sorted by time" — task detail view
CREATE INDEX idx_comment_task_created
    ON comment (task_id, created_at)
    WHERE deleted_at IS NULL;

-- "Unread notifications, newest first" — notification bell
CREATE INDEX idx_notification_user_unread
    ON notification (user_id, is_read, created_at DESC);

-- "Columns in a board, sorted by position" — board rendering
-- (covered by unique constraint uq_board_column_position)

-- =============================================
-- Tier 2: Important (Feature Queries)
-- =============================================

-- "Activity feed for a specific entity" — task/project detail
CREATE INDEX idx_activity_entity
    ON activity (entity_type, entity_id, created_at DESC);

-- "Workspace-wide activity feed" — workspace dashboard
CREATE INDEX idx_activity_workspace
    ON activity (workspace_id, created_at DESC);

-- "Sprint backlog" — sprint planning view
CREATE INDEX idx_task_sprint
    ON task (sprint_id)
    WHERE sprint_id IS NOT NULL AND deleted_at IS NULL;

-- "Tasks by due date" — calendar view / due date reminders
CREATE INDEX idx_task_due_date
    ON task (due_date)
    WHERE due_date IS NOT NULL AND deleted_at IS NULL;

-- "Sub-tasks of a parent" — task hierarchy
CREATE INDEX idx_task_parent
    ON task (parent_task_id)
    WHERE parent_task_id IS NOT NULL;

-- "My logged hours" — timesheet view
CREATE INDEX idx_time_log_user
    ON time_log (user_id, started_at);

-- "Time spent on a task" — task detail
CREATE INDEX idx_time_log_task
    ON time_log (task_id);

-- "Check if email already invited" — invitation dedup
CREATE INDEX idx_invitation_email_workspace
    ON invitation (email, workspace_id);

-- "All sessions for a user" — session management / revoke all
CREATE INDEX idx_refresh_token_user
    ON refresh_token (user_id);

-- =============================================
-- Tier 3: Supporting Indexes
-- =============================================

-- "My workspaces" — workspace switcher
CREATE INDEX idx_workspace_member_user
    ON workspace_member (user_id);

-- "My projects" — project list
CREATE INDEX idx_project_member_user
    ON project_member (user_id);

-- "Labels for a workspace" — label picker
CREATE INDEX idx_label_workspace
    ON label (workspace_id)
    WHERE deleted_at IS NULL;

-- "Labels on a task" — task card rendering
CREATE INDEX idx_task_label_task
    ON task_label (task_id);

-- "Tasks with a specific label" — label filter
CREATE INDEX idx_task_label_label
    ON task_label (label_id);

-- "Checklists for a task" — task detail
CREATE INDEX idx_checklist_task
    ON checklist (task_id);

-- "Watchers for a task" — notification dispatch
CREATE INDEX idx_task_watcher_task
    ON task_watcher (task_id);

-- "Tasks a user is watching" — my watched tasks
CREATE INDEX idx_task_watcher_user
    ON task_watcher (user_id);

-- "Dependencies of a task" — Gantt chart / blocking indicator
CREATE INDEX idx_task_dependency_task
    ON task_dependency (task_id);

-- "Tasks blocked by this task" — reverse dependency lookup
CREATE INDEX idx_task_dependency_depends_on
    ON task_dependency (depends_on_id);

-- "My uploaded files" — file management
CREATE INDEX idx_file_uploaded_by
    ON file (uploaded_by)
    WHERE deleted_at IS NULL;

-- "Boards in a project" — project view
CREATE INDEX idx_board_project
    ON board (project_id)
    WHERE deleted_at IS NULL;

-- "Tasks reported by me" — reporter filter
CREATE INDEX idx_task_reporter
    ON task (reporter_id)
    WHERE deleted_at IS NULL;


-- ============================================================================
-- TRIGGER: auto-update updated_at on row modification
-- ============================================================================
CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply the trigger to all tables with an updated_at column
DO $$
DECLARE
    tbl TEXT;
BEGIN
    FOR tbl IN
        SELECT table_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND column_name = 'updated_at'
    LOOP
        EXECUTE format(
'CREATE TRIGGER %I
 BEFORE UPDATE ON %I
 FOR EACH ROW
 EXECUTE FUNCTION fn_set_updated_at()',
'trg_' || tbl || '_updated_at',
tbl
);
    END LOOP;
END;
$$;


-- ============================================================================
-- SEED DATA: Default Roles & Permissions
-- ============================================================================

-- Default Roles
INSERT INTO role (id, name, description) VALUES
    (gen_random_uuid(), 'owner',  'Full control over the workspace'),
    (gen_random_uuid(), 'admin',  'Administrative access, can manage members and settings'),
    (gen_random_uuid(), 'member', 'Standard member, can create and manage tasks'),
    (gen_random_uuid(), 'viewer', 'Read-only access to workspace content');

-- Default Permissions
INSERT INTO permission (id, key, description) VALUES
    (gen_random_uuid(), 'workspace:manage',   'Create, update, and delete workspace settings'),
    (gen_random_uuid(), 'workspace:invite',   'Invite new members to the workspace'),
    (gen_random_uuid(), 'project:create',     'Create new projects'),
    (gen_random_uuid(), 'project:update',     'Update project settings'),
    (gen_random_uuid(), 'project:delete',     'Delete projects'),
    (gen_random_uuid(), 'project:read',       'View project details'),
    (gen_random_uuid(), 'board:create',       'Create new boards'),
    (gen_random_uuid(), 'board:update',       'Update board settings and columns'),
    (gen_random_uuid(), 'board:delete',       'Delete boards'),
    (gen_random_uuid(), 'board:read',         'View board details'),
    (gen_random_uuid(), 'task:create',        'Create new tasks'),
    (gen_random_uuid(), 'task:update',        'Update task details'),
    (gen_random_uuid(), 'task:delete',        'Delete tasks'),
    (gen_random_uuid(), 'task:read',          'View task details'),
    (gen_random_uuid(), 'task:assign',        'Assign users to tasks'),
    (gen_random_uuid(), 'comment:create',     'Add comments to tasks'),
    (gen_random_uuid(), 'comment:update',     'Edit own comments'),
    (gen_random_uuid(), 'comment:delete',     'Delete comments'),
    (gen_random_uuid(), 'file:upload',        'Upload files and attachments'),
    (gen_random_uuid(), 'file:delete',        'Delete files'),
    (gen_random_uuid(), 'sprint:manage',      'Create, start, and complete sprints'),
    (gen_random_uuid(), 'time_log:manage',    'Log and manage time entries'),
    (gen_random_uuid(), 'member:manage',      'Add and remove workspace members'),
    (gen_random_uuid(), 'role:manage',        'Assign and revoke roles');

-- Role-Permission Assignments (owner gets all, admin gets most, etc.)
-- NOTE: Run this after roles and permissions are inserted.
-- In production, use a migration script with fixed UUIDs or a procedural block.
DO $$
DECLARE
    v_owner_id   UUID;
    v_admin_id   UUID;
    v_member_id  UUID;
    v_viewer_id  UUID;
    v_perm       RECORD;
BEGIN
    SELECT id INTO v_owner_id  FROM role WHERE name = 'owner';
    SELECT id INTO v_admin_id  FROM role WHERE name = 'admin';
    SELECT id INTO v_member_id FROM role WHERE name = 'member';
    SELECT id INTO v_viewer_id FROM role WHERE name = 'viewer';

    -- Owner gets ALL permissions
    FOR v_perm IN SELECT id FROM permission LOOP
        INSERT INTO role_permission (role_id, permission_id)
        VALUES (v_owner_id, v_perm.id);
    END LOOP;

    -- Admin gets everything except workspace:manage and role:manage
    FOR v_perm IN SELECT id FROM permission WHERE key NOT IN ('workspace:manage', 'role:manage') LOOP
        INSERT INTO role_permission (role_id, permission_id)
        VALUES (v_admin_id, v_perm.id);
    END LOOP;

    -- Member gets create/update/read, no delete, no manage
    FOR v_perm IN SELECT id FROM permission WHERE key IN (
        'project:read', 'board:read', 'task:read',
        'task:create', 'task:update', 'task:assign',
        'comment:create', 'comment:update',
        'file:upload', 'time_log:manage'
    ) LOOP
        INSERT INTO role_permission (role_id, permission_id)
        VALUES (v_member_id, v_perm.id);
    END LOOP;

    -- Viewer gets read-only permissions
    FOR v_perm IN SELECT id FROM permission WHERE key LIKE '%:read' LOOP
        INSERT INTO role_permission (role_id, permission_id)
        VALUES (v_viewer_id, v_perm.id);
    END LOOP;
END;
$$;
