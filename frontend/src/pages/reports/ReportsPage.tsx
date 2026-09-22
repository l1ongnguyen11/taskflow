import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { dashboardApi } from '../../api/dashboardApi';
import {
  ProjectReport,
  TaskStatistics,
  SprintStatistics,
  TimeTrackingStatistics,
  ActivitySummary,
} from '../../types';
import {
  BarChart3,
  CheckCircle2,
  Clock,
  Zap,
  ListTodo,
  AlertTriangle,
  FolderKanban,
  Users,
  Activity,
  TrendingUp,
  PieChart,
  RefreshCw,
  FileSpreadsheet,
} from 'lucide-react';
import './Reports.css';

export const ReportsPage: React.FC = () => {
  const { workspaceId, projectId } = useParams<{ workspaceId?: string; projectId?: string }>();
  const { currentWorkspace, projects, selectWorkspace } = useWorkspace();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const activeWsId = workspaceId || currentWorkspace?.id;
  const [selectedProjectId, setSelectedProjectId] = useState<string>(projectId || '');

  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Statistics State
  const [reportData, setReportData] = useState<ProjectReport | null>(null);
  const [taskStats, setTaskStats] = useState<TaskStatistics | null>(null);
  const [sprintStats, setSprintStats] = useState<SprintStatistics | null>(null);
  const [timeStats, setTimeStats] = useState<TimeTrackingStatistics | null>(null);
  const [activityStats, setActivityStats] = useState<ActivitySummary | null>(null);

  const fetchReportsData = async () => {
    if (!activeWsId) return;
    setLoading(true);
    setError(null);

    try {
      selectWorkspace(activeWsId);

      if (selectedProjectId) {
        // Fetch specific project report
        const res = await dashboardApi.getProjectReport(selectedProjectId);
        if (res.success && res.data) {
          setReportData(res.data);
          setTaskStats(res.data.taskStats);
          setSprintStats(res.data.sprintStats);
          setTimeStats(res.data.timeStats);
          setActivityStats(res.data.activityStats);
        } else {
          setError(res.message || t('reports.no_data'));
        }
      } else if (projects && projects.length > 0) {
        // Aggregate statistics across all projects in workspace
        let aggregatedTotalTasks = 0;
        let aggregatedOpenTasks = 0;
        let aggregatedCompletedTasks = 0;
        let aggregatedOverdueTasks = 0;
        let aggregatedUnassignedTasks = 0;
        const aggregatedPriority: Record<string, number> = {};
        const aggregatedStatus: Record<string, number> = {};
        const aggregatedType: Record<string, number> = {};

        let aggregatedTotalSprints = 0;
        let aggregatedActiveSprints = 0;
        let aggregatedCompletedSprints = 0;
        let aggregatedPlanningSprints = 0;
        let aggregatedTasksInSprints = 0;

        let aggregatedTotalDuration = 0;
        let aggregatedTimeEntries = 0;
        const aggregatedTimeByUser: Record<string, number> = {};
        const aggregatedTimeByTask: Record<string, number> = {};

        await Promise.allSettled(
          projects.map(async (proj) => {
            const [tRes, sRes, tmRes] = await Promise.all([
              dashboardApi.getTaskStats(proj.id),
              dashboardApi.getSprintStats(proj.id),
              dashboardApi.getTimeStats(proj.id),
            ]);

            if (tRes.success && tRes.data) {
              aggregatedTotalTasks += tRes.data.totalTasks || 0;
              aggregatedOpenTasks += tRes.data.openTasks || 0;
              aggregatedCompletedTasks += tRes.data.completedTasks || 0;
              aggregatedOverdueTasks += tRes.data.overdueTasks || 0;
              aggregatedUnassignedTasks += tRes.data.unassignedTasks || 0;

              Object.entries(tRes.data.byPriority || {}).forEach(([k, v]) => {
                aggregatedPriority[k] = (aggregatedPriority[k] || 0) + v;
              });
              Object.entries(tRes.data.byType || {}).forEach(([k, v]) => {
                aggregatedType[k] = (aggregatedType[k] || 0) + v;
              });
              Object.entries(tRes.data.byStatus || {}).forEach(([k, v]) => {
                aggregatedStatus[k] = (aggregatedStatus[k] || 0) + v;
              });
            }

            if (sRes.success && sRes.data) {
              aggregatedTotalSprints += sRes.data.totalSprints || 0;
              aggregatedActiveSprints += sRes.data.activeSprints || 0;
              aggregatedCompletedSprints += sRes.data.completedSprints || 0;
              aggregatedPlanningSprints += sRes.data.planningSprints || 0;
              aggregatedTasksInSprints += sRes.data.totalTasksInSprints || 0;
            }

            if (tmRes.success && tmRes.data) {
              aggregatedTotalDuration += tmRes.data.totalDurationMinutes || 0;
              aggregatedTimeEntries += tmRes.data.totalEntries || 0;

              Object.entries(tmRes.data.byUser || {}).forEach(([k, v]) => {
                aggregatedTimeByUser[k] = (aggregatedTimeByUser[k] || 0) + v;
              });
              Object.entries(tmRes.data.byTask || {}).forEach(([k, v]) => {
                aggregatedTimeByTask[k] = (aggregatedTimeByTask[k] || 0) + v;
              });
            }
          })
        );

        // Fetch activity summary for the workspace
        const actRes = await dashboardApi.getActivitySummary(activeWsId);

        setTaskStats({
          totalTasks: aggregatedTotalTasks,
          openTasks: aggregatedOpenTasks,
          completedTasks: aggregatedCompletedTasks,
          overdueTasks: aggregatedOverdueTasks,
          unassignedTasks: aggregatedUnassignedTasks,
          byPriority: aggregatedPriority,
          byType: aggregatedType,
          byStatus: aggregatedStatus,
        });

        setSprintStats({
          totalSprints: aggregatedTotalSprints,
          planningSprints: aggregatedPlanningSprints,
          activeSprints: aggregatedActiveSprints,
          completedSprints: aggregatedCompletedSprints,
          cancelledSprints: 0,
          totalTasksInSprints: aggregatedTasksInSprints,
          byStatus: {},
        });

        setTimeStats({
          totalDurationMinutes: aggregatedTotalDuration,
          totalEntries: aggregatedTimeEntries,
          byUser: aggregatedTimeByUser,
          byTask: aggregatedTimeByTask,
        });

        if (actRes.success && actRes.data) {
          setActivityStats(actRes.data);
        }
      } else {
        // Workspace has no projects
        setTaskStats({
          totalTasks: 0,
          openTasks: 0,
          completedTasks: 0,
          overdueTasks: 0,
          unassignedTasks: 0,
          byPriority: {},
          byType: {},
          byStatus: {},
        });
      }
    } catch (err: any) {
      console.error('Error fetching report analytics:', err);
      setError(err.message || t('reports.no_data'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (projectId) {
      setSelectedProjectId(projectId);
    }
  }, [projectId]);

  useEffect(() => {
    fetchReportsData();
  }, [activeWsId, selectedProjectId, projects.length]);

  const totalTasks = taskStats?.totalTasks ?? 0;
  const completedTasks = taskStats?.completedTasks ?? 0;
  const completionRate = totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;

  const formatHours = (minutes: number) => {
    const hrs = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hrs > 0) {
      return `${hrs} ${t('reports.hours') || 'h'} ${mins} ${t('reports.minutes') || 'm'}`;
    }
    return `${mins} ${t('reports.minutes') || 'm'}`;
  };

  return (
    <MainLayout title={t('reports.title') || 'Reports & Analytics'}>
      <div className="reports-container">
        {/* Reports Header & Project Selector */}
        <div className="reports-header-card">
          <div className="reports-header-title">
            <h2>
              <BarChart3 size={24} style={{ color: 'var(--primary)' }} />
              {t('reports.title')}
            </h2>
            <p>{t('reports.subtitle')}</p>
          </div>

          <div className="reports-project-selector">
            <select
              className="project-select-dropdown"
              value={selectedProjectId}
              onChange={(e) => setSelectedProjectId(e.target.value)}
            >
              <option value="">{t('reports.all_projects')}</option>
              {projects.map((p) => (
                <option key={p.id} value={p.id}>
                  [{p.key}] {p.name}
                </option>
              ))}
            </select>

            <button className="btn btn-secondary btn-sm" onClick={fetchReportsData} title="Refresh Data">
              <RefreshCw size={16} />
            </button>
          </div>
        </div>

        {/* Loading State */}
        {loading ? (
          <div className="detail-loading-state" style={{ minHeight: '300px' }}>
            <div className="ws-loading-spinner" />
            <p>{t('reports.loading')}</p>
          </div>
        ) : error ? (
          /* Error State */
          <div className="detail-error-state card">
            <AlertTriangle size={40} />
            <h3>{t('projects.error_title')}</h3>
            <p>{error}</p>
            <button className="btn btn-primary" onClick={fetchReportsData} style={{ marginTop: 12 }}>
              <RefreshCw size={16} />
              {t('projects.retry')}
            </button>
          </div>
        ) : (
          <>
            {/* 4 Primary Key Indicator Cards */}
            <div className="reports-summary-grid">
              <div className="report-stat-card">
                <div className="report-stat-icon icon-completion">
                  <CheckCircle2 size={24} />
                </div>
                <div className="report-stat-content">
                  <span className="report-stat-label">{t('reports.completion_rate')}</span>
                  <span className="report-stat-value">{completionRate}%</span>
                </div>
              </div>

              <div className="report-stat-card">
                <div className="report-stat-icon icon-tasks">
                  <ListTodo size={24} />
                </div>
                <div className="report-stat-content">
                  <span className="report-stat-label">{t('dashboard.total_tasks')}</span>
                  <span className="report-stat-value">{totalTasks}</span>
                </div>
              </div>

              <div className="report-stat-card">
                <div className="report-stat-icon icon-sprints">
                  <Zap size={24} />
                </div>
                <div className="report-stat-content">
                  <span className="report-stat-label">{t('sprint.title')}</span>
                  <span className="report-stat-value">{sprintStats?.totalSprints || 0}</span>
                </div>
              </div>

              <div className="report-stat-card">
                <div className="report-stat-icon icon-time">
                  <Clock size={24} />
                </div>
                <div className="report-stat-content">
                  <span className="report-stat-label">{t('reports.total_time_logged')}</span>
                  <span className="report-stat-value">
                    {formatHours(timeStats?.totalDurationMinutes || 0)}
                  </span>
                </div>
              </div>
            </div>

            {/* Section 1: Task Completion Statistics & Tasks by Status */}
            <div className="reports-two-col">
              {/* Task Completion Statistics */}
              <div className="report-card">
                <div className="report-section-title">
                  <TrendingUp size={20} style={{ color: '#10b981' }} />
                  <span>{t('reports.task_completion')}</span>
                </div>

                <div style={{ marginBottom: 20 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, fontSize: '0.9rem', fontWeight: 600 }}>
                    <span>Progress ({completedTasks} / {totalTasks} Completed)</span>
                    <span style={{ color: '#10b981' }}>{completionRate}%</span>
                  </div>
                  <div className="progress-bar-outer">
                    <div
                      className="progress-bar-fill bg-success-fill"
                      style={{ width: `${completionRate}%` }}
                    />
                  </div>
                </div>

                <div className="report-stat-list">
                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('dashboard.open_tasks')}</span>
                      <span>{taskStats?.openTasks || 0}</span>
                    </div>
                    <div className="progress-bar-outer">
                      <div
                        className="progress-bar-fill bg-primary-fill"
                        style={{
                          width: `${totalTasks > 0 ? Math.round(((taskStats?.openTasks || 0) / totalTasks) * 100) : 0}%`,
                        }}
                      />
                    </div>
                  </div>

                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('dashboard.overdue_tasks')}</span>
                      <span style={{ color: '#ef4444' }}>{taskStats?.overdueTasks || 0}</span>
                    </div>
                    <div className="progress-bar-outer">
                      <div
                        className="progress-bar-fill bg-danger-fill"
                        style={{
                          width: `${totalTasks > 0 ? Math.round(((taskStats?.overdueTasks || 0) / totalTasks) * 100) : 0}%`,
                        }}
                      />
                    </div>
                  </div>

                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('board.no_assignees')}</span>
                      <span>{taskStats?.unassignedTasks || 0}</span>
                    </div>
                    <div className="progress-bar-outer">
                      <div
                        className="progress-bar-fill bg-warning-fill"
                        style={{
                          width: `${totalTasks > 0 ? Math.round(((taskStats?.unassignedTasks || 0) / totalTasks) * 100) : 0}%`,
                        }}
                      />
                    </div>
                  </div>
                </div>
              </div>

              {/* Tasks by Status */}
              <div className="report-card">
                <div className="report-section-title">
                  <PieChart size={20} style={{ color: '#6366f1' }} />
                  <span>{t('reports.tasks_by_status')}</span>
                </div>

                {Object.keys(taskStats?.byStatus || {}).length === 0 ? (
                  <div className="dependencies-empty" style={{ padding: '30px 0' }}>
                    <PieChart size={32} />
                    <span>{t('reports.no_data')}</span>
                  </div>
                ) : (
                  <div className="report-stat-list">
                    {Object.entries(taskStats?.byStatus || {}).map(([statusName, count]) => {
                      const pct = totalTasks > 0 ? Math.round((count / totalTasks) * 100) : 0;
                      return (
                        <div key={statusName} className="report-stat-item-row">
                          <div className="report-stat-item-meta">
                            <span>{statusName}</span>
                            <span>{count} ({pct}%)</span>
                          </div>
                          <div className="progress-bar-outer">
                            <div
                              className="progress-bar-fill bg-primary-fill"
                              style={{ width: `${pct}%` }}
                            />
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>

            {/* Section 2: Tasks by Priority & Sprint Progress */}
            <div className="reports-two-col">
              {/* Tasks by Priority */}
              <div className="report-card">
                <div className="report-section-title">
                  <AlertTriangle size={20} style={{ color: '#ef4444' }} />
                  <span>{t('reports.tasks_by_priority')}</span>
                </div>

                <div className="report-stat-list">
                  {['urgent', 'high', 'medium', 'low'].map((pri) => {
                    const count = taskStats?.byPriority[pri] || taskStats?.byPriority[pri.toLowerCase()] || 0;
                    const pct = totalTasks > 0 ? Math.round((count / totalTasks) * 100) : 0;
                    return (
                      <div key={pri} className="report-stat-item-row">
                        <div className="report-stat-item-meta">
                          <span style={{ textTransform: 'capitalize' }}>{pri}</span>
                          <span>{count} ({pct}%)</span>
                        </div>
                        <div className="progress-bar-outer">
                          <div
                            className={`progress-bar-fill ${
                              pri === 'urgent'
                                ? 'bg-danger-fill'
                                : pri === 'high'
                                ? 'bg-warning-fill'
                                : pri === 'medium'
                                ? 'bg-primary-fill'
                                : 'bg-success-fill'
                            }`}
                            style={{ width: `${pct}%` }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Sprint Progress */}
              <div className="report-card">
                <div className="report-section-title">
                  <Zap size={20} style={{ color: '#f59e0b' }} />
                  <span>{t('reports.sprint_progress')}</span>
                </div>

                <div className="report-stat-list">
                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('sprint.status_active')} Sprints</span>
                      <span className="badge badge-success">{sprintStats?.activeSprints || 0}</span>
                    </div>
                  </div>

                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('sprint.status_planning')} Sprints</span>
                      <span className="badge badge-warning">{sprintStats?.planningSprints || 0}</span>
                    </div>
                  </div>

                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('sprint.status_completed')} Sprints</span>
                      <span className="badge badge-primary">{sprintStats?.completedSprints || 0}</span>
                    </div>
                  </div>

                  <div className="report-stat-item-row">
                    <div className="report-stat-item-meta">
                      <span>{t('dashboard.total_tasks')} (Sprints)</span>
                      <span style={{ fontWeight: 700 }}>{sprintStats?.totalTasksInSprints || 0}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Section 3: Time Tracking Statistics */}
            <div className="report-card">
              <div className="report-section-title">
                <Clock size={20} style={{ color: '#0ea5e9' }} />
                <span>{t('reports.time_tracking')}</span>
              </div>

              <div className="reports-two-col">
                {/* Time by Team Member */}
                <div>
                  <h4 style={{ fontSize: '0.95rem', fontWeight: 600, marginBottom: 12, color: 'var(--text-muted)' }}>
                    {t('reports.time_by_user')}
                  </h4>
                  {Object.keys(timeStats?.byUser || {}).length === 0 ? (
                    <p style={{ fontSize: '0.875rem', color: 'var(--text-muted)' }}>{t('time.no_logs')}</p>
                  ) : (
                    <div className="report-time-logs-list">
                      {Object.entries(timeStats?.byUser || {}).map(([userName, minutes]) => (
                        <div key={userName} className="report-time-item">
                          <span className="report-time-user">{userName}</span>
                          <span className="report-time-val">{formatHours(minutes)}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Time by Top Tasks */}
                <div>
                  <h4 style={{ fontSize: '0.95rem', fontWeight: 600, marginBottom: 12, color: 'var(--text-muted)' }}>
                    {t('reports.top_time_tasks')}
                  </h4>
                  {Object.keys(timeStats?.byTask || {}).length === 0 ? (
                    <p style={{ fontSize: '0.875rem', color: 'var(--text-muted)' }}>{t('time.no_logs')}</p>
                  ) : (
                    <div className="report-time-logs-list">
                      {Object.entries(timeStats?.byTask || {})
                        .slice(0, 5)
                        .map(([taskTitle, minutes]) => (
                          <div key={taskTitle} className="report-time-item">
                            <span className="report-time-user" style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', maxWidth: '70%' }}>
                              {taskTitle}
                            </span>
                            <span className="report-time-val">{formatHours(minutes)}</span>
                          </div>
                        ))}
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Section 4: Project Activity Summary */}
            {activityStats && (
              <div className="report-card">
                <div className="report-section-title">
                  <Activity size={20} style={{ color: 'var(--primary)' }} />
                  <span>{t('reports.project_activity')}</span>
                </div>

                <div className="reports-summary-grid" style={{ marginBottom: 20 }}>
                  <div className="report-time-item">
                    <span className="report-time-user">Total Logged Activities</span>
                    <span className="report-time-val" style={{ color: 'var(--text-primary)' }}>
                      {activityStats.totalActivities}
                    </span>
                  </div>

                  <div className="report-time-item">
                    <span className="report-time-user">Last 7 Days</span>
                    <span className="report-time-val" style={{ color: '#10b981' }}>
                      {activityStats.last7DaysCount}
                    </span>
                  </div>

                  <div className="report-time-item">
                    <span className="report-time-user">Last 30 Days</span>
                    <span className="report-time-val" style={{ color: '#6366f1' }}>
                      {activityStats.last30DaysCount}
                    </span>
                  </div>
                </div>

                <h4 style={{ fontSize: '0.95rem', fontWeight: 600, marginBottom: 12, color: 'var(--text-muted)' }}>
                  Activities by Action Type
                </h4>
                <div className="reports-summary-grid">
                  {Object.entries(activityStats.byAction || {}).map(([action, count]) => (
                    <div key={action} className="report-time-item">
                      <span className="report-time-user" style={{ textTransform: 'capitalize' }}>
                        {action}
                      </span>
                      <span className="report-time-val">{count}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </MainLayout>
  );
};
