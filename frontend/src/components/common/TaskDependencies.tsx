import React, { useState } from 'react';
import { useLanguage } from '../../contexts/LanguageContext';
import { taskApi } from '../../api/taskApi';
import { getApiErrorMessage } from '../../api';
import { TaskDependencyDto, TaskItem } from '../../types';
import { Link2, Plus, Trash2, AlertCircle, Loader2, GitCommit } from 'lucide-react';
import './TaskDependencies.css';

interface TaskDependenciesProps {
  taskId: string;
  dependencies: TaskDependencyDto[];
  availableTasks: TaskItem[];
  onDependencyUpdated: () => void;
}

export const TaskDependencies: React.FC<TaskDependenciesProps> = ({
  taskId,
  dependencies = [],
  availableTasks = [],
  onDependencyUpdated,
}) => {
  const { t } = useLanguage();
  const [selectedDependsOnId, setSelectedDependsOnId] = useState<string>('');
  const [dependencyType, setDependencyType] = useState<string>('finish_to_start');
  const [isAdding, setIsAdding] = useState<boolean>(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string>('');
  const [showAddForm, setShowAddForm] = useState<boolean>(false);

  // Filter tasks to exclude current task and already existing dependencies
  const selectableTasks = availableTasks.filter(
    (tItem) => tItem.id !== taskId && !dependencies.some((d) => d.dependsOnId === tItem.id)
  );

  const handleAddDependency = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    if (!selectedDependsOnId) {
      setErrorMessage(t('dependency.select_task_placeholder') || 'Please select a task');
      return;
    }

    setIsAdding(true);
    try {
      const res = await taskApi.addDependency(taskId, selectedDependsOnId, dependencyType);
      if (res.success) {
        setSelectedDependsOnId('');
        setShowAddForm(false);
        onDependencyUpdated();
      } else {
        setErrorMessage(res.message || 'Failed to add dependency');
      }
    } catch (err: any) {
      setErrorMessage(getApiErrorMessage(err) || 'Failed to add dependency');
    } finally {
      setIsAdding(false);
    }
  };

  const handleRemoveDependency = async (dependsOnId: string) => {
    setErrorMessage('');
    setDeletingId(dependsOnId);
    try {
      const res = await taskApi.removeDependency(taskId, dependsOnId);
      if (res.success) {
        onDependencyUpdated();
      } else {
        setErrorMessage(res.message || 'Failed to remove dependency');
      }
    } catch (err: any) {
      setErrorMessage(getApiErrorMessage(err) || 'Failed to remove dependency');
    } finally {
      setDeletingId(null);
    }
  };

  const getTypeBadgeLabel = (type: string) => {
    switch (type?.toLowerCase()) {
      case 'start_to_start':
        return 'SS';
      case 'finish_to_finish':
        return 'FF';
      case 'start_to_finish':
        return 'SF';
      case 'finish_to_start':
      default:
        return 'FS';
    }
  };

  return (
    <div className="task-dependencies-container">
      <div className="dependencies-header">
        <div className="dependencies-title">
          <Link2 size={18} />
          <h4>{t('dependency.title') || 'Dependencies'}</h4>
          <span className="dependencies-count">{dependencies.length}</span>
        </div>
        {!showAddForm && (
          <button
            type="button"
            className="btn btn-sm btn-secondary add-dep-btn"
            onClick={() => {
              setShowAddForm(true);
              setErrorMessage('');
            }}
          >
            <Plus size={14} />
            <span>{t('dependency.add_button') || 'Add Dependency'}</span>
          </button>
        )}
      </div>

      {errorMessage && (
        <div className="dependencies-error-banner">
          <AlertCircle size={16} />
          <span>{errorMessage}</span>
        </div>
      )}

      {showAddForm && (
        <form onSubmit={handleAddDependency} className="add-dependency-form">
          <div className="form-row">
            <div className="form-group flex-2">
              <label>{t('dependency.depends_on') || 'Depends on Task'}</label>
              <select
                value={selectedDependsOnId}
                onChange={(e) => setSelectedDependsOnId(e.target.value)}
                className="form-input"
                disabled={isAdding}
              >
                <option value="">-- {t('dependency.select_task_placeholder') || 'Select task'} --</option>
                {selectableTasks.map((tItem) => (
                  <option key={tItem.id} value={tItem.id}>
                    #{tItem.taskNumber} - {tItem.title}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group flex-1">
              <label>{t('dependency.type_label') || 'Type'}</label>
              <select
                value={dependencyType}
                onChange={(e) => setDependencyType(e.target.value)}
                className="form-input"
                disabled={isAdding}
              >
                <option value="finish_to_start">{t('dependency.type_fs') || 'Finish to Start (FS)'}</option>
                <option value="start_to_start">{t('dependency.type_ss') || 'Start to Start (SS)'}</option>
                <option value="finish_to_finish">{t('dependency.type_ff') || 'Finish to Finish (FF)'}</option>
                <option value="start_to_finish">{t('dependency.type_sf') || 'Start to Finish (SF)'}</option>
              </select>
            </div>
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => {
                setShowAddForm(false);
                setErrorMessage('');
              }}
              disabled={isAdding}
            >
              {t('members.invite_cancel') || 'Cancel'}
            </button>
            <button
              type="submit"
              className="btn btn-primary btn-sm"
              disabled={isAdding || !selectedDependsOnId}
            >
              {isAdding ? (
                <>
                  <Loader2 size={14} className="spin" />
                  <span>{t('login.loading') || 'Adding...'}</span>
                </>
              ) : (
                <span>{t('dependency.add_button') || 'Add'}</span>
              )}
            </button>
          </div>
        </form>
      )}

      <div className="dependencies-list">
        {dependencies.length === 0 ? (
          <div className="dependencies-empty">
            <GitCommit size={20} />
            <span>{t('dependency.empty') || 'No dependencies configured for this task.'}</span>
          </div>
        ) : (
          dependencies.map((dep) => (
            <div key={dep.dependsOnId} className="dependency-item">
              <div className="dep-info">
                <span className={`dep-badge badge-${dep.type || 'fs'}`}>
                  {getTypeBadgeLabel(dep.type)}
                </span>
                <span className="dep-task-number">#{dep.taskNumber}</span>
                <span className="dep-task-title">{dep.title}</span>
              </div>
              <button
                type="button"
                className="dep-delete-btn"
                onClick={() => handleRemoveDependency(dep.dependsOnId)}
                disabled={deletingId === dep.dependsOnId}
                title={t('dependency.remove') || 'Remove dependency'}
              >
                {deletingId === dep.dependsOnId ? (
                  <Loader2 size={14} className="spin" />
                ) : (
                  <Trash2 size={14} />
                )}
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
