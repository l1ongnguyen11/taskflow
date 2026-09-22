import React from 'react';
import { AlertTriangle, Trash2, HelpCircle, X } from 'lucide-react';
import './ConfirmModal.css';

export interface ConfirmModalProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: 'danger' | 'warning' | 'info';
  isLoading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const ConfirmModal: React.FC<ConfirmModalProps> = ({
  isOpen,
  title,
  message,
  confirmText = 'Xóa',
  cancelText = 'Hủy',
  variant = 'danger',
  isLoading = false,
  onConfirm,
  onCancel,
}) => {
  if (!isOpen) return null;

  // Clean fallback in case translation key strings are passed
  const resolveText = (text: string | undefined, defaultText: string) => {
    if (!text || text.includes('.') || text.startsWith('board.') || text.startsWith('common.')) {
      return defaultText;
    }
    return text;
  };

  const displayTitle = resolveText(title, 'Xóa cột công việc?');
  const displayConfirm = resolveText(confirmText, 'Xóa cột');
  const displayCancel = resolveText(cancelText, 'Hủy');

  const renderIcon = () => {
    switch (variant) {
      case 'danger':
        return <Trash2 size={24} className="confirm-icon danger" />;
      case 'warning':
        return <AlertTriangle size={24} className="confirm-icon warning" />;
      default:
        return <HelpCircle size={24} className="confirm-icon info" />;
    }
  };

  return (
    <div className="confirm-modal-overlay" onClick={() => !isLoading && onCancel()}>
      <div
        className={`confirm-modal-card ${variant}`}
        onClick={(e) => e.stopPropagation()}
      >
        <button
          type="button"
          className="confirm-modal-close"
          onClick={() => !isLoading && onCancel()}
          disabled={isLoading}
          aria-label="Close"
        >
          <X size={18} />
        </button>

        <div className="confirm-modal-header">
          <div className={`confirm-icon-wrapper ${variant}`}>
            {renderIcon()}
          </div>
          <div className="confirm-modal-titles">
            <h3>{displayTitle}</h3>
            <p>{message}</p>
          </div>
        </div>

        <div className="confirm-modal-actions">
          <button
            type="button"
            className="confirm-btn-cancel"
            onClick={onCancel}
            disabled={isLoading}
          >
            {displayCancel}
          </button>
          <button
            type="button"
            className={`confirm-btn-submit ${variant === 'danger' ? 'btn-danger' : 'btn-primary'}`}
            onClick={onConfirm}
            disabled={isLoading}
          >
            {isLoading ? (
              <span className="confirm-spinner"></span>
            ) : (
              displayConfirm
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
