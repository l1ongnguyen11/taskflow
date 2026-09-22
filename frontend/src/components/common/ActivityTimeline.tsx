import React, { useEffect, useState, useCallback } from 'react';
import { activityApi, GetActivitiesParams } from '../../api/activityApi';
import { getApiErrorMessage } from '../../api';
import { ActivityItem } from '../../types';
import { useLanguage } from '../../contexts/LanguageContext';
import {
  History,
  Plus,
  Edit3,
  Trash2,
  MoveRight,
  MessageSquare,
  UserCheck,
  Clock,
  Loader2,
  AlertCircle,
  Filter,
  ArrowRight,
} from 'lucide-react';
import './ActivityTimeline.css';

export interface ActivityTimelineProps {
  workspaceId: string;
  entityType?: string; // 'Workspace' | 'Project' | 'Task'
  entityId?: string;
  actorId?: string;
  action?: string;
  limit?: number;
  showFilter?: boolean;
  className?: string;
  title?: string;
}

function getTimeAgo(dateString: string, locale: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return locale === 'vi' ? 'vừa xong' : 'just now';
  if (diffInSeconds < 3600) {
    const mins = Math.floor(diffInSeconds / 60);
    return locale === 'vi' ? `${mins} phút trước` : `${mins}m ago`;
  }
  if (diffInSeconds < 86400) {
    const hours = Math.floor(diffInSeconds / 3600);
    return locale === 'vi' ? `${hours} giờ trước` : `${hours}h ago`;
  }
  if (diffInSeconds < 604800) {
    const days = Math.floor(diffInSeconds / 86400);
    return locale === 'vi' ? `${days} ngày trước` : `${days}d ago`;
  }
  return date.toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getActionIcon(action: string) {
  const act = action.toLowerCase();
  if (act.includes('create') || act.includes('add')) {
    return { icon: <Plus size={13} />, className: 'created' };
  }
  if (act.includes('update') || act.includes('edit') || act.includes('change')) {
    return { icon: <Edit3 size={13} />, className: 'updated' };
  }
  if (act.includes('delete') || act.includes('remove')) {
    return { icon: <Trash2 size={13} />, className: 'deleted' };
  }
  if (act.includes('move')) {
    return { icon: <MoveRight size={13} />, className: 'moved' };
  }
  if (act.includes('comment')) {
    return { icon: <MessageSquare size={13} />, className: 'commented' };
  }
  if (act.includes('assign')) {
    return { icon: <UserCheck size={13} />, className: 'assigned' };
  }
  return { icon: <History size={13} />, className: 'default' };
}

export const ActivityTimeline: React.FC<ActivityTimelineProps> = ({
  workspaceId,
  entityType: initialEntityType,
  entityId,
  actorId,
  action,
  limit = 15,
  showFilter = false,
  className = '',
  title,
}) => {
  const { t, locale } = useLanguage();
  const [activities, setActivities] = useState<ActivityItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadingMore, setLoadingMore] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [selectedEntityType, setSelectedEntityType] = useState<string>(initialEntityType || '');

  const fetchActivities = useCallback(
    async (isInitial = true, currentOffset = 0) => {
      if (!workspaceId) return;

      if (isInitial) {
        setLoading(true);
        setError(null);
      } else {
        setLoadingMore(true);
      }

      try {
        const params: GetActivitiesParams = {
          limit,
          offset: currentOffset,
        };

        const activeEntityType = showFilter ? selectedEntityType : initialEntityType;
        if (activeEntityType) params.entityType = activeEntityType;
        if (entityId) params.entityId = entityId;
        if (actorId) params.actorId = actorId;
        if (action) params.action = action;

        const res = await activityApi.getActivities(workspaceId, params);

        if (res.success && res.data) {
          if (isInitial) {
            setActivities(res.data);
          } else {
            setActivities((prev) => [...prev, ...res.data]);
          }
          if (res.pagination) {
            setTotalCount(res.pagination.totalCount);
          }
        } else {
          setError(res.message || t('activity.load_error'));
        }
      } catch (err: any) {
        setError(getApiErrorMessage(err) || t('activity.load_error'));
      } finally {
        setLoading(false);
        setLoadingMore(false);
      }
    },
    [workspaceId, initialEntityType, selectedEntityType, entityId, actorId, action, limit, showFilter, t]
  );

  useEffect(() => {
    fetchActivities(true, 0);
  }, [fetchActivities]);

  const handleLoadMore = () => {
    if (loadingMore || activities.length >= totalCount) return;
    fetchActivities(false, activities.length);
  };

  const translateAction = (act: string) => {
    const lower = act.toLowerCase();
    if (lower === 'created' || lower === 'create') return t('activity.action_created');
    if (lower === 'updated' || lower === 'update') return t('activity.action_updated');
    if (lower === 'deleted' || lower === 'delete') return t('activity.action_deleted');
    if (lower === 'moved' || lower === 'move') return t('activity.action_moved');
    if (lower === 'commented' || lower === 'comment') return t('activity.action_commented');
    return act;
  };

  return (
    <div className={`activity-timeline-container ${className}`}>
      {/* Header */}
      <div className="activity-timeline-header">
        <div className="activity-timeline-title">
          <History size={18} />
          <span>{title || t('activity.title')}</span>
        </div>

        {showFilter && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <Filter size={14} style={{ color: 'var(--text-subtle)' }} />
            <select
              className="activity-filter-select"
              value={selectedEntityType}
              onChange={(e) => setSelectedEntityType(e.target.value)}
            >
              <option value="">{t('activity.filter_all')}</option>

              <option value="Workspace">{t('activity.filter_workspace')}</option>
              <option value="Project">{t('activity.filter_project')}</option>
              <option value="Task">{t('activity.filter_task')}</option>
            </select>
          </div>
        )}
      </div>

      {/* Main Content */}
      {loading ? (
        <div className="activity-loading-state">
          <Loader2 size={22} style={{ animation: 'spin 1s linear infinite', color: 'var(--primary)' }} />
          <span>{t('activity.loading')}</span>
        </div>
      ) : error ? (
        <div className="activity-error-state">
          <AlertCircle size={24} style={{ color: '#ef4444' }} />
          <span>{error}</span>
          <button className="btn btn-secondary btn-sm" onClick={() => fetchActivities(true, 0)}>
            {t('activity.retry')}
          </button>
        </div>
      ) : activities.length === 0 ? (
        <div className="activity-empty-state">
          <div className="activity-empty-icon">
            <History size={20} />
          </div>
          <span>{t('activity.empty')}</span>
        </div>
      ) : (
        <>
          <div className="activity-timeline-list">
            {activities.map((item) => {
              const { icon, className: badgeClass } = getActionIcon(item.action);
              const actorName = item.actorDisplayName || t('workspace.role_member');
              const initial = actorName.substring(0, 1).toUpperCase();

              return (
                <div key={item.id} className="activity-item">
                  <div className={`activity-icon-badge ${badgeClass}`}>{icon}</div>

                  <div className="activity-content">
                    <div className="activity-header-row">
                      {item.actorAvatarUrl ? (
                        <img src={item.actorAvatarUrl} alt={actorName} className="activity-actor-avatar" />
                      ) : (
                        <div className="activity-actor-avatar-fallback">{initial}</div>
                      )}
                      <span className="activity-actor-name">{actorName}</span>
                      <span className="activity-action-label">{translateAction(item.action)}</span>
                      <span className="activity-entity-tag">{item.entityType}</span>

                      <span className="activity-timestamp">
                        <Clock size={11} />
                        {getTimeAgo(item.createdAt, locale)}
                      </span>
                    </div>

                    {(item.oldValue || item.newValue) && (
                      <div className="activity-details-box">
                        <div className="activity-value-change">
                          {item.oldValue && <span className="activity-old-val">{item.oldValue}</span>}
                          {item.oldValue && item.newValue && <ArrowRight size={12} style={{ color: '#94a3b8' }} />}
                          {item.newValue && <span className="activity-new-val">{item.newValue}</span>}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>

          {activities.length < totalCount && (
            <div className="activity-load-more">
              <button
                className="activity-load-more-btn"
                onClick={handleLoadMore}
                disabled={loadingMore}
              >
                {loadingMore ? (
                  <>
                    <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} />
                    <span>{t('activity.loading')}</span>
                  </>
                ) : (
                  <span>
                    {t('activity.load_more')} ({activities.length}/{totalCount})
                  </span>
                )}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
};
