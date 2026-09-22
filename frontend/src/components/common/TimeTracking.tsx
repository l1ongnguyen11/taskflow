import React, { useEffect, useState, useCallback, useRef } from 'react';
import { timeLogApi, getApiErrorMessage } from '../../api';
import { TimeLogItem, TimeReportResponse, ManualLogRequest } from '../../types';
import { useLanguage } from '../../contexts/LanguageContext';
import { useAuth } from '../../contexts/AuthContext';
import {
  Play,
  Square,
  Clock,
  Plus,
  AlertCircle,
  CheckCircle2,
  Loader2,
  Calendar,
  User as UserIcon,
  FileText,
} from 'lucide-react';
import './TimeTracking.css';

export interface TimeTrackingProps {
  taskId: string;
  onTotalTimeChange?: (totalMinutes: number) => void;
}

function formatDuration(minutes: number): string {
  if (!minutes || minutes <= 0) return '0m';
  const hrs = Math.floor(minutes / 60);
  const mins = minutes % 60;
  if (hrs > 0 && mins > 0) return `${hrs}h ${mins}m`;
  if (hrs > 0) return `${hrs}h`;
  return `${mins}m`;
}

function formatDigits(num: number): string {
  return num < 10 ? `0${num}` : `${num}`;
}

export const TimeTracking: React.FC<TimeTrackingProps> = ({ taskId, onTotalTimeChange }) => {
  const { t, locale } = useLanguage();
  const { user } = useAuth();

  // Active Timer state
  const [activeTimer, setActiveTimer] = useState<TimeLogItem | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState<number>(0);
  const [timerDesc, setTimerDesc] = useState<string>('');
  const [isStartingTimer, setIsStartingTimer] = useState<boolean>(false);
  const [isStoppingTimer, setIsStoppingTimer] = useState<boolean>(false);

  // Time Report state
  const [report, setReport] = useState<TimeReportResponse | null>(null);
  const [loadingReport, setLoadingReport] = useState<boolean>(true);
  const [reportError, setReportError] = useState<string | null>(null);

  // Manual Log Form state
  const [showManualForm, setShowManualForm] = useState<boolean>(false);
  const [manualStartedAt, setManualStartedAt] = useState<string>(
    new Date(Date.now() - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16)
  );
  const [manualDuration, setManualDuration] = useState<number>(30);
  const [manualDesc, setManualDesc] = useState<string>('');
  const [manualError, setManualError] = useState<string | null>(null);
  const [isSubmittingManual, setIsSubmittingManual] = useState<boolean>(false);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  // Ticking interval ref
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // 1. Fetch Active Timer
  const fetchActiveTimer = useCallback(async () => {
    try {
      const res = await timeLogApi.getActiveTimer();
      if (res.success) {
        setActiveTimer(res.data || null);
      }
    } catch (err) {
      console.error('Failed to fetch active timer:', err);
    }
  }, []);

  // 2. Fetch Time Report
  const fetchTimeReport = useCallback(async () => {
    if (!taskId) return;
    setLoadingReport(true);
    setReportError(null);
    try {
      const res = await timeLogApi.getTimeReport(taskId, { limit: 100 });
      if (res.success && res.data) {
        setReport(res.data);
        if (onTotalTimeChange) {
          onTotalTimeChange(res.data.totalDurationMinutes);
        }
      } else {
        setReportError(res.message || 'Failed to load time logs.');
      }
    } catch (err: any) {
      setReportError(getApiErrorMessage(err) || 'Failed to load time logs.');
    } finally {
      setLoadingReport(false);
    }
  }, [taskId, onTotalTimeChange]);

  useEffect(() => {
    fetchActiveTimer();
    fetchTimeReport();
  }, [fetchActiveTimer, fetchTimeReport]);

  // 3. Handle Live Ticking for Running Timer
  useEffect(() => {
    if (activeTimer && activeTimer.isRunning) {
      const startTime = new Date(activeTimer.startedAt).getTime();

      const updateElapsed = () => {
        const now = Date.now();
        const diffSec = Math.max(0, Math.floor((now - startTime) / 1000));
        setElapsedSeconds(diffSec);
      };

      updateElapsed();
      timerRef.current = setInterval(updateElapsed, 1000);
    } else {
      setElapsedSeconds(0);
      if (timerRef.current) clearInterval(timerRef.current);
    }

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [activeTimer]);

  const notifySuccess = (msg: string) => {
    setActionSuccess(msg);
    setTimeout(() => setActionSuccess(null), 3000);
  };

  // --- Handlers ---
  const handleStartTimer = async () => {
    if (!taskId) return;
    setIsStartingTimer(true);
    setReportError(null);

    try {
      const res = await timeLogApi.startTimer(taskId, timerDesc.trim() || undefined);
      if (res.success && res.data) {
        setActiveTimer(res.data);
        setTimerDesc('');
        notifySuccess(t('time.timer_running'));
        await fetchTimeReport();
      } else {
        setReportError(res.message || 'Failed to start timer.');
      }
    } catch (err: any) {
      setReportError(getApiErrorMessage(err) || 'Failed to start timer.');
    } finally {
      setIsStartingTimer(false);
    }
  };

  const handleStopTimer = async () => {
    setIsStoppingTimer(true);
    setReportError(null);

    try {
      const res = await timeLogApi.stopTimer();
      if (res.success) {
        setActiveTimer(null);
        notifySuccess('Timer stopped and logged successfully!');
        await fetchTimeReport();
      } else {
        setReportError(res.message || 'Failed to stop timer.');
      }
    } catch (err: any) {
      setReportError(getApiErrorMessage(err) || 'Failed to stop timer.');
    } finally {
      setIsStoppingTimer(false);
    }
  };

  const handleManualSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setManualError(null);

    // Validation rules
    if (!manualStartedAt) {
      setManualError(t('time.error_started_at'));
      return;
    }

    if (!manualDuration || manualDuration <= 0) {
      setManualError(t('time.error_duration'));
      return;
    }

    if (manualDesc && manualDesc.length > 1000) {
      setManualError(t('time.error_desc_len'));
      return;
    }

    setIsSubmittingManual(true);

    try {
      const payload: ManualLogRequest = {
        startedAt: new Date(manualStartedAt).toISOString(),
        durationMinutes: Number(manualDuration),
        description: manualDesc.trim() || undefined,
      };

      const res = await timeLogApi.manualLog(taskId, payload);
      if (res.success) {
        notifySuccess(t('time.log_btn') + ' successful!');
        setManualDesc('');
        setShowManualForm(false);
        await fetchTimeReport();
      } else {
        setManualError(res.message || 'Failed to log time.');
      }
    } catch (err: any) {
      setManualError(getApiErrorMessage(err) || 'Failed to log time.');
    } finally {
      setIsSubmittingManual(false);
    }
  };

  // Format HH:MM:SS for live timer
  const hours = Math.floor(elapsedSeconds / 3600);
  const minutes = Math.floor((elapsedSeconds % 3600) / 60);
  const seconds = elapsedSeconds % 60;
  const digitsDisplay = `${formatDigits(hours)}:${formatDigits(minutes)}:${formatDigits(seconds)}`;

  const isTimerRunningOnThisTask = activeTimer && activeTimer.taskId === taskId;
  const isTimerRunningOnOtherTask = activeTimer && activeTimer.taskId !== taskId;

  return (
    <div className="time-tracking-container">
      {/* Alert Banners */}
      {actionSuccess && (
        <div className="badge badge-success" style={{ padding: '10px 14px', borderRadius: 8 }}>
          <CheckCircle2 size={16} />
          <span>{actionSuccess}</span>
        </div>
      )}

      {reportError && (
        <div className="badge badge-danger" style={{ padding: '10px 14px', borderRadius: 8 }}>
          <AlertCircle size={16} />
          <span>{reportError}</span>
        </div>
      )}

      {/* 1. Timer Controls Section */}
      <div className={`active-timer-card ${activeTimer ? 'running' : ''}`}>
        <div className="active-timer-header">
          <div className="active-timer-title">
            {activeTimer ? (
              <>
                <span className="pulse-dot" />
                <span>
                  {isTimerRunningOnThisTask ? t('time.timer_running') : t('time.running_on_other')}
                </span>
              </>
            ) : (
              <>
                <Clock size={16} />
                <span>{t('time.title')}</span>
              </>
            )}
          </div>

          {activeTimer && isTimerRunningOnThisTask && (
            <span style={{ fontSize: 12, opacity: 0.8 }}>
              Started {new Date(activeTimer.startedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
            </span>
          )}
        </div>

        {activeTimer ? (
          <div>
            <div className="timer-digits">{digitsDisplay}</div>
            {activeTimer.description && (
              <div style={{ fontSize: 13, opacity: 0.9, fontStyle: 'italic', marginBottom: 12 }}>
                "{activeTimer.description}"
              </div>
            )}

            <div className="active-timer-actions">
              <button
                type="button"
                className="btn btn-danger"
                onClick={handleStopTimer}
                disabled={isStoppingTimer}
                style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}
              >
                {isStoppingTimer ? (
                  <Loader2 size={16} style={{ animation: 'spin 1s linear infinite' }} />
                ) : (
                  <Square size={16} />
                )}
                <span>{t('time.stop_timer')}</span>
              </button>
            </div>
          </div>
        ) : (
          <div>
            <div className="form-group" style={{ marginBottom: 12 }}>
              <input
                type="text"
                className="form-control"
                placeholder={t('time.desc_placeholder')}
                value={timerDesc}
                onChange={(e) => setTimerDesc(e.target.value)}
                disabled={isStartingTimer}
                style={{ background: 'rgba(255, 255, 255, 0.1)', color: '#ffffff', border: '1px solid rgba(255, 255, 255, 0.2)' }}
              />
            </div>

            <button
              type="button"
              className="btn btn-primary"
              onClick={handleStartTimer}
              disabled={isStartingTimer}
              style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}
            >
              {isStartingTimer ? (
                <Loader2 size={16} style={{ animation: 'spin 1s linear infinite' }} />
              ) : (
                <Play size={16} />
              )}
              <span>{t('time.start_timer')}</span>
            </button>
          </div>
        )}
      </div>

      {/* 2. Total Time Stats Header */}
      <div className="time-stats-card">
        <div className="stat-group">
          <span className="stat-label">{t('time.total_spent')}</span>
          <span className="stat-value">{formatDuration(report?.totalDurationMinutes ?? 0)}</span>
        </div>

        <button
          type="button"
          className="btn btn-secondary btn-sm"
          onClick={() => setShowManualForm(!showManualForm)}
          style={{ display: 'inline-flex', alignItems: 'center', gap: 5 }}
        >
          <Plus size={14} />
          <span>{t('time.add_manual')}</span>
        </button>
      </div>

      {/* 3. Manual Time Log Form */}
      {showManualForm && (
        <div className="manual-log-box">
          <div className="manual-log-header">
            <Clock size={16} />
            <span>{t('time.add_manual')}</span>
          </div>

          {manualError && (
            <div className="badge badge-danger" style={{ marginBottom: 12 }}>
              <AlertCircle size={14} />
              <span>{manualError}</span>
            </div>
          )}

          <form onSubmit={handleManualSubmit}>
            <div className="manual-log-grid">
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{t('time.started_at')} *</label>
                <input
                  type="datetime-local"
                  className="form-control"
                  value={manualStartedAt}
                  onChange={(e) => setManualStartedAt(e.target.value)}
                  required
                  disabled={isSubmittingManual}
                />
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">{t('time.duration_minutes')} *</label>
                <input
                  type="number"
                  min="1"
                  className="form-control"
                  value={manualDuration}
                  onChange={(e) => setManualDuration(Number(e.target.value))}
                  required
                  disabled={isSubmittingManual}
                />
              </div>
            </div>

            <div className="form-group" style={{ marginBottom: 12 }}>
              <label className="form-label">{t('time.description')}</label>
              <textarea
                className="form-control"
                rows={2}
                placeholder={t('time.desc_placeholder')}
                value={manualDesc}
                onChange={(e) => setManualDesc(e.target.value)}
                maxLength={1000}
                disabled={isSubmittingManual}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={() => setShowManualForm(false)}
                disabled={isSubmittingManual}
              >
                {t('board.cancel')}
              </button>
              <button type="submit" className="btn btn-primary btn-sm" disabled={isSubmittingManual}>
                {isSubmittingManual ? <Loader2 size={14} style={{ animation: 'spin 1s linear infinite' }} /> : <Plus size={14} />}
                <span>{t('time.log_btn')}</span>
              </button>
            </div>
          </form>
        </div>
      )}

      {/* 4. Time Logs History List */}
      <div>
        <div style={{ fontSize: 14, fontWeight: 600, color: 'var(--text-main)', marginBottom: 12 }}>
          {t('time.log_history')} ({report?.entries?.length ?? 0})
        </div>

        {loadingReport ? (
          <div style={{ textAlign: 'center', padding: '24px 0', color: 'var(--text-subtle)' }}>
            <Loader2 size={18} style={{ animation: 'spin 1s linear infinite' }} />
            <span style={{ marginLeft: 8 }}>Loading logs...</span>
          </div>
        ) : !report?.entries || report.entries.length === 0 ? (
          <div style={{ fontStyle: 'italic', fontSize: 13, color: 'var(--text-subtle)', textAlign: 'center', padding: '20px 0' }}>
            {t('time.no_logs')}
          </div>
        ) : (
          <div className="time-logs-list">
            {report.entries.map((log) => {
              const startDate = new Date(log.startedAt).toLocaleDateString(locale === 'vi' ? 'vi-VN' : 'en-US', {
                year: 'numeric',
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              });

              return (
                <div key={log.id} className="time-log-item-card">
                  <div>
                    <div className="time-log-user">
                      <UserIcon size={14} style={{ marginRight: 5, verticalAlign: 'middle' }} />
                      {log.userDisplayName || 'User'}
                    </div>

                    {log.description && <div className="time-log-desc">{log.description}</div>}

                    <div className="time-log-meta">
                      <span>
                        <Calendar size={12} style={{ marginRight: 4, verticalAlign: 'middle' }} />
                        {startDate}
                      </span>
                    </div>
                  </div>

                  <div className="time-log-duration">{formatDuration(log.durationMinutes)}</div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
