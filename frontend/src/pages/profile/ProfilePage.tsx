import React, { useState, useEffect } from 'react';
import { MainLayout } from '../../components/layout/MainLayout';
import { useAuth } from '../../contexts/AuthContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { userApi, getApiErrorMessage } from '../../api';
import { User, Lock, Upload, Check, ShieldCheck, Calendar, AlertTriangle, Loader2 } from 'lucide-react';
import './Profile.css';

export const ProfilePage: React.FC = () => {
  const { user, isLoading, updateUser } = useAuth();
  const { t } = useLanguage();

  const [displayName, setDisplayName] = useState(user?.displayName || '');
  const [isUpdating, setIsUpdating] = useState(false);
  const [profileMsg, setProfileMsg] = useState('');
  const [profileErr, setProfileErr] = useState('');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [passwordMsg, setPasswordMsg] = useState('');
  const [passwordErr, setPasswordErr] = useState('');

  useEffect(() => {
    if (user?.displayName) {
      setDisplayName(user.displayName);
    }
  }, [user]);

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setProfileMsg('');
    setProfileErr('');

    if (!displayName.trim()) {
      setProfileErr('Display name is required.');
      return;
    }

    setIsUpdating(true);
    try {
      const res = await userApi.updateProfile({ displayName: displayName.trim() });
      if (res.success && res.data) {
        updateUser(res.data);
        setProfileMsg(t('profile.update_success'));
      } else {
        setProfileErr(res.message || t('profile.update_error'));
      }
    } catch (err: any) {
      setProfileErr(getApiErrorMessage(err) || t('profile.update_error'));
    } finally {
      setIsUpdating(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordMsg('');
    setPasswordErr('');

    if (!currentPassword || !newPassword) {
      setPasswordErr('Both current and new passwords are required.');
      return;
    }

    setIsChangingPassword(true);
    try {
      const res = await userApi.changePassword({ currentPassword, newPassword });
      if (res.success) {
        setPasswordMsg(t('profile.password_success'));
        setCurrentPassword('');
        setNewPassword('');
      } else {
        setPasswordErr(res.message || t('profile.password_error'));
      }
    } catch (err: any) {
      setPasswordErr(getApiErrorMessage(err) || t('profile.password_error'));
    } finally {
      setIsChangingPassword(false);
    }
  };

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      setProfileMsg('');
      setProfileErr('');
      try {
        const res = await userApi.uploadAvatar(file);
        if (res.success && res.data) {
          updateUser(res.data);
          setProfileMsg(t('profile.avatar_success'));
        } else {
          setProfileErr(res.message || 'Failed to upload avatar.');
        }
      } catch (err: any) {
        setProfileErr(getApiErrorMessage(err) || 'Failed to upload avatar.');
      }
    }
  };

  return (
    <MainLayout title={t('profile.title')}>
      {isLoading ? (
        <div className="detail-loading-state" style={{ minHeight: '300px' }}>
          <div className="ws-loading-spinner" />
          <p>Loading profile information...</p>
        </div>
      ) : (
        <>
          <div className="profile-header">
            <h2>{t('profile.title')}</h2>
            <p className="subtitle">{t('profile.subtitle')}</p>
          </div>

          <div className="profile-grid">
            {/* Card Profile Overview & Personal Info */}
            <div className="card profile-card">
              <div className="card-header">
                <h3>{t('profile.personal_info')}</h3>
                {user?.isActive !== undefined && (
                  <span className={`badge ${user.isActive ? 'badge-success' : 'badge-danger'}`}>
                    <ShieldCheck size={13} style={{ marginRight: 4 }} />
                    {user.isActive ? 'Active Account' : 'Inactive'}
                  </span>
                )}
              </div>

              <div className="avatar-section">
                <div className="profile-avatar">
                  {user?.avatarUrl ? (
                    <img src={user.avatarUrl} alt={user.displayName} />
                  ) : (
                    <div className="profile-avatar-placeholder">
                      {user?.displayName?.substring(0, 2).toUpperCase() || 'TF'}
                    </div>
                  )}
                </div>
                <label className="btn btn-secondary btn-sm upload-btn">
                  <Upload size={14} /> {t('profile.upload_avatar')}
                  <input type="file" accept="image/*" onChange={handleAvatarChange} style={{ display: 'none' }} />
                </label>
              </div>

              {profileMsg && <div className="badge badge-success" style={{ marginBottom: 16, display: 'block', padding: '8px 12px' }}>{profileMsg}</div>}
              {profileErr && <div className="badge badge-danger" style={{ marginBottom: 16, display: 'block', padding: '8px 12px' }}>{profileErr}</div>}

              <form onSubmit={handleUpdateProfile}>
                <div className="form-group">
                  <label className="form-label">{t('profile.name_label')}</label>
                  <input
                    type="text"
                    required
                    disabled={isUpdating}
                    value={displayName}
                    onChange={(e) => setDisplayName(e.target.value)}
                    className="form-input"
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">{t('profile.email_label')}</label>
                  <input type="email" disabled value={user?.email || ''} className="form-input disabled" />
                </div>

                {user?.createdAt && (
                  <div className="form-group">
                    <label className="form-label" style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                      <Calendar size={14} /> Joined Date
                    </label>
                    <input
                      type="text"
                      disabled
                      value={new Date(user.createdAt).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric',
                      })}
                      className="form-input disabled"
                    />
                  </div>
                )}

                <button type="submit" className="btn btn-primary" disabled={isUpdating} style={{ marginTop: 12 }}>
                  {isUpdating ? 'Saving...' : t('profile.save_changes')}
                </button>
              </form>
            </div>

            {/* Card Security */}
            <div className="card profile-card">
              <div className="card-header">
                <h3>{t('profile.change_password')}</h3>
              </div>

              {passwordMsg && <div className="badge badge-success" style={{ marginBottom: 16, display: 'block', padding: '8px 12px' }}>{passwordMsg}</div>}
              {passwordErr && <div className="badge badge-danger" style={{ marginBottom: 16, display: 'block', padding: '8px 12px' }}>{passwordErr}</div>}

              <form onSubmit={handleChangePassword}>
                <div className="form-group">
                  <label className="form-label">{t('profile.current_password')}</label>
                  <input
                    type="password"
                    required
                    disabled={isChangingPassword}
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    className="form-input"
                  />
                </div>

                <div className="form-group">
                  <label className="form-label">{t('profile.new_password')}</label>
                  <input
                    type="password"
                    required
                    minLength={8}
                    disabled={isChangingPassword}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="form-input"
                  />
                </div>

                <button type="submit" className="btn btn-secondary" disabled={isChangingPassword} style={{ marginTop: 12 }}>
                  {isChangingPassword ? 'Updating Password...' : t('profile.update_password')}
                </button>
              </form>
            </div>
          </div>
        </>
      )}
    </MainLayout>
  );
};
