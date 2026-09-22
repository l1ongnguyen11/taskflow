import React, { useState } from 'react';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { ProjectModal } from '../project/ProjectModal';

interface MainLayoutProps {
  children: React.ReactNode;
  title?: string;
}

export const MainLayout: React.FC<MainLayoutProps> = ({ children, title }) => {
  const { currentWorkspace, createWorkspace } = useWorkspace();
  const { t } = useLanguage();
  const [showWsModal, setShowWsModal] = useState(false);
  const [showProjModal, setShowProjModal] = useState(false);

  // New Workspace form state
  const [wsName, setWsName] = useState('');
  const [wsSlug, setWsSlug] = useState('');
  const [wsDesc, setWsDesc] = useState('');
  const [wsError, setWsError] = useState('');

  const slugify = (text: string) => {
    return text
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/đ/g, 'd')
      .replace(/[^a-z0-9 -]/g, '')
      .trim()
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-');
  };

  const handleCreateWorkspace = async (e: React.FormEvent) => {
    e.preventDefault();
    setWsError('');
    const cleanSlug = slugify(wsSlug || wsName);

    try {
      await createWorkspace({
        name: wsName,
        slug: cleanSlug,
        description: wsDesc,
      });
      setShowWsModal(false);
      setWsName('');
      setWsSlug('');
      setWsDesc('');
    } catch (err: any) {
      const apiErrors = err.response?.data?.errors;
      let errMsg = t('modal.ws_error_generic');
      if (Array.isArray(apiErrors) && apiErrors.length > 0) {
        errMsg = apiErrors.join(' ');
      } else if (err.response?.data?.message) {
        errMsg = err.response.data.message;
      } else if (err.message) {
        errMsg = err.message;
      }
      setWsError(errMsg);
    }
  };

  return (
    <div className="app-container">
      <Sidebar
        onOpenCreateWorkspace={() => setShowWsModal(true)}
        onOpenCreateProject={() => setShowProjModal(true)}
      />

      <div className="main-wrapper">
        <Header title={title || currentWorkspace?.name || 'TaskFlow'} />
        <main className="main-content">{children}</main>
      </div>

      {/* Modal Create Workspace */}
      {showWsModal && (
        <div className="modal-overlay" onClick={() => setShowWsModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{t('modal.create_workspace')}</h3>
              <button className="btn-ghost" onClick={() => setShowWsModal(false)}>✕</button>
            </div>
            <form onSubmit={handleCreateWorkspace}>
              <div className="modal-body">
                {wsError && <div className="badge badge-danger" style={{ marginBottom: 12, display: 'block' }}>{wsError}</div>}
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_name')}</label>
                  <input
                    type="text"
                    required
                    value={wsName}
                    onChange={(e) => {
                      setWsName(e.target.value);
                      if (!wsSlug) setWsSlug(slugify(e.target.value));
                    }}
                    placeholder={t('modal.ws_name_placeholder')}
                    className="form-input"
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_slug')}</label>
                  <input
                    type="text"
                    required
                    value={wsSlug}
                    onChange={(e) => setWsSlug(slugify(e.target.value))}
                    placeholder={t('modal.ws_slug_placeholder')}
                    className="form-input"
                  />
                  <span style={{ fontSize: '11px', color: '#64748B', marginTop: '4px' }}>
                    {t('modal.ws_slug_hint')}
                  </span>
                </div>
                <div className="form-group">
                  <label className="form-label">{t('modal.ws_description')}</label>
                  <textarea
                    value={wsDesc}
                    onChange={(e) => setWsDesc(e.target.value)}
                    placeholder={t('modal.ws_desc_placeholder')}
                    className="form-input"
                    rows={3}
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setShowWsModal(false)}>{t('modal.ws_cancel')}</button>
                <button type="submit" className="btn btn-primary">{t('modal.ws_create')}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Reusable Modal Create Project */}
      {showProjModal && currentWorkspace && (
        <ProjectModal
          isOpen={showProjModal}
          onClose={() => setShowProjModal(false)}
          workspaceId={currentWorkspace.id}
        />
      )}
    </div>
  );
};

