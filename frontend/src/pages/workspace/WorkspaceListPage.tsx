import React, { useState, useEffect, useCallback } from 'react';
import { MainLayout } from '../../components/layout/MainLayout';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { useNavigate } from 'react-router-dom';
import { Building2, ArrowRight, Users, AlertTriangle, RefreshCw, Plus, Loader2, Mail, Check, X } from 'lucide-react';
import { workspaceApi } from '../../api/workspaceApi';
import { getApiErrorMessage } from '../../api';
import { WorkspaceInvitation } from '../../types';
import './Workspace.css';

export const WorkspaceListPage: React.FC = () => {
  const {
    workspaces,
    currentWorkspace,
    selectWorkspace,
    isLoadingWorkspaces,
    workspaceError,
    refreshWorkspaces,
    clearWorkspaceError,
    createWorkspace,
  } = useWorkspace();
  const { t } = useLanguage();
  const navigate = useNavigate();

  // Create Workspace Modal State
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState('');
  const [formSuccess, setFormSuccess] = useState('');

  // My Invitations State
  const [myInvitations, setMyInvitations] = useState<WorkspaceInvitation[]>([]);
  const [actionLoadingToken, setActionLoadingToken] = useState<string | null>(null);
  const [invitationFeedback, setInvitationFeedback] = useState<{ message: string; isError: boolean } | null>(null);

  const fetchMyInvitations = useCallback(async () => {
    try {
      const res = await workspaceApi.getMyInvitations();
      if (res.success && res.data) {
        setMyInvitations(res.data.filter((inv) => inv.status?.toLowerCase() === 'pending'));
      }
    } catch (err) {
      console.error('Failed to fetch my invitations:', err);
    }
  }, []);

  useEffect(() => {
    fetchMyInvitations();
  }, [fetchMyInvitations]);

  const handleAcceptInvitation = async (invitation: WorkspaceInvitation) => {
    setActionLoadingToken(invitation.token);
    setInvitationFeedback(null);
    try {
      const res = await workspaceApi.acceptInvitation(invitation.token);
      if (res.success) {
        setInvitationFeedback({
          message: t('invitations.accept_success') || 'Invitation accepted! Workspace added.',
          isError: false,
        });
        await refreshWorkspaces();
        await fetchMyInvitations();
      } else {
        setInvitationFeedback({
          message: res.message || t('invitations.accept_error'),
          isError: true,
        });
      }
    } catch (err: any) {
      const errorMsg = getApiErrorMessage(err) || t('invitations.accept_error');
      setInvitationFeedback({ message: errorMsg, isError: true });
    } finally {
      setActionLoadingToken(null);
    }
  };

  const handleRejectInvitation = async (invitation: WorkspaceInvitation) => {
    setActionLoadingToken(invitation.token);
    setInvitationFeedback(null);
    try {
      const res = await workspaceApi.rejectInvitation(invitation.token);
      if (res.success) {
        setInvitationFeedback({
          message: t('invitations.reject_success') || 'Invitation rejected.',
          isError: false,
        });
        await fetchMyInvitations();
      } else {
        setInvitationFeedback({
          message: res.message || t('invitations.reject_error'),
          isError: true,
        });
      }
    } catch (err: any) {
      const errorMsg = getApiErrorMessage(err) || t('invitations.reject_error');
      setInvitationFeedback({ message: errorMsg, isError: true });
    } finally {
      setActionLoadingToken(null);
    }
  };

  const handleSelect = (wsId: string) => {
    selectWorkspace(wsId);
    navigate(`/workspaces/${wsId}/dashboard`);
  };

  const handleRetry = async () => {
    clearWorkspaceError();
    await refreshWorkspaces();
    await fetchMyInvitations();
  };

  const slugify = (text: string) => {
    return text
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/[^a-z0-9 -]/g, '')
      .trim()
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-');
  };

  const handleCreateWorkspaceSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError('');
    setFormSuccess('');

    // Frontend validation for required field
    if (!name.trim()) {
      setFormError(t('modal.ws_name_required') || 'Workspace name is required.');
      return;
    }

    setIsSubmitting(true);
    const slug = slugify(name);

    try {
      const newWs = await createWorkspace({
        name: name.trim(),
        slug: slug || 'workspace-' + Date.now(),
        description: description.trim() || undefined,
      });

      setFormSuccess(t('workspace.create_success') || 'Workspace created successfully!');
      
      // Reset fields
      setName('');
      setDescription('');

      // Auto-select and navigate after brief success feedback
      setTimeout(() => {
        setIsSubmitting(false);
        setShowCreateModal(false);
        setFormSuccess('');
        handleSelect(newWs.id);
      }, 1000);
    } catch (err: any) {
      setIsSubmitting(false);
      const apiErrors = err.response?.data?.errors;
      let errMsg = t('modal.ws_error_generic');
      if (Array.isArray(apiErrors) && apiErrors.length > 0) {
        errMsg = apiErrors.join(' ');
      } else if (err.response?.data?.message) {
        errMsg = err.response.data.message;
      } else if (err.message) {
        errMsg = err.message;
      }
      setFormError(errMsg);
    }
  };

  return (
    <MainLayout title={t('workspace.title')}>
      <div className="workspace-page-header">
        <div>
          <h2>{t('workspace.title')}</h2>
          <p className="subtitle">{t('workspace.subtitle')}</p>
        </div>
        <button
          className="btn btn-primary"
          onClick={() => {
            setFormError('');
            setFormSuccess('');
            setShowCreateModal(true);
          }}
        >
          <Plus size={18} />
          {t('sidebar.create_workspace')}
        </button>
      </div>

      {/* My Invitations Section */}
      {myInvitations.length > 0 && (
        <div className="invitations-section card" style={{ marginBottom: 24, padding: '20px' }}>
          <div className="invitations-section-header" style={{ marginBottom: 16 }}>
            <h3 style={{ fontSize: 16, fontWeight: 700, display: 'flex', alignItems: 'center', gap: 8, color: 'var(--text-main)' }}>
              <Mail size={18} style={{ color: 'var(--primary)' }} />
              {t('invitations.my_invitations')} ({myInvitations.length})
            </h3>
            <p className="subtitle" style={{ fontSize: 13, marginTop: 4 }}>
              {t('invitations.my_invitations_subtitle')}
            </p>
          </div>

          {invitationFeedback && (
            <div
              className={`badge ${invitationFeedback.isError ? 'badge-danger' : 'badge-success'}`}
              style={{ marginBottom: 16, display: 'block', padding: '8px 12px' }}
            >
              {invitationFeedback.message}
            </div>
          )}

          <div className="invitations-list" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            {myInvitations.map((inv) => (
              <div
                key={inv.id}
                className="invitation-item-card"
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '14px 18px',
                  background: 'var(--bg-subtle, #f8fafc)',
                  borderRadius: 'var(--radius, 8px)',
                  border: '1px solid var(--border-color-light, #e2e8f0)',
                  gap: '16px',
                  flexWrap: 'wrap',
                }}
              >
                <div className="invitation-info" style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
                  <div className="ws-avatar" style={{ width: 38, height: 38, fontSize: 14 }}>
                    {(inv.workspaceName || 'WS').substring(0, 2).toUpperCase()}
                  </div>
                  <div>
                    <div style={{ fontWeight: 600, fontSize: 14, color: 'var(--text-main)' }}>
                      {inv.workspaceName || inv.workspaceSlug || 'Workspace'}
                    </div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>
                      {inv.inviterName ? `${t('invitations.table_inviter')}: ${inv.inviterName} • ` : ''}
                      {t('invitations.table_role')}:{' '}
                      <span style={{ textTransform: 'capitalize', fontWeight: 500 }}>{inv.role || 'Member'}</span>
                    </div>
                  </div>
                </div>

                <div className="invitation-actions" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <button
                    type="button"
                    className="btn btn-secondary"
                    style={{ padding: '6px 14px', fontSize: 13, display: 'inline-flex', alignItems: 'center', gap: 6 }}
                    disabled={actionLoadingToken === inv.token}
                    onClick={() => handleRejectInvitation(inv)}
                  >
                    {actionLoadingToken === inv.token ? (
                      <Loader2 size={14} className="ws-spinner-icon" style={{ animation: 'wsSpinner 0.8s linear infinite' }} />
                    ) : (
                      <X size={14} />
                    )}
                    {t('invitations.reject')}
                  </button>
                  <button
                    type="button"
                    className="btn btn-primary"
                    style={{ padding: '6px 14px', fontSize: 13, display: 'inline-flex', alignItems: 'center', gap: 6 }}
                    disabled={actionLoadingToken === inv.token}
                    onClick={() => handleAcceptInvitation(inv)}
                  >
                    {actionLoadingToken === inv.token ? (
                      <Loader2 size={14} className="ws-spinner-icon" style={{ animation: 'wsSpinner 0.8s linear infinite' }} />
                    ) : (
                      <Check size={14} />
                    )}
                    {t('invitations.accept')}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Loading State */}
      {isLoadingWorkspaces ? (
        <div className="ws-loading-state">
          <div className="ws-loading-spinner" />
          <p>{t('workspace.loading')}</p>
        </div>
      ) : workspaceError ? (
        /* Error State */
        <div className="ws-error-state card">
          <div className="ws-error-icon-wrapper">
            <AlertTriangle size={40} />
          </div>
          <h3>{t('workspace.error_title')}</h3>
          <p>{workspaceError}</p>
          <button className="btn btn-primary" onClick={handleRetry}>
            <RefreshCw size={16} />
            {t('workspace.retry')}
          </button>
        </div>
      ) : workspaces.length === 0 ? (
        /* Empty State */
        <div className="empty-state card">
          <Building2 size={48} className="empty-icon" />
          <h3>{t('workspace.empty_title')}</h3>
          <p>{t('workspace.empty_desc')}</p>
          <button
            className="btn btn-primary"
            style={{ marginTop: 12 }}
            onClick={() => {
              setFormError('');
              setFormSuccess('');
              setShowCreateModal(true);
            }}
          >
            <Plus size={18} />
            {t('sidebar.create_workspace')}
          </button>
        </div>
      ) : (
        /* Workspace Grid */
        <div className="workspace-grid">
          {workspaces.map((ws) => {
            const isActive = currentWorkspace?.id === ws.id;
            return (
              <div
                key={ws.id}
                className={`card card-hover workspace-card ${isActive ? 'ws-active' : ''}`}
                onClick={() => handleSelect(ws.id)}
              >
                {isActive && (
                  <span className="ws-active-badge">{t('workspace.active')}</span>
                )}

                <div className="ws-card-header">
                  <div className="ws-avatar">
                    {ws.name.substring(0, 2).toUpperCase()}
                  </div>
                  <div className="ws-meta">
                    <h3 className="ws-title">{ws.name}</h3>
                    <span className="ws-slug">@{ws.slug}</span>
                  </div>
                </div>

                <p className="ws-desc">
                  {ws.description || t('workspace.no_description')}
                </p>

                <div className="ws-card-footer">
                  <div className="ws-footer-info">
                    {ws.memberCount != null && ws.memberCount > 0 && (
                      <span className="ws-member-count">
                        <Users size={13} />
                        {ws.memberCount} {ws.memberCount === 1 ? t('workspace.member') : t('workspace.members')}
                      </span>
                    )}
                    <span className="ws-date">
                      {t('workspace.created')} {new Date(ws.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                  <span className="btn-link">
                    {t('workspace.open')} <ArrowRight size={14} />
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal Create Workspace */}
      {showCreateModal && (
        <div className="modal-overlay" onClick={() => !isSubmitting && setShowCreateModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{t('modal.create_workspace')}</h3>
              <button
                className="btn-ghost"
                onClick={() => !isSubmitting && setShowCreateModal(false)}
                disabled={isSubmitting}
              >
                ✕
              </button>
            </div>
            <form onSubmit={handleCreateWorkspaceSubmit}>
              <div className="modal-body">
                {formSuccess && (
                  <div className="badge badge-success" style={{ marginBottom: 12, display: 'block', padding: '8px 12px' }}>
                    {formSuccess}
                  </div>
                )}
                {formError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'block', padding: '8px 12px' }}>
                    {formError}
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_name')} *</label>
                  <input
                    type="text"
                    required
                    disabled={isSubmitting}
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder={t('modal.ws_name_placeholder')}
                    className="form-input"
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_description')}</label>
                  <textarea
                    disabled={isSubmitting}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder={t('modal.ws_desc_placeholder')}
                    className="form-input"
                    rows={3}
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowCreateModal(false)}
                  disabled={isSubmitting}
                >
                  {t('modal.ws_cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                  {isSubmitting ? (
                    <>
                      <Loader2 size={16} className="ws-spinner-icon" style={{ animation: 'wsSpinner 0.8s linear infinite' }} />
                      {t('workspace.creating') || 'Creating...'}
                    </>
                  ) : (
                    t('modal.ws_create')
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
