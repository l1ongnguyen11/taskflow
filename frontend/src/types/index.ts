// API Response Wrappers
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[];
}

export interface PaginationMetadata {
  totalCount: number;
  limit: number;
  offset: number;
}

export interface PaginatedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  pagination: PaginationMetadata;
}

// User & Auth
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  isActive?: boolean;
  createdAt: string;
}

export interface ValidateTokenResponse {
  userId: string;
  email: string;
  displayName: string;
}

// Workspace & Members
export interface Workspace {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  logoUrl?: string | null;
  createdBy?: string | null;
  createdAt: string;
  memberCount?: number;
}

export interface WorkspaceMember {
  userId: string;
  displayName: string;
  email: string;
  avatarUrl?: string | null;
  joinedAt: string;
  roles: string[];
}

export interface CreateWorkspaceRequest {
  name: string;
  slug: string;
  description?: string;
}

export interface InviteMemberRequest {
  email: string;
  role?: string;
}

export interface AcceptInvitationResponse {
  workspaceId: string;
  status: string;
}

export interface WorkspaceInvitation {
  id: string;
  workspaceId: string;
  workspaceName?: string;
  workspaceSlug?: string;
  email: string;
  role?: string;
  invitedBy?: string | null;
  inviterName?: string;
  token: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  updatedAt?: string;
}

// Project & Members
export interface Project {
  id: string;
  workspaceId: string;
  name: string;
  key: string;
  description?: string | null;
  leadId?: string | null;
  leadName?: string;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  key: string;
  description?: string;
  leadId?: string;
}

// Boards & Columns
export interface BoardColumn {
  id: string;
  boardId: string;
  name: string;
  position: number;
  color?: string | null;
  wipLimit?: number | null;
  isDoneColumn: boolean;
  tasks?: TaskItem[];
  createdAt: string;
  updatedAt: string;
}

export interface Board {
  id: string;
  projectId: string;
  name: string;
  description?: string | null;
  createdAt: string;
  updatedAt: string;
  columns: BoardColumn[];
}

export interface CreateBoardRequest {
  name: string;
  description?: string;
}

export interface CreateBoardColumnRequest {
  name: string;
  color?: string;
  wipLimit?: number;
  isDoneColumn?: boolean;
}

export interface UpdateBoardColumnRequest {
  name?: string;
  position?: number;
  color?: string | null;
  wipLimit?: number | null;
  isDoneColumn?: boolean;
}

// Tasks
export interface TaskUserDto {
  id: string;
  displayName: string;
  email: string;
  avatarUrl?: string | null;
}

export interface TaskDependencyDto {
  dependsOnId: string;
  title: string;
  taskNumber: number;
  type: string;
}

export interface TaskLabelDto {
  id: string;
  name: string;
  color: string;
}

export interface LabelItem {
  id: string;
  workspaceId: string;
  name: string;
  color: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLabelRequest {
  name: string;
  color: string;
}

export interface UpdateLabelRequest {
  name?: string;
  color?: string;
}

export interface ChecklistItem {
  id: string;
  checklistId: string;
  content: string;
  isCompleted: boolean;
  position: number;
  assigneeId?: string | null;
  assigneeName?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Checklist {
  id: string;
  taskId: string;
  title: string;
  position: number;
  createdAt: string;
  updatedAt: string;
  items: ChecklistItem[];
}

export interface CreateChecklistRequest {
  title: string;
}

export interface UpdateChecklistRequest {
  title?: string;
  position?: number;
}

export interface CreateChecklistItemRequest {
  content: string;
  assigneeId?: string;
}

export interface UpdateChecklistItemRequest {
  content?: string;
  isCompleted?: boolean;
  position?: number;
  assigneeId?: string;
}

export interface TaskItem {
  id: string;
  boardColumnId?: string | null;
  parentTaskId?: string | null;
  sprintId?: string | null;
  reporterId?: string | null;
  title: string;
  description?: string | null;
  type: string;
  priority: string;
  taskNumber: number;
  position: number;
  storyPoints?: number | null;
  startDate?: string | null;
  dueDate?: string | null;
  completedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  assignees: TaskUserDto[];
  watchers: TaskUserDto[];
  dependencies: TaskDependencyDto[];
  labels: TaskLabelDto[];
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  type?: string;
  priority?: string;
  storyPoints?: number;
  startDate?: string;
  dueDate?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  boardColumnId?: string;
  type?: string;
  priority?: string;
  position?: number;
  storyPoints?: number;
  dueDate?: string;
}

// Comments
export interface Comment {
  id: string;
  taskId: string;
  userId?: string | null;
  userDisplayName?: string;
  userAvatarUrl?: string | null;
  authorId?: string;
  authorName?: string;
  authorAvatarUrl?: string | null;
  body: string;
  parentCommentId?: string | null;
  createdAt: string;
  updatedAt: string;
  replies?: Comment[];
}

// Sprints
export interface Sprint {
  id: string;
  projectId: string;
  name: string;
  goal?: string | null;
  status: 'planning' | 'active' | 'completed' | 'cancelled' | string;
  startDate?: string | null;
  endDate?: string | null;
  taskCount?: number;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateSprintRequest {
  name: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
}

export interface UpdateSprintRequest {
  name?: string;
  goal?: string;
  startDate?: string;
  endDate?: string;
  status?: string;
}

export interface SprintTaskRequest {
  taskId: string;
}

// Dashboard Statistics
export interface WorkspaceStatistics {
  memberCount: number;
  projectCount: number;
  taskCount: number;
  activeSprintCount: number;
  unreadNotificationCount: number;
  openTaskCount: number;
  completedTaskCount: number;
}

export interface ProjectStatistics {
  boardCount: number;
  memberCount: number;
  taskCount: number;
  openTaskCount: number;
  completedTaskCount: number;
  sprintCount: number;
  activeSprintCount: number;
  tasksByPriority: Record<string, number>;
}

export interface TaskStatistics {
  totalTasks: number;
  openTasks: number;
  completedTasks: number;
  overdueTasks: number;
  unassignedTasks: number;
  byPriority: Record<string, number>;
  byType: Record<string, number>;
  byStatus?: Record<string, number>;
}

export interface SprintStatistics {
  totalSprints: number;
  planningSprints: number;
  activeSprints: number;
  completedSprints: number;
  cancelledSprints: number;
  totalTasksInSprints: number;
  byStatus: Record<string, number>;
}

export interface ActivitySummary {
  totalActivities: number;
  last7DaysCount: number;
  last30DaysCount: number;
  byAction: Record<string, number>;
  byEntityType: Record<string, number>;
}

export interface TimeTrackingStatistics {
  totalDurationMinutes: number;
  totalEntries: number;
  byUser: Record<string, number>;
  byTask: Record<string, number>;
}

export interface ProjectReport {
  projectId: string;
  projectName: string;
  taskStats: TaskStatistics;
  sprintStats: SprintStatistics;
  timeStats: TimeTrackingStatistics;
  activityStats: ActivitySummary;
}

// Notifications
export interface NotificationItem {
  id: string;
  userId: string;
  workspaceId?: string | null;
  type: string;
  title: string;
  body?: string | null;
  content?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  isRead: boolean;
  createdAt: string;
  updatedAt?: string;
}

// Activity
export interface ActivityItem {
  id: string;
  workspaceId: string;
  actorId?: string | null;
  actorDisplayName?: string | null;
  actorAvatarUrl?: string | null;
  entityType: string;
  entityId: string;
  action: string;
  oldValue?: string | null;
  newValue?: string | null;
  createdAt: string;
}

// Files & Attachments
export interface FileItem {
  id: string;
  originalName: string;
  mimeType: string;
  sizeBytes: number;
  uploadedBy?: string | null;
  downloadUrl?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface TaskAttachmentItem {
  id: string;
  taskId: string;
  fileId: string;
  file: FileItem;
  createdAt: string;
}

// Time Tracking & Logs
export interface TimeLogItem {
  id: string;
  taskId: string;
  userId: string;
  userDisplayName?: string | null;
  description?: string | null;
  startedAt: string;
  endedAt?: string | null;
  durationMinutes: number;
  isRunning: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface TimeReportUserSummary {
  userId: string;
  userDisplayName?: string | null;
  totalDurationMinutes: number;
  entryCount: number;
}

export interface TimeReportResponse {
  taskId: string;
  from?: string | null;
  to?: string | null;
  totalDurationMinutes: number;
  totalEntries: number;
  byUser: TimeReportUserSummary[];
  entries: TimeLogItem[];
}

export interface StartTimerRequest {
  description?: string;
}

export interface ManualLogRequest {
  startedAt: string;
  durationMinutes: number;
  description?: string;
}

