import React, { useState, useRef, useEffect } from 'react';
import { Search, Bell, LogOut, Globe, Sun, Moon } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { useTheme } from '../../contexts/ThemeContext';
import { useNavigate } from 'react-router-dom';
import { notificationApi } from '../../api/notificationApi';
import './Header.css';

interface HeaderProps {
  title?: string;
}

export const Header: React.FC<HeaderProps> = ({ title = 'Dashboard' }) => {
  const { user, logout } = useAuth();
  const { locale, setLocale, t } = useLanguage();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();

  const [showLangMenu, setShowLangMenu] = useState(false);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const langDropdownRef = useRef<HTMLDivElement>(null);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  // Fetch unread notifications count
  useEffect(() => {
    const fetchUnreadCount = async () => {
      try {
        const res = await notificationApi.getNotifications(false, 1, 0);
        if (res.success && res.pagination) {
          setUnreadCount(res.pagination.totalCount);
        } else if (res.data) {
          setUnreadCount(res.data.filter((n) => !n.isRead).length);
        }
      } catch (err) {
        // silent catch for unread header badge
      }
    };
    fetchUnreadCount();
  }, []);

  // Close language menu on outside click
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (langDropdownRef.current && !langDropdownRef.current.contains(e.target as Node)) {
        setShowLangMenu(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <header className="header">
      <div className="header-left">
        <h1 className="header-title">{title}</h1>
      </div>

      <div className="header-search">
        <Search className="search-icon" size={18} />
        <input type="text" placeholder={t('header.search_placeholder')} className="search-input" />
      </div>

      <div className="header-right">
        {/* Notifications Button */}
        <button className="header-action-btn" onClick={() => navigate('/notifications')} title={t('header.notifications')}>
          <Bell size={20} />
          {unreadCount > 0 && (
            <span className="notification-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
          )}
        </button>

        {/* Language Switcher Dropdown (Beside Notifications) */}
        <div className="lang-switcher-container" ref={langDropdownRef}>
          <button
            className="header-action-btn lang-btn"
            onClick={() => setShowLangMenu(!showLangMenu)}
            title={t('header.language')}
          >
            <Globe size={20} />
            <span className="lang-code">{locale.toUpperCase()}</span>
          </button>

          {showLangMenu && (
            <div className="lang-dropdown-menu">
              <button
                className={`lang-option ${locale === 'en' ? 'active' : ''}`}
                onClick={() => {
                  setLocale('en');
                  setShowLangMenu(false);
                }}
              >
                <span className="flag">🇺🇸</span>
                <span>English</span>
                {locale === 'en' && <span className="check-mark">✓</span>}
              </button>
              <button
                className={`lang-option ${locale === 'vi' ? 'active' : ''}`}
                onClick={() => {
                  setLocale('vi');
                  setShowLangMenu(false);
                }}
              >
                <span className="flag">🇻🇳</span>
                <span>Tiếng Việt</span>
                {locale === 'vi' && <span className="check-mark">✓</span>}
              </button>
            </div>
          )}
        </div>

        {/* Theme Switcher Button */}
        <button
          className="header-action-btn theme-btn"
          onClick={toggleTheme}
          title={theme === 'light' ? t('header.theme_dark') : t('header.theme_light')}
        >
          {theme === 'light' ? <Moon size={20} /> : <Sun size={20} />}
        </button>

        <div className="user-profile-menu">
          <div className="avatar" onClick={() => navigate('/profile')}>
            {user?.avatarUrl ? (
              <img src={user.avatarUrl} alt={user.displayName} />
            ) : (
              <div className="avatar-placeholder">{user?.displayName?.substring(0, 2).toUpperCase() || 'TF'}</div>
            )}
          </div>
          <div className="user-info">
            <span className="user-name">{user?.displayName || 'User'}</span>
            <span className="user-email">{user?.email}</span>
          </div>

          <button className="logout-btn" onClick={handleLogout} title={t('header.logout')}>
            <LogOut size={18} />
          </button>
        </div>
      </div>
    </header>
  );
};

