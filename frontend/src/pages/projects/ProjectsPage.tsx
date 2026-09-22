import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { projectApi, ProjectMember } from '../../api/projectApi';
import { getApiErrorMessage } from '../../api';
import { Project } from '../../types';
import { ProjectModal } from '../../components/project/ProjectModal';
import {
  Search,
  X,
  FolderKanban,
  Plus,
  AlertCircle,
  Archive,
  ArchiveRestore,
  Key,
  CalendarDays,
  Users,
  MoreVertical,
  Layers,
  FolderOpen,
  Pencil,
  Trash2,
} from 'lucide-react';
import './Projects.css';

type StatusFilter = 'active' | 'archived' | 'all';

export const ProjectsPage: React.FC = () => {
  const { workspaceId: routeWorkspaceId } = useParams<{ workspaceId?: string }>();
  const { currentWorkspace, selectWorkspace, refreshProjects } = useWorkspace();
  const { t } = useLanguage();
  const navigate = useNavigate();

  // Synchronize route workspaceId with WorkspaceContext
  useEffect(() => {
    if (routeWorkspaceId && currentWorkspace?.id !== routeWorkspaceId) {
      selectWorkspace(routeWorkspaceId);
    }
  }, [routeWorkspaceId, currentWorkspace?.id, selectWorkspace]);

  // State
  const [projects, setProjects] = useState<Project[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('active');
  const [projectMembers, setProjectMembers] = useState<Record<string, ProjectMember[]>>({});

  // Modal State
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [projectToEdit, setProjectToEdit] = useState<Project | null>(null);

  // Delete Modal State
  const [showDeleteModal, setShowDeleteModal] = useState<boolean>(false);
  const [projectToDelete, setProjectToDelete] = useState<Project | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  // Action menu state
  const [activeMenu, setActiveMenu] = useState<string | null>(null);

  // Fetch projects
  const fetchProjects = useCallback(async () => {
    if (!currentWorkspace) return;
    setLoading(true);
    setError(null);
    try {
      const includeArchived = statusFilter === 'archived' || statusFilter === 'all';
      const res = await projectApi.getProjects(currentWorkspace.id, includeArchived, 100, 0);
      if (res.success && res.data) {
        setProjects(res.data);
        // Fetch members for each project
        const memberPromises = res.data.map(async (proj) => {
          try {
            const memberRes = await projectApi.getProjectMembers(proj.id, 5, 0);
            return { projectId: proj.id, members: memberRes.data || [] };
          } catch {
            return { projectId: proj.id, members: [] };
          }
        });
        const memberResults = await Promise.all(memberPromises);
        const membersMap: Record<string, ProjectMember[]> = {};
        memberResults.forEach(({ projectId, members }) => {
          membersMap[projectId] = members;
        });
        setProjectMembers(membersMap);
      } else {
        setError(res.message || t('projects.error_title'));
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || t('projects.error_title'));
    } finally {
      setLoading(false);
    }
  }, [currentWorkspace, statusFilter, t]);

  useEffect(() => {
    fetchProjects();
  }, [fetchProjects]);

  // Close menu on outside click
  useEffect(() => {
    const handleClickOutside = () => setActiveMenu(null);
    if (activeMenu) {
      document.addEventListener('click', handleClickOutside);
      return () => document.removeEventListener('click', handleClickOutside);
    }
  }, [activeMenu]);

  // Filter projects
  const filteredProjects = useMemo(() => {
    let result = projects;

    // Status filter
    if (statusFilter === 'active') {
      result = result.filter((p) => !p.isArchived);
    } else if (statusFilter === 'archived') {
      result = result.filter((p) => p.isArchived);
    }

    // Search filter
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        (p) =>
          p.name.toLowerCase().includes(q) ||
          p.key.toLowerCase().includes(q) ||
          (p.description && p.description.toLowerCase().includes(q))
      );
    }

    return result;
  }, [projects, searchQuery, statusFilter]);

  // Counts
  const activeCount = projects.filter((p) => !p.isArchived).length;
  const archivedCount = projects.filter((p) => p.isArchived).length;

  // Archive/Restore actions
  const handleArchive = async (projectId: string) => {
    try {
      await projectApi.archiveProject(projectId);
      await fetchProjects();
      await refreshProjects();
    } catch (err: any) {
      console.error('Failed to archive project:', err);
    }
    setActiveMenu(null);
  };

  const handleRestore = async (projectId: string) => {
    try {
      await projectApi.restoreProject(projectId);
      await fetchProjects();
      await refreshProjects();
    } catch (err: any) {
      console.error('Failed to restore project:', err);
    }
    setActiveMenu(null);
  };

  const handleDeleteSubmit = async () => {
    if (!projectToDelete) return;
    setDeleteError(null);
    setIsDeleting(true);
    try {
      await projectApi.deleteProject(projectToDelete.id);
      setShowDeleteModal(false);
      setProjectToDelete(null);
      await fetchProjects();
      await refreshProjects();
    } catch (err: any) {
      setDeleteError(getApiErrorMessage(err) || t('projects.delete_error'));
    } finally {
      setIsDeleting(false);
    }
  };

  const handleProjectClick = (project: Project) => {
    navigate(`/projects/${project.id}/board`);
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  };

  return (
    <MainLayout title={t('projects.title')}>
      <div className="projects-container">
        {/* Header */}
        <div className="projects-header">
          <div>
            <h2>
              <FolderKanban size={24} style={{ marginRight: 8, verticalAlign: 'middle' }} />
              {t('projects.title')}
            </h2>
            <p className="subtitle">{t('projects.subtitle')}</p>
          </div>
          <button
            className="btn btn-primary"
            onClick={() => setShowCreateModal(true)}
          >
            <Plus size={16} />
            <span>{t('modal.create_project')}</span>
          </button>
        </div>

        {/* Status Tabs */}
        <div className="projects-tabs">
          <button
            className={`tab-btn ${statusFilter === 'active' ? 'active' : ''}`}
            onClick={() => setStatusFilter('active')}
          >
            <FolderOpen size={16} />
            {t('projects.tab_active')}
            <span className="tab-count">{activeCount}</span>
          </button>
          <button
            className={`tab-btn ${statusFilter === 'archived' ? 'active' : ''}`}
            onClick={() => setStatusFilter('archived')}
          >
            <Archive size={16} />
            {t('projects.tab_archived')}
            <span className="tab-count">{archivedCount}</span>
          </button>
          <button
            className={`tab-btn ${statusFilter === 'all' ? 'active' : ''}`}
            onClick={() => setStatusFilter('all')}
          >
            <Layers size={16} />
            {t('projects.tab_all')}
            <span className="tab-count">{projects.length}</span>
          </button>
        </div>

        {/* Toolbar */}
        <div className="projects-toolbar">
          <div className="search-box">
            <Search size={16} className="search-icon" />
            <input
              type="text"
              className="search-input"
              placeholder={t('projects.search_placeholder')}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            {searchQuery && (
              <button className="search-clear-btn" onClick={() => setSearchQuery('')}>
                <X size={14} />
              </button>
            )}
          </div>
          <span className="projects-count-badge">
            {t('projects.total_projects').replace('{count}', String(filteredProjects.length))}
          </span>
        </div>

        {/* Content */}
        {loading ? (
          <div className="projects-loading-state">
            <div className="loading-spinner" />
            <p>{t('projects.loading')}</p>
          </div>
        ) : error ? (
          <div className="projects-error-state">
            <AlertCircle size={48} />
            <h3>{t('projects.error_title')}</h3>
            <p>{error}</p>
            <button className="btn btn-primary" onClick={fetchProjects}>
              {t('projects.retry')}
            </button>
          </div>
        ) : filteredProjects.length === 0 ? (
          <div className="projects-empty-state">
            <FolderKanban size={56} />
            <h3>{t('projects.empty_title')}</h3>
            <p>{t('projects.empty_desc')}</p>
          </div>
        ) : (
          <div className="projects-grid">
            {filteredProjects.map((project) => {
              const members = projectMembers[project.id] || [];
              return (
                <div
                  key={project.id}
                  className={`project-card ${project.isArchived ? 'archived' : ''}`}
                  onClick={() => handleProjectClick(project)}
                >
                  {/* Card Header */}
                  <div className="project-card-header">
                    <div className="project-key-badge">{project.key}</div>
                    <div className="project-card-actions" onClick={(e) => e.stopPropagation()}>
                      <button
                        className="btn-icon-sm"
                        onClick={(e) => {
                          e.stopPropagation();
                          setActiveMenu(activeMenu === project.id ? null : project.id);
                        }}
                      >
                        <MoreVertical size={16} />
                      </button>
                      {activeMenu === project.id && (
                        <div className="action-dropdown">
                          <button
                            className="dropdown-item"
                            onClick={() => {
                              setProjectToEdit(project);
                              setActiveMenu(null);
                            }}
                          >
                            <Pencil size={14} />
                            {t('modal.edit_project')}
                          </button>
                          <button
                            className="dropdown-item"
                            onClick={() => {
                              navigate(`/projects/${project.id}/members`);
                              setActiveMenu(null);
                            }}
                          >
                            <Users size={14} />
                            {t('proj_members.title')}
                          </button>
                          {project.isArchived ? (
                            <button
                              className="dropdown-item"
                              onClick={() => handleRestore(project.id)}
                            >
                              <ArchiveRestore size={14} />
                              {t('projects.restore')}
                            </button>
                          ) : (
                            <button
                              className="dropdown-item"
                              onClick={() => handleArchive(project.id)}
                            >
                              <Archive size={14} />
                              {t('projects.archive')}
                            </button>
                          )}
                          <button
                            className="dropdown-item"
                            style={{ color: 'var(--danger)' }}
                            onClick={() => {
                              setProjectToDelete(project);
                              setDeleteError(null);
                              setShowDeleteModal(true);
                              setActiveMenu(null);
                            }}
                          >
                            <Trash2 size={14} />
                            {t('projects.delete')}
                          </button>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Card Body */}
                  <div className="project-card-body">
                    <h3 className="project-card-name">{project.name}</h3>
                    <p className="project-card-desc">
                      {project.description || t('projects.no_description')}
                    </p>
                  </div>

                  {/* Status Badge */}
                  <div className="project-card-status">
                    {project.isArchived ? (
                      <span className="status-badge status-archived">
                        <Archive size={12} />
                        {t('projects.status_archived')}
                      </span>
                    ) : (
                      <span className="status-badge status-active">
                        <FolderOpen size={12} />
                        {t('projects.status_active')}
                      </span>
                    )}
                  </div>

                  {/* Card Footer */}
                  <div className="project-card-footer">
                    <div className="project-card-meta">
                      <span className="meta-item" title={t('projects.lead')}>
                        <Key size={13} />
                        {project.leadName || '—'}
                      </span>
                      <span className="meta-item" title={t('projects.created')}>
                        <CalendarDays size={13} />
                        {formatDate(project.createdAt)}
                      </span>
                    </div>
                    <div className="project-card-members">
                      {members.length > 0 ? (
                        <div className="member-avatars">
                          {members.slice(0, 4).map((m) => (
                            <div
                              key={m.userId}
                              className="member-avatar-sm"
                              title={m.displayName}
                            >
                              {m.avatarUrl ? (
                                <img src={m.avatarUrl} alt={m.displayName} />
                              ) : (
                                <span>{getInitials(m.displayName)}</span>
                              )}
                            </div>
                          ))}
                          {members.length > 4 && (
                            <div className="member-avatar-sm more-count">
                              +{members.length - 4}
                            </div>
                          )}
                        </div>
                      ) : (
                        <span className="meta-item">
                          <Users size={13} />
                          0
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Create Project Modal */}
        {showCreateModal && currentWorkspace && (
          <ProjectModal
            isOpen={showCreateModal}
            onClose={() => setShowCreateModal(false)}
            workspaceId={currentWorkspace.id}
            onSuccess={fetchProjects}
          />
        )}

        {/* Edit Project Modal */}
        {projectToEdit && currentWorkspace && (
          <ProjectModal
            isOpen={!!projectToEdit}
            onClose={() => setProjectToEdit(null)}
            workspaceId={currentWorkspace.id}
            projectToEdit={projectToEdit}
            onSuccess={fetchProjects}
          />
        )}

        {/* Delete Project Modal */}
        {showDeleteModal && projectToDelete && (
          <div className="modal-overlay" onClick={() => setShowDeleteModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 420 }}>
              <div className="modal-header">
                <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                  <Trash2 size={20} style={{ color: 'var(--danger)' }} />
                  <h3>{t('projects.delete')}</h3>
                </div>
                <button className="btn-ghost" onClick={() => setShowDeleteModal(false)} disabled={isDeleting}>
                  ✕
                </button>
              </div>
              <div className="modal-body">
                {deleteError && (
                  <div className="badge badge-danger" style={{ marginBottom: 14 }}>
                    <AlertCircle size={14} />
                    <span>{deleteError}</span>
                  </div>
                )}
                <p style={{ fontSize: 14, color: 'var(--text-main)', margin: 0 }}>
                  {t('projects.delete_confirm')?.replace('{name}', projectToDelete.name) ||
                    `Are you sure you want to delete project ${projectToDelete.name}? This action cannot be undone.`}
                </p>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowDeleteModal(false)}
                  disabled={isDeleting}
                >
                  {t('modal.proj_cancel') || 'Cancel'}
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleDeleteSubmit}
                  disabled={isDeleting}
                >
                  <Trash2 size={14} />
                  {isDeleting ? '...' : t('projects.delete')}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};
