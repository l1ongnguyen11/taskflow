import React, { useEffect, useState, useCallback } from 'react';
import { MainLayout } from '../../components/layout/MainLayout';
import { useLanguage } from '../../contexts/LanguageContext';
import { useNavigate } from 'react-router-dom';
import { notificationApi } from '../../api/notificationApi';
import { getApiErrorMessage } from '../../api';
import { NotificationItem } from '../../types';
import {
  Bell,
  CheckCheck,
  Check,
  Info,
  AlertCircle,
  Trash2,
  MessageSquare,
  UserPlus,
  ClipboardList,
  FolderKanban,
  Loader2,
  RefreshCw,
} from 'lucide-react';
import './Notifications.css';

export const NotificationsPage: React.FC = () => {
  const { t } = useLanguage();
  const navigate = useNavigate();

  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [activeTab, setActiveTab] = useState<'all' | 'unread'>('all');
  const [markingAllRead, setMarkingAllRead] = useState(false);

  const loadNotifications = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const isReadFilter = activeTab === 'unread' ? false : undefined;
      const res = await notificationApi.getNotifications(isReadFilter, 100, 0);
      if (res.success && res.data) {
        setNotifications(res.data);
      } else {
        setNotifications([]);
        if (res.message) setError(res.message);
      }
    } catch (err: any) {
      console.error('Failed to load notifications:', err);
      setError(getApiErrorMessage(err) || t('notif.error_title'));
    } finally {
      setLoading(false);
    }
  }, [activeTab, t]);

  useEffect(() => {
    loadNotifications();
  }, [loadNotifications]);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  // Mark single notification as read
  const handleMarkAsRead = async (notifId: string) => {
    try {
      const res = await notificationApi.markAsRead(notifId);
      if (res.success) {
        setNotifications((prev) =>
          prev.map((n) => (n.id === notifId ? { ...n, isRead: true } : n))
        );
      }
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
    }
  };

  // Mark all as read
  const handleMarkAllRead = async () => {
    if (unreadCount === 0) return;
    setMarkingAllRead(true);
    try {
      const res = await notificationApi.markAllAsRead();
      if (res.success) {
        setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      }
    } catch (err) {
      console.error('Failed to mark all notifications read:', err);
    } finally {
      setMarkingAllRead(false);
    }
  };

  // Delete notification
  const handleDelete = async (notifId: string) => {
    try {
      const res = await notificationApi.deleteNotification(notifId);
      if (res.success) {
        setNotifications((prev) => prev.filter((n) => n.id !== notifId));
      }
    } catch (err) {
      console.error('Failed to delete notification:', err);
    }
  };

  // Navigate to related entity
  const handleNotificationClick = async (n: NotificationItem) => {
    // Mark as read on click
    if (!n.isRead) {
      await handleMarkAsRead(n.id);
    }

    // Navigate based on entityType
    if (n.entityType && n.entityId) {
      const entityTypeLower = n.entityType.toLowerCase();
      if (entityTypeLower === 'task') {
        // Tasks don't have standalone routes in this app, do nothing special
        return;
      }
      if (entityTypeLower === 'project') {
        navigate(`/projects/${n.entityId}/board`);
        return;
      }
      if (entityTypeLower === 'workspace') {
        navigate(`/workspaces/${n.entityId}/dashboard`);
        return;
      }
      if (entityTypeLower === 'invitation') {
        // Navigate to workspaces list to see invitations
        navigate('/workspaces');
        return;
      }
    }
  };

  // Get icon by notification type
  const getNotifIcon = (type: string) => {
    const typeLower = type.toLowerCase();
    if (typeLower.includes('comment')) return <MessageSquare size={18} />;
    if (typeLower.includes('invite') || typeLower.includes('member')) return <UserPlus size={18} />;
    if (typeLower.includes('task') || typeLower.includes('assign')) return <ClipboardList size={18} />;
    if (typeLower.includes('project') || typeLower.includes('board')) return <FolderKanban size={18} />;
    return <Info size={18} />;
  };

  // Get icon color class by type
  const getNotifIconClass = (type: string) => {
    const typeLower = type.toLowerCase();
    if (typeLower.includes('comment')) return 'notif-icon-comment';
    if (typeLower.includes('invite') || typeLower.includes('member')) return 'notif-icon-member';
    if (typeLower.includes('task') || typeLower.includes('assign')) return 'notif-icon-task';
    if (typeLower.includes('project') || typeLower.includes('board')) return 'notif-icon-project';
    return '';
  };

  // Time ago formatter
  const getTimeAgo = (dateStr: string) => {
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  return (
    <MainLayout title={t('sidebar.notifications')}>
      <div className="notif-header">
        <div>
          <h2>{t('notif.title')}</h2>
          <p className="subtitle">{t('notif.subtitle')}</p>
        </div>
        <div className="notif-header-actions">
          <button
            className="btn btn-secondary btn-sm"
            onClick={loadNotifications}
            disabled={loading}
            title="Refresh"
          >
            <RefreshCw size={14} className={loading ? 'spin-icon' : ''} />
          </button>
          <button
            className="btn btn-secondary"
            onClick={handleMarkAllRead}
            disabled={markingAllRead || unreadCount === 0}
          >
            <CheckCheck size={16} />
            <span>{markingAllRead ? '...' : t('notif.mark_all_read')}</span>
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="notif-tabs">
        <button
          className={`notif-tab ${activeTab === 'all' ? 'active' : ''}`}
          onClick={() => setActiveTab('all')}
        >
          {t('notif.tab_all')}
        </button>
        <button
          className={`notif-tab ${activeTab === 'unread' ? 'active' : ''}`}
          onClick={() => setActiveTab('unread')}
        >
          {t('notif.tab_unread')}
          {unreadCount > 0 && <span className="notif-tab-badge">{unreadCount}</span>}
        </button>
      </div>

      {/* Error state */}
      {error && (
        <div className="notif-error-banner">
          <AlertCircle size={16} />
          <span>{error}</span>
          <button className="btn btn-secondary btn-sm" onClick={loadNotifications}>
            {t('notif.retry')}
          </button>
        </div>
      )}

      {/* Loading state */}
      {loading ? (
        <div className="loading-state notif-loading">
          <Loader2 size={24} className="spin-icon" />
          <span>{t('notif.loading')}</span>
        </div>
      ) : notifications.length === 0 ? (
        /* Empty state */
        <div className="card empty-notif-card">
          <div className="empty-icon-circle">
            <Bell size={40} className="empty-icon" />
          </div>
          <h3>{t('notif.empty_title')}</h3>
          <p>{t('notif.empty_desc')}</p>
        </div>
      ) : (
        /* Notifications list */
        <div className="notifications-list card">
          {notifications.map((n) => (
            <div
              key={n.id}
              className={`notif-item ${!n.isRead ? 'unread' : ''}`}
              onClick={() => handleNotificationClick(n)}
            >
              <div className={`notif-icon ${getNotifIconClass(n.type)}`}>
                {getNotifIcon(n.type)}
              </div>
              <div className="notif-content">
                <div className="notif-content-top">
                  <h4 className="notif-title">{n.title}</h4>
                  {!n.isRead && <span className="notif-unread-dot" />}
                </div>
                <p className="notif-body">{n.body || n.content || ''}</p>
                <span className="notif-time">{getTimeAgo(n.createdAt)}</span>
              </div>
              <div className="notif-actions" onClick={(e) => e.stopPropagation()}>
                {!n.isRead && (
                  <button
                    className="notif-action-btn"
                    onClick={() => handleMarkAsRead(n.id)}
                    title={t('notif.mark_read')}
                  >
                    <Check size={14} />
                  </button>
                )}
                <button
                  className="notif-action-btn notif-action-danger"
                  onClick={() => handleDelete(n.id)}
                  title={t('notif.delete')}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </MainLayout>
  );
};
