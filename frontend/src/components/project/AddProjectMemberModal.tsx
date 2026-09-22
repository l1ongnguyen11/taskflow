import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { UserPlus, AlertCircle, Check, Search } from 'lucide-react';
import { projectApi } from '../../api/projectApi';
import { workspaceApi } from '../../api/workspaceApi';
import { getApiErrorMessage } from '../../api';
import { useLanguage } from '../../contexts/LanguageContext';
import { WorkspaceMember } from '../../types';

interface AddProjectMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  workspaceId: string;
  existingMemberUserIds: string[];
  onSuccess?: () => void;
}

export const AddProjectMemberModal: React.FC<AddProjectMemberModalProps> = ({
  isOpen,
  onClose,
  projectId,
  workspaceId,
  existingMemberUserIds,
  onSuccess,
}) => {
  const { t } = useLanguage();
  const navigate = useNavigate();

  const [workspaceMembers, setWorkspaceMembers] = useState<WorkspaceMember[]>([]);
  const [loadingWorkspaceMembers, setLoadingWorkspaceMembers] = useState<boolean>(false);

  const [selectedUserId, setSelectedUserId] = useState<string>('');
  const [selectedRole, setSelectedRole] = useState<string>('member');
  const [searchQuery, setSearchQuery] = useState<string>('');

  const [errors, setErrors] = useState<string[]>([]);
  const [successMsg, setSuccessMsg] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  useEffect(() => {
    if (isOpen && workspaceId) {
      setErrors([]);
      setSuccessMsg('');
      setSelectedUserId('');
      setSelectedRole('member');
      setSearchQuery('');

      fetchWorkspaceMembers();
    }
  }, [isOpen, workspaceId]);

  const fetchWorkspaceMembers = async () => {
    setLoadingWorkspaceMembers(true);
    try {
      const res = await workspaceApi.getMembers(workspaceId, '', 100, 0);
      if (res.success && res.data) {
        setWorkspaceMembers(res.data);
      }
    } catch (err: any) {
      console.error('Failed to fetch workspace members:', err);
    } finally {
      setLoadingWorkspaceMembers(false);
    }
  };

  if (!isOpen) return null;

  // Available workspace members who aren't already project members
  const availableMembers = workspaceMembers.filter(
    (wm) => !existingMemberUserIds.includes(wm.userId)
  );

  const filteredMembers = availableMembers.filter(
    (wm) =>
      wm.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      wm.email.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);
    setSuccessMsg('');

    if (!selectedUserId) {
      setErrors([t('proj_members.select_member_required') || 'Please select a member to add.']);
      return;
    }

    setIsSubmitting(true);

    try {
      const res = await projectApi.addProjectMember(projectId, {
        userId: selectedUserId,
        role: selectedRole,
      });

      if (res.success) {
        setSuccessMsg(t('proj_members.add_success') || 'Member added to project successfully!');
        if (onSuccess) onSuccess();
        setTimeout(() => {
          onClose();
        }, 1200);
      } else {
        const apiErrs = res.errors && res.errors.length > 0 ? res.errors : [res.message || t('proj_members.add_error')];
        setErrors(apiErrs);
      }
    } catch (err: any) {
      const respData = err.response?.data;
      let errList: string[] = [];
      if (Array.isArray(respData?.errors) && respData.errors.length > 0) {
        errList = respData.errors;
      } else {
        const msg = getApiErrorMessage(err) || t('proj_members.add_error') || 'Failed to add member.';
        errList = [msg];
      }
      setErrors(errList);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 520 }}>
        {/* Header */}
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <UserPlus size={20} style={{ color: 'var(--primary)' }} />
            <h3>{t('proj_members.add_title') || 'Add Member to Project'}</h3>
          </div>
          <button className="btn-ghost" onClick={onClose} disabled={isSubmitting}>
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {/* Success Alert */}
            {successMsg && (
              <div
                className="badge badge-success"
                style={{
                  marginBottom: 16,
                  width: '100%',
                  padding: '10px 14px',
                  borderRadius: 8,
                  fontSize: 13,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 8,
                }}
              >
                <Check size={16} />
                <span>{successMsg}</span>
              </div>
            )}

            {/* Error Alerts */}
            {errors.length > 0 && (
              <div
                className="badge badge-danger"
                style={{
                  marginBottom: 16,
                  width: '100%',
                  padding: '10px 14px',
                  borderRadius: 8,
                  fontSize: 13,
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'flex-start',
                  gap: 6,
                }}
              >
                {errors.map((err, idx) => (
                  <div key={idx} style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{err}</span>
                  </div>
                ))}
              </div>
            )}

            {/* Member Selection */}
            <div className="form-group">
              <label className="form-label">{t('proj_members.select_member') || 'Select Workspace Member'}</label>
              {loadingWorkspaceMembers ? (
                <div style={{ fontSize: 13, color: 'var(--text-muted)', padding: '8px 0' }}>
                  {t('proj_members.loading_ws_members') || 'Loading workspace members...'}
                </div>
              ) : availableMembers.length === 0 ? (
                <div
                  style={{
                    padding: '16px',
                    borderRadius: 'var(--radius-md)',
                    backgroundColor: 'var(--bg-surface-hover)',
                    textAlign: 'center',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    gap: 12,
                    border: '1px dashed var(--border-color)',
                  }}
                >
                  <div style={{ fontSize: 13, color: 'var(--text-muted)', lineHeight: 1.5 }}>
                    {t('proj_members.all_added') ||
                      'Tất cả thành viên trong Không gian làm việc hiện đã có mặt trong dự án này.'}
                    <br />
                    Hãy mời thêm thành viên mới vào <strong>Không gian làm việc</strong> trước khi thêm họ vào dự án.
                  </div>
                  <button
                    type="button"
                    className="btn btn-secondary btn-sm"
                    onClick={() => {
                      onClose();
                      navigate(`/workspaces/${workspaceId}/members`);
                    }}
                    style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}
                  >
                    <UserPlus size={14} />
                    <span>Quy trình mời thành viên vào Không gian làm việc</span>
                  </button>
                </div>
              ) : (
                <>
                  <div className="search-box" style={{ marginBottom: 10 }}>
                    <Search size={14} className="search-icon" />
                    <input
                      type="text"
                      className="search-input"
                      placeholder={t('proj_members.search_ws_member') || 'Search workspace members...'}
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                    />
                  </div>

                  <div
                    style={{
                      maxHeight: 180,
                      overflowY: 'auto',
                      border: '1px solid var(--border-color)',
                      borderRadius: 'var(--radius-sm)',
                      padding: 6,
                    }}
                  >
                    {filteredMembers.map((member) => (
                      <label
                        key={member.userId}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 10,
                          padding: '8px 10px',
                          borderRadius: 6,
                          cursor: 'pointer',
                          backgroundColor:
                            selectedUserId === member.userId ? 'var(--bg-surface-hover)' : 'transparent',
                        }}
                      >
                        <input
                          type="radio"
                          name="selectedProjectUser"
                          value={member.userId}
                          checked={selectedUserId === member.userId}
                          onChange={() => setSelectedUserId(member.userId)}
                          disabled={isSubmitting}
                        />
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ fontWeight: 600, fontSize: 13, color: 'var(--text-main)' }}>
                            {member.displayName}
                          </div>
                          <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{member.email}</div>
                        </div>
                      </label>
                    ))}
                  </div>
                </>
              )}
            </div>

            {/* Project Role Selection */}
            <div className="form-group" style={{ marginTop: 16 }}>
              <label className="form-label">{t('proj_members.select_role') || 'Project Role'}</label>
              <div style={{ display: 'flex', gap: 10, marginTop: 4 }}>
                {['lead', 'member', 'viewer'].map((roleKey) => (
                  <label
                    key={roleKey}
                    className={`role-option-card ${selectedRole === roleKey ? 'selected' : ''}`}
                    style={{ flex: 1, padding: '8px 12px', cursor: 'pointer', textAlign: 'center' }}
                  >
                    <input
                      type="radio"
                      name="projectMemberRole"
                      value={roleKey}
                      checked={selectedRole === roleKey}
                      onChange={(e) => setSelectedRole(e.target.value)}
                      disabled={isSubmitting}
                    />
                    <span style={{ fontSize: 13, fontWeight: 600, textTransform: 'capitalize' }}>
                      {roleKey}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="modal-footer">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={isSubmitting}
            >
              {t('modal.ws_cancel')}
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting || !selectedUserId || availableMembers.length === 0}
            >
              {isSubmitting ? '...' : t('proj_members.add_btn') || 'Add to Project'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
