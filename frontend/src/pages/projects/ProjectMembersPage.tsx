import React, { useEffect, useState, useMemo, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useAuth } from '../../contexts/AuthContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { projectApi, ProjectMember } from '../../api/projectApi';
import { getApiErrorMessage } from '../../api';
import { Project } from '../../types';
import { AddProjectMemberModal } from '../../components/project/AddProjectMemberModal';
import {
  Users,
  UserPlus,
  UserX,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
  AlertCircle,
  ShieldCheck,
  UserCog,
  FolderKanban,
  Trash2,
  Check,
} from 'lucide-react';
import './ProjectMembers.css';

export const ProjectMembersPage: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { currentWorkspace } = useWorkspace();
  const { user: currentUser } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

  // Project state
  const [project, setProject] = useState<Project | null>(null);
  const [loadingProject, setLoadingProject] = useState<boolean>(true);

  // Members state
  const [members, setMembers] = useState<ProjectMember[]>([]);
  const [loadingMembers, setLoadingMembers] = useState<boolean>(true);
  const [membersError, setMembersError] = useState<string | null>(null);

  // Search & Pagination
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const pageSize = 10;

  // Add Member Modal State
  const [showAddModal, setShowAddModal] = useState<boolean>(false);

  // Role Change Modal State
  const [showRoleModal, setShowRoleModal] = useState<boolean>(false);
  const [selectedMember, setSelectedMember] = useState<ProjectMember | null>(null);
  const [targetRole, setTargetRole] = useState<string>('member');
  const [roleError, setRoleError] = useState<string>('');
  const [roleSuccess, setRoleSuccess] = useState<string>('');
  const [isSubmittingRole, setIsSubmittingRole] = useState<boolean>(false);

  // Remove Member Confirm Modal State
  const [showRemoveModal, setShowRemoveModal] = useState<boolean>(false);
  const [memberToRemove, setMemberToRemove] = useState<ProjectMember | null>(null);
  const [removeError, setRemoveError] = useState<string>('');
  const [isRemoving, setIsRemoving] = useState<boolean>(false);

  // General Notification Alert
  const [actionSuccessMsg, setActionSuccessMsg] = useState<string>('');
  const [actionErrorMsg, setActionErrorMsg] = useState<string>('');

  // 1. Fetch Project Details
  const fetchProjectDetails = useCallback(async () => {
    if (!projectId) return;
    setLoadingProject(true);
    try {
      const res = await projectApi.getProject(projectId);
      if (res.success && res.data) {
        setProject(res.data);
      }
    } catch (err) {
      console.error('Failed to fetch project details:', err);
    } finally {
      setLoadingProject(false);
    }
  }, [projectId]);

  // 2. Fetch Project Members
  const fetchProjectMembers = useCallback(async () => {
    if (!projectId) return;
    setLoadingMembers(true);
    setMembersError(null);
    try {
      const res = await projectApi.getProjectMembers(projectId, 100, 0);
      if (res.success && res.data) {
        setMembers(res.data);
      } else {
        setMembersError(res.message || t('proj_members.error_title') || 'Failed to load project members.');
      }
    } catch (err: any) {
      setMembersError(getApiErrorMessage(err) || t('proj_members.error_title') || 'Failed to load project members.');
    } finally {
      setLoadingMembers(false);
    }
  }, [projectId, t]);

  useEffect(() => {
    fetchProjectDetails();
    fetchProjectMembers();
  }, [fetchProjectDetails, fetchProjectMembers]);

  // Filtered members by search
  const filteredMembers = useMemo(() => {
    if (!searchQuery.trim()) return members;
    const q = searchQuery.toLowerCase();
    return members.filter(
      (m) =>
        m.displayName.toLowerCase().includes(q) ||
        m.email.toLowerCase().includes(q) ||
        m.role.toLowerCase().includes(q)
    );
  }, [members, searchQuery]);

  // Pagination slicing
  const totalPages = Math.ceil(filteredMembers.length / pageSize) || 1;
  const paginatedMembers = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filteredMembers.slice(start, start + pageSize);
  }, [filteredMembers, page, pageSize]);

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
    setPage(1);
  };

  // Change Member Role Handler
  const openRoleModal = (member: ProjectMember) => {
    setSelectedMember(member);
    setTargetRole(member.role || 'member');
    setRoleError('');
    setRoleSuccess('');
    setShowRoleModal(true);
  };

  const handleUpdateRoleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId || !selectedMember) return;
    setRoleError('');
    setRoleSuccess('');
    setIsSubmittingRole(true);

    try {
      const res = await projectApi.updateProjectMemberRole(projectId, selectedMember.userId, targetRole);
      if (res.success) {
        setRoleSuccess(t('proj_members.role_update_success') || 'Member role updated successfully!');
        await fetchProjectMembers();
        setTimeout(() => {
          setShowRoleModal(false);
          setSelectedMember(null);
        }, 1200);
      } else {
        setRoleError(res.message || t('proj_members.role_update_error') || 'Failed to update role.');
      }
    } catch (err: any) {
      setRoleError(getApiErrorMessage(err) || t('proj_members.role_update_error') || 'Failed to update role.');
    } finally {
      setIsSubmittingRole(false);
    }
  };

  // Remove Member Handler
  const openRemoveModal = (member: ProjectMember) => {
    setMemberToRemove(member);
    setRemoveError('');
    setShowRemoveModal(true);
  };

  const handleRemoveSubmit = async () => {
    if (!projectId || !memberToRemove) return;
    setRemoveError('');
    setIsRemoving(true);
    try {
      const res = await projectApi.removeProjectMember(projectId, memberToRemove.userId);
      if (res.success) {
        setActionSuccessMsg(t('proj_members.remove_success') || 'Member removed from project successfully!');
        setShowRemoveModal(false);
        setMemberToRemove(null);
        await fetchProjectMembers();
        setTimeout(() => setActionSuccessMsg(''), 3000);
      } else {
        setRemoveError(res.message || t('proj_members.remove_error') || 'Failed to remove member.');
      }
    } catch (err: any) {
      setRemoveError(getApiErrorMessage(err) || t('proj_members.remove_error') || 'Failed to remove member.');
    } finally {
      setIsRemoving(false);
    }
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

  const existingUserIds = useMemo(() => members.map((m) => m.userId), [members]);

  const canManageMembers = useMemo(() => {
    if (!currentUser) return false;
    if (project?.leadId === currentUser.id) return true;
    const projectMember = members.find((m) => m.userId === currentUser.id);
    const role = projectMember?.role?.toLowerCase();
    return role === 'lead' || role === 'owner' || role === 'admin';
  }, [currentUser, project, members]);

  return (
    <MainLayout title={project ? `${project.name} - ${t('proj_members.title')}` : t('proj_members.title')}>
      <div className="proj-members-container">
        {/* Navigation Breadcrumb */}
        <div className="proj-members-breadcrumb">
          <button className="btn-link" onClick={() => navigate(`/projects/${projectId}/board`)}>
            ← {t('proj_members.back_to_board') || 'Back to Board'}
          </button>
        </div>

        {/* Page Header */}
        <div className="proj-members-header">
          <div>
            <h2>
              <Users size={24} style={{ marginRight: 8, verticalAlign: 'middle' }} />
              {project ? `${project.name} (${project.key})` : t('proj_members.title')}
            </h2>
            <p className="subtitle">{t('proj_members.subtitle') || 'Manage members and roles for this project.'}</p>
          </div>
          {canManageMembers && (
            <button className="btn btn-primary" onClick={() => setShowAddModal(true)}>
              <UserPlus size={16} />
              <span>{t('proj_members.add_btn') || 'Add Member'}</span>
            </button>
          )}
        </div>

        {/* Success Alert */}
        {actionSuccessMsg && (
          <div className="badge badge-success" style={{ padding: '10px 14px', borderRadius: 8 }}>
            <Check size={16} />
            <span>{actionSuccessMsg}</span>
          </div>
        )}

        {/* Error Alert */}
        {actionErrorMsg && (
          <div className="badge badge-danger" style={{ padding: '10px 14px', borderRadius: 8 }}>
            <AlertCircle size={16} />
            <span>{actionErrorMsg}</span>
          </div>
        )}

        {/* Toolbar */}
        <div className="proj-members-toolbar">
          <div className="search-box">
            <Search size={16} className="search-icon" />
            <input
              type="text"
              className="search-input"
              placeholder={t('proj_members.search_placeholder') || 'Search project members...'}
              value={searchQuery}
              onChange={handleSearchChange}
            />
            {searchQuery && (
              <button className="search-clear-btn" onClick={() => setSearchQuery('')}>
                <X size={14} />
              </button>
            )}
          </div>

          <span className="proj-members-count">
            {t('proj_members.total_count')?.replace('{count}', String(filteredMembers.length)) ||
              `Total: ${filteredMembers.length} member(s)`}
          </span>
        </div>

        {/* Table Content */}
        {loadingMembers ? (
          <div className="proj-members-loading">
            <div className="loading-spinner" />
            <p>{t('proj_members.loading') || 'Loading project members...'}</p>
          </div>
        ) : membersError ? (
          <div className="proj-members-error">
            <AlertCircle size={48} />
            <h3>{t('proj_members.error_title') || 'Failed to Load Project Members'}</h3>
            <p>{membersError}</p>
            <button className="btn btn-primary" onClick={fetchProjectMembers}>
              {t('workspace.retry') || 'Retry'}
            </button>
          </div>
        ) : filteredMembers.length === 0 ? (
          <div className="proj-members-empty">
            <Users size={56} />
            <h3>{t('proj_members.empty_title') || 'No Members Found'}</h3>
            <p>{t('proj_members.empty_desc') || 'No project members match your search criteria.'}</p>
          </div>
        ) : (
          <div className="proj-members-table-card">
            <div className="table-wrapper">
              <table className="proj-members-table">
                <thead>
                  <tr>
                    <th>{t('members.table_member') || 'Member'}</th>
                    <th>{t('members.table_email') || 'Email'}</th>
                    <th>{t('members.table_roles') || 'Project Role'}</th>
                    <th>{t('members.table_joined') || 'Joined Date'}</th>
                    <th style={{ textAlign: 'right' }}>{t('members.table_actions') || 'Actions'}</th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedMembers.map((member) => {
                    const isSelf = currentUser?.id === member.userId;
                    const roleName = member.role || 'member';

                    return (
                      <tr key={member.userId}>
                        <td>
                          <div className="member-cell">
                            <div className="member-avatar">
                              {member.avatarUrl ? (
                                <img src={member.avatarUrl} alt={member.displayName} />
                              ) : (
                                <span>{getInitials(member.displayName)}</span>
                              )}
                            </div>
                            <div>
                              <span className="member-name">
                                {member.displayName} {isSelf && <span className="you-tag">({t('members.you') || 'You'})</span>}
                              </span>
                            </div>
                          </div>
                        </td>

                        <td>
                          <span className="member-email">{member.email}</span>
                        </td>

                        <td>
                          <span className={`role-badge role-${roleName.toLowerCase()}`}>
                            <ShieldCheck size={13} />
                            {roleName.toUpperCase()}
                          </span>
                        </td>

                        <td>
                          <span className="joined-date">{formatDate(member.joinedAt)}</span>
                        </td>

                        <td style={{ textAlign: 'right' }}>
                          <div className="actions-cell">
                            <button
                              className="btn-action btn-action-edit"
                              onClick={() => openRoleModal(member)}
                              title={t('members.change_role') || 'Change Role'}
                            >
                              <UserCog size={15} />
                            </button>
                            <button
                              className="btn-action btn-action-danger"
                              onClick={() => openRemoveModal(member)}
                              title={t('members.remove') || 'Remove Member'}
                            >
                              <UserX size={15} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            {totalPages > 1 && (
              <div className="pagination-bar">
                <span>
                  Page {page} of {totalPages}
                </span>
                <div style={{ display: 'flex', gap: 6 }}>
                  <button
                    className="btn btn-secondary btn-sm"
                    disabled={page === 1}
                    onClick={() => setPage((p) => Math.max(p - 1, 1))}
                  >
                    <ChevronLeft size={14} />
                  </button>
                  <button
                    className="btn btn-secondary btn-sm"
                    disabled={page >= totalPages}
                    onClick={() => setPage((p) => Math.min(p + 1, totalPages))}
                  >
                    <ChevronRight size={14} />
                  </button>
                </div>
              </div>
            )}
          </div>
        )}

        {/* Add Project Member Modal */}
        {showAddModal && currentWorkspace && projectId && (
          <AddProjectMemberModal
            isOpen={showAddModal}
            onClose={() => setShowAddModal(false)}
            projectId={projectId}
            workspaceId={currentWorkspace.id}
            existingMemberUserIds={existingUserIds}
            onSuccess={fetchProjectMembers}
          />
        )}

        {/* Change Role Modal */}
        {showRoleModal && selectedMember && (
          <div className="modal-overlay" onClick={() => setShowRoleModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
              <div className="modal-header">
                <h3>{t('members.change_role_title') || 'Change Member Role'}</h3>
                <button className="btn-ghost" onClick={() => setShowRoleModal(false)} disabled={isSubmittingRole}>
                  ✕
                </button>
              </div>
              <form onSubmit={handleUpdateRoleSubmit}>
                <div className="modal-body">
                  {roleSuccess && (
                    <div className="badge badge-success" style={{ marginBottom: 14 }}>
                      <Check size={14} />
                      <span>{roleSuccess}</span>
                    </div>
                  )}

                  {roleError && (
                    <div className="badge badge-danger" style={{ marginBottom: 14 }}>
                      <AlertCircle size={14} />
                      <span>{roleError}</span>
                    </div>
                  )}

                  <div style={{ marginBottom: 14, fontSize: 14 }}>
                    Updating role for <strong>{selectedMember.displayName}</strong>
                  </div>

                  <div className="form-group">
                    <label className="form-label">{t('proj_members.select_role') || 'Project Role'}</label>
                    <div style={{ display: 'flex', gap: 10, marginTop: 4 }}>
                      {['lead', 'member', 'viewer'].map((rKey) => (
                        <label
                          key={rKey}
                          className={`role-option-card ${targetRole === rKey ? 'selected' : ''}`}
                          style={{ flex: 1, padding: '10px', cursor: 'pointer', textAlign: 'center' }}
                        >
                          <input
                            type="radio"
                            name="updateRoleSelect"
                            value={rKey}
                            checked={targetRole === rKey}
                            onChange={(e) => setTargetRole(e.target.value)}
                            disabled={isSubmittingRole}
                          />
                          <span style={{ fontSize: 13, fontWeight: 600, textTransform: 'capitalize' }}>
                            {rKey}
                          </span>
                        </label>
                      ))}
                    </div>
                  </div>
                </div>

                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setShowRoleModal(false)}
                    disabled={isSubmittingRole}
                  >
                    {t('modal.ws_cancel') || 'Cancel'}
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={isSubmittingRole}>
                    {isSubmittingRole ? '...' : t('profile.save_changes') || 'Save Changes'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Remove Member Confirmation Modal */}
        {showRemoveModal && memberToRemove && (
          <div className="modal-overlay" onClick={() => setShowRemoveModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 420 }}>
              <div className="modal-header">
                <h3>{t('members.remove') || 'Remove Member'}</h3>
                <button className="btn-ghost" onClick={() => setShowRemoveModal(false)} disabled={isRemoving}>
                  ✕
                </button>
              </div>
              <div className="modal-body">
                {removeError && (
                  <div className="badge badge-danger" style={{ marginBottom: 14 }}>
                    <AlertCircle size={14} />
                    <span>{removeError}</span>
                  </div>
                )}
                <p style={{ fontSize: 14, color: 'var(--text-main)', margin: 0 }}>
                  Are you sure you want to remove <strong>{memberToRemove.displayName}</strong> from this project?
                </p>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowRemoveModal(false)}
                  disabled={isRemoving}
                >
                  {t('modal.ws_cancel') || 'Cancel'}
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleRemoveSubmit}
                  disabled={isRemoving}
                >
                  <Trash2 size={14} />
                  {isRemoving ? '...' : t('members.remove') || 'Remove'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};
