import React, { useEffect, useState, useCallback } from 'react';
import { checklistApi } from '../../api/checklistApi';
import { getApiErrorMessage } from '../../api';
import { Checklist, ChecklistItem } from '../../types';
import { useLanguage } from '../../contexts/LanguageContext';
import {
  CheckSquare,
  Plus,
  Pencil,
  Trash2,
  X,
  Check,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import './TaskChecklists.css';

export interface TaskChecklistsProps {
  taskId: string;
  onProgressChange?: (completed: number, total: number) => void;
}

export const TaskChecklists: React.FC<TaskChecklistsProps> = ({ taskId, onProgressChange }) => {
  const { t } = useLanguage();

  const [checklists, setChecklists] = useState<Checklist[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // New Checklist Form state
  const [showAddChecklist, setShowAddChecklist] = useState<boolean>(false);
  const [newChecklistTitle, setNewChecklistTitle] = useState<string>('');
  const [isCreatingChecklist, setIsCreatingChecklist] = useState<boolean>(false);

  // Editing Checklist Title state
  const [editingChecklistId, setEditingChecklistId] = useState<string | null>(null);
  const [editingChecklistTitle, setEditingChecklistTitle] = useState<string>('');
  const [isUpdatingChecklistTitle, setIsUpdatingChecklistTitle] = useState<boolean>(false);

  // New Item Form per Checklist ID
  const [newItemTexts, setNewItemTexts] = useState<Record<string, string>>({});
  const [addingItemChecklistId, setAddingItemChecklistId] = useState<string | null>(null);

  // Editing Item state
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [editingItemContent, setEditingItemContent] = useState<string>('');
  const [isUpdatingItem, setIsUpdatingItem] = useState<boolean>(false);

  // Toggling item ID state for spinner
  const [togglingItemId, setTogglingItemId] = useState<string | null>(null);

  // Fetch Checklists
  const fetchChecklists = useCallback(async () => {
    if (!taskId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await checklistApi.getTaskChecklists(taskId);
      if (res.success && res.data) {
        setChecklists(res.data);
        if (onProgressChange) {
          let comp = 0;
          let tot = 0;
          res.data.forEach((cl) => {
            if (cl.items) {
              tot += cl.items.length;
              comp += cl.items.filter((i) => i.isCompleted).length;
            }
          });
          onProgressChange(comp, tot);
        }
      } else {
        setError(res.message || 'Failed to load checklists.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to load checklists.');
    } finally {
      setLoading(false);
    }
  }, [taskId, onProgressChange]);

  useEffect(() => {
    fetchChecklists();
  }, [fetchChecklists]);

  // --- Create Checklist Handler ---
  const handleCreateChecklist = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newChecklistTitle.trim()) return;

    setIsCreatingChecklist(true);
    try {
      const res = await checklistApi.createChecklist(taskId, newChecklistTitle.trim());
      if (res.success) {
        setNewChecklistTitle('');
        setShowAddChecklist(false);
        await fetchChecklists();
      } else {
        setError(res.message || 'Failed to create checklist.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to create checklist.');
    } finally {
      setIsCreatingChecklist(false);
    }
  };

  // --- Update Checklist Title Handler ---
  const handleUpdateChecklistTitle = async (checklistId: string) => {
    if (!editingChecklistTitle.trim()) return;
    setIsUpdatingChecklistTitle(true);
    try {
      const res = await checklistApi.updateChecklist(checklistId, editingChecklistTitle.trim());
      if (res.success) {
        setEditingChecklistId(null);
        await fetchChecklists();
      } else {
        setError(res.message || 'Failed to update checklist.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to update checklist.');
    } finally {
      setIsUpdatingChecklistTitle(false);
    }
  };

  // --- Delete Checklist Handler ---
  const handleDeleteChecklist = async (checklist: Checklist) => {
    if (!window.confirm(t('checklist.delete_confirm'))) return;
    try {
      const res = await checklistApi.deleteChecklist(checklist.id);
      if (res.success) {
        await fetchChecklists();
      } else {
        alert(res.message || 'Failed to delete checklist.');
      }
    } catch (err: any) {
      const msg = getApiErrorMessage(err) || 'Failed to delete checklist.';
      alert(msg);
    }
  };

  // --- Create Checklist Item Handler ---
  const handleAddItem = async (e: React.FormEvent, checklistId: string) => {
    e.preventDefault();
    const text = newItemTexts[checklistId]?.trim();
    if (!text) return;

    setAddingItemChecklistId(checklistId);
    try {
      const res = await checklistApi.createItem(checklistId, { content: text });
      if (res.success) {
        setNewItemTexts((prev) => ({ ...prev, [checklistId]: '' }));
        await fetchChecklists();
      } else {
        setError(res.message || 'Failed to add item.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to add item.');
    } finally {
      setAddingItemChecklistId(null);
    }
  };

  // --- Toggle Item Completion ---
  const handleToggleItem = async (item: ChecklistItem) => {
    setTogglingItemId(item.id);
    try {
      const res = await checklistApi.toggleItem(item.id);
      if (res.success) {
        await fetchChecklists();
      } else {
        setError(res.message || 'Failed to toggle item.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to toggle item.');
    } finally {
      setTogglingItemId(null);
    }
  };

  // --- Edit Item Content Handler ---
  const handleUpdateItemContent = async (item: ChecklistItem) => {
    if (!editingItemContent.trim()) return;
    setIsUpdatingItem(true);
    try {
      const res = await checklistApi.updateItem(item.id, {
        content: editingItemContent.trim(),
        position: item.position,
      });
      if (res.success) {
        setEditingItemId(null);
        await fetchChecklists();
      } else {
        setError(res.message || 'Failed to update item.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to update item.');
    } finally {
      setIsUpdatingItem(false);
    }
  };

  // --- Delete Item Handler ---
  const handleDeleteItem = async (itemId: string) => {
    try {
      const res = await checklistApi.deleteItem(itemId);
      if (res.success) {
        await fetchChecklists();
      } else {
        setError(res.message || 'Failed to delete item.');
      }
    } catch (err: any) {
      setError(getApiErrorMessage(err) || 'Failed to delete item.');
    }
  };

  return (
    <div className="task-checklists-container">
      {/* Checklists Header & Add Button */}
      <div className="checklists-header">
        <div className="checklists-header-title">
          <CheckSquare size={18} style={{ color: 'var(--primary-color, #4f46e5)' }} />
          <span>{t('checklist.title')}</span>
        </div>

        <button
          type="button"
          className="btn btn-secondary btn-sm"
          onClick={() => setShowAddChecklist(!showAddChecklist)}
          style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}
        >
          <Plus size={14} />
          <span>{t('checklist.add_btn')}</span>
        </button>
      </div>

      {/* Create Checklist Inline Form */}
      {showAddChecklist && (
        <form onSubmit={handleCreateChecklist} className="create-checklist-form">
          <input
            type="text"
            className="form-control"
            placeholder={t('checklist.title_placeholder')}
            value={newChecklistTitle}
            onChange={(e) => setNewChecklistTitle(e.target.value)}
            disabled={isCreatingChecklist}
            style={{ flex: 1, fontSize: 13 }}
            autoFocus
          />
          <button
            type="submit"
            className="btn btn-primary btn-sm"
            disabled={isCreatingChecklist || !newChecklistTitle.trim()}
          >
            {isCreatingChecklist ? (
              <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} />
            ) : (
              t('checklist.create')
            )}
          </button>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => setShowAddChecklist(false)}
          >
            <X size={16} />
          </button>
        </form>
      )}

      {/* Error Alert Banner */}
      {error && (
        <div className="badge badge-danger" style={{ padding: '8px 12px', borderRadius: 6 }}>
          <AlertCircle size={15} />
          <span>{error}</span>
        </div>
      )}

      {/* Main Checklists List */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '16px 0', color: 'var(--text-subtle)' }}>
          <Loader2 size={16} style={{ animation: 'spin 1s linear infinite' }} />
          <span style={{ marginLeft: 6 }}>{t('board.loading')}</span>
        </div>
      ) : checklists.length === 0 ? (
        <div style={{ fontStyle: 'italic', fontSize: 13, color: 'var(--text-subtle)', textAlign: 'center', padding: '12px 0' }}>
          {t('checklist.no_checklists')}
        </div>
      ) : (
        checklists.map((cl) => {
          const items = cl.items || [];
          const totalItems = items.length;
          const completedItems = items.filter((i) => i.isCompleted).length;
          const percent = totalItems > 0 ? Math.round((completedItems / totalItems) * 100) : 0;
          const isComplete = totalItems > 0 && completedItems === totalItems;

          const isEditingTitle = editingChecklistId === cl.id;

          return (
            <div key={cl.id} className="checklist-card">
              {/* Title Bar */}
              <div className="checklist-title-bar">
                {isEditingTitle ? (
                  <div style={{ display: 'flex', gap: 6, flex: 1, marginRight: 8 }}>
                    <input
                      type="text"
                      className="form-control"
                      value={editingChecklistTitle}
                      onChange={(e) => setEditingChecklistTitle(e.target.value)}
                      disabled={isUpdatingChecklistTitle}
                      style={{ fontSize: 14 }}
                      autoFocus
                    />
                    <button
                      type="button"
                      className="btn btn-primary btn-sm"
                      onClick={() => handleUpdateChecklistTitle(cl.id)}
                      disabled={isUpdatingChecklistTitle || !editingChecklistTitle.trim()}
                    >
                      <Check size={14} />
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() => setEditingChecklistId(null)}
                    >
                      <X size={14} />
                    </button>
                  </div>
                ) : (
                  <div className="checklist-title-text">
                    <CheckSquare size={16} style={{ color: isComplete ? '#10b981' : 'var(--primary-color)' }} />
                    <span>{cl.title}</span>
                  </div>
                )}

                {!isEditingTitle && (
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button
                      type="button"
                      className="btn-action btn-action-edit"
                      onClick={() => {
                        setEditingChecklistId(cl.id);
                        setEditingChecklistTitle(cl.title);
                      }}
                      title="Edit Title"
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      type="button"
                      className="btn-action btn-action-danger"
                      onClick={() => handleDeleteChecklist(cl)}
                      title="Delete Checklist"
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                )}
              </div>

              {/* Progress Bar */}
              <div className="checklist-progress-box">
                <div className="checklist-progress-info">
                  <span>
                    {t('checklist.progress')
                      .replace('{completed}', String(completedItems))
                      .replace('{total}', String(totalItems))}
                  </span>
                  <span>{percent}%</span>
                </div>

                <div className="checklist-progress-track">
                  <div
                    className={`checklist-progress-fill ${isComplete ? 'complete' : ''}`}
                    style={{ width: `${percent}%` }}
                  />
                </div>
              </div>

              {/* Items List */}
              <div className="checklist-items-list">
                {items.map((item) => {
                  const isEditingThisItem = editingItemId === item.id;
                  const isToggling = togglingItemId === item.id;

                  return (
                    <div key={item.id} className="checklist-item-row">
                      {isEditingThisItem ? (
                        <div style={{ display: 'flex', gap: 6, flex: 1 }}>
                          <input
                            type="text"
                            className="form-control"
                            value={editingItemContent}
                            onChange={(e) => setEditingItemContent(e.target.value)}
                            disabled={isUpdatingItem}
                            style={{ fontSize: 13 }}
                            autoFocus
                          />
                          <button
                            type="button"
                            className="btn btn-primary btn-sm"
                            onClick={() => handleUpdateItemContent(item)}
                            disabled={isUpdatingItem || !editingItemContent.trim()}
                          >
                            <Check size={14} />
                          </button>
                          <button
                            type="button"
                            className="btn btn-ghost btn-sm"
                            onClick={() => setEditingItemId(null)}
                          >
                            <X size={14} />
                          </button>
                        </div>
                      ) : (
                        <>
                          <div className="checklist-item-left">
                            {isToggling ? (
                              <Loader2 size={15} style={{ animation: 'spin 1s linear infinite' }} />
                            ) : (
                              <input
                                type="checkbox"
                                className="checklist-item-checkbox"
                                checked={item.isCompleted}
                                onChange={() => handleToggleItem(item)}
                              />
                            )}

                            <span className={`checklist-item-content ${item.isCompleted ? 'completed' : ''}`}>
                              {item.content}
                            </span>
                          </div>

                          <div className="checklist-item-actions">
                            <button
                              type="button"
                              className="btn-action btn-action-edit"
                              onClick={() => {
                                setEditingItemId(item.id);
                                setEditingItemContent(item.content);
                              }}
                              title="Edit item"
                            >
                              <Pencil size={13} />
                            </button>
                            <button
                              type="button"
                              className="btn-action btn-action-danger"
                              onClick={() => handleDeleteItem(item.id)}
                              title="Delete item"
                            >
                              <Trash2 size={13} />
                            </button>
                          </div>
                        </>
                      )}
                    </div>
                  );
                })}
              </div>

              {/* Add Item Form */}
              <form onSubmit={(e) => handleAddItem(e, cl.id)} className="add-item-form">
                <input
                  type="text"
                  className="form-control"
                  placeholder={t('checklist.item_placeholder')}
                  value={newItemTexts[cl.id] || ''}
                  onChange={(e) =>
                    setNewItemTexts((prev) => ({ ...prev, [cl.id]: e.target.value }))
                  }
                  disabled={addingItemChecklistId === cl.id}
                  style={{ fontSize: 13, flex: 1 }}
                />
                <button
                  type="submit"
                  className="btn btn-secondary btn-sm"
                  disabled={addingItemChecklistId === cl.id || !newItemTexts[cl.id]?.trim()}
                >
                  {addingItemChecklistId === cl.id ? (
                    <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} />
                  ) : (
                    <Plus size={14} />
                  )}
                  <span>{t('checklist.create')}</span>
                </button>
              </form>
            </div>
          );
        })
      )}
    </div>
  );
};
