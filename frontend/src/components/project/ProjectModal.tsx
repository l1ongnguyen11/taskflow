import React, { useState, useEffect } from 'react';
import { AlertCircle, FolderKanban, Key, Check } from 'lucide-react';
import { projectApi } from '../../api/projectApi';
import { getApiErrorMessage } from '../../api';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { Project } from '../../types';

interface ProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  workspaceId: string;
  projectToEdit?: Project | null;
  onSuccess?: () => void;
}

export const ProjectModal: React.FC<ProjectModalProps> = ({
  isOpen,
  onClose,
  workspaceId,
  projectToEdit,
  onSuccess,
}) => {
  const { t } = useLanguage();
  const { refreshProjects } = useWorkspace();

  const isEditMode = !!projectToEdit;

  // Form State
  const [name, setName] = useState<string>('');
  const [key, setKey] = useState<string>('');
  const [description, setDescription] = useState<string>('');
  const [isArchived, setIsArchived] = useState<boolean>(false);
  const [isKeyManuallyEdited, setIsKeyManuallyEdited] = useState<boolean>(false);

  // UI State
  const [errors, setErrors] = useState<string[]>([]);
  const [successMsg, setSuccessMsg] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  // Reset or populate state when modal opens/changes
  useEffect(() => {
    if (isOpen) {
      setErrors([]);
      setSuccessMsg('');
      setIsSubmitting(false);

      if (projectToEdit) {
        setName(projectToEdit.name || '');
        setKey(projectToEdit.key || '');
        setDescription(projectToEdit.description || '');
        setIsArchived(projectToEdit.isArchived || false);
        setIsKeyManuallyEdited(true);
      } else {
        setName('');
        setKey('');
        setDescription('');
        setIsArchived(false);
        setIsKeyManuallyEdited(false);
      }
    }
  }, [isOpen, projectToEdit]);

  if (!isOpen) return null;

  // Sanitize key string for project key validation (uppercase alphanumeric only)
  const sanitizeKey = (text: string): string => {
    return text
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, '')
      .substring(0, 10);
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setName(val);

    // Auto-generate key if creating new project and user hasn't manually edited key
    if (!isEditMode && !isKeyManuallyEdited) {
      const generatedKey = sanitizeKey(
        val
          .trim()
          .split(/\s+/)
          .map((w) => w[0])
          .join('') || val
      );
      setKey(generatedKey);
    }
  };

  const handleKeyChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setIsKeyManuallyEdited(true);
    setKey(sanitizeKey(e.target.value));
  };

  // Client-side validation before submission
  const validateForm = (): boolean => {
    const errs: string[] = [];

    if (!name.trim()) {
      errs.push(t('modal.proj_name_required') || 'Project name is required.');
    } else if (name.trim().length > 100) {
      errs.push(t('modal.proj_name_max') || 'Project name must not exceed 100 characters.');
    }

    if (!isEditMode) {
      if (!key.trim()) {
        errs.push(t('modal.proj_key_required') || 'Project key is required.');
      } else if (key.trim().length < 2 || key.trim().length > 10) {
        errs.push(t('modal.proj_key_length') || 'Project key must be between 2 and 10 characters.');
      } else if (!/^[A-Z0-9]+$/.test(key.trim())) {
        errs.push(
          t('modal.proj_key_invalid') ||
            'Project key must contain only uppercase alphanumeric characters (A-Z, 0-9).'
        );
      }
    }

    if (errs.length > 0) {
      setErrors(errs);
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);
    setSuccessMsg('');

    if (!validateForm()) return;

    setIsSubmitting(true);

    try {
      if (isEditMode && projectToEdit) {
        // 1. Update project details (Name, Description)
        const res = await projectApi.updateProject(projectToEdit.id, {
          name: name.trim(),
          description: description.trim() || undefined,
        });

        if (!res.success) {
          const apiErrs = res.errors && res.errors.length > 0 ? res.errors : [res.message || t('modal.proj_error')];
          setErrors(apiErrs);
          setIsSubmitting(false);
          return;
        }

        // 2. Handle Status change (Active <-> Archived)
        if (isArchived !== projectToEdit.isArchived) {
          if (isArchived) {
            await projectApi.archiveProject(projectToEdit.id);
          } else {
            await projectApi.restoreProject(projectToEdit.id);
          }
        }

        setSuccessMsg(t('modal.proj_update_success') || 'Project updated successfully!');
      } else {
        // Create new project
        const res = await projectApi.createProject(workspaceId, {
          name: name.trim(),
          key: key.trim(),
          description: description.trim() || undefined,
        });

        if (!res.success) {
          const apiErrs = res.errors && res.errors.length > 0 ? res.errors : [res.message || t('modal.proj_error')];
          setErrors(apiErrs);
          setIsSubmitting(false);
          return;
        }

        // If initial status selected was Archived
        if (isArchived && res.data?.id) {
          await projectApi.archiveProject(res.data.id);
        }

        setSuccessMsg(t('modal.proj_create_success') || 'Project created successfully!');
      }

      await refreshProjects();
      if (onSuccess) {
        onSuccess();
      }

      setTimeout(() => {
        onClose();
      }, 1200);
    } catch (err: any) {
      const respData = err.response?.data;
      let errList: string[] = [];
      if (Array.isArray(respData?.errors) && respData.errors.length > 0) {
        errList = respData.errors;
      } else {
        const msg = getApiErrorMessage(err) || t('modal.proj_error_generic') || 'An error occurred. Please try again.';
        errList = [msg];
      }
      setErrors(errList);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <FolderKanban size={20} style={{ color: 'var(--primary)' }} />
            <h3>{isEditMode ? t('modal.edit_project') : t('modal.create_project')}</h3>
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

            {/* Project Name */}
            <div className="form-group">
              <label className="form-label">{t('modal.proj_name')} *</label>
              <input
                type="text"
                required
                maxLength={100}
                value={name}
                onChange={handleNameChange}
                placeholder={t('modal.proj_name_placeholder')}
                className="form-input"
                disabled={isSubmitting}
              />
            </div>

            {/* Project Key */}
            <div className="form-group" style={{ marginTop: 14 }}>
              <label className="form-label">
                {t('modal.proj_key')} *
              </label>
              <input
                type="text"
                required
                maxLength={10}
                value={key}
                onChange={handleKeyChange}
                placeholder={t('modal.proj_key_placeholder')}
                className="form-input"
                disabled={isSubmitting || isEditMode}
                style={{ textTransform: 'uppercase', letterSpacing: '1px', fontWeight: 600 }}
              />
              <span style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px', display: 'block' }}>
                {isEditMode ? t('modal.proj_key_readonly') : t('modal.proj_key_hint')}
              </span>
            </div>

            {/* Project Description */}
            <div className="form-group" style={{ marginTop: 14 }}>
              <label className="form-label">{t('modal.proj_description')}</label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t('modal.proj_desc_placeholder')}
                className="form-input"
                rows={3}
                disabled={isSubmitting}
              />
            </div>

            {/* Project Status */}
            <div className="form-group" style={{ marginTop: 14 }}>
              <label className="form-label">{t('modal.proj_status')}</label>
              <div style={{ display: 'flex', gap: 12, marginTop: 4 }}>
                <label
                  className={`role-option-card ${!isArchived ? 'selected' : ''}`}
                  style={{ flex: 1, padding: '10px 14px', cursor: 'pointer' }}
                >
                  <input
                    type="radio"
                    name="projectStatus"
                    checked={!isArchived}
                    onChange={() => setIsArchived(false)}
                    disabled={isSubmitting}
                  />
                  <div className="role-option-info">
                    <span className="role-option-title" style={{ color: '#10B981', fontWeight: 600 }}>
                      ● {t('modal.proj_status_active')}
                    </span>
                  </div>
                </label>

                <label
                  className={`role-option-card ${isArchived ? 'selected' : ''}`}
                  style={{ flex: 1, padding: '10px 14px', cursor: 'pointer' }}
                >
                  <input
                    type="radio"
                    name="projectStatus"
                    checked={isArchived}
                    onChange={() => setIsArchived(true)}
                    disabled={isSubmitting}
                  />
                  <div className="role-option-info">
                    <span className="role-option-title" style={{ color: '#F59E0B', fontWeight: 600 }}>
                      ● {t('modal.proj_status_archived')}
                    </span>
                  </div>
                </label>
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
              {t('modal.proj_cancel')}
            </button>
            <button type="submit" className="btn btn-primary" disabled={isSubmitting || !name.trim() || (!isEditMode && !key.trim())}>
              {isSubmitting
                ? '...'
                : isEditMode
                ? t('modal.proj_save')
                : t('modal.proj_create')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
