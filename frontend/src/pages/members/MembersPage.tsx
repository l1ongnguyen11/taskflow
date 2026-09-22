import React, { useEffect, useState, useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useAuth } from '../../contexts/AuthContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { workspaceApi } from '../../api/workspaceApi';
import { WorkspaceMember, WorkspaceInvitation } from '../../types';
import { InviteMemberModal } from '../../components/workspace/InviteMemberModal';
import {
  UserPlus,
  UserX,
  Mail,
  Shield,
  Search,
  X,
  ChevronLeft,
  ChevronRight,
  Users,
  AlertCircle,
  ShieldCheck,
  UserCog,
  RefreshCw,
  Clock,
  Trash2,
} from 'lucide-react';
import './Members.css';

export const MembersPage: React.FC = () => {
  const { workspaceId: routeWorkspaceId } = useParams<{ workspaceId?: string }>();
  const { currentWorkspace, selectWorkspace } = useWorkspace();
  const { user: currentUser } = useAuth();
  const { t } = useLanguage();

  // Synchronize route workspaceId with WorkspaceContext
  useEffect(() => {
    if (routeWorkspaceId && currentWorkspace?.id !== routeWorkspaceId) {
      selectWorkspace(routeWorkspaceId);
    }
  }, [routeWorkspaceId, currentWorkspace?.id, selectWorkspace]);

  // Tab State
  const [activeTab, setActiveTab] = useState<'members' | 'pending'>('members');

  // Members State
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [totalMembersCount, setTotalMembersCount] = useState<number>(0);
  const [loadingMembers, setLoadingMembers] = useState<boolean>(true);
  const [membersError, setMembersError] = useState<string | null>(null);

  // Pending Invitations State
  const [invitations, setInvitations] = useState<WorkspaceInvitation[]>([]);
  const [totalInvitationsCount, setTotalInvitationsCount] = useState<number>(0);
  const [loadingInvitations, setLoadingInvitations] = useState<boolean>(false);
  const [invitationsError, setInvitationsError] = useState<string | null>(null);

  // Search & Pagination
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const pageSize = 10;

  // Invite Modal State
  const [showInviteModal, setShowInviteModal] = useState<boolean>(false);

  // Change Role Modal State
  const [showRoleModal, setShowRoleModal] = useState<boolean>(false);
  const [selectedMember, setSelectedMember] = useState<WorkspaceMember | null>(null);
  const [targetRole, setTargetRole] = useState<string>('member');
  const [roleError, setRoleError] = useState<string>('');
  const [roleSuccess, setRoleSuccess] = useState<string>('');
  const [isSubmittingRole, setIsSubmittingRole] = useState<boolean>(false);

  // Remove Member Confirm Modal State
  const [showRemoveModal, setShowRemoveModal] = useState<boolean>(false);
  const [memberToRemove, setMemberToRemove] = useState<WorkspaceMember | null>(null);
  const [removeError, setRemoveError] = useState<string>('');
  const [isRemoving, setIsRemoving] = useState<boolean>(false);

  // Cancel Invitation Modal State
  const [showCancelModal, setShowCancelModal] = useState<boolean>(false);
  const [invitationToCancel, setInvitationToCancel] = useState<WorkspaceInvitation | null>(null);
  const [cancelError, setCancelError] = useState<string>('');
  const [isCancelling, setIsCancelling] = useState<boolean>(false);

  const loadMembers = async (search = searchQuery) => {
    if (!currentWorkspace) return;
    setLoadingMembers(true);
    setMembersError(null);
    try {
      const offset = (page - 1) * pageSize;
      const res = await workspaceApi.getMembers(currentWorkspace.id, search, pageSize, offset);
      if (res.success && res.data) {
        setMembers(res.data);
        setTotalMembersCount(res.pagination?.totalCount ?? res.data.length);
      } else {
        setMembersError(res.message || t('members.error_title'));
      }
    } catch (err: any) {
      console.error('Failed to load members:', err);
      const msg = err.response?.data?.message || err.message || t('members.error_title');
      setMembersError(msg);
    } finally {
      setLoadingMembers(false);
    }
  };

  const loadInvitations = async () => {
    if (!currentWorkspace) return;
    setLoadingInvitations(true);
    setInvitationsError(null);
    try {
      const offset = (page - 1) * pageSize;
      const res = await workspaceApi.getInvitations(currentWorkspace.id, 'pending', pageSize, offset);
      if (res.success && res.data) {
        setInvitations(res.data);
        setTotalInvitationsCount(res.pagination?.totalCount ?? res.data.length);
      } else {
        setInvitationsError(res.message || t('invitations.cancel_error'));
      }
    } catch (err: any) {
      console.error('Failed to load invitations:', err);
      const msg = err.response?.data?.message || err.message || t('invitations.cancel_error');
      setInvitationsError(msg);
    } finally {
      setLoadingInvitations(false);
    }
  };

  useEffect(() => {
    setPage(1);
    setSearchQuery('');
  }, [currentWorkspace?.id, activeTab]);

  useEffect(() => {
    if (!currentWorkspace?.id) return;
    if (activeTab === 'members') {
      const timer = setTimeout(() => {
        loadMembers(searchQuery);
      }, 300);
      return () => clearTimeout(timer);
    } else {
      loadInvitations();
    }
  }, [currentWorkspace?.id, page, activeTab, searchQuery]);

  const handleSearchInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchQuery(e.target.value);
    setPage(1);
  };

  // Determine current user's role in this workspace
  const currentUserRoles = useMemo(() => {
    if (!currentUser || !members.length) return [];
    const found = members.find((m) => m.userId === currentUser.id);
    return found?.roles || [];
  }, [currentUser, members]);

  const isOwner = useMemo(() => {
    return (
      currentUserRoles.some((r) => r.toLowerCase() === 'owner') ||
      currentWorkspace?.createdBy === currentUser?.id
    );
  }, [currentUserRoles, currentWorkspace, currentUser]);

  const isAdmin = useMemo(() => {
    return isOwner || currentUserRoles.some((r) => r.toLowerCase() === 'admin');
  }, [isOwner, currentUserRoles]);

  // Filter members by search query (client-side safety check)
  const filteredMembers = useMemo(() => {
    if (!searchQuery.trim()) return members;
    const query = searchQuery.toLowerCase().trim();
    return members.filter(
      (m) =>
        m.displayName.toLowerCase().includes(query) ||
        m.email.toLowerCase().includes(query)
    );
  }, [members, searchQuery]);

  // Filter invitations by search query
  const filteredInvitations = useMemo(() => {
    if (!searchQuery.trim()) return invitations;
    const query = searchQuery.toLowerCase().trim();
    return invitations.filter((i) => i.email.toLowerCase().includes(query));
  }, [invitations, searchQuery]);

  const totalCount = activeTab === 'members' ? totalMembersCount : totalInvitationsCount;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  // Open invite modal
  const handleOpenInviteModal = () => {
    setShowInviteModal(true);
  };

  // Change Role actions
  const handleOpenRoleModal = (member: WorkspaceMember) => {
    setSelectedMember(member);
    const primaryRole = member.roles?.[0]?.toLowerCase() || 'member';
    setTargetRole(primaryRole);
    setRoleError('');
    setRoleSuccess('');
    setShowRoleModal(true);
  };

  const handleChangeRoleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentWorkspace || !selectedMember) return;

    setRoleError('');
    setRoleSuccess('');
    setIsSubmittingRole(true);

    try {
      const res = await workspaceApi.changeRole(
        currentWorkspace.id,
        selectedMember.userId,
        targetRole
      );

      if (res.success) {
        setRoleSuccess(t('members.change_role_success'));
        await loadMembers();
        setTimeout(() => {
          setShowRoleModal(false);
          setRoleSuccess('');
        }, 1200);
      } else {
        setRoleError(res.message || t('members.change_role_error'));
      }
    } catch (err: any) {
      setRoleError(err.response?.data?.message || t('members.change_role_error'));
    } finally {
      setIsSubmittingRole(false);
    }
  };

  // Remove Member actions
  const handleOpenRemoveModal = (member: WorkspaceMember) => {
    setMemberToRemove(member);
    setRemoveError('');
    setShowRemoveModal(true);
  };

  const handleConfirmRemove = async () => {
    if (!currentWorkspace || !memberToRemove) return;

    setIsRemoving(true);
    setRemoveError('');

    try {
      const res = await workspaceApi.removeMember(currentWorkspace.id, memberToRemove.userId);
      if (res.success) {
        setShowRemoveModal(false);
        setMemberToRemove(null);
        await loadMembers();
      } else {
        setRemoveError(res.message || t('members.remove_error'));
      }
    } catch (err: any) {
      setRemoveError(err.response?.data?.message || t('members.remove_error'));
    } finally {
      setIsRemoving(false);
    }
  };

  // Cancel Invitation actions
  const handleOpenCancelModal = (invitation: WorkspaceInvitation) => {
    setInvitationToCancel(invitation);
    setCancelError('');
    setShowCancelModal(true);
  };

  const handleConfirmCancelInvitation = async () => {
    if (!currentWorkspace || !invitationToCancel) return;

    setIsCancelling(true);
    setCancelError('');

    try {
      const res = await workspaceApi.cancelInvitation(currentWorkspace.id, invitationToCancel.id);
      if (res.success) {
        setShowCancelModal(false);
        setInvitationToCancel(null);
        await loadInvitations();
      } else {
        setCancelError(res.message || t('invitations.cancel_error'));
      }
    } catch (err: any) {
      setCancelError(err.response?.data?.message || t('invitations.cancel_error'));
    } finally {
      setIsCancelling(false);
    }
  };

  const renderRoleBadge = (roleName: string, key: number) => {
    const lower = roleName.toLowerCase();
    let badgeClass = 'role-badge member';
    let icon = <Shield size={12} />;

    if (lower === 'owner') {
      badgeClass = 'role-badge owner';
      icon = <ShieldCheck size={12} />;
    } else if (lower === 'admin') {
      badgeClass = 'role-badge admin';
      icon = <Shield size={12} />;
    } else if (lower === 'viewer') {
      badgeClass = 'role-badge viewer';
      icon = <Shield size={12} />;
    }

    return (
      <span key={key} className={badgeClass}>
        {icon} {roleName}
      </span>
    );
  };

  return (
    <MainLayout title={`${t('members.title')} - ${currentWorkspace?.name || 'Workspace'}`}>
      <div className="members-container">
        {/* Header section */}
        <div className="members-header">
          <div>
            <h2>{t('members.title')}</h2>
            <p className="subtitle">{t('members.subtitle')}</p>
          </div>
          {isAdmin && (
            <button className="btn btn-primary" onClick={handleOpenInviteModal}>
              <UserPlus size={18} /> {t('members.invite')}
            </button>
          )}
        </div>

        {/* Navigation Tabs */}
        <div className="members-tabs">
          <button
            className={`tab-btn ${activeTab === 'members' ? 'active' : ''}`}
            onClick={() => setActiveTab('members')}
          >
            <Users size={16} />
            {t('invitations.tab_members')}
            <span className="tab-count">{totalMembersCount}</span>
          </button>
          <button
            className={`tab-btn ${activeTab === 'pending' ? 'active' : ''}`}
            onClick={() => setActiveTab('pending')}
          >
            <Clock size={16} />
            {t('invitations.tab_pending')}
            <span className="tab-count">{totalInvitationsCount}</span>
          </button>
        </div>

        {/* Search Toolbar */}
        <div className="members-toolbar">
          <div className="search-box">
            <Search size={18} className="search-icon" />
            <input
              type="text"
              placeholder={t('members.search_placeholder')}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
            />
            {searchQuery && (
              <button
                className="search-clear-btn"
                onClick={() => setSearchQuery('')}
                title="Clear"
              >
                <X size={16} />
              </button>
            )}
          </div>
          <div className="members-count-badge">
            {activeTab === 'members'
              ? t('members.total_members').replace('{count}', String(totalMembersCount))
              : `${totalInvitationsCount} ${t('invitations.status_pending').toLowerCase()}`}
          </div>
        </div>

        {/* TAB 1: ACTIVE MEMBERS */}
        {activeTab === 'members' && (
          <>
            {loadingMembers ? (
              <div className="members-loading-state">
                <div className="spinner"></div>
                <p>{t('members.loading')}</p>
              </div>
            ) : membersError ? (
              <div className="members-error-state">
                <div className="error-icon-wrapper">
                  <AlertCircle size={32} />
                </div>
                <h3 className="state-title">{t('members.error_title')}</h3>
                <p className="state-desc">{membersError}</p>
                <button className="btn btn-secondary" onClick={() => loadMembers()}>
                  <RefreshCw size={16} /> {t('members.retry')}
                </button>
              </div>
            ) : filteredMembers.length === 0 ? (
              <div className="members-empty-state">
                <div className="empty-icon-wrapper">
                  <Users size={32} />
                </div>
                <h3 className="state-title">{t('members.empty_title')}</h3>
                <p className="state-desc">{t('members.empty_desc')}</p>
                {isAdmin && (
                  <button className="btn btn-primary" onClick={handleOpenInviteModal}>
                    <UserPlus size={16} /> {t('members.invite')}
                  </button>
                )}
              </div>
            ) : (
              <div className="members-table-card">
                <div className="members-table-wrapper">
                  <table className="members-table">
                    <thead>
                      <tr>
                        <th>{t('members.table_member')}</th>
                        <th>{t('members.table_email')}</th>
                        <th>{t('members.table_roles')}</th>
                        <th>{t('members.table_joined')}</th>
                        {isAdmin && <th style={{ textAlign: 'right' }}>{t('members.table_actions')}</th>}
                      </tr>
                    </thead>
                    <tbody>
                      {filteredMembers.map((m) => {
                        const isSelf = m.userId === currentUser?.id;
                        const isTargetOwner = m.roles?.some((r) => r.toLowerCase() === 'owner');

                        return (
                          <tr key={m.userId}>
                            <td>
                              <div className="member-cell">
                                {m.avatarUrl ? (
                                  <img
                                    src={m.avatarUrl}
                                    alt={m.displayName}
                                    className="member-avatar"
                                  />
                                ) : (
                                  <div className="member-avatar">
                                    {m.displayName?.substring(0, 2).toUpperCase() || 'U'}
                                  </div>
                                )}
                                <div className="member-info-col">
                                  <span className="member-name">
                                    {m.displayName}
                                    {isSelf && <span className="you-badge">{t('members.you')}</span>}
                                  </span>
                                </div>
                              </div>
                            </td>
                            <td>{m.email}</td>
                            <td>
                              <div className="roles-group">
                                {m.roles && m.roles.length > 0 ? (
                                  m.roles.map((r, i) => renderRoleBadge(r, i))
                                ) : (
                                  <span className="role-badge member">
                                    <Shield size={12} /> {t('members.role_member')}
                                  </span>
                                )}
                              </div>
                            </td>
                            <td>{new Date(m.joinedAt).toLocaleDateString()}</td>
                            {isAdmin && (
                              <td style={{ textAlign: 'right' }}>
                                <div className="action-buttons">
                                  {isOwner && !isSelf && !isTargetOwner && (
                                    <button
                                      className="action-btn"
                                      onClick={() => handleOpenRoleModal(m)}
                                      title={t('members.change_role')}
                                    >
                                      <UserCog size={15} /> {t('members.change_role')}
                                    </button>
                                  )}

                                  {!isSelf && !isTargetOwner && (
                                    <button
                                      className="action-btn danger"
                                      onClick={() => handleOpenRemoveModal(m)}
                                      title={t('members.remove')}
                                    >
                                      <UserX size={15} />
                                    </button>
                                  )}
                                </div>
                              </td>
                            )}
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {totalMembersCount > pageSize && (
                  <div className="pagination-bar">
                    <div className="pagination-info">
                      {t('members.page')
                        .replace('{current}', String(page))
                        .replace('{total}', String(totalPages))}
                    </div>
                    <div className="pagination-controls">
                      <button
                        className="page-btn"
                        disabled={page <= 1}
                        onClick={() => setPage((prev) => Math.max(1, prev - 1))}
                      >
                        <ChevronLeft size={16} /> {t('members.prev')}
                      </button>
                      <button
                        className="page-btn"
                        disabled={page >= totalPages}
                        onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
                      >
                        {t('members.next')} <ChevronRight size={16} />
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
          </>
        )}

        {/* TAB 2: PENDING INVITATIONS */}
        {activeTab === 'pending' && (
          <>
            {loadingInvitations ? (
              <div className="members-loading-state">
                <div className="spinner"></div>
                <p>{t('members.loading')}</p>
              </div>
            ) : invitationsError ? (
              <div className="members-error-state">
                <div className="error-icon-wrapper">
                  <AlertCircle size={32} />
                </div>
                <h3 className="state-title">{t('members.error_title')}</h3>
                <p className="state-desc">{invitationsError}</p>
                <button className="btn btn-secondary" onClick={loadInvitations}>
                  <RefreshCw size={16} /> {t('members.retry')}
                </button>
              </div>
            ) : filteredInvitations.length === 0 ? (
              <div className="members-empty-state">
                <div className="empty-icon-wrapper">
                  <Mail size={32} />
                </div>
                <h3 className="state-title">{t('invitations.no_pending')}</h3>
                <p className="state-desc">{t('invitations.pending_subtitle')}</p>
                {isAdmin && (
                  <button className="btn btn-primary" onClick={handleOpenInviteModal}>
                    <UserPlus size={16} /> {t('members.invite')}
                  </button>
                )}
              </div>
            ) : (
              <div className="members-table-card">
                <div className="members-table-wrapper">
                  <table className="members-table">
                    <thead>
                      <tr>
                        <th>{t('invitations.table_email')}</th>
                        <th>{t('invitations.table_role')}</th>
                        <th>{t('invitations.table_inviter')}</th>
                        <th>{t('invitations.table_expires')}</th>
                        <th>{t('invitations.table_status')}</th>
                        {isAdmin && <th style={{ textAlign: 'right' }}>{t('members.table_actions')}</th>}
                      </tr>
                    </thead>
                    <tbody>
                      {filteredInvitations.map((i) => (
                        <tr key={i.id}>
                          <td>
                            <div className="member-cell">
                              <div className="member-avatar" style={{ background: 'linear-gradient(135deg, #8B5CF6, #C084FC)' }}>
                                <Mail size={16} />
                              </div>
                              <span className="member-name">{i.email}</span>
                            </div>
                          </td>
                          <td>{renderRoleBadge(i.role || 'member', 0)}</td>
                          <td>{i.inviterName || 'System'}</td>
                          <td>{new Date(i.expiresAt).toLocaleDateString()}</td>
                          <td>
                            <span className="badge badge-warning">
                              <Clock size={11} /> {t('invitations.status_pending')}
                            </span>
                          </td>
                          {isAdmin && (
                            <td style={{ textAlign: 'right' }}>
                              <button
                                className="action-btn danger"
                                onClick={() => handleOpenCancelModal(i)}
                                title={t('invitations.cancel')}
                              >
                                <Trash2 size={15} /> {t('invitations.cancel')}
                              </button>
                            </td>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                {totalInvitationsCount > pageSize && (
                  <div className="pagination-bar">
                    <div className="pagination-info">
                      {t('members.page')
                        .replace('{current}', String(page))
                        .replace('{total}', String(totalPages))}
                    </div>
                    <div className="pagination-controls">
                      <button
                        className="page-btn"
                        disabled={page <= 1}
                        onClick={() => setPage((prev) => Math.max(1, prev - 1))}
                      >
                        <ChevronLeft size={16} /> {t('members.prev')}
                      </button>
                      <button
                        className="page-btn"
                        disabled={page >= totalPages}
                        onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
                      >
                        {t('members.next')} <ChevronRight size={16} />
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
          </>
        )}

        {/* Modal: Invite Member */}
        {currentWorkspace && (
          <InviteMemberModal
            isOpen={showInviteModal}
            onClose={() => setShowInviteModal(false)}
            workspaceId={currentWorkspace.id}
            onSuccess={loadInvitations}
          />
        )}

        {/* Modal: Change Member Role */}
        {showRoleModal && selectedMember && (
          <div className="modal-overlay" onClick={() => setShowRoleModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h3>{t('members.change_role_title')}</h3>
                <button className="btn-ghost" onClick={() => setShowRoleModal(false)}>
                  ✕
                </button>
              </div>
              <form onSubmit={handleChangeRoleSubmit}>
                <div className="modal-body">
                  {roleSuccess && (
                    <div className="badge badge-success" style={{ marginBottom: 12, width: '100%', padding: '10px 14px' }}>
                      {roleSuccess}
                    </div>
                  )}
                  {roleError && (
                    <div className="badge badge-danger" style={{ marginBottom: 12, width: '100%', padding: '10px 14px' }}>
                      {roleError}
                    </div>
                  )}
                  <p style={{ fontSize: 14, color: 'var(--text-muted)', marginBottom: 12 }}>
                    Change role for <strong>{selectedMember.displayName}</strong> ({selectedMember.email})
                  </p>

                  <div className="role-select-grid">
                    <label className={`role-option-card ${targetRole === 'admin' ? 'selected' : ''}`}>
                      <input
                        type="radio"
                        name="memberRole"
                        value="admin"
                        checked={targetRole === 'admin'}
                        onChange={(e) => setTargetRole(e.target.value)}
                      />
                      <div className="role-option-info">
                        <span className="role-option-title">{t('members.role_admin')}</span>
                        <span className="role-option-desc">{t('invitations.role_admin_desc')}</span>
                      </div>
                    </label>

                    <label className={`role-option-card ${targetRole === 'member' ? 'selected' : ''}`}>
                      <input
                        type="radio"
                        name="memberRole"
                        value="member"
                        checked={targetRole === 'member'}
                        onChange={(e) => setTargetRole(e.target.value)}
                      />
                      <div className="role-option-info">
                        <span className="role-option-title">{t('members.role_member')}</span>
                        <span className="role-option-desc">{t('invitations.role_member_desc')}</span>
                      </div>
                    </label>

                    <label className={`role-option-card ${targetRole === 'viewer' ? 'selected' : ''}`}>
                      <input
                        type="radio"
                        name="memberRole"
                        value="viewer"
                        checked={targetRole === 'viewer'}
                        onChange={(e) => setTargetRole(e.target.value)}
                      />
                      <div className="role-option-info">
                        <span className="role-option-title">{t('members.role_viewer')}</span>
                        <span className="role-option-desc">{t('invitations.role_viewer_desc')}</span>
                      </div>
                    </label>
                  </div>
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setShowRoleModal(false)}
                  >
                    {t('members.invite_cancel')}
                  </button>
                  <button type="submit" className="btn btn-primary" disabled={isSubmittingRole}>
                    {isSubmittingRole ? '...' : t('members.change_role')}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Modal: Remove Member Confirmation */}
        {showRemoveModal && memberToRemove && (
          <div className="modal-overlay" onClick={() => setShowRemoveModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h3>{t('members.remove')}</h3>
                <button className="btn-ghost" onClick={() => setShowRemoveModal(false)}>
                  ✕
                </button>
              </div>
              <div className="modal-body">
                {removeError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, width: '100%', padding: '10px 14px' }}>
                    {removeError}
                  </div>
                )}
                <p style={{ fontSize: 14, color: 'var(--text-main)' }}>
                  {t('members.remove_confirm').replace('{name}', memberToRemove.displayName)}
                </p>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowRemoveModal(false)}
                >
                  {t('members.invite_cancel')}
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleConfirmRemove}
                  disabled={isRemoving}
                >
                  {isRemoving ? '...' : t('members.remove')}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Modal: Cancel Invitation Confirmation */}
        {showCancelModal && invitationToCancel && (
          <div className="modal-overlay" onClick={() => setShowCancelModal(false)}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h3>{t('invitations.cancel')}</h3>
                <button className="btn-ghost" onClick={() => setShowCancelModal(false)}>
                  ✕
                </button>
              </div>
              <div className="modal-body">
                {cancelError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, width: '100%', padding: '10px 14px' }}>
                    {cancelError}
                  </div>
                )}
                <p style={{ fontSize: 14, color: 'var(--text-main)' }}>
                  {t('invitations.cancel_confirm').replace('{email}', invitationToCancel.email)}
                </p>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowCancelModal(false)}
                >
                  {t('members.invite_cancel')}
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleConfirmCancelInvitation}
                  disabled={isCancelling}
                >
                  {isCancelling ? '...' : t('invitations.cancel')}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};



