import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { useAuth } from '../../contexts/AuthContext';
import { workspaceApi, dashboardApi, sprintApi, notificationApi, getApiErrorMessage } from '../../api';
import {
  Workspace,
  WorkspaceMember,
  WorkspaceStatistics,
  Sprint,
  NotificationItem,
} from '../../types';
import { ActivityTimeline } from '../../components/common/ActivityTimeline';
import {
  Users,
  FolderKanban,
  CheckCircle2,
  Clock,
  ArrowUpRight,
  Shield,
  Settings,
  AlertTriangle,
  RefreshCw,
  Edit,
  Loader2,
  Zap,
  Bell,
  BarChart3,
  Calendar,
  PieChart,
  ListTodo,
} from 'lucide-react';
import './Dashboard.css';

interface ActiveSprintInfo {
  sprint: Sprint;
  projectKey: string;
  projectName: string;
}

export const DashboardPage: React.FC = () => {
  const { workspaceId } = useParams<{ workspaceId: string }>();
  const { currentWorkspace, selectWorkspace, projects, updateWorkspace } = useWorkspace();
  const { user } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const [workspace, setWorkspace] = useState<Workspace | null>(null);
  const [stats, setStats] = useState<WorkspaceStatistics | null>(null);
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [activeSprints, setActiveSprints] = useState<ActiveSprintInfo[]>([]);
  const [overdueTaskCount, setOverdueTaskCount] = useState<number>(0);
  const [priorityBreakdown, setPriorityBreakdown] = useState<Record<string, number>>({});
  const [typeBreakdown, setTypeBreakdown] = useState<Record<string, number>>({});

  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Edit Workspace Modal State
  const [showEditModal, setShowEditModal] = useState<boolean>(false);
  const [editName, setEditName] = useState<string>('');
  const [editDescription, setEditDescription] = useState<string>('');
  const [editLogoUrl, setEditLogoUrl] = useState<string>('');
  const [isUpdating, setIsUpdating] = useState<boolean>(false);
  const [editError, setEditError] = useState<string>('');
  const [editSuccess, setEditSuccess] = useState<string>('');

  const activeWsId = workspaceId || currentWorkspace?.id;

  const loadWorkspaceDetail = async (wsId: string) => {
    setLoading(true);
    setError(null);
    try {
      selectWorkspace(wsId);

      // Fetch Workspace Info, Statistics, Members, and Notifications in parallel
      const [wsRes, statsRes, membersRes, notifRes] = await Promise.allSettled([
        workspaceApi.getWorkspace(wsId),
        dashboardApi.getWorkspaceStats(wsId),
        workspaceApi.getMembers(wsId),
        notificationApi.getNotifications(undefined, 5),
      ]);

      if (wsRes.status === 'fulfilled' && wsRes.value.success && wsRes.value.data) {
        setWorkspace(wsRes.value.data);
      } else if (currentWorkspace && currentWorkspace.id === wsId) {
        setWorkspace(currentWorkspace);
      } else {
        setError(t('workspace.load_error'));
      }

      if (statsRes.status === 'fulfilled' && statsRes.value.success && statsRes.value.data) {
        setStats(statsRes.value.data);
      }

      if (membersRes.status === 'fulfilled' && membersRes.value.success && membersRes.value.data) {
        setMembers(membersRes.value.data);
      }

      if (notifRes.status === 'fulfilled' && notifRes.value.success && notifRes.value.data) {
        setNotifications(notifRes.value.data);
      }

      // Fetch active sprints and task stats across workspace projects
      if (projects && projects.length > 0) {
        let totalOverdue = 0;
        const priorityMap: Record<string, number> = {};
        const typeMap: Record<string, number> = {};
        const sprintList: ActiveSprintInfo[] = [];

        await Promise.allSettled(
          projects.map(async (proj) => {
            // Task statistics per project
            const tStatsRes = await dashboardApi.getTaskStats(proj.id);
            if (tStatsRes.success && tStatsRes.data) {
              totalOverdue += tStatsRes.data.overdueTasks || 0;
              if (tStatsRes.data.byPriority) {
                Object.entries(tStatsRes.data.byPriority).forEach(([k, v]) => {
                  priorityMap[k] = (priorityMap[k] || 0) + v;
                });
              }
              if (tStatsRes.data.byType) {
                Object.entries(tStatsRes.data.byType).forEach(([k, v]) => {
                  typeMap[k] = (typeMap[k] || 0) + v;
                });
              }
            }

            // Sprints per project
            const sprintRes = await sprintApi.getProjectSprints(proj.id);
            if (sprintRes.success && sprintRes.data) {
              const active = sprintRes.data.filter((s) => s.status === 'active');
              active.forEach((s) => {
                sprintList.push({
                  sprint: s,
                  projectKey: proj.key,
                  projectName: proj.name,
                });
              });
            }
          })
        );

        setOverdueTaskCount(totalOverdue);
        setPriorityBreakdown(priorityMap);
        setTypeBreakdown(typeMap);
        setActiveSprints(sprintList);
      }
    } catch (err: any) {
      console.error('Error loading workspace detail:', err);
      setError(getApiErrorMessage(err) || t('workspace.load_error'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (activeWsId) {
      loadWorkspaceDetail(activeWsId);
    } else {
      setLoading(false);
    }
  }, [activeWsId]);

  // Determine current user's role in this workspace
  const currentUserMember = members.find(
    (m) => m.userId === user?.id || m.email?.toLowerCase() === user?.email?.toLowerCase()
  );
  const isOwner = workspace?.createdBy === user?.id || currentWorkspace?.createdBy === user?.id;
  const userRoles = currentUserMember?.roles?.length
    ? currentUserMember.roles
    : isOwner
    ? [t('workspace.role_owner')]
    : [t('workspace.role_member')];

  const canEdit =
    isOwner ||
    userRoles.some(
      (r) =>
        r.toLowerCase().includes('owner') ||
        r.toLowerCase().includes('admin')
    );

  const handleRetry = () => {
    if (activeWsId) {
      loadWorkspaceDetail(activeWsId);
    }
  };

  const handleOpenEditModal = () => {
    const currentName = workspace?.name || currentWorkspace?.name || '';
    const currentDesc = workspace?.description || currentWorkspace?.description || '';
    const currentLogo = workspace?.logoUrl || currentWorkspace?.logoUrl || '';

    setEditName(currentName);
    setEditDescription(currentDesc);
    setEditLogoUrl(currentLogo);
    setEditError('');
    setEditSuccess('');
    setShowEditModal(true);
  };

  const handleUpdateWorkspaceSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeWsId) return;
    setEditError('');
    setEditSuccess('');

    if (!editName.trim()) {
      setEditError(t('modal.ws_name_required') || 'Workspace name is required.');
      return;
    }

    setIsUpdating(true);
    try {
      const updatedWs = await updateWorkspace(activeWsId, {
        name: editName.trim(),
        description: editDescription.trim() || undefined,
        logoUrl: editLogoUrl.trim() || undefined,
      });

      setWorkspace(updatedWs);
      setEditSuccess(t('workspace.update_success') || 'Workspace updated successfully!');

      setTimeout(() => {
        setIsUpdating(false);
        setShowEditModal(false);
        setEditSuccess('');
      }, 1000);
    } catch (err: any) {
      setIsUpdating(false);
      setEditError(getApiErrorMessage(err));
    }
  };

  // Total Tasks Count
  const totalTasks = stats?.taskCount ?? 0;
  const completedTasks = stats?.completedTaskCount ?? 0;
  const totalProjects = stats?.projectCount ?? projects.length ?? 0;

  return (
    <MainLayout title={`${workspace?.name || currentWorkspace?.name || t('sidebar.dashboard')}`}>
      {/* Loading State */}
      {loading ? (
        <div className="detail-loading-state">
          <div className="ws-loading-spinner" />
          <p>{t('dashboard.loading')}</p>
        </div>
      ) : error ? (
        /* Error State */
        <div className="detail-error-state card">
          <div className="detail-error-icon">
            <AlertTriangle size={40} />
          </div>
          <h3>{t('workspace.error_title')}</h3>
          <p>{error}</p>
          <div style={{ display: 'flex', gap: 12, marginTop: 8 }}>
            <button className="btn btn-secondary" onClick={() => navigate('/workspaces')}>
              {t('workspace.title')}
            </button>
            <button className="btn btn-primary" onClick={handleRetry}>
              <RefreshCw size={16} />
              {t('workspace.retry')}
            </button>
          </div>
        </div>
      ) : (
        <>
          {/* Workspace Hero / Overview Header */}
          <div className="ws-hero-card">
            <div className="ws-hero-main">
              <div className="ws-hero-identity">
                <div className="ws-hero-avatar">
                  {workspace?.logoUrl || currentWorkspace?.logoUrl ? (
                    <img
                      src={workspace?.logoUrl || currentWorkspace?.logoUrl || ''}
                      alt={workspace?.name || 'Workspace'}
                      style={{ width: '100%', height: '100%', borderRadius: 'inherit', objectFit: 'cover' }}
                    />
                  ) : (
                    (workspace?.name || currentWorkspace?.name || 'TF')
                      .substring(0, 2)
                      .toUpperCase()
                  )}
                </div>
                <div className="ws-hero-info">
                  <div className="ws-hero-title-row">
                    <h2 className="ws-hero-title">
                      {workspace?.name || currentWorkspace?.name}
                    </h2>
                    <span className="ws-hero-slug">
                      @{workspace?.slug || currentWorkspace?.slug}
                    </span>
                  </div>
                  <p className="ws-hero-desc">
                    {workspace?.description || currentWorkspace?.description || t('workspace.no_description')}
                  </p>
                </div>
              </div>

              {/* Current User Role Badge & Edit Action */}
              <div style={{ display: 'flex', alignItems: 'center', gap: 12, flexWrap: 'wrap' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <span className="stat-label">{t('workspace.your_role')}:</span>
                  <span className="badge badge-primary role-badge">
                    <Shield size={14} />
                    {userRoles.join(', ')}
                  </span>
                </div>

                {canEdit && (
                  <button className="btn btn-secondary btn-sm" onClick={handleOpenEditModal}>
                    <Edit size={15} />
                    {t('workspace.edit_btn')}
                  </button>
                )}
              </div>
            </div>

            {/* Navigation Quick Action Buttons */}
            <div className="ws-quick-actions">
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => {
                  const el = document.getElementById('projects-section');
                  el?.scrollIntoView({ behavior: 'smooth' });
                }}
              >
                <FolderKanban size={16} />
                {t('workspace.nav_projects')} ({projects.length})
              </button>

              <button
                className="btn btn-secondary btn-sm"
                onClick={() => activeWsId && navigate(`/workspaces/${activeWsId}/members`)}
              >
                <Users size={16} />
                {t('workspace.nav_members')} ({stats?.memberCount || members.length || 1})
              </button>

              <button
                className="btn btn-secondary btn-sm"
                onClick={() => navigate('/notifications')}
              >
                <Bell size={16} />
                {t('dashboard.notifications')} ({stats?.unreadNotificationCount || 0})
              </button>

              <button
                className="btn btn-secondary btn-sm"
                onClick={() => navigate('/profile')}
              >
                <Settings size={16} />
                {t('workspace.nav_settings')}
              </button>
            </div>
          </div>

          {/* 4 Core KPI Stat Cards */}
          <div className="stats-grid">
            <div className="card stat-card">
              <div className="stat-icon icon-blue">
                <FolderKanban size={22} />
              </div>
              <div className="stat-content">
                <span className="stat-label">{t('dashboard.total_projects')}</span>
                <span className="stat-value">{totalProjects}</span>
              </div>
            </div>

            <div className="card stat-card">
              <div className="stat-icon icon-purple">
                <ListTodo size={22} />
              </div>
              <div className="stat-content">
                <span className="stat-label">{t('dashboard.total_tasks')}</span>
                <span className="stat-value">{totalTasks}</span>
              </div>
            </div>

            <div className="card stat-card">
              <div className="stat-icon icon-green">
                <CheckCircle2 size={22} />
              </div>
              <div className="stat-content">
                <span className="stat-label">{t('dashboard.completed_tasks')}</span>
                <span className="stat-value">{completedTasks}</span>
              </div>
            </div>

            <div className="card stat-card">
              <div className="stat-icon icon-red">
                <AlertTriangle size={22} />
              </div>
              <div className="stat-content">
                <span className="stat-label">{t('dashboard.overdue_tasks')}</span>
                <span className="stat-value">{overdueTaskCount}</span>
              </div>
            </div>
          </div>

          {/* 2-Column Section: Active Sprints & Recent Notifications */}
          <div className="dashboard-grid-2col">
            {/* Active Sprints */}
            <div className="dashboard-section card" style={{ padding: 20 }}>
              <div className="section-header">
                <h3>
                  <Zap size={18} style={{ marginRight: 6, color: '#eab308' }} />
                  <span>{t('dashboard.active_sprint')}</span>
                </h3>
                <span className="badge badge-warning">{activeSprints.length} Active</span>
              </div>

              {activeSprints.length === 0 ? (
                <div className="dependencies-empty">
                  <Zap size={24} />
                  <span>{t('dashboard.no_active_sprint')}</span>
                </div>
              ) : (
                activeSprints.map(({ sprint, projectKey, projectName }) => (
                  <div key={sprint.id} className="sprint-active-card">
                    <div className="sprint-card-header">
                      <div className="sprint-title-row">
                        <span className="project-key-badge">{projectKey}</span>
                        <span className="sprint-title">{sprint.name}</span>
                      </div>
                      <span className="badge badge-success">ACTIVE</span>
                    </div>
                    {sprint.goal && (
                      <p style={{ fontSize: 13, color: 'var(--text-muted)', margin: 0 }}>
                        {sprint.goal}
                      </p>
                    )}
                    <div className="sprint-dates">
                      <Calendar size={13} />
                      <span>
                        {sprint.startDate ? new Date(sprint.startDate).toLocaleDateString() : 'N/A'} -{' '}
                        {sprint.endDate ? new Date(sprint.endDate).toLocaleDateString() : 'N/A'}
                      </span>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Notifications */}
            <div className="dashboard-section card" style={{ padding: 20 }}>
              <div className="section-header">
                <h3>
                  <Bell size={18} style={{ marginRight: 6, color: 'var(--primary)' }} />
                  <span>{t('dashboard.notifications')}</span>
                </h3>
                <button className="btn-link" onClick={() => navigate('/notifications')}>
                  {t('dashboard.view_all_notifications')}
                </button>
              </div>

              {notifications.length === 0 ? (
                <div className="dependencies-empty">
                  <Bell size={24} />
                  <span>{t('dashboard.no_notifications')}</span>
                </div>
              ) : (
                <div className="notification-preview-list">
                  {notifications.slice(0, 5).map((n) => (
                    <div
                      key={n.id}
                      className={`notification-preview-item ${!n.isRead ? 'unread' : ''}`}
                      onClick={() => navigate('/notifications')}
                      style={{ cursor: 'pointer' }}
                    >
                      <div className="notification-preview-content">
                        <span className="notification-preview-title">{n.title}</span>
                        {n.body && (
                          <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{n.body}</span>
                        )}
                        <span className="notification-preview-time">
                          {new Date(n.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {new Date(n.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Task Statistics Breakdown */}
          <div className="dashboard-section">
            <div className="section-header">
              <h3>
                <BarChart3 size={18} style={{ marginRight: 6, color: 'var(--primary)' }} />
                <span>{t('dashboard.task_statistics')}</span>
              </h3>
            </div>

            <div className="dashboard-grid-2col">
              {/* Priority Breakdown */}
              <div className="stat-breakdown-card">
                <h4 style={{ fontSize: 15, fontWeight: 600, marginBottom: 16 }}>
                  {t('dashboard.priority_breakdown')}
                </h4>
                {['urgent', 'high', 'medium', 'low'].map((p) => {
                  const count = priorityBreakdown[p] || priorityBreakdown[p.toLowerCase()] || 0;
                  const pct = totalTasks > 0 ? Math.round((count / totalTasks) * 100) : 0;
                  return (
                    <div key={p} className="stat-row">
                      <div className="stat-row-header">
                        <span style={{ textTransform: 'capitalize' }}>{p}</span>
                        <span>
                          {count} ({pct}%)
                        </span>
                      </div>
                      <div className="stat-bar-container">
                        <div
                          className={`stat-bar-fill bg-priority-${p}`}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Type Breakdown */}
              <div className="stat-breakdown-card">
                <h4 style={{ fontSize: 15, fontWeight: 600, marginBottom: 16 }}>
                  {t('dashboard.type_breakdown')}
                </h4>
                {['task', 'bug', 'story', 'epic'].map((tp) => {
                  const count = typeBreakdown[tp] || typeBreakdown[tp.toLowerCase()] || 0;
                  const pct = totalTasks > 0 ? Math.round((count / totalTasks) * 100) : 0;
                  return (
                    <div key={tp} className="stat-row">
                      <div className="stat-row-header">
                        <span style={{ textTransform: 'capitalize' }}>{tp}</span>
                        <span>
                          {count} ({pct}%)
                        </span>
                      </div>
                      <div className="stat-bar-container">
                        <div
                          className={`stat-bar-fill bg-type-${tp}`}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Active Projects Section */}
          <div className="dashboard-section" id="projects-section">
            <div className="section-header">
              <h3>{t('dashboard.active_projects')}</h3>
            </div>

            {projects.length === 0 ? (
              <div className="card empty-projects-card">
                <FolderKanban size={36} className="empty-icon" />
                <h4>{t('dashboard.no_projects_title')}</h4>
                <p>{t('dashboard.no_projects_desc')}</p>
              </div>
            ) : (
              <div className="projects-grid">
                {projects.map((proj) => (
                  <div
                    key={proj.id}
                    className="card card-hover project-card"
                    onClick={() => navigate(`/projects/${proj.id}/board`)}
                  >
                    <div className="project-card-header">
                      <div className="project-key-badge">{proj.key}</div>
                      <ArrowUpRight size={18} className="arrow-icon" />
                    </div>
                    <h4 className="project-title">{proj.name}</h4>
                    <p className="project-desc">{proj.description || t('board.no_description')}</p>
                    <div className="project-card-footer">
                      <span className="badge badge-primary">{t('dashboard.kanban_board')}</span>
                      <span className="project-date">
                        {new Date(proj.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Members Preview Section */}
          <div className="dashboard-section">
            <div className="section-header">
              <h3>{t('workspace.recent_members')}</h3>
              {activeWsId && (
                <button
                  className="btn-link"
                  onClick={() => navigate(`/workspaces/${activeWsId}/members`)}
                >
                  {t('workspace.view_all_members')}
                </button>
              )}
            </div>

            {members.length === 0 ? (
              <div className="card empty-projects-card">
                <Users size={36} className="empty-icon" />
                <h4>{t('workspace.no_members')}</h4>
              </div>
            ) : (
              <div className="members-preview-grid">
                {members.slice(0, 6).map((m) => (
                  <div key={m.userId} className="member-preview-card">
                    <div className="member-preview-avatar">
                      {m.displayName?.substring(0, 2).toUpperCase()}
                    </div>
                    <div className="member-preview-info">
                      <span className="member-preview-name">{m.displayName}</span>
                      <span className="member-preview-email">{m.email}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Workspace Recent Activity History Section */}
          {activeWsId && (
            <div className="dashboard-section card" style={{ padding: 24, marginTop: 24 }}>
              <div className="section-header">
                <h3>{t('dashboard.recent_activities')}</h3>
              </div>
              <ActivityTimeline
                workspaceId={activeWsId}
                showFilter={true}
                limit={10}
              />
            </div>
          )}
        </>
      )}

      {/* Edit Workspace Modal */}
      {showEditModal && (
        <div className="modal-overlay" onClick={() => !isUpdating && setShowEditModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{t('workspace.edit_title')}</h3>
              <button
                className="btn-ghost"
                onClick={() => !isUpdating && setShowEditModal(false)}
                disabled={isUpdating}
              >
                ✕
              </button>
            </div>
            <form onSubmit={handleUpdateWorkspaceSubmit}>
              <div className="modal-body">
                {editSuccess && (
                  <div className="badge badge-success" style={{ marginBottom: 12, display: 'block', padding: '8px 12px' }}>
                    {editSuccess}
                  </div>
                )}
                {editError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'block', padding: '8px 12px' }}>
                    {editError}
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_name')} *</label>
                  <input
                    type="text"
                    required
                    disabled={isUpdating}
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    placeholder={t('modal.ws_name_placeholder')}
                    className="form-input"
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_description')}</label>
                  <textarea
                    disabled={isUpdating}
                    value={editDescription}
                    onChange={(e) => setEditDescription(e.target.value)}
                    placeholder={t('modal.ws_desc_placeholder')}
                    className="form-input"
                    rows={3}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">{t('workspace.logo_url')}</label>
                  <input
                    type="url"
                    disabled={isUpdating}
                    value={editLogoUrl}
                    onChange={(e) => setEditLogoUrl(e.target.value)}
                    placeholder={t('workspace.logo_url_placeholder')}
                    className="form-input"
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowEditModal(false)}
                  disabled={isUpdating}
                >
                  {t('modal.ws_cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isUpdating}>
                  {isUpdating ? (
                    <>
                      <Loader2 size={16} className="ws-spinner-icon" style={{ animation: 'wsSpinner 0.8s linear infinite' }} />
                      {t('workspace.updating')}
                    </>
                  ) : (
                    t('workspace.update_btn')
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </MainLayout>
  );
};
