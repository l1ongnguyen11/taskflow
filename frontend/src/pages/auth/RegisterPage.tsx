import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowRight, Eye, EyeOff } from 'lucide-react';
import { authApi } from '../../api/authApi';
import { useAuth } from '../../contexts/AuthContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { Logo } from '../../components/common/Logo';
import './Auth.css';

export const RegisterPage: React.FC = () => {
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { login } = useAuth();
  const { t } = useLanguage();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const res = await authApi.register({ displayName, email, password });
      if (res.success && res.data) {
        await login(res.data.accessToken, res.data.refreshToken);
        navigate('/workspaces');
      } else {
        const errs = res.errors && res.errors.length > 0 ? res.errors.join(' ') : res.message;
        setError(errs || t('register.error_default'));
      }
    } catch (err: any) {
      const apiErrors = err.response?.data?.errors;
      let errMsg = t('register.error_network');
      if (Array.isArray(apiErrors) && apiErrors.length > 0) {
        errMsg = apiErrors.join(' ');
      } else if (err.response?.data?.message) {
        errMsg = err.response.data.message;
      }
      setError(errMsg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="auth-header">
          <div style={{ marginBottom: 16 }}>
            <Logo size={40} showText={true} />
          </div>
          <h2 className="auth-title">{t('register.title')}</h2>
          <p className="auth-subtitle">{t('register.subtitle')}</p>
        </div>

        {error && <div className="auth-error">{error}</div>}

        <form onSubmit={handleSubmit} className="auth-form">
          <div className="form-group">
            <label className="form-label">{t('register.name_label')}</label>
            <input
              type="text"
              required
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder={t('register.name_placeholder')}
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label className="form-label">{t('register.email_label')}</label>
            <input
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder={t('register.email_placeholder')}
              className="form-input"
            />
          </div>

          <div className="form-group">
            <label className="form-label">{t('register.password_label')}</label>
            <div className="password-input-wrapper">
              <input
                type={showPassword ? 'text' : 'password'}
                required
                minLength={8}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={t('register.password_placeholder')}
                className="form-input"
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowPassword(!showPassword)}
                title={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
            <span style={{ fontSize: '11px', color: '#64748B', marginTop: '4px' }}>
              {t('register.password_hint')}
            </span>
          </div>

          <button type="submit" disabled={loading} className="btn btn-primary full-width" style={{ marginTop: 12 }}>
            {loading ? t('register.loading') : t('register.submit')} <ArrowRight size={16} />
          </button>
        </form>

        <div className="auth-footer">
          {t('register.has_account')} <Link to="/login">{t('register.sign_in')}</Link>
        </div>
      </div>
    </div>
  );
};

