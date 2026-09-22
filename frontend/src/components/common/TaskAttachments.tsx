import React, { useEffect, useState, useCallback, useRef } from 'react';
import { fileApi, getApiErrorMessage } from '../../api';
import { TaskAttachmentItem } from '../../types';
import { useLanguage } from '../../contexts/LanguageContext';
import { useAuth } from '../../contexts/AuthContext';
import {
  UploadCloud,
  File as FileIcon,
  FileImage,
  FileText,
  FileSpreadsheet,
  FileArchive,
  Download,
  Trash2,
  Loader2,
  AlertCircle,
  Clock,
  CheckCircle2,
  Link2,
} from 'lucide-react';
import './TaskAttachments.css';

const MAX_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB
const ALLOWED_EXTENSIONS = ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.png', '.jpg', '.jpeg', '.gif', '.zip', '.txt'];

export interface TaskAttachmentsProps {
  taskId: string;
  onCountChange?: (count: number) => void;
}

function formatFileSize(bytes: number): string {
  if (bytes <= 0) return '0 B';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getFileCategory(filename: string): { icon: React.ReactNode; className: string } {
  const ext = filename.substring(filename.lastIndexOf('.')).toLowerCase();
  if (['.png', '.jpg', '.jpeg', '.gif'].includes(ext)) {
    return { icon: <FileImage size={18} />, className: 'image' };
  }
  if (['.pdf'].includes(ext)) {
    return { icon: <FileText size={18} />, className: 'pdf' };
  }
  if (['.doc', '.docx'].includes(ext)) {
    return { icon: <FileText size={18} />, className: 'doc' };
  }
  if (['.xls', '.xlsx'].includes(ext)) {
    return { icon: <FileSpreadsheet size={18} />, className: 'sheet' };
  }
  if (['.zip'].includes(ext)) {
    return { icon: <FileArchive size={18} />, className: 'archive' };
  }
  return { icon: <FileIcon size={18} />, className: 'default' };
}

export const TaskAttachments: React.FC<TaskAttachmentsProps> = ({ taskId, onCountChange }) => {
  const { t, locale } = useLanguage();
  const { user } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [attachments, setAttachments] = useState<TaskAttachmentItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);

  // Uploading state
  const [isUploading, setIsUploading] = useState<boolean>(false);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [currentUploadFileName, setCurrentUploadFileName] = useState<string>('');
  const [isDragActive, setIsDragActive] = useState<boolean>(false);

  // Deleting attachment state
  const [deletingId, setDeletingId] = useState<string | null>(null);

  // Attach existing file state
  const [showExistingForm, setShowExistingForm] = useState<boolean>(false);
  const [existingFileId, setExistingFileId] = useState<string>('');
  const [isAttachingExisting, setIsAttachingExisting] = useState<boolean>(false);

  const fetchAttachments = useCallback(async () => {
    if (!taskId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await fileApi.getTaskAttachments(taskId);
      if (res.success && res.data) {
        setAttachments(res.data);
        if (onCountChange) onCountChange(res.data.length);
      } else {
        setError(res.message || 'Failed to load attachments.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to load attachments.');
    } finally {
      setLoading(false);
    }
  }, [taskId, onCountChange]);

  useEffect(() => {
    fetchAttachments();
  }, [fetchAttachments]);

  const validateFile = (file: File): string | null => {
    if (file.size > MAX_SIZE_BYTES) {
      return t('file.error_size');
    }
    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!ext || !ALLOWED_EXTENSIONS.includes(ext)) {
      return t('file.error_extension');
    }
    return null;
  };

  const handleFileUpload = async (file: File) => {
    setUploadError(null);
    const err = validateFile(file);
    if (err) {
      setUploadError(err);
      return;
    }

    setIsUploading(true);
    setUploadProgress(0);
    setCurrentUploadFileName(file.name);

    try {
      // 1. Upload File
      const uploadRes = await fileApi.uploadFile(file, (percent) => {
        setUploadProgress(percent);
      });

      if (!uploadRes.success || !uploadRes.data) {
        throw new Error(uploadRes.message || 'Upload failed.');
      }

      // 2. Attach File to Task
      const attachRes = await fileApi.attachToTask(taskId, uploadRes.data.id);
      if (!attachRes.success) {
        throw new Error(attachRes.message || 'Failed to attach file to task.');
      }

      // Refresh list
      await fetchAttachments();
    } catch (err: any) {
      setUploadError(getApiErrorMessage(err) || 'File upload failed.');
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
      setCurrentUploadFileName('');
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      handleFileUpload(e.target.files[0]);
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragActive(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragActive(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFileUpload(e.dataTransfer.files[0]);
    }
  };

  const handleDownload = async (attachment: TaskAttachmentItem) => {
    try {
      await fileApi.downloadFile(attachment.fileId, attachment.file.originalName);
    } catch (err: any) {
      console.error('Failed to download file:', err);
      alert(getApiErrorMessage(err) || 'Failed to download file.');
    }
  };

  const handleDeleteAttachment = async (attachment: TaskAttachmentItem) => {
    if (!window.confirm(t('file.delete_confirm'))) return;
    setDeletingId(attachment.id);
    try {
      const res = await fileApi.detachFromTask(taskId, attachment.id);
      if (res.success) {
        await fetchAttachments();
      } else {
        alert(res.message || 'Failed to delete attachment.');
      }
    } catch (err: any) {
      alert(getApiErrorMessage(err) || 'Failed to delete attachment.');
    } finally {
      setDeletingId(null);
    }
  };

  const handleAttachExisting = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!existingFileId.trim()) {
      setUploadError(t('file.invalid_file_id'));
      return;
    }
    setIsAttachingExisting(true);
    setUploadError(null);
    try {
      const res = await fileApi.attachToTask(taskId, existingFileId.trim());
      if (res.success) {
        setExistingFileId('');
        setShowExistingForm(false);
        await fetchAttachments();
      } else {
        setUploadError(res.message || 'Failed to attach file.');
      }
    } catch (err: any) {
      setUploadError(getApiErrorMessage(err) || 'Failed to attach file.');
    } finally {
      setIsAttachingExisting(false);
    }
  };

  return (
    <div className="task-attachments-container">
      {/* Upload Dropzone */}
      <div
        className={`attachment-dropzone ${isDragActive ? 'drag-active' : ''}`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <input
          ref={fileInputRef}
          type="file"
          className="attachment-file-input"
          onChange={handleFileChange}
          disabled={isUploading}
        />
        <div className="attachment-dropzone-icon">
          <UploadCloud size={22} />
        </div>
        <div className="attachment-dropzone-title">{t('file.drag_drop')}</div>
        <div className="attachment-dropzone-sub">
          {t('file.max_size')} • {t('file.allowed_types')}
        </div>
      </div>

      {/* Attach Existing File Toggle & Form */}
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 8, marginBottom: 8 }}>
        <button
          type="button"
          className="attachment-toggle-existing-btn"
          onClick={() => setShowExistingForm(!showExistingForm)}
          style={{
            background: 'none',
            border: 'none',
            color: 'var(--primary-color, #4f46e5)',
            fontSize: 13,
            fontWeight: 500,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: 5,
          }}
        >
          <Link2 size={14} />
          {t('file.attach_existing')}
        </button>
      </div>

      {showExistingForm && (
        <form onSubmit={handleAttachExisting} style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
          <input
            type="text"
            className="form-control"
            placeholder={t('file.file_id_placeholder')}
            value={existingFileId}
            onChange={(e) => setExistingFileId(e.target.value)}
            disabled={isAttachingExisting}
            style={{ flex: 1, fontSize: 13 }}
          />
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isAttachingExisting || !existingFileId.trim()}
            style={{ fontSize: 13, display: 'inline-flex', alignItems: 'center', gap: 5, whiteSpace: 'nowrap' }}
          >
            {isAttachingExisting ? <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} /> : <Link2 size={14} />}
            {t('file.attach_btn')}
          </button>
        </form>
      )}

      {/* Upload Progress Bar */}
      {isUploading && (
        <div className="upload-progress-box">
          <div className="upload-progress-header">
            <span>
              {t('file.uploading')} {currentUploadFileName}
            </span>
            <span>{uploadProgress}%</span>
          </div>
          <div className="upload-progress-track">
            <div className="upload-progress-fill" style={{ width: `${uploadProgress}%` }} />
          </div>
        </div>
      )}

      {/* Upload Error Alert */}
      {uploadError && (
        <div className="attachment-error-banner">
          <AlertCircle size={16} />
          <span>{uploadError}</span>
        </div>
      )}

      {/* Main Attachments List */}
      {loading ? (
        <div className="attachment-loading-state">
          <Loader2 size={18} style={{ animation: 'spin 1s linear infinite' }} />
          <span>{t('board.loading')}</span>
        </div>
      ) : error ? (
        <div className="attachment-error-banner">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      ) : attachments.length === 0 ? (
        <div style={{ fontStyle: 'italic', color: 'var(--text-subtle)', fontSize: 13, textAlign: 'center', padding: '16px 0' }}>
          {t('file.no_attachments')}
        </div>
      ) : (
        <div className="attachments-list">
          {attachments.map((att) => {
            const { icon, className: categoryClass } = getFileCategory(att.file.originalName);
            const isDeleting = deletingId === att.id;
            const canDelete = !att.file.uploadedBy || att.file.uploadedBy === user?.id;

            return (
              <div key={att.id} className="attachment-item-card">
                <div className="attachment-item-main">
                  <div className={`attachment-icon-box ${categoryClass}`}>{icon}</div>

                  <div className="attachment-details">
                    <span className="attachment-filename" title={att.file.originalName}>
                      {att.file.originalName}
                    </span>
                    <div className="attachment-meta-row">
                      <span className="attachment-size">{formatFileSize(att.file.sizeBytes)}</span>
                      <span>•</span>
                      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 3 }}>
                        <Clock size={11} />
                        {new Date(att.createdAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US')}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="attachment-actions">
                  <button
                    type="button"
                    className="attachment-action-btn"
                    onClick={() => handleDownload(att)}
                    title={t('file.download')}
                  >
                    <Download size={15} />
                  </button>

                  {canDelete && (
                    <button
                      type="button"
                      className="attachment-action-btn delete"
                      onClick={() => handleDeleteAttachment(att)}
                      disabled={isDeleting}
                      title={t('file.delete')}
                    >
                      {isDeleting ? (
                        <Loader2 size={15} style={{ animation: 'spin 1s linear infinite' }} />
                      ) : (
                        <Trash2 size={15} />
                      )}
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
