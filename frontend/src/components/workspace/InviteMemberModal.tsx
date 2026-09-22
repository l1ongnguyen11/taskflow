import React, { useState } from 'react';
import { Mail, AlertCircle } from 'lucide-react';
import { workspaceApi } from '../../api/workspaceApi';
import { useLanguage } from '../../contexts/LanguageContext';

interface InviteMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  workspaceId: string;
  onSuccess?: () => void;
}

export const InviteMemberModal: React.FC<InviteMemberModalProps> = ({
  isOpen,
  onClose,
  workspaceId,
  onSuccess,
}) => {
  const { t } = useLanguage();
  const [inviteEmail, setInviteEmail] = useState<string>('');
  const [inviteRole, setInviteRole] = useState<string>('member');
  const [inviteMsg, setInviteMsg] = useState<string>('');
  const [inviteErrors, setInviteErrors] = useState<string[]>([]);
  const [isInviting, setIsInviting] = useState<boolean>(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workspaceId || !inviteEmail.trim()) return;

    setInviteMsg('');
    setInviteErrors([]);
    setIsInviting(true);

    try {
      const res = await workspaceApi.inviteMember(workspaceId, {
        email: inviteEmail.trim(),
        role: inviteRole,
      });

      if (res.success) {
        setInviteMsg(`${t('members.invite_success')} ${inviteEmail.trim()} (${inviteRole})!`);
        setInviteEmail('');
        if (onSuccess) {
          onSuccess();
        }
        setTimeout(() => {
          onClose();
          setInviteMsg('');
        }, 1800);
      } else {
        const errList = res.errors && res.errors.length > 0 ? res.errors : [res.message || t('members.invite_error')];
        setInviteErrors(errList);
      }
    } catch (err: any) {
      const respData = err.response?.data;
      const errList = respData?.errors && respData.errors.length > 0
        ? respData.errors
        : [respData?.message || err.message || t('members.invite_error')];
      setInviteErrors(errList);
    } finally {
      setIsInviting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>{t('members.invite_title')}</h3>
          <button className="btn-ghost" onClick={onClose}>
            ✕
          </button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {/* Success Alert */}
            {inviteMsg && (
              <div className="badge badge-success" style={{ marginBottom: 16, width: '100%', padding: '10px 14px', borderRadius: 8, fontSize: 13 }}>
                {inviteMsg}
              </div>
            )}

            {/* Validation / Backend Error Alerts */}
            {inviteErrors.length > 0 && (
              <div className="badge badge-danger" style={{ marginBottom: 16, width: '100%', padding: '10px 14px', borderRadius: 8, fontSize: 13, flexDirection: 'column', alignItems: 'flex-start', gap: 4 }}>
                {inviteErrors.map((err, idx) => (
                  <div key={idx} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{err}</span>
                  </div>
                ))}
              </div>
            )}

            {/* Email Input */}
            <div className="form-group">
              <label className="form-label">{t('members.invite_email_label')}</label>
              <input
                type="email"
                required
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                placeholder={t('members.invite_email_placeholder')}
                className="form-input"
                disabled={isInviting}
              />
            </div>

            {/* Role Select Radio Options */}
            <div className="form-group" style={{ marginTop: 16 }}>
              <label className="form-label">{t('invitations.select_role_label')}</label>
              <div className="role-select-grid">
                <label className={`role-option-card ${inviteRole === 'admin' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="inviteRoleModal"
                    value="admin"
                    checked={inviteRole === 'admin'}
                    onChange={(e) => setInviteRole(e.target.value)}
                    disabled={isInviting}
                  />
                  <div className="role-option-info">
                    <span className="role-option-title">{t('members.role_admin')}</span>
                    <span className="role-option-desc">{t('invitations.role_admin_desc')}</span>
                  </div>
                </label>

                <label className={`role-option-card ${inviteRole === 'member' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="inviteRoleModal"
                    value="member"
                    checked={inviteRole === 'member'}
                    onChange={(e) => setInviteRole(e.target.value)}
                    disabled={isInviting}
                  />
                  <div className="role-option-info">
                    <span className="role-option-title">{t('members.role_member')}</span>
                    <span className="role-option-desc">{t('invitations.role_member_desc')}</span>
                  </div>
                </label>

                <label className={`role-option-card ${inviteRole === 'viewer' ? 'selected' : ''}`}>
                  <input
                    type="radio"
                    name="inviteRoleModal"
                    value="viewer"
                    checked={inviteRole === 'viewer'}
                    onChange={(e) => setInviteRole(e.target.value)}
                    disabled={isInviting}
                  />
                  <div className="role-option-info">
                    <span className="role-option-title">{t('members.role_viewer')}</span>
                    <span className="role-option-desc">{t('invitations.role_viewer_desc')}</span>
                  </div>
                </label>
              </div>
            </div>
          </div>
          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={isInviting}
            >
              {t('members.invite_cancel')}
            </button>
            <button type="submit" className="btn btn-primary" disabled={isInviting || !inviteEmail.trim()}>
              <Mail size={16} /> {isInviting ? '...' : t('members.invite_send')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
