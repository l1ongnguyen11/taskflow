import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useLanguage } from '../../contexts/LanguageContext';
import { sprintApi, getApiErrorMessage } from '../../api';
import { projectApi } from '../../api/projectApi';
import { boardApi } from '../../api/boardApi';
import { taskApi } from '../../api/taskApi';
import { Sprint, Project, TaskItem, CreateSprintRequest, UpdateSprintRequest } from '../../types';
import {
  Layers,
  Plus,
  Play,
  CheckCircle,
  Pencil,
  Trash2,
  Calendar,
  CheckSquare,
  AlertCircle,
  Check,
  ChevronLeft,
  Loader2,
  X,
  ListTodo,
} from 'lucide-react';
import './Sprints.css';

export const SprintsPage: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { t, locale } = useLanguage();
  const navigate = useNavigate();

  // Project & Sprints State
  const [project, setProject] = useState<Project | null>(null);
  const [sprints, setSprints] = useState<Sprint[]>([]);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Success & Error Banners
  const [alertSuccess, setAlertSuccess] = useState<string | null>(null);
  const [alertError, setAlertError] = useState<string | null>(null);

  // Create Modal State
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [createForm, setCreateForm] = useState<CreateSprintRequest>({
    name: '',
    goal: '',
    startDate: '',
    endDate: '',
  });
  const [isCreating, setIsCreating] = useState<boolean>(false);
  const [createModalError, setCreateModalError] = useState<string | null>(null);

  // Edit Modal State
  const [showEditModal, setShowEditModal] = useState<boolean>(false);
  const [editingSprint, setEditingSprint] = useState<Sprint | null>(null);
  const [editForm, setEditForm] = useState<UpdateSprintRequest>({
    name: '',
    goal: '',
    startDate: '',
    endDate: '',
  });
  const [isEditing, setIsEditing] = useState<boolean>(false);
  const [editModalError, setEditModalError] = useState<string | null>(null);

  // Manage Tasks Modal State
  const [showTasksModal, setShowTasksModal] = useState<boolean>(false);
  const [activeSprintForTasks, setActiveSprintForTasks] = useState<Sprint | null>(null);
  const [taskSearch, setTaskSearch] = useState<string>('');
  const [isAssigningTask, setIsAssigningTask] = useState<string | null>(null);

  // 1. Fetch Project Details & Sprints & Tasks
  const fetchData = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    setError(null);
    try {
      const [projRes, sprintsRes, boardsRes] = await Promise.all([
        projectApi.getProject(projectId),
        sprintApi.getProjectSprints(projectId, 100, 0),
        boardApi.getBoards(projectId, 50, 0),
      ]);

      if (projRes.success && projRes.data) {
        setProject(projRes.data);
      }

      if (sprintsRes.success && sprintsRes.data) {
        setSprints(sprintsRes.data);
      } else {
        setError(sprintsRes.message || 'Failed to load sprints.');
      }

      if (boardsRes.success && boardsRes.data) {
        const allTasks: TaskItem[] = [];
        for (const b of boardsRes.data) {
          const tRes = await taskApi.getBoardTasks(b.id);
          if (tRes.success && tRes.data) {
            allTasks.push(...tRes.data);
          }
        }
        setTasks(allTasks);
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to load project sprints.');
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Group sprints by status
  const activeSprints = useMemo(() => sprints.filter((s) => s.status === 'active'), [sprints]);
  const planningSprints = useMemo(() => sprints.filter((s) => s.status === 'planning'), [sprints]);
  const completedSprints = useMemo(
    () => sprints.filter((s) => s.status === 'completed' || s.status === 'cancelled'),
    [sprints]
  );

  // Helper to show temporary alerts
  const notifySuccess = (msg: string) => {
    setAlertSuccess(msg);
    setTimeout(() => setAlertSuccess(null), 3500);
  };

  const notifyError = (msg: string) => {
    setAlertError(msg);
    setTimeout(() => setAlertError(null), 4000);
  };

  // --- Create Sprint Handler ---
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId || !createForm.name.trim()) return;

    setIsCreating(true);
    setCreateModalError(null);

    try {
      const payload: CreateSprintRequest = {
        name: createForm.name.trim(),
        goal: createForm.goal?.trim() || undefined,
        startDate: createForm.startDate || undefined,
        endDate: createForm.endDate || undefined,
      };

      const res = await sprintApi.createSprint(projectId, payload);
      if (res.success) {
        notifySuccess(t('sprint.create_success'));
        setShowCreateModal(false);
        setCreateForm({ name: '', goal: '', startDate: '', endDate: '' });
        await fetchData();
      } else {
        setCreateModalError(res.message || 'Failed to create sprint.');
      }
    } catch (err: any) {
      setCreateModalError(getApiErrorMessage(err) || 'Failed to create sprint.');
    } finally {
      setIsCreating(false);
    }
  };

  // --- Edit Sprint Handler ---
  const openEditModal = (sprint: Sprint) => {
    setEditingSprint(sprint);
    setEditForm({
      name: sprint.name,
      goal: sprint.goal || '',
      startDate: sprint.startDate || '',
      endDate: sprint.endDate || '',
    });
    setEditModalError(null);
    setShowEditModal(true);
  };

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingSprint || !editForm.name?.trim()) return;

    setIsEditing(true);
    setEditModalError(null);

    try {
      const payload: UpdateSprintRequest = {
        name: editForm.name.trim(),
        goal: editForm.goal?.trim() || undefined,
        startDate: editForm.startDate || undefined,
        endDate: editForm.endDate || undefined,
      };

      const res = await sprintApi.updateSprint(editingSprint.id, payload);
      if (res.success) {
        notifySuccess(t('sprint.update_success'));
        setShowEditModal(false);
        setEditingSprint(null);
        await fetchData();
      } else {
        setEditModalError(res.message || 'Failed to update sprint.');
      }
    } catch (err: any) {
      setEditModalError(getApiErrorMessage(err) || 'Failed to update sprint.');
    } finally {
      setIsEditing(false);
    }
  };

  // --- Start Sprint Handler ---
  const handleStartSprint = async (sprint: Sprint) => {
    const confirmMsg = t('sprint.start_confirm').replace('{name}', sprint.name);
    if (!window.confirm(confirmMsg)) return;

    try {
      const res = await sprintApi.startSprint(sprint.id);
      if (res.success) {
        notifySuccess(t('sprint.start_success'));
        await fetchData();
      } else {
        notifyError(res.message || 'Failed to start sprint.');
      }
    } catch (err: any) {
      notifyError(getApiErrorMessage(err) || 'Failed to start sprint.');
    }
  };

  // --- Complete Sprint Handler ---
  const handleCompleteSprint = async (sprint: Sprint) => {
    const confirmMsg = t('sprint.complete_confirm').replace('{name}', sprint.name);
    if (!window.confirm(confirmMsg)) return;

    try {
      const res = await sprintApi.completeSprint(sprint.id);
      if (res.success) {
        notifySuccess(t('sprint.complete_success'));
        await fetchData();
      } else {
        notifyError(res.message || 'Failed to complete sprint.');
      }
    } catch (err: any) {
      notifyError(getApiErrorMessage(err) || 'Failed to complete sprint.');
    }
  };

  // --- Delete Sprint Handler ---
  const handleDeleteSprint = async (sprint: Sprint) => {
    const confirmMsg = t('sprint.delete_confirm').replace('{name}', sprint.name);
    if (!window.confirm(confirmMsg)) return;

    try {
      const res = await sprintApi.deleteSprint(sprint.id);
      if (res.success) {
        notifySuccess(t('sprint.delete_success'));
        await fetchData();
      } else {
        notifyError(res.message || 'Failed to delete sprint.');
      }
    } catch (err: any) {
      notifyError(getApiErrorMessage(err) || 'Failed to delete sprint.');
    }
  };

  // --- Manage Tasks Modal Handlers ---
  const openTasksModal = (sprint: Sprint) => {
    setActiveSprintForTasks(sprint);
    setTaskSearch('');
    setShowTasksModal(true);
  };

  const handleToggleTaskSprint = async (task: TaskItem, sprintId: string) => {
    setIsAssigningTask(task.id);
    try {
      if (task.sprintId === sprintId) {
        // Remove from sprint
        const res = await sprintApi.removeTaskFromSprint(sprintId, task.id);
        if (res.success) {
          notifySuccess(t('sprint.task_removed'));
          await fetchData();
        } else {
          notifyError(res.message || 'Failed to remove task from sprint.');
        }
      } else {
        // Add to sprint
        const res = await sprintApi.addTaskToSprint(sprintId, task.id);
        if (res.success) {
          notifySuccess(t('sprint.task_added'));
          await fetchData();
        } else {
          notifyError(res.message || 'Failed to assign task to sprint.');
        }
      }
    } catch (err: any) {
      notifyError(getApiErrorMessage(err) || 'Failed to update task sprint.');
    } finally {
      setIsAssigningTask(null);
    }
  };

  const formatDate = (dateStr?: string | null) => {
    if (!dateStr) return 'N/A';
    return new Date(dateStr).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const renderSprintCard = (sprint: Sprint) => {
    const isAct = sprint.status === 'active';
    const isPlan = sprint.status === 'planning';
    const isComp = sprint.status === 'completed' || sprint.status === 'cancelled';

    return (
      <div key={sprint.id} className={`sprint-card ${isAct ? 'active' : ''}`}>
        <div>
          <div className="sprint-card-header">
            <div>
              <h3 className="sprint-title">{sprint.name}</h3>
              <span className={`sprint-status-badge ${sprint.status}`}>
                {t(`sprint.status_${sprint.status}`) || sprint.status}
              </span>
            </div>
          </div>

          {sprint.goal && <div className="sprint-goal">{sprint.goal}</div>}

          <div className="sprint-meta">
            <div className="sprint-meta-item">
              <Calendar size={14} />
              <span>
                {formatDate(sprint.startDate)} – {formatDate(sprint.endDate)}
              </span>
            </div>
            <div className="sprint-meta-item">
              <CheckSquare size={14} />
              <span>
                {t('sprint.tasks_count').replace('{count}', String(sprint.taskCount ?? 0))}
              </span>
            </div>
          </div>
        </div>

        <div className="sprint-card-footer">
          <div className="sprint-actions-left">
            {isPlan && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={() => handleStartSprint(sprint)}
                title={t('sprint.start_btn')}
                style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}
              >
                <Play size={13} />
                <span>{t('sprint.start_btn')}</span>
              </button>
            )}

            {isAct && (
              <button
                type="button"
                className="btn btn-success btn-sm"
                onClick={() => handleCompleteSprint(sprint)}
                title={t('sprint.complete_btn')}
                style={{ display: 'inline-flex', alignItems: 'center', gap: 5, backgroundColor: '#10b981', color: '#fff' }}
              >
                <CheckCircle size={13} />
                <span>{t('sprint.complete_btn')}</span>
              </button>
            )}

            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => openTasksModal(sprint)}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}
            >
              <ListTodo size={13} />
              <span>{t('sprint.assign_tasks')}</span>
            </button>
          </div>

          <div className="sprint-actions-right">
            <button
              type="button"
              className="btn-action btn-action-edit"
              onClick={() => openEditModal(sprint)}
              title={t('sprint.edit_btn')}
            >
              <Pencil size={15} />
            </button>
            <button
              type="button"
              className="btn-action btn-action-danger"
              onClick={() => handleDeleteSprint(sprint)}
              title={t('sprint.delete_btn')}
            >
              <Trash2 size={15} />
            </button>
          </div>
        </div>
      </div>
    );
  };

  return (
    <MainLayout title={project ? `${project.name} - ${t('sprint.title')}` : t('sprint.title')}>
      <div className="sprints-container">
        {/* Navigation Breadcrumb */}
        <div className="sprints-breadcrumb">
          <button className="btn-link" onClick={() => navigate(`/projects/${projectId}/board`)}>
            <ChevronLeft size={16} style={{ verticalAlign: 'middle' }} />
            <span>{t('sprint.back_to_board')}</span>
          </button>
        </div>

        {/* Page Header */}
        <div className="sprints-header">
          <div>
            <h2>
              <Layers size={24} style={{ marginRight: 8, verticalAlign: 'middle', color: '#6366f1' }} />
              {project ? `${project.name} - ${t('sprint.title')}` : t('sprint.title')}
            </h2>
            <p className="subtitle">{t('sprint.subtitle')}</p>
          </div>

          <button className="btn btn-primary" onClick={() => setShowCreateModal(true)}>
            <Plus size={16} />
            <span>{t('sprint.create_btn')}</span>
          </button>
        </div>

        {/* Banners */}
        {alertSuccess && (
          <div className="sprint-alert sprint-alert-success">
            <Check size={16} />
            <span>{alertSuccess}</span>
          </div>
        )}

        {alertError && (
          <div className="sprint-alert sprint-alert-danger">
            <AlertCircle size={16} />
            <span>{alertError}</span>
          </div>
        )}

        {/* Main Content */}
        {loading ? (
          <div className="sprints-loading">
            <Loader2 size={24} style={{ animation: 'spin 1s linear infinite' }} />
            <p style={{ marginTop: 8 }}>Loading sprints...</p>
          </div>
        ) : error ? (
          <div className="sprint-alert sprint-alert-danger">
            <AlertCircle size={16} />
            <span>{error}</span>
          </div>
        ) : sprints.length === 0 ? (
          <div className="sprints-empty">
            <Layers size={54} style={{ color: 'var(--text-subtle)' }} />
            <h3>{t('sprint.no_sprints')}</h3>
            <button className="btn btn-primary" onClick={() => setShowCreateModal(true)} style={{ marginTop: 12 }}>
              <Plus size={16} />
              <span>{t('sprint.create_btn')}</span>
            </button>
          </div>
        ) : (
          <div>
            {/* Active Sprint Section */}
            {activeSprints.length > 0 && (
              <div>
                <h3 className="sprints-section-title">
                  <Play size={16} style={{ color: '#10b981' }} />
                  <span>{t('sprint.status_active')} ({activeSprints.length})</span>
                </h3>
                <div className="sprints-grid">{activeSprints.map(renderSprintCard)}</div>
              </div>
            )}

            {/* Planning Sprints Section */}
            {planningSprints.length > 0 && (
              <div>
                <h3 className="sprints-section-title">
                  <Layers size={16} style={{ color: '#0369a1' }} />
                  <span>{t('sprint.status_planning')} ({planningSprints.length})</span>
                </h3>
                <div className="sprints-grid">{planningSprints.map(renderSprintCard)}</div>
              </div>
            )}

            {/* Completed Sprints Section */}
            {completedSprints.length > 0 && (
              <div>
                <h3 className="sprints-section-title">
                  <CheckCircle size={16} style={{ color: '#64748b' }} />
                  <span>{t('sprint.status_completed')} ({completedSprints.length})</span>
                </h3>
                <div className="sprints-grid">{completedSprints.map(renderSprintCard)}</div>
              </div>
            )}
          </div>
        )}

        {/* Create Sprint Modal */}
        {showCreateModal && (
          <div className="modal-overlay" onClick={() => setShowCreateModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 480 }}>
              <div className="modal-header">
                <h3>{t('sprint.create_btn')}</h3>
                <button className="btn-ghost" onClick={() => setShowCreateModal(false)} disabled={isCreating}>
                  <X size={18} />
                </button>
              </div>

              <form onSubmit={handleCreateSubmit}>
                <div className="modal-body">
                  {createModalError && (
                    <div className="sprint-alert sprint-alert-danger">
                      <AlertCircle size={16} />
                      <span>{createModalError}</span>
                    </div>
                  )}

                  <div className="form-group">
                    <label className="form-label">{t('sprint.name_label')} *</label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder={t('sprint.name_placeholder')}
                      value={createForm.name}
                      onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                      required
                      disabled={isCreating}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">{t('sprint.goal_label')}</label>
                    <textarea
                      className="form-control"
                      rows={3}
                      placeholder={t('sprint.goal_placeholder')}
                      value={createForm.goal || ''}
                      onChange={(e) => setCreateForm({ ...createForm, goal: e.target.value })}
                      disabled={isCreating}
                    />
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                    <div className="form-group">
                      <label className="form-label">{t('sprint.start_date')}</label>
                      <input
                        type="date"
                        className="form-control"
                        value={createForm.startDate || ''}
                        onChange={(e) => setCreateForm({ ...createForm, startDate: e.target.value })}
                        disabled={isCreating}
                      />
                    </div>
                    <div className="form-group">
                      <label className="form-label">{t('sprint.end_date')}</label>
                      <input
                        type="date"
                        className="form-control"
                        value={createForm.endDate || ''}
                        onChange={(e) => setCreateForm({ ...createForm, endDate: e.target.value })}
                        disabled={isCreating}
                      />
                    </div>
                  </div>
                </div>

                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setShowCreateModal(false)}
                    disabled={isCreating}
                  >
                    {t('board.cancel')}
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={isCreating || !createForm.name.trim()}>
                    {isCreating ? <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} /> : <Plus size={14} />}
                    <span>{t('sprint.create_btn')}</span>
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Edit Sprint Modal */}
        {showEditModal && editingSprint && (
          <div className="modal-overlay" onClick={() => setShowEditModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 480 }}>
              <div className="modal-header">
                <h3>{t('sprint.edit_btn')}</h3>
                <button className="btn-ghost" onClick={() => setShowEditModal(false)} disabled={isEditing}>
                  <X size={18} />
                </button>
              </div>

              <form onSubmit={handleEditSubmit}>
                <div className="modal-body">
                  {editModalError && (
                    <div className="sprint-alert sprint-alert-danger">
                      <AlertCircle size={16} />
                      <span>{editModalError}</span>
                    </div>
                  )}

                  <div className="form-group">
                    <label className="form-label">{t('sprint.name_label')} *</label>
                    <input
                      type="text"
                      className="form-control"
                      placeholder={t('sprint.name_placeholder')}
                      value={editForm.name}
                      onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                      required
                      disabled={isEditing}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">{t('sprint.goal_label')}</label>
                    <textarea
                      className="form-control"
                      rows={3}
                      placeholder={t('sprint.goal_placeholder')}
                      value={editForm.goal || ''}
                      onChange={(e) => setEditForm({ ...editForm, goal: e.target.value })}
                      disabled={isEditing}
                    />
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                    <div className="form-group">
                      <label className="form-label">{t('sprint.start_date')}</label>
                      <input
                        type="date"
                        className="form-control"
                        value={editForm.startDate || ''}
                        onChange={(e) => setEditForm({ ...editForm, startDate: e.target.value })}
                        disabled={isEditing}
                      />
                    </div>
                    <div className="form-group">
                      <label className="form-label">{t('sprint.end_date')}</label>
                      <input
                        type="date"
                        className="form-control"
                        value={editForm.endDate || ''}
                        onChange={(e) => setEditForm({ ...editForm, endDate: e.target.value })}
                        disabled={isEditing}
                      />
                    </div>
                  </div>
                </div>

                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setShowEditModal(false)}
                    disabled={isEditing}
                  >
                    {t('board.cancel')}
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={isEditing || !editForm.name?.trim()}>
                    {isEditing ? <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} /> : <Check size={14} />}
                    <span>{t('board.save_task')}</span>
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Manage Tasks Modal */}
        {showTasksModal && activeSprintForTasks && (
          <div className="modal-overlay" onClick={() => setShowTasksModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 560 }}>
              <div className="modal-header">
                <div>
                  <h3>{t('sprint.assign_tasks')}</h3>
                  <span style={{ fontSize: 13, color: 'var(--text-subtle)' }}>{activeSprintForTasks.name}</span>
                </div>
                <button className="btn-ghost" onClick={() => setShowTasksModal(false)}>
                  <X size={18} />
                </button>
              </div>

              <div className="modal-body">
                <div className="form-group">
                  <input
                    type="text"
                    className="form-control"
                    placeholder="Search tasks..."
                    value={taskSearch}
                    onChange={(e) => setTaskSearch(e.target.value)}
                  />
                </div>

                <div className="sprint-tasks-list">
                  {tasks.length === 0 ? (
                    <div style={{ fontStyle: 'italic', fontSize: 13, color: 'var(--text-subtle)', textAlign: 'center', padding: 16 }}>
                      No tasks found in project.
                    </div>
                  ) : (
                    tasks
                      .filter((t) => !taskSearch || t.title.toLowerCase().includes(taskSearch.toLowerCase()))
                      .map((task) => {
                        const isAssignedToThis = task.sprintId === activeSprintForTasks.id;
                        const isAssignedToOther = task.sprintId && task.sprintId !== activeSprintForTasks.id;
                        const isUpdating = isAssigningTask === task.id;

                        return (
                          <div key={task.id} className="sprint-task-item">
                            <div>
                              <div className="sprint-task-title">{task.title}</div>
                              {isAssignedToOther && (
                                <span style={{ fontSize: 11, color: 'var(--text-subtle)' }}>
                                  Assigned to another sprint
                                </span>
                              )}
                            </div>

                            <button
                              type="button"
                              className={`btn btn-sm ${isAssignedToThis ? 'btn-danger' : 'btn-secondary'}`}
                              disabled={isUpdating}
                              onClick={() => handleToggleTaskSprint(task, activeSprintForTasks.id)}
                            >
                              {isUpdating ? (
                                <Loader2 size={13} style={{ animation: 'spin 1s linear infinite' }} />
                              ) : isAssignedToThis ? (
                                'Remove'
                              ) : (
                                'Assign'
                              )}
                            </button>
                          </div>
                        );
                      })
                  )}
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-primary" onClick={() => setShowTasksModal(false)}>
                  Done
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};
