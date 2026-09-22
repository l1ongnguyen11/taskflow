import React, { useEffect, useState, useCallback, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../components/layout/MainLayout';
import { useLanguage } from '../../contexts/LanguageContext';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { boardApi } from '../../api/boardApi';
import { taskApi } from '../../api/taskApi';
import { labelApi } from '../../api/labelApi';
import { projectApi, ProjectMember } from '../../api/projectApi';
import { commentApi } from '../../api/commentApi';
import { getApiErrorMessage } from '../../api';
import { Board, BoardColumn, TaskItem, Project, Comment, TaskUserDto, LabelItem, TaskLabelDto } from '../../types';
import { ActivityTimeline } from '../../components/common/ActivityTimeline';
import { TaskAttachments } from '../../components/common/TaskAttachments';
import { TimeTracking } from '../../components/common/TimeTracking';
import { TaskChecklists } from '../../components/common/TaskChecklists';
import { TaskDependencies } from '../../components/common/TaskDependencies';
import { ConfirmModal } from '../../components/common/ConfirmModal';
import {
  Plus,
  Trash2,
  Calendar,
  User as UserIcon,
  X,
  Users,
  Pencil,
  AlertCircle,
  Check,
  FolderKanban,
  LayoutDashboard,
  ChevronLeft,
  ChevronRight,
  Palette,
  GripVertical,
  UserPlus,
  UserMinus,
  Eye,
  EyeOff,
  MessageSquare,
  Send,
  CornerDownRight,
  Loader2,
  History,
  Paperclip,
  Layers,
  Clock,
  Link2,
  Tag,
} from 'lucide-react';
import './Board.css';

export const BoardPage: React.FC = () => {
  const { projectId } = useParams<{ projectId: string }>();
  const { t } = useLanguage();
  const { user: currentUser } = useAuth();
  const { currentWorkspace } = useWorkspace();
  const navigate = useNavigate();

  // Activity & Tabs State
  const [activeTaskTab, setActiveTaskTab] = useState<'comments' | 'activity' | 'attachments' | 'time' | 'dependencies'>('comments');
  const [showProjectActivityModal, setShowProjectActivityModal] = useState<boolean>(false);
  const [attachmentCount, setAttachmentCount] = useState<number>(0);

  // Project & Boards State
  const [project, setProject] = useState<Project | null>(null);
  const [boards, setBoards] = useState<Board[]>([]);
  const [board, setBoard] = useState<Board | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  // Create Board Modal State
  const [showCreateBoardModal, setShowCreateBoardModal] = useState<boolean>(false);
  const [newBoardName, setNewBoardName] = useState<string>('');
  const [newBoardDesc, setNewBoardDesc] = useState<string>('');
  const [createBoardError, setCreateBoardError] = useState<string>('');
  const [isCreatingBoard, setIsCreatingBoard] = useState<boolean>(false);

  // Edit Board Modal State
  const [showEditBoardModal, setShowEditBoardModal] = useState<boolean>(false);
  const [editBoardName, setEditBoardName] = useState<string>('');
  const [editBoardDesc, setEditBoardDesc] = useState<string>('');
  const [editBoardError, setEditBoardError] = useState<string>('');
  const [isEditingBoard, setIsEditingBoard] = useState<boolean>(false);

  // Delete Board Modal State
  const [showDeleteBoardModal, setShowDeleteBoardModal] = useState<boolean>(false);
  const [deleteBoardError, setDeleteBoardError] = useState<string>('');
  const [isDeletingBoard, setIsDeletingBoard] = useState<boolean>(false);

  // Column creation
  const [showAddColumnModal, setShowAddColumnModal] = useState<boolean>(false);
  const [newColumnName, setNewColumnName] = useState<string>('');
  const [newColumnWipLimit, setNewColumnWipLimit] = useState<string>('');
  const [newColumnColor, setNewColumnColor] = useState<string>('');
  const [addColumnError, setAddColumnError] = useState<string>('');
  const [isAddingColumn, setIsAddingColumn] = useState<boolean>(false);

  // Edit Column Modal State
  const [showEditColumnModal, setShowEditColumnModal] = useState<boolean>(false);
  const [editColumnId, setEditColumnId] = useState<string>('');
  const [editColumnName, setEditColumnName] = useState<string>('');
  const [editColumnWipLimit, setEditColumnWipLimit] = useState<string>('');
  const [editColumnColor, setEditColumnColor] = useState<string>('');
  const [editColumnError, setEditColumnError] = useState<string>('');
  const [isEditingColumn, setIsEditingColumn] = useState<boolean>(false);

  // Delete Column Error State
  const [deleteColumnError, setDeleteColumnError] = useState<string>('');

  // Column accent color palette
  const COLUMN_COLORS = [
    '#3B82F6', '#8B5CF6', '#EC4899', '#EF4444', '#F97316',
    '#EAB308', '#22C55E', '#14B8A6', '#06B6D4', '#6366F1',
  ];

  // Task creation
  const [showAddTaskModal, setShowAddTaskModal] = useState<boolean>(false);
  const [activeColumnId, setActiveColumnId] = useState<string | null>(null);
  const [taskTitle, setTaskTitle] = useState<string>('');
  const [taskDesc, setTaskDesc] = useState<string>('');
  const [taskPriority, setTaskPriority] = useState<string>('medium');
  const [taskDueDate, setTaskDueDate] = useState<string>('');
  const [createTaskError, setCreateTaskError] = useState<string>('');
  const [isCreatingTask, setIsCreatingTask] = useState<boolean>(false);

  // Task detail & edit state
  const [selectedTask, setSelectedTask] = useState<TaskItem | null>(null);
  const [isEditingTask, setIsEditingTask] = useState<boolean>(false);
  const [editTaskTitle, setEditTaskTitle] = useState<string>('');
  const [editTaskDesc, setEditTaskDesc] = useState<string>('');
  const [editTaskPriority, setEditTaskPriority] = useState<string>('medium');
  const [editTaskDueDate, setEditTaskDueDate] = useState<string>('');
  const [editTaskColumnId, setEditTaskColumnId] = useState<string>('');
  const [taskError, setTaskError] = useState<string>('');
  const [isSavingTask, setIsSavingTask] = useState<boolean>(false);
  const [isDeletingTask, setIsDeletingTask] = useState<boolean>(false);

  // Project Members & Assignee State
  const [projectMembers, setProjectMembers] = useState<ProjectMember[]>([]);
  const [assigneeSearchQuery, setAssigneeSearchQuery] = useState<string>('');
  const [showAssigneeDropdown, setShowAssigneeDropdown] = useState<boolean>(false);

  // Labels State
  const [workspaceLabels, setWorkspaceLabels] = useState<LabelItem[]>([]);
  const [showLabelDropdown, setShowLabelDropdown] = useState<boolean>(false);
  const [showCreateLabelModal, setShowCreateLabelModal] = useState<boolean>(false);
  const [newLabelName, setNewLabelName] = useState<string>('');
  const [newLabelColor, setNewLabelColor] = useState<string>('#3B82F6');
  const [isCreatingLabel, setIsCreatingLabel] = useState<boolean>(false);
  const [createLabelError, setCreateLabelError] = useState<string>('');

  // Comment State
  const [comments, setComments] = useState<Comment[]>([]);
  const [commentsLoading, setCommentsLoading] = useState<boolean>(false);
  const [commentError, setCommentError] = useState<string>('');
  const [newCommentText, setNewCommentText] = useState<string>('');
  const [isSubmittingComment, setIsSubmittingComment] = useState<boolean>(false);

  // Edit Comment State
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null);
  const [editCommentText, setEditCommentText] = useState<string>('');
  const [isSavingComment, setIsSavingComment] = useState<boolean>(false);

  // Reply Comment State
  const [replyingCommentId, setReplyingCommentId] = useState<string | null>(null);
  const [replyCommentText, setReplyCommentText] = useState<string>('');

  // 1. Fetch Board Details & Tasks
  const loadSingleBoard = useCallback(async (targetBoardId: string) => {
    try {
      const boardRes = await boardApi.getBoard(targetBoardId);
      if (boardRes.success && boardRes.data) {
        setBoard(boardRes.data);
      }

      const tasksRes = await taskApi.getBoardTasks(targetBoardId);
      if (tasksRes.success && tasksRes.data) {
        setTasks(tasksRes.data);
      } else {
        setTasks([]);
      }
    } catch (err) {
      console.error('Error loading single board data:', err);
    }
  }, []);

  // 2. Load Project & Boards Data
  const loadBoardData = useCallback(async () => {
    if (!projectId) return;
    setLoading(true);
    try {
      // Fetch Project Details & Members
      const projRes = await projectApi.getProject(projectId);
      if (projRes.success && projRes.data) {
        setProject(projRes.data);
        if (projRes.data.workspaceId) {
          try {
            const labelsRes = await labelApi.getWorkspaceLabels(projRes.data.workspaceId);
            if (labelsRes.success && labelsRes.data) {
              setWorkspaceLabels(labelsRes.data);
            }
          } catch (lErr) {
            console.error('Error loading workspace labels:', lErr);
          }
        }
      }

      const membersRes = await projectApi.getProjectMembers(projectId, 100, 0);
      if (membersRes.success && membersRes.data) {
        setProjectMembers(membersRes.data);
      }

      // Fetch Boards in Project
      const boardsRes = await boardApi.getBoards(projectId, 100, 0);
      let currentBoard: Board | null = null;

      if (boardsRes.success && boardsRes.data && boardsRes.data.length > 0) {
        setBoards(boardsRes.data);
        currentBoard = boardsRes.data[0];
      } else {
        // Auto create default board if none exists
        const createRes = await boardApi.createBoard(projectId, {
          name: 'Main Kanban Board',
          description: 'Default board for project',
        });
        if (createRes.success && createRes.data) {
          currentBoard = createRes.data;
          setBoards([currentBoard]);
          // Auto create standard columns
          await boardApi.createColumn(currentBoard.id, { name: 'To Do' });
          await boardApi.createColumn(currentBoard.id, { name: 'In Progress' });
          await boardApi.createColumn(currentBoard.id, { name: 'Done' });
        }
      }

      if (currentBoard) {
        await loadSingleBoard(currentBoard.id);
      } else {
        setBoard(null);
        setTasks([]);
      }
    } catch (err) {
      console.error('Error loading project/boards:', err);
    } finally {
      setLoading(false);
    }
  }, [projectId, loadSingleBoard]);

  useEffect(() => {
    loadBoardData();
  }, [loadBoardData]);

  // Handle Switch Board
  const handleSwitchBoard = async (targetBoardId: string) => {
    setLoading(true);
    await loadSingleBoard(targetBoardId);
    setLoading(false);
  };

  // Handle Create Board
  const handleCreateBoardSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId || !newBoardName.trim()) return;

    setCreateBoardError('');
    setIsCreatingBoard(true);

    try {
      const res = await boardApi.createBoard(projectId, {
        name: newBoardName.trim(),
        description: newBoardDesc.trim() || undefined,
      });

      if (res.success && res.data) {
        const createdBoard = res.data;
        // Auto create standard columns for new board
        await boardApi.createColumn(createdBoard.id, { name: 'To Do' });
        await boardApi.createColumn(createdBoard.id, { name: 'In Progress' });
        await boardApi.createColumn(createdBoard.id, { name: 'Done' });

        setNewBoardName('');
        setNewBoardDesc('');
        setShowCreateBoardModal(false);

        // Reload boards list & switch to newly created board
        const boardsRes = await boardApi.getBoards(projectId, 100, 0);
        if (boardsRes.success && boardsRes.data) {
          setBoards(boardsRes.data);
        }
        await loadSingleBoard(createdBoard.id);
      } else {
        setCreateBoardError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setCreateBoardError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsCreatingBoard(false);
    }
  };

  // Handle Edit Board Name & Desc
  const openEditBoardModal = () => {
    if (!board) return;
    setEditBoardName(board.name);
    setEditBoardDesc(board.description || '');
    setEditBoardError('');
    setShowEditBoardModal(true);
  };

  const handleEditBoardSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!board || !editBoardName.trim()) return;

    setEditBoardError('');
    setIsEditingBoard(true);

    try {
      const res = await boardApi.updateBoard(board.id, {
        name: editBoardName.trim(),
        description: editBoardDesc.trim() || undefined,
      });

      if (res.success && res.data) {
        setShowEditBoardModal(false);
        // Refresh local board state & boards list
        setBoard((prev) => (prev ? { ...prev, name: res.data!.name, description: res.data!.description } : null));
        if (projectId) {
          const boardsRes = await boardApi.getBoards(projectId, 100, 0);
          if (boardsRes.success && boardsRes.data) {
            setBoards(boardsRes.data);
          }
        }
      } else {
        setEditBoardError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setEditBoardError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsEditingBoard(false);
    }
  };

  // Handle Delete Board
  const openDeleteBoardModal = () => {
    setDeleteBoardError('');
    setShowDeleteBoardModal(true);
  };

  const handleDeleteBoardSubmit = async () => {
    if (!board || !projectId) return;

    setDeleteBoardError('');
    setIsDeletingBoard(true);

    try {
      const res = await boardApi.deleteBoard(board.id);
      if (res.success) {
        setShowDeleteBoardModal(false);
        // Refresh project boards
        await loadBoardData();
      } else {
        setDeleteBoardError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setDeleteBoardError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsDeletingBoard(false);
    }
  };

  // Handle Column Creation
  const handleAddColumn = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!board || !newColumnName.trim()) return;

    setAddColumnError('');
    setIsAddingColumn(true);

    try {
      const payload: any = { name: newColumnName.trim() };
      if (newColumnWipLimit && parseInt(newColumnWipLimit) > 0) {
        payload.wipLimit = parseInt(newColumnWipLimit);
      }
      if (newColumnColor) {
        payload.color = newColumnColor;
      }

      const res = await boardApi.createColumn(board.id, payload);
      if (res.success && res.data) {
        setBoard({
          ...board,
          columns: [...(board.columns || []), res.data],
        });
        setNewColumnName('');
        setNewColumnWipLimit('');
        setNewColumnColor('');
        setShowAddColumnModal(false);
      } else {
        setAddColumnError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setAddColumnError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsAddingColumn(false);
    }
  };

  // Open Edit Column Modal
  const openEditColumnModal = (col: BoardColumn) => {
    setEditColumnId(col.id);
    setEditColumnName(col.name);
    setEditColumnWipLimit(col.wipLimit ? String(col.wipLimit) : '');
    setEditColumnColor(col.color || '');
    setEditColumnError('');
    setShowEditColumnModal(true);
  };

  // Handle Edit Column Submit
  const handleEditColumnSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editColumnId || !editColumnName.trim()) return;

    setEditColumnError('');
    setIsEditingColumn(true);

    try {
      const payload: any = { name: editColumnName.trim() };
      payload.wipLimit = editColumnWipLimit ? parseInt(editColumnWipLimit) : null;
      payload.color = editColumnColor || null;

      const res = await boardApi.updateColumn(editColumnId, payload);
      if (res.success && res.data && board) {
        setBoard({
          ...board,
          columns: board.columns.map((c) =>
            c.id === editColumnId ? { ...c, ...res.data } : c
          ),
        });
        setShowEditColumnModal(false);
      } else {
        setEditColumnError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setEditColumnError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsEditingColumn(false);
    }
  };

  // Handle Column Reorder (Move Left / Move Right)
  const handleMoveColumn = async (columnId: string, direction: 'left' | 'right') => {
    if (!board) return;

    const sortedColumns = [...board.columns].sort((a, b) => a.position - b.position);
    const currentIndex = sortedColumns.findIndex((c) => c.id === columnId);
    if (currentIndex < 0) return;

    const targetIndex = direction === 'left' ? currentIndex - 1 : currentIndex + 1;
    if (targetIndex < 0 || targetIndex >= sortedColumns.length) return;

    // Swap positions
    const updated = [...sortedColumns];
    [updated[currentIndex], updated[targetIndex]] = [updated[targetIndex], updated[currentIndex]];

    // Assign new sequential positions
    const reorderPayload = updated.map((col, idx) => ({
      columnId: col.id,
      position: idx,
    }));

    // Optimistic UI update
    const optimisticColumns = updated.map((col, idx) => ({ ...col, position: idx }));
    setBoard({ ...board, columns: optimisticColumns });

    try {
      await boardApi.reorderColumns(board.id, reorderPayload);
    } catch (err: any) {
      console.error('Failed to reorder columns:', err);
      // Revert on failure
      await loadSingleBoard(board.id);
    }
  };

  // Custom Confirm Modal State for Column Deletion
  const [columnToDeleteId, setColumnToDeleteId] = useState<string | null>(null);
  const [isDeletingColumn, setIsDeletingColumn] = useState<boolean>(false);

  const openDeleteColumnModal = (columnId: string) => {
    setColumnToDeleteId(columnId);
    setDeleteColumnError('');
  };

  const handleConfirmDeleteColumn = async () => {
    if (!columnToDeleteId) return;

    setDeleteColumnError('');
    setIsDeletingColumn(true);
    try {
      const res = await boardApi.deleteColumn(columnToDeleteId);
      if (res.success && board) {
        setBoard({
          ...board,
          columns: board.columns.filter((c) => c.id !== columnToDeleteId),
        });
        // Also remove tasks belonging to this column
        setTasks((prev) => prev.filter((t) => t.boardColumnId !== columnToDeleteId));
        setColumnToDeleteId(null);
      } else {
        const msg = res.message || t('board.action_error');
        setDeleteColumnError(msg);
        alert(msg);
      }
    } catch (err: any) {
      const msg = getApiErrorMessage(err) || t('board.action_error');
      setDeleteColumnError(msg);
      alert(msg);
    } finally {
      setIsDeletingColumn(false);
    }
  };

  // Drag and Drop Task Handlers
  const [draggedTaskId, setDraggedTaskId] = useState<string | null>(null);
  const [dragOverColumnId, setDragOverColumnId] = useState<string | null>(null);
  const dragJustEndedRef = useRef<boolean>(false);

  const handleTaskDragStart = (e: React.DragEvent, taskId: string) => {
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', taskId);
    setDraggedTaskId(taskId);
    dragJustEndedRef.current = false;
  };

  const handleTaskDragOver = (e: React.DragEvent, columnId: string) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (dragOverColumnId !== columnId) {
      setDragOverColumnId(columnId);
    }
  };

  const handleTaskDragEnter = (e: React.DragEvent, columnId: string) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    if (dragOverColumnId !== columnId) {
      setDragOverColumnId(columnId);
    }
  };

  const handleTaskDragLeave = (e: React.DragEvent, columnId: string) => {
    e.preventDefault();
    if (e.currentTarget.contains(e.relatedTarget as Node)) return;
    if (dragOverColumnId === columnId) {
      setDragOverColumnId(null);
    }
  };

  const handleTaskDragEnd = () => {
    setDraggedTaskId(null);
    setDragOverColumnId(null);
    dragJustEndedRef.current = true;
    setTimeout(() => {
      dragJustEndedRef.current = false;
    }, 200);
  };

  const handleTaskDrop = async (e: React.DragEvent, targetColumnId: string) => {
    e.preventDefault();
    setDragOverColumnId(null);
    const taskId = e.dataTransfer.getData('text/plain') || draggedTaskId;
    setDraggedTaskId(null);

    if (!taskId) return;

    const taskToMove = tasks.find((t) => t.id === taskId);
    if (!taskToMove || taskToMove.boardColumnId === targetColumnId) return;

    const originalColumnId = taskToMove.boardColumnId;

    // Optimistic UI update
    setTasks((prev) =>
      prev.map((t) => (t.id === taskId ? { ...t, boardColumnId: targetColumnId } : t))
    );

    try {
      const res = await taskApi.moveTask(taskId, targetColumnId, 0);
      if (!res.success) {
        // Rollback
        setTasks((prev) =>
          prev.map((t) => (t.id === taskId ? { ...t, boardColumnId: originalColumnId } : t))
        );
        alert(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      // Rollback UI
      setTasks((prev) =>
        prev.map((t) => (t.id === taskId ? { ...t, boardColumnId: originalColumnId } : t))
      );
      const msg = getApiErrorMessage(err) || t('board.action_error');
      alert(msg);
    }
  };

  // Handle Task Creation
  const handleAddTask = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeColumnId || !taskTitle.trim()) return;

    setCreateTaskError('');
    setIsCreatingTask(true);

    try {
      const payload: any = {
        title: taskTitle.trim(),
        description: taskDesc.trim() || undefined,
        priority: taskPriority,
      };
      if (taskDueDate) {
        payload.dueDate = taskDueDate;
      }

      const res = await taskApi.createTask(activeColumnId, payload);
      if (res.success && res.data) {
        setTasks((prev) => [...prev, res.data!]);
        setTaskTitle('');
        setTaskDesc('');
        setTaskDueDate('');
        setTaskPriority('medium');
        setShowAddTaskModal(false);
      } else {
        setCreateTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setCreateTaskError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsCreatingTask(false);
    }
  };

  const fetchComments = useCallback(async (taskId: string) => {
    setCommentsLoading(true);
    setCommentError('');
    try {
      const res = await commentApi.getTaskComments(taskId);
      if (res.success && res.data) {
        setComments(res.data);
      } else {
        setComments([]);
        if (res.message) setCommentError(res.message);
      }
    } catch (err: any) {
      console.error('Failed to load comments:', err);
      setCommentError(getApiErrorMessage(err) || t('board.comment_error'));
    } finally {
      setCommentsLoading(false);
    }
  }, [t]);

  // Open Task Detail
  const openTaskDetail = async (task: TaskItem) => {
    if (dragJustEndedRef.current) return;
    setSelectedTask(task);
    setIsEditingTask(false);
    setEditTaskTitle(task.title);
    setEditTaskDesc(task.description || '');
    setEditTaskPriority(task.priority || 'medium');
    setEditTaskDueDate(task.dueDate ? task.dueDate.split('T')[0] : '');
    setEditTaskColumnId(task.boardColumnId || '');
    setTaskError('');

    setActiveTaskTab('comments');
    setEditingCommentId(null);
    setReplyingCommentId(null);
    setNewCommentText('');
    setReplyCommentText('');
    setCommentError('');

    await fetchComments(task.id);
  };

  // Handle Save Task Edits
  const handleSaveTask = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTask || !editTaskTitle.trim()) return;

    setTaskError('');
    setIsSavingTask(true);

    try {
      const payload: any = {
        title: editTaskTitle.trim(),
        description: editTaskDesc.trim() || undefined,
        priority: editTaskPriority,
        dueDate: editTaskDueDate || undefined,
      };

      const res = await taskApi.updateTask(selectedTask.id, payload);
      let updatedTask = res.data || selectedTask;

      // Handle Column / Status Change if column changed
      if (editTaskColumnId && editTaskColumnId !== selectedTask.boardColumnId) {
        const moveRes = await taskApi.moveTask(selectedTask.id, editTaskColumnId, 0);
        if (moveRes.success && moveRes.data) {
          updatedTask = moveRes.data;
        } else {
          updatedTask = { ...updatedTask, boardColumnId: editTaskColumnId };
        }
      } else {
        updatedTask = { ...updatedTask, title: payload.title, description: payload.description, priority: payload.priority, dueDate: payload.dueDate };
      }

      setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, ...updatedTask } : tItem)));
      setSelectedTask((prev) => (prev ? { ...prev, ...updatedTask } : null));
      setIsEditingTask(false);
    } catch (err: any) {
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsSavingTask(false);
    }
  };

  // Handle Delete Task
  const [showDeleteTaskModal, setShowDeleteTaskModal] = useState<boolean>(false);

  const handleConfirmDeleteTask = async () => {
    if (!selectedTask) return;
    setTaskError('');
    setIsDeletingTask(true);
    try {
      const res = await taskApi.deleteTask(selectedTask.id);
      if (res.success) {
        setTasks((prev) => prev.filter((tItem) => tItem.id !== selectedTask.id));
        setSelectedTask(null);
        setShowDeleteTaskModal(false);
      } else {
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsDeletingTask(false);
    }
  };

  // Quick Sidebar Field Updates (Column, Priority, Due Date)
  const handleSidebarColumnChange = async (newColumnId: string) => {
    if (!selectedTask || newColumnId === selectedTask.boardColumnId) return;
    const prevColumnId = selectedTask.boardColumnId;
    setTaskError('');
    setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, boardColumnId: newColumnId } : tItem)));
    setSelectedTask((prev) => (prev ? { ...prev, boardColumnId: newColumnId } : null));
    setEditTaskColumnId(newColumnId);
    try {
      const res = await taskApi.moveTask(selectedTask.id, newColumnId, 0);
      if (!res.success) {
        setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, boardColumnId: prevColumnId } : tItem)));
        setSelectedTask((prev) => (prev ? { ...prev, boardColumnId: prevColumnId } : null));
        setEditTaskColumnId(prevColumnId || '');
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, boardColumnId: prevColumnId } : tItem)));
      setSelectedTask((prev) => (prev ? { ...prev, boardColumnId: prevColumnId } : null));
      setEditTaskColumnId(prevColumnId || '');
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  const handleSidebarPriorityChange = async (newPriority: string) => {
    if (!selectedTask || newPriority === selectedTask.priority) return;
    const prevPriority = selectedTask.priority;
    setTaskError('');
    setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, priority: newPriority } : tItem)));
    setSelectedTask((prev) => (prev ? { ...prev, priority: newPriority } : null));
    setEditTaskPriority(newPriority);
    try {
      const res = await taskApi.updateTask(selectedTask.id, { priority: newPriority });
      if (!res.success) {
        setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, priority: prevPriority } : tItem)));
        setSelectedTask((prev) => (prev ? { ...prev, priority: prevPriority } : null));
        setEditTaskPriority(prevPriority || 'medium');
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, priority: prevPriority } : tItem)));
      setSelectedTask((prev) => (prev ? { ...prev, priority: prevPriority } : null));
      setEditTaskPriority(prevPriority || 'medium');
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  const handleSidebarDueDateChange = async (newDueDate: string) => {
    if (!selectedTask) return;
    const prevDueDate = selectedTask.dueDate;
    setTaskError('');
    setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, dueDate: newDueDate || null } : tItem)));
    setSelectedTask((prev) => (prev ? { ...prev, dueDate: newDueDate || null } : null));
    setEditTaskDueDate(newDueDate);
    try {
      const res = await taskApi.updateTask(selectedTask.id, { dueDate: newDueDate || undefined });
      if (!res.success) {
        setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, dueDate: prevDueDate } : tItem)));
        setSelectedTask((prev) => (prev ? { ...prev, dueDate: prevDueDate } : null));
        setEditTaskDueDate(prevDueDate ? prevDueDate.split('T')[0] : '');
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTasks((prev) => prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, dueDate: prevDueDate } : tItem)));
      setSelectedTask((prev) => (prev ? { ...prev, dueDate: prevDueDate } : null));
      setEditTaskDueDate(prevDueDate ? prevDueDate.split('T')[0] : '');
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  // Assignee Handlers
  const handleAddAssignee = async (member: { userId: string; displayName: string; email: string; avatarUrl?: string | null }) => {
    if (!selectedTask) return;
    setTaskError('');

    try {
      const res = await taskApi.addAssignee(selectedTask.id, member.userId);
      if (res.success) {
        const newAssignee: TaskUserDto = {
          id: member.userId,
          displayName: member.displayName,
          email: member.email,
          avatarUrl: member.avatarUrl,
        };

        const updatedAssignees = [...(selectedTask.assignees || []), newAssignee];

        setSelectedTask((prev) => (prev ? { ...prev, assignees: updatedAssignees } : null));
        setTasks((prev) =>
          prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, assignees: updatedAssignees } : tItem))
        );

        setShowAssigneeDropdown(false);
        setAssigneeSearchQuery('');
      } else {
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  const handleRemoveAssignee = async (assigneeUserId: string) => {
    if (!selectedTask) return;
    setTaskError('');

    try {
      const res = await taskApi.removeAssignee(selectedTask.id, assigneeUserId);
      if (res.success) {
        const updatedAssignees = (selectedTask.assignees || []).filter((a) => a.id !== assigneeUserId);

        setSelectedTask((prev) => (prev ? { ...prev, assignees: updatedAssignees } : null));
        setTasks((prev) =>
          prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, assignees: updatedAssignees } : tItem))
        );
      } else {
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  // Watcher Handlers
  const isCurrentWatcher = selectedTask?.watchers?.some((w) => w.id === currentUser?.id);

  const handleToggleWatch = async () => {
    if (!selectedTask || !currentUser) return;
    setTaskError('');

    try {
      if (isCurrentWatcher) {
        // Unwatch
        const res = await taskApi.removeWatcher(selectedTask.id, currentUser.id);
        if (res.success) {
          const updatedWatchers = (selectedTask.watchers || []).filter((w) => w.id !== currentUser.id);
          setSelectedTask((prev) => (prev ? { ...prev, watchers: updatedWatchers } : null));
          setTasks((prev) =>
            prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, watchers: updatedWatchers } : tItem))
          );
        } else {
          setTaskError(res.message || t('board.action_error'));
        }
      } else {
        // Watch
        const res = await taskApi.addWatcher(selectedTask.id, currentUser.id);
        if (res.success) {
          const newWatcher: TaskUserDto = {
            id: currentUser.id,
            displayName: currentUser.displayName,
            email: currentUser.email,
            avatarUrl: currentUser.avatarUrl,
          };
          const updatedWatchers = [...(selectedTask.watchers || []), newWatcher];
          setSelectedTask((prev) => (prev ? { ...prev, watchers: updatedWatchers } : null));
          setTasks((prev) =>
            prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, watchers: updatedWatchers } : tItem))
          );
        } else {
          setTaskError(res.message || t('board.action_error'));
        }
      }
    } catch (err: any) {
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  const handleRemoveWatcher = async (watcherUserId: string) => {
    if (!selectedTask) return;
    setTaskError('');

    try {
      const res = await taskApi.removeWatcher(selectedTask.id, watcherUserId);
      if (res.success) {
        const updatedWatchers = (selectedTask.watchers || []).filter((w) => w.id !== watcherUserId);
        setSelectedTask((prev) => (prev ? { ...prev, watchers: updatedWatchers } : null));
        setTasks((prev) =>
          prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, watchers: updatedWatchers } : tItem))
        );
      } else {
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  // Label Handlers
  const handleAddLabelToTask = async (label: LabelItem | TaskLabelDto) => {
    if (!selectedTask) return;
    if (selectedTask.labels?.some((l) => l.id === label.id)) {
      setShowLabelDropdown(false);
      return;
    }
    setTaskError('');

    const newLabelDto: TaskLabelDto = {
      id: label.id,
      name: label.name,
      color: label.color,
    };

    const updatedLabels = [...(selectedTask.labels || []), newLabelDto];
    setSelectedTask((prev) => (prev ? { ...prev, labels: updatedLabels } : null));
    setTasks((prev) =>
      prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, labels: updatedLabels } : tItem))
    );

    try {
      const res = await labelApi.addLabelToTask(selectedTask.id, label.id);
      if (!res.success) {
        const reverted = (selectedTask.labels || []).filter((l) => l.id !== label.id);
        setSelectedTask((prev) => (prev ? { ...prev, labels: reverted } : null));
        setTasks((prev) =>
          prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, labels: reverted } : tItem))
        );
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      const reverted = (selectedTask.labels || []).filter((l) => l.id !== label.id);
      setSelectedTask((prev) => (prev ? { ...prev, labels: reverted } : null));
      setTasks((prev) =>
        prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, labels: reverted } : tItem))
      );
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
    setShowLabelDropdown(false);
  };

  const handleRemoveLabelFromTask = async (labelId: string) => {
    if (!selectedTask) return;
    setTaskError('');

    const previousLabels = selectedTask.labels || [];
    const updatedLabels = previousLabels.filter((l) => l.id !== labelId);
    setSelectedTask((prev) => (prev ? { ...prev, labels: updatedLabels } : null));
    setTasks((prev) =>
      prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, labels: updatedLabels } : tItem))
    );

    try {
      const res = await labelApi.removeLabelFromTask(selectedTask.id, labelId);
      if (!res.success) {
        setSelectedTask((prev) => (prev ? { ...prev, labels: previousLabels } : null));
        setTasks((prev) =>
          prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, labels: previousLabels } : tItem))
        );
        setTaskError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setSelectedTask((prev) => (prev ? { ...prev, labels: previousLabels } : null));
      setTasks((prev) =>
        prev.map((tItem) => (tItem.id === selectedTask.id ? { ...tItem, labels: previousLabels } : tItem))
      );
      setTaskError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  const handleCreateLabelSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const wsId = project?.workspaceId || currentWorkspace?.id;
    if (!wsId || !newLabelName.trim()) return;

    setCreateLabelError('');
    setIsCreatingLabel(true);

    try {
      const res = await labelApi.createLabel(wsId, {
        name: newLabelName.trim(),
        color: newLabelColor || '#3B82F6',
      });
      if (res.success && res.data) {
        const created = res.data;
        setWorkspaceLabels((prev) => [...prev, created]);
        setNewLabelName('');
        setNewLabelColor('#3B82F6');
        setShowCreateLabelModal(false);
        if (selectedTask) {
          await handleAddLabelToTask(created);
        }
      } else {
        setCreateLabelError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setCreateLabelError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsCreatingLabel(false);
    }
  };

  // Add Comment (Root or Reply)
  const handleAddComment = async (e: React.FormEvent, parentCommentId?: string) => {
    e.preventDefault();
    const text = parentCommentId ? replyCommentText : newCommentText;
    if (!selectedTask || !text.trim()) return;

    setCommentError('');
    setIsSubmittingComment(true);

    try {
      const res = await commentApi.createComment(selectedTask.id, text.trim(), parentCommentId);
      if (res.success) {
        if (parentCommentId) {
          setReplyCommentText('');
          setReplyingCommentId(null);
        } else {
          setNewCommentText('');
        }
        await fetchComments(selectedTask.id);
      } else {
        setCommentError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setCommentError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsSubmittingComment(false);
    }
  };

  // Start Editing Comment
  const handleStartEditComment = (comment: Comment) => {
    setEditingCommentId(comment.id);
    setEditCommentText(comment.body);
    setCommentError('');
  };

  // Save Edit Comment
  const handleSaveEditComment = async (commentId: string) => {
    if (!selectedTask || !editCommentText.trim()) return;

    setCommentError('');
    setIsSavingComment(true);

    try {
      const res = await commentApi.updateComment(commentId, editCommentText.trim());
      if (res.success) {
        setEditingCommentId(null);
        setEditCommentText('');
        await fetchComments(selectedTask.id);
      } else {
        setCommentError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setCommentError(getApiErrorMessage(err) || t('board.action_error'));
    } finally {
      setIsSavingComment(false);
    }
  };

  // Delete Comment
  const handleDeleteComment = async (commentId: string) => {
    if (!selectedTask) return;
    if (!confirm(t('board.comment_delete_confirm') || 'Are you sure you want to delete this comment?')) return;

    setCommentError('');
    try {
      const res = await commentApi.deleteComment(commentId);
      if (res.success) {
        await fetchComments(selectedTask.id);
      } else {
        setCommentError(res.message || t('board.action_error'));
      }
    } catch (err: any) {
      setCommentError(getApiErrorMessage(err) || t('board.action_error'));
    }
  };

  // Count total comments recursively
  const countTotalComments = (list: Comment[]): number => {
    let count = 0;
    for (const item of list) {
      count += 1;
      if (item.replies && item.replies.length > 0) {
        count += countTotalComments(item.replies);
      }
    }
    return count;
  };

  // Recursive Comment Component Renderer
  const renderCommentItem = (c: Comment, isNested = false) => {
    const authorName = c.userDisplayName || c.authorName || 'Unknown User';
    const avatarUrl = c.userAvatarUrl || c.authorAvatarUrl;
    const isAuthor = (c.userId && currentUser?.id) ? c.userId === currentUser.id : (c.authorId === currentUser?.id);
    const isEditing = editingCommentId === c.id;
    const isReplying = replyingCommentId === c.id;
    const isEdited = Boolean(c.updatedAt && c.createdAt && new Date(c.updatedAt).getTime() - new Date(c.createdAt).getTime() > 1000);

    return (
      <div key={c.id} className={`comment-item ${isNested ? 'is-nested' : ''}`}>
        {avatarUrl ? (
          <img src={avatarUrl} alt={authorName} className="comment-avatar-circle" />
        ) : (
          <div className="comment-avatar-circle">
            {authorName.charAt(0).toUpperCase()}
          </div>
        )}

        <div className="comment-main-box">
          <div className="comment-meta-header">
            <div className="comment-author-info">
              <span className="comment-author">{authorName}</span>
              <span className="comment-date">{new Date(c.createdAt).toLocaleString()}</span>
              {isEdited && <span className="comment-edited-tag">{t('board.comment_edited')}</span>}
            </div>
          </div>

          {isEditing ? (
            <div className="comment-edit-box">
              <textarea
                value={editCommentText}
                onChange={(e) => setEditCommentText(e.target.value)}
                className="form-input"
                rows={3}
                disabled={isSavingComment}
              />
              <div className="comment-edit-actions">
                <button
                  type="button"
                  className="btn btn-secondary btn-xs"
                  onClick={() => setEditingCommentId(null)}
                  disabled={isSavingComment}
                >
                  {t('board.comment_cancel')}
                </button>
                <button
                  type="button"
                  className="btn btn-primary btn-xs"
                  onClick={() => handleSaveEditComment(c.id)}
                  disabled={isSavingComment || !editCommentText.trim()}
                >
                  {isSavingComment ? '...' : t('board.comment_save')}
                </button>
              </div>
            </div>
          ) : (
            <>
              <p className="comment-body">{c.body}</p>

              <div className="comment-actions">
                <button
                  type="button"
                  className="comment-action-btn"
                  onClick={() => {
                    setReplyingCommentId(replyingCommentId === c.id ? null : c.id);
                    setReplyCommentText('');
                  }}
                >
                  <CornerDownRight size={12} />
                  <span>{t('board.comment_reply')}</span>
                </button>

                {isAuthor && (
                  <button
                    type="button"
                    className="comment-action-btn"
                    onClick={() => handleStartEditComment(c)}
                  >
                    <Pencil size={12} />
                    <span>{t('board.comment_edit')}</span>
                  </button>
                )}

                <button
                  type="button"
                  className="comment-action-btn danger"
                  onClick={() => handleDeleteComment(c.id)}
                >
                  <Trash2 size={12} />
                  <span>{t('board.comment_delete')}</span>
                </button>
              </div>
            </>
          )}

          {/* Inline Reply Form */}
          {isReplying && (
            <form onSubmit={(e) => handleAddComment(e, c.id)} className="comment-reply-box">
              <textarea
                value={replyCommentText}
                onChange={(e) => setReplyCommentText(e.target.value)}
                placeholder={t('board.comment_reply_placeholder')}
                className="form-input"
                rows={2}
                autoFocus
                disabled={isSubmittingComment}
              />
              <div className="comment-reply-actions">
                <button
                  type="button"
                  className="btn btn-secondary btn-xs"
                  onClick={() => setReplyingCommentId(null)}
                  disabled={isSubmittingComment}
                >
                  {t('board.comment_cancel')}
                </button>
                <button
                  type="submit"
                  className="btn btn-primary btn-xs"
                  disabled={isSubmittingComment || !replyCommentText.trim()}
                >
                  {isSubmittingComment ? '...' : t('board.comment_submit')}
                </button>
              </div>
            </form>
          )}

          {/* Render Nested Replies */}
          {c.replies && c.replies.length > 0 && (
            <div className="comment-replies" style={{ marginTop: 10, display: 'flex', flexDirection: 'column', gap: 10 }}>
              {c.replies.map((reply) => renderCommentItem(reply, true))}
            </div>
          )}
        </div>
      </div>
    );
  };

  const getPriorityBadgeClass = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'high':
        return 'badge-danger';
      case 'medium':
        return 'badge-warning';
      case 'low':
        return 'badge-primary';
      default:
        return 'badge-subtle';
    }
  };

  const getPriorityText = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'high':
        return t('board.priority_high');
      case 'medium':
        return t('board.priority_medium');
      case 'low':
        return t('board.priority_low');
      default:
        return priority;
    }
  };

  return (
    <MainLayout title={project ? `${project.name} (${project.key})` : t('board.title')}>
      {/* Board Header Bar */}
      <div className="board-header-bar">
        <div className="board-info">
          <h2>
            <LayoutDashboard size={24} style={{ verticalAlign: 'middle', color: 'var(--primary)' }} />
            {board?.name || t('board.title')}

            {/* Board Selector & Controls */}
            {boards.length > 0 && (
              <div className="board-switcher-group">
                <select
                  className="board-select"
                  value={board?.id || ''}
                  onChange={(e) => handleSwitchBoard(e.target.value)}
                >
                  {boards.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.name}
                    </option>
                  ))}
                </select>

                <button
                  className="btn-icon-action"
                  onClick={() => setShowCreateBoardModal(true)}
                  title={t('board.create_board')}
                >
                  <Plus size={16} />
                </button>

                {board && (
                  <>
                    <button
                      className="btn-icon-action"
                      onClick={openEditBoardModal}
                      title={t('board.edit_board')}
                    >
                      <Pencil size={15} />
                    </button>
                    <button
                      className="btn-icon-action btn-icon-danger"
                      onClick={openDeleteBoardModal}
                      title={t('board.delete_board')}
                    >
                      <Trash2 size={15} />
                    </button>
                  </>
                )}
              </div>
            )}
          </h2>
          <p className="subtitle">
            {board?.description || project?.description || t('board.default_subtitle')}
          </p>
        </div>

        <div style={{ display: 'flex', gap: 10 }}>
          {projectId && (
            <button className="btn btn-secondary" onClick={() => setShowProjectActivityModal(true)}>
              <History size={18} /> <span>{t('activity.title')}</span>
            </button>
          )}
          {projectId && (
            <button className="btn btn-secondary" onClick={() => navigate(`/projects/${projectId}/sprints`)}>
              <Layers size={18} /> <span>{t('sprint.title') || 'Sprints'}</span>
            </button>
          )}
          {projectId && (
            <button className="btn btn-secondary" onClick={() => navigate(`/projects/${projectId}/members`)}>
              <Users size={18} /> <span>{t('proj_members.title') || 'Project Members'}</span>
            </button>
          )}
          <button className="btn btn-primary" onClick={() => setShowAddColumnModal(true)} disabled={!board}>
            <Plus size={18} /> <span>{t('board.add_column')}</span>
          </button>
        </div>
      </div>

      {/* Board Canvas */}
      {loading ? (
        <div className="loading-state">{t('board.loading')}</div>
      ) : !board || !board.columns || board.columns.length === 0 ? (
        <div className="card empty-board-card">
          <h3>{t('board.no_columns_title')}</h3>
          <p>{t('board.no_columns_desc')}</p>
          <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={() => setShowAddColumnModal(true)}>
            + {t('board.add_column')}
          </button>
        </div>
      ) : (
        <div className="board-canvas">
          {[...board.columns].sort((a, b) => a.position - b.position).map((col, colIndex, sortedArr) => {
            const colTasks = tasks.filter((tItem) => tItem.boardColumnId === col.id);
            const isWipExceeded = col.wipLimit != null && col.wipLimit > 0 && colTasks.length > col.wipLimit;
            return (
              <div
                key={col.id}
                className={`board-column ${dragOverColumnId === col.id ? 'drag-over' : ''}`}
                onDragOver={(e) => handleTaskDragOver(e, col.id)}
                onDragEnter={(e) => handleTaskDragEnter(e, col.id)}
                onDragLeave={(e) => handleTaskDragLeave(e, col.id)}
                onDrop={(e) => handleTaskDrop(e, col.id)}
              >
                {/* Accent Color Bar */}
                {col.color && (
                  <div
                    className="column-accent-bar"
                    style={{ backgroundColor: col.color }}
                  />
                )}

                <div className="column-header">
                  <div className="column-title-group">
                    <span className="column-name">{col.name}</span>
                    <span className="task-count">{colTasks.length}</span>
                    {col.wipLimit != null && col.wipLimit > 0 && (
                      <span className={`wip-badge ${isWipExceeded ? 'wip-exceeded' : ''}`}>
                        {colTasks.length}/{col.wipLimit}
                      </span>
                    )}
                  </div>
                  <div
                    className="column-actions-group"
                    onMouseDown={(e) => e.stopPropagation()}
                    onClick={(e) => e.stopPropagation()}
                  >
                    {colIndex > 0 && (
                      <button
                        type="button"
                        className="icon-btn"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleMoveColumn(col.id, 'left');
                        }}
                        title={t('board.move_left')}
                      >
                        <ChevronLeft size={15} />
                      </button>
                    )}
                    {colIndex < sortedArr.length - 1 && (
                      <button
                        type="button"
                        className="icon-btn"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleMoveColumn(col.id, 'right');
                        }}
                        title={t('board.move_right')}
                      >
                        <ChevronRight size={15} />
                      </button>
                    )}
                    <button
                      type="button"
                      className="icon-btn"
                      onClick={(e) => {
                        e.stopPropagation();
                        openEditColumnModal(col);
                      }}
                      title={t('board.edit_column')}
                    >
                      <Pencil size={14} />
                    </button>
                    <button
                      type="button"
                      className="icon-btn"
                      onClick={(e) => {
                        e.stopPropagation();
                        openDeleteColumnModal(col.id);
                      }}
                      title={t('board.delete_column')}
                      style={{ color: 'var(--text-muted)' }}
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                </div>

                <div
                  className={`column-tasks ${dragOverColumnId === col.id ? 'drag-over-tasks' : ''}`}
                  onDragOver={(e) => handleTaskDragOver(e, col.id)}
                  onDrop={(e) => handleTaskDrop(e, col.id)}
                >
                  {colTasks.map((tItem) => (
                    <div
                      key={tItem.id}
                      className={`card task-card ${draggedTaskId === tItem.id ? 'is-dragging' : ''}`}
                      draggable
                      onDragStart={(e) => handleTaskDragStart(e, tItem.id)}
                      onDragEnd={handleTaskDragEnd}
                      onClick={() => openTaskDetail(tItem)}
                    >
                      <div className="task-header">
                        <span className={`badge ${getPriorityBadgeClass(tItem.priority)}`}>
                          {getPriorityText(tItem.priority)}
                        </span>
                        <span className="task-num">#{tItem.taskNumber}</span>
                      </div>
                      {tItem.labels && tItem.labels.length > 0 && (
                        <div className="task-labels-row" style={{ display: 'flex', flexWrap: 'wrap', gap: 4, margin: '4px 0 6px 0' }}>
                          {tItem.labels.map((lbl) => (
                            <span
                              key={lbl.id}
                              className="task-label-tag"
                              style={{
                                backgroundColor: lbl.color ? `${lbl.color}22` : 'rgba(59, 130, 246, 0.15)',
                                color: lbl.color || '#3B82F6',
                                border: `1px solid ${lbl.color ? `${lbl.color}55` : 'rgba(59, 130, 246, 0.3)'}`,
                                padding: '1px 6px',
                                borderRadius: 4,
                                fontSize: 11,
                                fontWeight: 600,
                              }}
                            >
                              {lbl.name}
                            </span>
                          ))}
                        </div>
                      )}
                      <h4 className="task-title">{tItem.title}</h4>
                      {tItem.description && <p className="task-desc">{tItem.description}</p>}
                      <div className="task-footer">
                        {tItem.dueDate ? (
                          <span className="task-meta">
                            <Calendar size={13} /> {new Date(tItem.dueDate).toLocaleDateString()}
                          </span>
                        ) : (
                          <span></span>
                        )}
                        {tItem.dependencies && tItem.dependencies.length > 0 && (
                          <span className="task-meta" title={`${tItem.dependencies.length} dependencies`}>
                            <Link2 size={13} /> {tItem.dependencies.length}
                          </span>
                        )}
                        {tItem.assignees && tItem.assignees.length > 0 && (
                          <div className="task-assignee">
                            <UserIcon size={14} /> {tItem.assignees[0].displayName}
                          </div>
                        )}
                      </div>
                    </div>
                  ))}

                  {dragOverColumnId === col.id && draggedTaskId && (
                    <div className="drop-placeholder">
                      <span>{t('board.drop_here') || 'Thả công việc vào đây'}</span>
                    </div>
                  )}
                </div>

                <button
                  className="add-task-btn"
                  onClick={() => {
                    setActiveColumnId(col.id);
                    setShowAddTaskModal(true);
                  }}
                >
                  <Plus size={16} /> {t('board.add_task')}
                </button>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal Create Board */}
      {showCreateBoardModal && (
        <div className="modal-overlay" onClick={() => setShowCreateBoardModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
            <div className="modal-header">
              <h3>{t('board.create_board')}</h3>
              <button className="btn-ghost" onClick={() => setShowCreateBoardModal(false)} disabled={isCreatingBoard}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={handleCreateBoardSubmit}>
              <div className="modal-body">
                {createBoardError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{createBoardError}</span>
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('board.name_label')} *</label>
                  <input
                    type="text"
                    required
                    value={newBoardName}
                    onChange={(e) => setNewBoardName(e.target.value)}
                    placeholder={t('board.name_placeholder')}
                    className="form-input"
                    disabled={isCreatingBoard}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.desc_label')}</label>
                  <textarea
                    value={newBoardDesc}
                    onChange={(e) => setNewBoardDesc(e.target.value)}
                    placeholder={t('board.desc_placeholder')}
                    className="form-input"
                    rows={3}
                    disabled={isCreatingBoard}
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowCreateBoardModal(false)}
                  disabled={isCreatingBoard}
                >
                  {t('board.cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isCreatingBoard || !newBoardName.trim()}>
                  {isCreatingBoard ? '...' : t('board.create_board')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Edit Board */}
      {showEditBoardModal && (
        <div className="modal-overlay" onClick={() => setShowEditBoardModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
            <div className="modal-header">
              <h3>{t('board.edit_board')}</h3>
              <button className="btn-ghost" onClick={() => setShowEditBoardModal(false)} disabled={isEditingBoard}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={handleEditBoardSubmit}>
              <div className="modal-body">
                {editBoardError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{editBoardError}</span>
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('board.name_label')} *</label>
                  <input
                    type="text"
                    required
                    value={editBoardName}
                    onChange={(e) => setEditBoardName(e.target.value)}
                    placeholder={t('board.name_placeholder')}
                    className="form-input"
                    disabled={isEditingBoard}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.desc_label')}</label>
                  <textarea
                    value={editBoardDesc}
                    onChange={(e) => setEditBoardDesc(e.target.value)}
                    placeholder={t('board.desc_placeholder')}
                    className="form-input"
                    rows={3}
                    disabled={isEditingBoard}
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowEditBoardModal(false)}
                  disabled={isEditingBoard}
                >
                  {t('board.cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isEditingBoard || !editBoardName.trim()}>
                  {isEditingBoard ? '...' : t('profile.save_changes')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Delete Board Confirmation */}
      {showDeleteBoardModal && board && (
        <div className="modal-overlay" onClick={() => setShowDeleteBoardModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
            <div className="modal-header">
              <h3>{t('board.delete_board')}</h3>
              <button className="btn-ghost" onClick={() => setShowDeleteBoardModal(false)} disabled={isDeletingBoard}>
                <X size={18} />
              </button>
            </div>
            <div className="modal-body">
              {deleteBoardError && (
                <div className="badge badge-danger" style={{ marginBottom: 14, display: 'flex', alignItems: 'center', gap: 6 }}>
                  <AlertCircle size={14} />
                  <span>{deleteBoardError}</span>
                </div>
              )}
              <p style={{ fontSize: 14, color: 'var(--text-main)', margin: 0 }}>
                {t('board.delete_confirm')?.replace('{name}', board.name) ||
                  `Are you sure you want to delete board "${board.name}"?`}
              </p>
            </div>
            <div className="modal-footer">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => setShowDeleteBoardModal(false)}
                disabled={isDeletingBoard}
              >
                {t('board.cancel')}
              </button>
              <button
                type="button"
                className="btn btn-danger"
                onClick={handleDeleteBoardSubmit}
                disabled={isDeletingBoard}
              >
                <Trash2 size={14} />
                {isDeletingBoard ? '...' : t('board.delete_board')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Add Column */}
      {showAddColumnModal && (
        <div className="modal-overlay" onClick={() => setShowAddColumnModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
            <div className="modal-header">
              <h3>{t('board.modal_add_column')}</h3>
              <button className="btn-ghost" onClick={() => setShowAddColumnModal(false)} disabled={isAddingColumn}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={handleAddColumn}>
              <div className="modal-body">
                {addColumnError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{addColumnError}</span>
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('board.column_name')} *</label>
                  <input
                    type="text"
                    required
                    value={newColumnName}
                    onChange={(e) => setNewColumnName(e.target.value)}
                    placeholder={t('board.column_placeholder')}
                    className="form-input"
                    disabled={isAddingColumn}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.column_wip_label')}</label>
                  <input
                    type="number"
                    min="0"
                    value={newColumnWipLimit}
                    onChange={(e) => setNewColumnWipLimit(e.target.value)}
                    placeholder={t('board.column_wip_placeholder')}
                    className="form-input"
                    disabled={isAddingColumn}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.column_color_label')}</label>
                  <div className="color-swatch-picker">
                    {COLUMN_COLORS.map((color) => (
                      <button
                        key={color}
                        type="button"
                        className={`color-swatch-btn ${newColumnColor === color ? 'selected' : ''}`}
                        style={{ backgroundColor: color }}
                        onClick={() => setNewColumnColor(newColumnColor === color ? '' : color)}
                      />
                    ))}
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setShowAddColumnModal(false)} disabled={isAddingColumn}>
                  {t('board.cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isAddingColumn || !newColumnName.trim()}>
                  {isAddingColumn ? '...' : t('board.create_column')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Edit Column */}
      {showEditColumnModal && (
        <div className="modal-overlay" onClick={() => setShowEditColumnModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
            <div className="modal-header">
              <h3>{t('board.edit_column')}</h3>
              <button className="btn-ghost" onClick={() => setShowEditColumnModal(false)} disabled={isEditingColumn}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={handleEditColumnSubmit}>
              <div className="modal-body">
                {editColumnError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{editColumnError}</span>
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('board.column_name')} *</label>
                  <input
                    type="text"
                    required
                    value={editColumnName}
                    onChange={(e) => setEditColumnName(e.target.value)}
                    placeholder={t('board.column_placeholder')}
                    className="form-input"
                    disabled={isEditingColumn}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.column_wip_label')}</label>
                  <input
                    type="number"
                    min="0"
                    value={editColumnWipLimit}
                    onChange={(e) => setEditColumnWipLimit(e.target.value)}
                    placeholder={t('board.column_wip_placeholder')}
                    className="form-input"
                    disabled={isEditingColumn}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.column_color_label')}</label>
                  <div className="color-swatch-picker">
                    {COLUMN_COLORS.map((color) => (
                      <button
                        key={color}
                        type="button"
                        className={`color-swatch-btn ${editColumnColor === color ? 'selected' : ''}`}
                        style={{ backgroundColor: color }}
                        onClick={() => setEditColumnColor(editColumnColor === color ? '' : color)}
                      />
                    ))}
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setShowEditColumnModal(false)} disabled={isEditingColumn}>
                  {t('board.cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isEditingColumn || !editColumnName.trim()}>
                  {isEditingColumn ? '...' : t('profile.save_changes')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Add Task */}
      {showAddTaskModal && (
        <div className="modal-overlay" onClick={() => setShowAddTaskModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 480 }}>
            <div className="modal-header">
              <h3>{t('board.modal_create_task')}</h3>
              <button className="btn-ghost" onClick={() => setShowAddTaskModal(false)} disabled={isCreatingTask}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={handleAddTask}>
              <div className="modal-body">
                {createTaskError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{createTaskError}</span>
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">{t('board.task_title')} *</label>
                  <input
                    type="text"
                    required
                    value={taskTitle}
                    onChange={(e) => setTaskTitle(e.target.value)}
                    placeholder={t('board.task_title_placeholder')}
                    className="form-input"
                    disabled={isCreatingTask}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.description')}</label>
                  <textarea
                    value={taskDesc}
                    onChange={(e) => setTaskDesc(e.target.value)}
                    placeholder={t('board.task_desc_placeholder')}
                    className="form-input"
                    rows={3}
                    disabled={isCreatingTask}
                  />
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginTop: 12 }}>
                  <div className="form-group">
                    <label className="form-label">{t('board.status_column')}</label>
                    <select
                      value={activeColumnId || ''}
                      onChange={(e) => setActiveColumnId(e.target.value)}
                      className="form-input"
                      disabled={isCreatingTask}
                    >
                      {board?.columns?.map((col) => (
                        <option key={col.id} value={col.id}>
                          {col.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">{t('board.priority')}</label>
                    <select
                      value={taskPriority}
                      onChange={(e) => setTaskPriority(e.target.value)}
                      className="form-input"
                      disabled={isCreatingTask}
                    >
                      <option value="low">{t('board.priority_low')}</option>
                      <option value="medium">{t('board.priority_medium')}</option>
                      <option value="high">{t('board.priority_high')}</option>
                    </select>
                  </div>
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">{t('board.due_date')}</label>
                  <input
                    type="date"
                    value={taskDueDate}
                    onChange={(e) => setTaskDueDate(e.target.value)}
                    className="form-input"
                    disabled={isCreatingTask}
                  />
                </div>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-secondary" onClick={() => setShowAddTaskModal(false)} disabled={isCreatingTask}>
                  {t('board.cancel')}
                </button>
                <button type="submit" className="btn btn-primary" disabled={isCreatingTask || !taskTitle.trim()}>
                  {isCreatingTask ? '...' : t('board.create_task')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Task Detail & Edit & Delete */}
      {selectedTask && (
        <div className="modal-overlay" onClick={() => setSelectedTask(null)}>
          <div className="modal-content modal-lg" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div className="task-detail-title-group">
                <span className="badge badge-primary">#{selectedTask.taskNumber}</span>
                <h3>{isEditingTask ? t('board.edit_task') : selectedTask.title}</h3>
              </div>
              <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <button
                  type="button"
                  className={`watch-btn ${isCurrentWatcher ? 'watching' : ''}`}
                  onClick={handleToggleWatch}
                  title={isCurrentWatcher ? t('board.unwatch') : t('board.watch')}
                >
                  {isCurrentWatcher ? <EyeOff size={14} /> : <Eye size={14} />}
                  <span>{isCurrentWatcher ? t('board.unwatch') : t('board.watch')} ({selectedTask.watchers?.length || 0})</span>
                </button>
                {!isEditingTask && (
                  <>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => setIsEditingTask(true)}
                      title={t('board.edit_task')}
                    >
                      <Pencil size={14} /> <span>{t('board.edit_task')}</span>
                    </button>
                    <button
                      className="btn btn-danger btn-sm"
                      onClick={() => setShowDeleteTaskModal(true)}
                      disabled={isDeletingTask}
                      title={t('board.delete_task')}
                    >
                      <Trash2 size={14} /> <span>{isDeletingTask ? '...' : t('board.delete_task')}</span>
                    </button>
                  </>
                )}
                <button className="btn-ghost" onClick={() => setSelectedTask(null)}>
                  <X size={18} />
                </button>
              </div>
            </div>
            <div className="modal-body">
              {taskError && (
                <div className="badge badge-danger" style={{ marginBottom: 14, display: 'flex', alignItems: 'center', gap: 6 }}>
                  <AlertCircle size={14} />
                  <span>{taskError}</span>
                </div>
              )}

              <div className="task-detail-grid">
                <div className="task-detail-main">
                  {isEditingTask ? (
                    <form onSubmit={handleSaveTask}>
                      <div className="form-group">
                        <label className="form-label">{t('board.task_title')} *</label>
                        <input
                          type="text"
                          required
                          value={editTaskTitle}
                          onChange={(e) => setEditTaskTitle(e.target.value)}
                          className="form-input"
                          disabled={isSavingTask}
                        />
                      </div>
                      <div className="form-group" style={{ marginTop: 12 }}>
                        <label className="form-label">{t('board.description')}</label>
                        <textarea
                          value={editTaskDesc}
                          onChange={(e) => setEditTaskDesc(e.target.value)}
                          className="form-input"
                          rows={4}
                          disabled={isSavingTask}
                        />
                      </div>
                      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginTop: 12 }}>
                        <div className="form-group">
                          <label className="form-label">{t('board.priority')}</label>
                          <select
                            value={editTaskPriority}
                            onChange={(e) => setEditTaskPriority(e.target.value)}
                            className="form-input"
                            disabled={isSavingTask}
                          >
                            <option value="low">{t('board.priority_low')}</option>
                            <option value="medium">{t('board.priority_medium')}</option>
                            <option value="high">{t('board.priority_high')}</option>
                          </select>
                        </div>
                        <div className="form-group">
                          <label className="form-label">{t('board.due_date')}</label>
                          <input
                            type="date"
                            value={editTaskDueDate}
                            onChange={(e) => setEditTaskDueDate(e.target.value)}
                            className="form-input"
                            disabled={isSavingTask}
                          />
                        </div>
                      </div>
                      <div className="form-group" style={{ marginTop: 12 }}>
                        <label className="form-label">{t('board.status_column')}</label>
                        <select
                          value={editTaskColumnId}
                          onChange={(e) => setEditTaskColumnId(e.target.value)}
                          className="form-input"
                          disabled={isSavingTask}
                        >
                          {board?.columns?.map((col) => (
                            <option key={col.id} value={col.id}>
                              {col.name}
                            </option>
                          ))}
                        </select>
                      </div>

                      <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', marginTop: 20 }}>
                        <button
                          type="button"
                          className="btn btn-secondary"
                          onClick={() => setIsEditingTask(false)}
                          disabled={isSavingTask}
                        >
                          {t('board.cancel')}
                        </button>
                        <button type="submit" className="btn btn-primary" disabled={isSavingTask || !editTaskTitle.trim()}>
                          {isSavingTask ? '...' : t('board.save_task')}
                        </button>
                      </div>
                    </form>
                  ) : (
                    <>
                      <h4>{t('board.description')}</h4>
                      <p className="task-detail-desc">{selectedTask.description || t('board.no_description')}</p>

                      {/* Checklists Section */}
                      <TaskChecklists taskId={selectedTask.id} />

                      {/* Task Dependencies Section */}
                      <TaskDependencies
                        taskId={selectedTask.id}
                        dependencies={selectedTask.dependencies || []}
                        availableTasks={tasks}
                        onDependencyUpdated={async () => {
                          try {
                            const res = await taskApi.getTask(selectedTask.id);
                            if (res.success && res.data) {
                              setSelectedTask(res.data);
                              setTasks((prev) => prev.map((t) => (t.id === selectedTask.id ? res.data! : t)));
                            }
                          } catch (err) {
                            console.error('Failed to refresh task:', err);
                          }
                        }}
                      />

                      {/* Task Detail Tabs */}
                      <div className="task-detail-tabs" style={{ display: 'flex', borderBottom: '1px solid var(--border-color)', marginBottom: 16 }}>
                        <button
                          type="button"
                          className={`task-tab-btn ${activeTaskTab === 'comments' ? 'active' : ''}`}
                          onClick={() => setActiveTaskTab('comments')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 6,
                            padding: '8px 16px',
                            border: 'none',
                            background: 'none',
                            borderBottom: activeTaskTab === 'comments' ? '2px solid var(--primary)' : '2px solid transparent',
                            color: activeTaskTab === 'comments' ? 'var(--primary)' : 'var(--text-subtle)',
                            fontWeight: activeTaskTab === 'comments' ? 600 : 500,
                            cursor: 'pointer',
                            fontSize: 14,
                          }}
                        >
                          <MessageSquare size={16} />
                          <span>{t('activity.tab_comments')}</span>
                          <span className="comments-count-badge">{countTotalComments(comments)}</span>
                        </button>
                        <button
                          type="button"
                          className={`task-tab-btn ${activeTaskTab === 'activity' ? 'active' : ''}`}
                          onClick={() => setActiveTaskTab('activity')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 6,
                            padding: '8px 16px',
                            border: 'none',
                            background: 'none',
                            borderBottom: activeTaskTab === 'activity' ? '2px solid var(--primary)' : '2px solid transparent',
                            color: activeTaskTab === 'activity' ? 'var(--primary)' : 'var(--text-subtle)',
                            fontWeight: activeTaskTab === 'activity' ? 600 : 500,
                            cursor: 'pointer',
                            fontSize: 14,
                          }}
                        >
                          <History size={16} />
                          <span>{t('activity.tab_activity')}</span>
                        </button>
                        <button
                          type="button"
                          className={`task-tab-btn ${activeTaskTab === 'attachments' ? 'active' : ''}`}
                          onClick={() => setActiveTaskTab('attachments')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 6,
                            padding: '8px 16px',
                            border: 'none',
                            background: 'none',
                            borderBottom: activeTaskTab === 'attachments' ? '2px solid var(--primary)' : '2px solid transparent',
                            color: activeTaskTab === 'attachments' ? 'var(--primary)' : 'var(--text-subtle)',
                            fontWeight: activeTaskTab === 'attachments' ? 600 : 500,
                            cursor: 'pointer',
                            fontSize: 14,
                          }}
                        >
                          <Paperclip size={16} />
                          <span>{t('file.tab_attachments')}</span>
                          {attachmentCount > 0 && (
                            <span className="comments-count-badge">{attachmentCount}</span>
                          )}
                        </button>
                        <button
                          type="button"
                          className={`task-tab-btn ${activeTaskTab === 'time' ? 'active' : ''}`}
                          onClick={() => setActiveTaskTab('time')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 6,
                            padding: '8px 16px',
                            border: 'none',
                            background: 'none',
                            borderBottom: activeTaskTab === 'time' ? '2px solid var(--primary)' : '2px solid transparent',
                            color: activeTaskTab === 'time' ? 'var(--primary)' : 'var(--text-subtle)',
                            fontWeight: activeTaskTab === 'time' ? 600 : 500,
                            cursor: 'pointer',
                            fontSize: 14,
                          }}
                        >
                          <Clock size={16} />
                          <span>{t('time.tab_time')}</span>
                        </button>
                        <button
                          type="button"
                          className={`task-tab-btn ${activeTaskTab === 'dependencies' ? 'active' : ''}`}
                          onClick={() => setActiveTaskTab('dependencies')}
                          style={{
                            display: 'flex',
                            alignItems: 'center',
                            gap: 6,
                            padding: '8px 16px',
                            border: 'none',
                            background: 'none',
                            borderBottom: activeTaskTab === 'dependencies' ? '2px solid var(--primary)' : '2px solid transparent',
                            color: activeTaskTab === 'dependencies' ? 'var(--primary)' : 'var(--text-subtle)',
                            fontWeight: activeTaskTab === 'dependencies' ? 600 : 500,
                            cursor: 'pointer',
                            fontSize: 14,
                          }}
                        >
                          <Link2 size={16} />
                          <span>{t('dependency.tab_dependencies')}</span>
                          {selectedTask.dependencies && selectedTask.dependencies.length > 0 && (
                            <span className="comments-count-badge">{selectedTask.dependencies.length}</span>
                          )}
                        </button>
                      </div>

                      {activeTaskTab === 'comments' ? (
                        /* Comments Section */
                        <div className="comments-section">
                          <div className="comments-header-title">
                            <h4>
                              <MessageSquare size={18} />
                              <span>{t('board.comments')}</span>
                              <span className="comments-count-badge">{countTotalComments(comments)}</span>
                            </h4>
                          </div>

                          {/* Error banner */}
                          {commentError && (
                            <div className="comments-error-banner">
                              <AlertCircle size={14} />
                              <span>{commentError}</span>
                            </div>
                          )}

                          {/* Loading state */}
                          {commentsLoading ? (
                            <div className="comments-loading-state">
                              <Loader2 size={16} style={{ animation: 'spin 1s linear infinite' }} />
                              <span>{t('board.comment_loading')}</span>
                            </div>
                          ) : (
                            <div className="comments-list">
                              {comments.length === 0 ? (
                                <div className="no-comments" style={{ fontStyle: 'italic', color: 'var(--text-subtle)', fontSize: 13 }}>
                                  {t('board.no_comments')}
                                </div>
                              ) : (
                                comments.map((c) => renderCommentItem(c))
                              )}
                            </div>
                          )}

                          {/* New Comment Form */}
                          <form onSubmit={(e) => handleAddComment(e)} className="comment-form-container">
                            <textarea
                              value={newCommentText}
                              onChange={(e) => setNewCommentText(e.target.value)}
                              placeholder={t('board.comment_placeholder')}
                              className="form-input"
                              rows={3}
                              disabled={isSubmittingComment}
                            />
                            <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                              <button
                                type="submit"
                                className="btn btn-primary btn-sm"
                                disabled={isSubmittingComment || !newCommentText.trim()}
                              >
                                <Send size={14} style={{ marginRight: 4 }} />
                                <span>{isSubmittingComment ? '...' : t('board.comment_submit')}</span>
                              </button>
                            </div>
                          </form>
                        </div>
                      ) : activeTaskTab === 'activity' ? (
                        <div className="task-activity-section" style={{ marginTop: 12 }}>
                          {project?.workspaceId || currentWorkspace?.id ? (
                            <ActivityTimeline
                              workspaceId={project?.workspaceId || currentWorkspace?.id || ''}
                              entityType="Task"
                              entityId={selectedTask.id}
                            />
                          ) : (
                            <div className="no-comments">{t('activity.empty')}</div>
                          )}
                        </div>
                      ) : activeTaskTab === 'attachments' ? (
                        <div style={{ marginTop: 12 }}>
                          <TaskAttachments
                            taskId={selectedTask.id}
                            onCountChange={(count) => setAttachmentCount(count)}
                          />
                        </div>
                      ) : activeTaskTab === 'time' ? (
                        <div style={{ marginTop: 12 }}>
                          <TimeTracking taskId={selectedTask.id} />
                        </div>
                      ) : (
                        <div style={{ marginTop: 12 }}>
                          <TaskDependencies
                            taskId={selectedTask.id}
                            dependencies={selectedTask.dependencies || []}
                            availableTasks={tasks}
                            onDependencyUpdated={async () => {
                              try {
                                const res = await taskApi.getTask(selectedTask.id);
                                if (res.success && res.data) {
                                  setSelectedTask(res.data);
                                  setTasks((prev) => prev.map((t) => (t.id === selectedTask.id ? res.data! : t)));
                                }
                              } catch (err) {
                                console.error('Failed to refresh task:', err);
                              }
                            }}
                          />
                        </div>
                      )}
                    </>
                  )}
                </div>

                <div className="task-detail-sidebar">
                  <div className="meta-box">
                    <span className="meta-label">{t('board.status_column')}</span>
                    <select
                      value={selectedTask.boardColumnId || ''}
                      onChange={(e) => handleSidebarColumnChange(e.target.value)}
                      className="form-input"
                      style={{ fontSize: 13, padding: '4px 8px' }}
                    >
                      {board?.columns?.map((col) => (
                        <option key={col.id} value={col.id}>
                          {col.name}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="meta-box">
                    <span className="meta-label">{t('board.priority')}</span>
                    <select
                      value={selectedTask.priority || 'medium'}
                      onChange={(e) => handleSidebarPriorityChange(e.target.value)}
                      className="form-input"
                      style={{ fontSize: 13, padding: '4px 8px' }}
                    >
                      <option value="low">{t('board.priority_low')}</option>
                      <option value="medium">{t('board.priority_medium')}</option>
                      <option value="high">{t('board.priority_high')}</option>
                    </select>
                  </div>

                  <div className="meta-box">
                    <span className="meta-label">{t('board.due_date')}</span>
                    <input
                      type="date"
                      value={selectedTask.dueDate ? selectedTask.dueDate.split('T')[0] : ''}
                      onChange={(e) => handleSidebarDueDateChange(e.target.value)}
                      className="form-input"
                      style={{ fontSize: 13, padding: '4px 8px' }}
                    />
                  </div>

                  {/* Assignees Meta Box */}
                  <div className="meta-box">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span className="meta-label">{t('board.assignees')}</span>
                      <button
                        type="button"
                        className="btn-ghost"
                        style={{ padding: '2px 6px', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}
                        onClick={() => setShowAssigneeDropdown((prev) => !prev)}
                        title={t('board.add_assignee')}
                      >
                        <UserPlus size={14} />
                      </button>
                    </div>

                    {/* Current Assignees List */}
                    {selectedTask.assignees && selectedTask.assignees.length > 0 ? (
                      <div className="assignees-list">
                        {selectedTask.assignees.map((assignee) => (
                          <div key={assignee.id} className="assignee-chip">
                            <div className="assignee-info">
                              {assignee.avatarUrl ? (
                                <img src={assignee.avatarUrl} alt={assignee.displayName} className="assignee-avatar-circle" />
                              ) : (
                                <div className="assignee-avatar-circle">
                                  {assignee.displayName ? assignee.displayName.charAt(0).toUpperCase() : 'U'}
                                </div>
                              )}
                              <span className="assignee-name">{assignee.displayName}</span>
                            </div>
                            <button
                              type="button"
                              className="assignee-remove-btn"
                              onClick={() => handleRemoveAssignee(assignee.id)}
                              title="Remove assignee"
                            >
                              <X size={13} />
                            </button>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div style={{ fontSize: 13, color: 'var(--text-subtle)', fontStyle: 'italic', marginTop: 4 }}>
                        {t('board.no_assignees')}
                      </div>
                    )}

                    {/* Member Search & Picker Dropdown */}
                    {showAssigneeDropdown && (
                      <div className="assignee-picker-box">
                        <input
                          type="text"
                          autoFocus
                          value={assigneeSearchQuery}
                          onChange={(e) => setAssigneeSearchQuery(e.target.value)}
                          placeholder={t('board.search_members')}
                          className="assignee-search-input"
                        />
                        <div className="assignee-dropdown-list">
                          {projectMembers
                            .filter(
                              (m) =>
                                !selectedTask.assignees?.some((a) => a.id === m.userId) &&
                                (m.displayName.toLowerCase().includes(assigneeSearchQuery.toLowerCase()) ||
                                  m.email.toLowerCase().includes(assigneeSearchQuery.toLowerCase()))
                            )
                            .map((m) => (
                              <div
                                key={m.userId}
                                className="assignee-dropdown-item"
                                onClick={() => handleAddAssignee(m)}
                              >
                                {m.avatarUrl ? (
                                  <img src={m.avatarUrl} alt={m.displayName} className="assignee-avatar-circle" />
                                ) : (
                                  <div className="assignee-avatar-circle">
                                    {m.displayName ? m.displayName.charAt(0).toUpperCase() : 'U'}
                                  </div>
                                )}
                                <div>
                                  <div style={{ fontWeight: 600, fontSize: 12 }}>{m.displayName}</div>
                                  <div style={{ fontSize: 11, color: 'var(--text-subtle)' }}>{m.email}</div>
                                </div>
                              </div>
                            ))}

                          {projectMembers.filter(
                            (m) =>
                              !selectedTask.assignees?.some((a) => a.id === m.userId) &&
                              (m.displayName.toLowerCase().includes(assigneeSearchQuery.toLowerCase()) ||
                                m.email.toLowerCase().includes(assigneeSearchQuery.toLowerCase()))
                          ).length === 0 && (
                              <div style={{ padding: '8px 10px', fontSize: 12, color: 'var(--text-subtle)', fontStyle: 'italic' }}>
                                No matching members found
                              </div>
                            )}
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Watchers Meta Box */}
                  <div className="meta-box">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span className="meta-label">
                        {t('board.watchers')} ({selectedTask.watchers?.length || 0})
                      </span>
                      <button
                        type="button"
                        className={`btn-ghost ${isCurrentWatcher ? 'watching' : ''}`}
                        style={{ padding: '2px 6px', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}
                        onClick={handleToggleWatch}
                      >
                        {isCurrentWatcher ? <EyeOff size={13} /> : <Eye size={13} />}
                        <span>{isCurrentWatcher ? t('board.unwatch') : t('board.watch')}</span>
                      </button>
                    </div>

                    {selectedTask.watchers && selectedTask.watchers.length > 0 ? (
                      <div className="watchers-list">
                        {selectedTask.watchers.map((watcher) => (
                          <div key={watcher.id} className="watcher-chip">
                            <div className="watcher-info">
                              {watcher.avatarUrl ? (
                                <img src={watcher.avatarUrl} alt={watcher.displayName} className="watcher-avatar-circle" />
                              ) : (
                                <div className="watcher-avatar-circle">
                                  {watcher.displayName ? watcher.displayName.charAt(0).toUpperCase() : 'U'}
                                </div>
                              )}
                              <span className="assignee-name">{watcher.displayName}</span>
                            </div>
                            <button
                              type="button"
                              className="assignee-remove-btn"
                              onClick={() => handleRemoveWatcher(watcher.id)}
                              title="Remove watcher"
                            >
                              <X size={13} />
                            </button>
                          </div>
                        ))}
                      </div>
                    ) : (
                      <div style={{ fontSize: 13, color: 'var(--text-subtle)', fontStyle: 'italic', marginTop: 4 }}>
                        {t('board.no_watchers')}
                      </div>
                    )}
                  </div>

                  {/* Labels Meta Box */}
                  <div className="meta-box">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <span className="meta-label">
                        {t('board.labels') || 'Labels'} ({selectedTask.labels?.length || 0})
                      </span>
                      <button
                        type="button"
                        className="btn-ghost"
                        style={{ padding: '2px 6px', fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }}
                        onClick={() => setShowLabelDropdown((prev) => !prev)}
                        title="Add Label"
                      >
                        <Plus size={14} />
                      </button>
                    </div>

                    {/* Assigned Labels List */}
                    {selectedTask.labels && selectedTask.labels.length > 0 ? (
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginTop: 8 }}>
                        {selectedTask.labels.map((lbl) => (
                          <span
                            key={lbl.id}
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: 4,
                              backgroundColor: lbl.color ? `${lbl.color}22` : 'rgba(59, 130, 246, 0.15)',
                              color: lbl.color || '#3B82F6',
                              border: `1px solid ${lbl.color ? `${lbl.color}55` : 'rgba(59, 130, 246, 0.3)'}`,
                              padding: '2px 8px',
                              borderRadius: 12,
                              fontSize: 12,
                              fontWeight: 600,
                            }}
                          >
                            {lbl.name}
                            <button
                              type="button"
                              style={{
                                background: 'none',
                                border: 'none',
                                color: 'inherit',
                                cursor: 'pointer',
                                display: 'inline-flex',
                                padding: 0,
                                marginLeft: 2,
                              }}
                              onClick={() => handleRemoveLabelFromTask(lbl.id)}
                              title="Remove label"
                            >
                              <X size={12} />
                            </button>
                          </span>
                        ))}
                      </div>
                    ) : (
                      <div style={{ fontSize: 13, color: 'var(--text-subtle)', fontStyle: 'italic', marginTop: 4 }}>
                        No labels assigned
                      </div>
                    )}

                    {/* Label Picker Dropdown */}
                    {showLabelDropdown && (
                      <div className="assignee-picker-box" style={{ marginTop: 8 }}>
                        <div style={{ padding: '6px 8px', fontWeight: 600, fontSize: 12, color: 'var(--text-muted)', borderBottom: '1px solid var(--border-color)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <span>Select Label</span>
                          <button
                            type="button"
                            className="btn-ghost"
                            style={{ padding: '1px 4px', fontSize: 11, color: 'var(--primary)' }}
                            onClick={() => {
                              setShowLabelDropdown(false);
                              setShowCreateLabelModal(true);
                            }}
                          >
                            + Create New
                          </button>
                        </div>
                        <div className="assignee-dropdown-list">
                          {workspaceLabels.map((lbl) => {
                            const isAssigned = selectedTask.labels?.some((l) => l.id === lbl.id);
                            return (
                              <div
                                key={lbl.id}
                                className="assignee-dropdown-item"
                                style={{
                                  display: 'flex',
                                  alignItems: 'center',
                                  justifyContent: 'space-between',
                                  opacity: isAssigned ? 0.6 : 1,
                                  cursor: isAssigned ? 'default' : 'pointer',
                                }}
                                onClick={() => {
                                  if (!isAssigned) handleAddLabelToTask(lbl);
                                }}
                              >
                                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                  <span
                                    style={{
                                      width: 12,
                                      height: 12,
                                      borderRadius: '50%',
                                      backgroundColor: lbl.color || '#3B82F6',
                                      display: 'inline-block',
                                    }}
                                  />
                                  <span style={{ fontSize: 13, fontWeight: 500 }}>{lbl.name}</span>
                                </div>
                                {isAssigned && <Check size={14} style={{ color: 'var(--primary)' }} />}
                              </div>
                            );
                          })}

                          {workspaceLabels.length === 0 && (
                            <div style={{ padding: '8px 10px', fontSize: 12, color: 'var(--text-subtle)', fontStyle: 'italic' }}>
                              No labels created yet
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </div>

                  <div className="meta-box">
                    <span className="meta-label">{t('board.created_at')}</span>
                    <span className="meta-value">{new Date(selectedTask.createdAt).toLocaleDateString()}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Modal Create Label */}
      {showCreateLabelModal && (
        <div className="modal-overlay" onClick={() => setShowCreateLabelModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 400 }}>
            <div className="modal-header">
              <h3>Create Workspace Label</h3>
              <button className="btn-ghost" onClick={() => setShowCreateLabelModal(false)} disabled={isCreatingLabel}>
                <X size={18} />
              </button>
            </div>
            <form onSubmit={handleCreateLabelSubmit}>
              <div className="modal-body">
                {createLabelError && (
                  <div className="badge badge-danger" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <AlertCircle size={14} />
                    <span>{createLabelError}</span>
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">Label Name *</label>
                  <input
                    type="text"
                    required
                    value={newLabelName}
                    onChange={(e) => setNewLabelName(e.target.value)}
                    placeholder="e.g. Bug, Feature, Urgent"
                    className="form-input"
                    disabled={isCreatingLabel}
                  />
                </div>
                <div className="form-group" style={{ marginTop: 12 }}>
                  <label className="form-label">Color</label>
                  <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginTop: 6 }}>
                    {COLUMN_COLORS.map((c) => (
                      <button
                        key={c}
                        type="button"
                        style={{
                          width: 24,
                          height: 24,
                          borderRadius: '50%',
                          backgroundColor: c,
                          border: newLabelColor === c ? '2px solid var(--text-color)' : 'none',
                          cursor: 'pointer',
                        }}
                        onClick={() => setNewLabelColor(c)}
                      />
                    ))}
                  </div>
                </div>
              </div>
              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => setShowCreateLabelModal(false)}
                  disabled={isCreatingLabel}
                >
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={isCreatingLabel || !newLabelName.trim()}>
                  {isCreatingLabel ? '...' : 'Create Label'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Project Activity */}
      {showProjectActivityModal && (
        <div className="modal-overlay" onClick={() => setShowProjectActivityModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 640, maxHeight: '85vh', overflowY: 'auto' }}>
            <div className="modal-header">
              <h3 style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <History size={20} style={{ color: 'var(--primary)' }} />
                <span>{t('activity.title')} - {project?.name}</span>
              </h3>
              <button className="btn-ghost" onClick={() => setShowProjectActivityModal(false)}>
                <X size={18} />
              </button>
            </div>
            <div className="modal-body" style={{ padding: 20 }}>
              {project?.workspaceId || currentWorkspace?.id ? (
                <ActivityTimeline
                  workspaceId={project?.workspaceId || currentWorkspace?.id || ''}
                  entityType="Project"
                  entityId={projectId}
                  showFilter={false}
                />
              ) : null}
            </div>
          </div>
        </div>
      )}
      {/* Custom Confirm Modal for Column Deletion */}
      <ConfirmModal
        isOpen={Boolean(columnToDeleteId)}
        title={t('board.delete_column_title') || 'Xóa cột công việc?'}
        message={
          columnToDeleteId && board?.columns
            ? `Bạn có chắc chắn muốn xóa cột "${board.columns.find((c) => c.id === columnToDeleteId)?.name || ''}"? Tất cả các công việc thuộc cột này sẽ bị bỏ liên kết.`
            : 'Bạn có chắc chắn muốn xóa cột này?'
        }
        confirmText={t('board.delete_confirm_btn') || 'Xóa cột'}
        cancelText={t('common.cancel') || 'Hủy'}
        variant="danger"
        isLoading={isDeletingColumn}
        onConfirm={handleConfirmDeleteColumn}
        onCancel={() => !isDeletingColumn && setColumnToDeleteId(null)}
      />
      {/* Custom Confirm Modal for Task Deletion */}
      <ConfirmModal
        isOpen={showDeleteTaskModal}
        title="Xóa công việc?"
        message={
          selectedTask
            ? `Bạn có chắc chắn muốn xóa công việc "${selectedTask.title}"? Thao tác này không thể hoàn tác.`
            : 'Bạn có chắc chắn muốn xóa công việc này?'
        }
        confirmText="Xóa công việc"
        cancelText={t('common.cancel') || 'Hủy'}
        variant="danger"
        isLoading={isDeletingTask}
        onConfirm={handleConfirmDeleteTask}
        onCancel={() => !isDeletingTask && setShowDeleteTaskModal(false)}
      />
    </MainLayout>
  );
};
