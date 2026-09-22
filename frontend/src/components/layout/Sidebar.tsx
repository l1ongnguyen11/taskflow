import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { 
  LayoutDashboard, 
  FolderKanban, 
  CheckSquare, 
  Users, 
  Bell, 
  Settings, 
  Plus, 
  ChevronDown,
  Layers,
  BarChart3
} from 'lucide-react';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useLanguage } from '../../contexts/LanguageContext';
import { Logo } from '../common/Logo';
import './Sidebar.css';

interface SidebarProps {
  onOpenCreateWorkspace?: () => void;
  onOpenCreateProject?: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ onOpenCreateWorkspace, onOpenCreateProject }) => {
  const { workspaces, currentWorkspace, projects, selectWorkspace } = useWorkspace();
  const { t } = useLanguage();
  const navigate = useNavigate();

  return (
    <aside className="sidebar">
      {/* Brand Header */}
      <div className="sidebar-brand" onClick={() => navigate('/workspaces')}>
        <Logo size={32} showText={true} textColor="#FFFFFF" />
      </div>

      {/* Workspace Selector */}
      <div className="sidebar-section">
        <div className="section-title">
          <span>{t('sidebar.workspace')}</span>
          <button className="icon-btn" onClick={onOpenCreateWorkspace} title={t('sidebar.create_workspace')}>
            <Plus size={16} />
          </button>
        </div>
        
        {currentWorkspace ? (
          <div className="workspace-dropdown">
            <select
              value={currentWorkspace.id}
              onChange={(e) => selectWorkspace(e.target.value)}
              className="workspace-select"
            >
              {workspaces.map((ws) => (
                <option key={ws.id} value={ws.id}>
                  {ws.name}
                </option>
              ))}
            </select>
            <ChevronDown className="select-arrow" size={16} />
          </div>
        ) : (
          <button className="btn btn-secondary btn-sm full-width" onClick={onOpenCreateWorkspace}>
            {t('sidebar.create_workspace_btn')}
          </button>
        )}
      </div>

      {/* Main Navigation */}
      <nav className="sidebar-nav">
        <div className="section-title">{t('sidebar.menu')}</div>

        {currentWorkspace && (
          <>
            <NavLink to={`/workspaces/${currentWorkspace.id}/dashboard`} className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
              <LayoutDashboard size={18} />
              <span>{t('sidebar.dashboard')}</span>
            </NavLink>

            <NavLink to={`/workspaces/${currentWorkspace.id}/projects`} className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
              <FolderKanban size={18} />
              <span>{t('sidebar.projects_nav')}</span>
            </NavLink>

            <NavLink to={`/workspaces/${currentWorkspace.id}/reports`} className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
              <BarChart3 size={18} />
              <span>{t('sidebar.reports')}</span>
            </NavLink>

            <NavLink to={`/workspaces/${currentWorkspace.id}/members`} className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
              <Users size={18} />
              <span>{t('sidebar.members')}</span>
            </NavLink>

            <NavLink to="/notifications" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
              <Bell size={18} />
              <span>{t('sidebar.notifications')}</span>
            </NavLink>
          </>
        )}
      </nav>

      {/* Projects Navigation Section */}
      <div className="sidebar-section projects-section">
        <div className="section-title">
          <span>{t('sidebar.projects')}</span>
          <button className="icon-btn" onClick={onOpenCreateProject} title={t('sidebar.create_project')}>
            <Plus size={16} />
          </button>
        </div>

        <div className="projects-list">
          {projects.length > 0 ? (
            projects.map((proj) => (
              <NavLink
                key={proj.id}
                to={`/projects/${proj.id}/board`}
                className={({ isActive }) => `project-item ${isActive ? 'active' : ''}`}
              >
                <div className="project-badge">{proj.key}</div>
                <span className="project-name">{proj.name}</span>
              </NavLink>
            ))
          ) : (
            <div className="empty-projects">
              <span>{t('sidebar.no_projects')}</span>
              <button className="btn-link" onClick={onOpenCreateProject}>
                {t('sidebar.add_project')}
              </button>
            </div>
          )}
        </div>
      </div>

      {/* User Footer Settings */}
      <div className="sidebar-footer">
        <NavLink to="/profile" className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}>
          <Settings size={18} />
          <span>{t('sidebar.account_settings')}</span>
        </NavLink>
      </div>
    </aside>
  );
};

