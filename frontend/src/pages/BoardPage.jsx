import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router';
import { deleteTask, listProjects, saveTask } from '../services/api.js';
import KanbanColumn from './KanbanColumn.jsx';
import TaskForm, { taskPriorities, taskStatuses } from './TaskForm.jsx';
import './board.css';

const noFilters = { priority: '', search: '' };
const TaskDetailsPanel = lazy(() => import('./TaskDetailsPanel.jsx'));

export default function BoardPage() {
  const { projectId } = useParams();
  const id = Number(projectId);
  return <section className="board" aria-label="Project board">
    <Link to="/projects">← Projects</Link>
    {/^[1-9]\d*$/.test(projectId) && Number.isSafeInteger(id) ? <OwnedBoard key={id} projectId={id} /> :
      <p role="alert">This project is unavailable.</p>}
  </section>;
}

function OwnedBoard({ projectId }) {
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState(null);
  useEffect(() => {
    const request = new AbortController();
    listProjects({ signal: request.signal }).then((projects) => {
      if (!request.signal.aborted) setResult({ attempt, project: projects.find((project) => project.id === projectId) });
    }).catch((error) => {
      if (!request.signal.aborted) setResult({ attempt, error: error.message });
    });
    return () => request.abort();
  }, [projectId, attempt]);

  if (result?.attempt !== attempt) return <p role="status">Loading project…</p>;
  if (result.error) return <div>
    <p role="alert" className="board-error">{result.error}</p>
    <button type="button" onClick={() => setAttempt((previous) => previous + 1)}>Retry project</button>
  </div>;
  if (!result.project) return <p role="alert">This project is unavailable.</p>;
  return <TaskBoard project={result.project} />;
}

function TaskBoard({ project }) {
  const [draft, setDraft] = useState(noFilters);
  const [filters, setFilters] = useState(noFilters);
  const [revision, setRevision] = useState(0);
  const [recent, setRecent] = useState(null);
  const [dialog, setDialog] = useState(null);
  const [detailTask, setDetailTask] = useState(null);
  const [busy, setBusy] = useState(false);
  const [feedback, setFeedback] = useState(null);
  const [formError, setFormError] = useState(null);
  const write = useRef(null);
  useEffect(() => () => write.current?.abort(), []);

  function showDialog(kind, task) {
    if (write.current) return;
    setFormError(null);
    setFeedback(null);
    setDialog({ kind, task });
  }

  function applyFilters(next) {
    setDraft(next);
    setFilters(next);
    setFeedback(null);
    setRecent(null);
    setRevision((previous) => previous + 1);
  }

  async function mutate(kind, task, fields) {
    if (write.current) return;
    const request = new AbortController();
    write.current = request;
    setBusy(true);
    setFeedback(null);
    setFormError(null);
    try {
      const options = { signal: request.signal };
      const result = kind === 'delete' ? await deleteTask(task.id, options) :
        await saveTask(project.id, task?.id, fields, options);
      if (request.signal.aborted) return;
      const changedColumn = kind === 'create' || (result && result.status !== task?.status);
      if (changedColumn) {
        setDraft(noFilters);
        setFilters(noFilters);
      }
      setRecent(changedColumn || (!filters.search && !filters.priority) ? result : null);
      setFeedback({ message: {
        create: 'Task created.', edit: 'Task saved.', move: 'Task moved.', delete: 'Task deleted.',
      }[kind] });
      setDialog(null);
      setRevision((previous) => previous + 1);
    } catch (error) {
      if (request.signal.aborted) return;
      if ((kind === 'create' || kind === 'delete') && !error.fields) {
        setDialog(null);
        setRecent(null);
        setRevision((previous) => previous + 1);
        setFeedback({ error: true, message: `${error.message} Check the refreshed board before trying again.` });
      } else if (kind === 'move') {
        setRecent(null);
        setRevision((previous) => previous + 1);
        setFeedback({ error: true, message: error.message });
      } else {
        setFormError(error);
      }
    } finally {
      write.current = null;
      if (!request.signal.aborted) setBusy(false);
    }
  }

  function move(task, status) {
    if (status === task.status || !taskStatuses.some(([value]) => value === status)) return;
    mutate('move', task, { ...task, status });
  }

  return <>
    <header className="board-heading">
      <div>
        <h2>{project.name}</h2>
        {project.description && <p>{project.description}</p>}
      </div>
      <button type="button" className="board-primary" disabled={busy} onClick={() => showDialog('create')}>
        Create task
      </button>
    </header>
    <form className="board-filters" aria-label="Task filters" onSubmit={(event) => {
      event.preventDefault();
      applyFilters({ ...draft, search: draft.search.trim() });
    }}>
      <label>Search by title
        <input type="search" value={draft.search} maxLength={200} disabled={busy}
          onChange={(event) => setDraft((previous) => ({ ...previous, search: event.target.value }))} />
      </label>
      <label>Filter by priority
        <select value={draft.priority} disabled={busy}
          onChange={(event) => setDraft((previous) => ({ ...previous, priority: event.target.value }))}>
          <option value="">All priorities</option>
          {taskPriorities.map((value) => <option key={value}>{value}</option>)}
        </select>
      </label>
      <div className="board-actions">
        <button type="submit" disabled={busy}>Apply filters</button>
        <button type="button" disabled={busy} onClick={() => applyFilters(noFilters)}>Clear filters</button>
      </div>
    </form>
    {feedback && <p role={feedback.error ? 'alert' : 'status'} className={feedback.error ? 'board-error' : 'board-feedback'}>
      {feedback.message}
    </p>}
    <div className="kanban-grid">
      {taskStatuses.map(([status, label]) => <KanbanColumn key={`${status}:${revision}`}
        projectId={project.id} status={status} label={label} {...filters} recent={recent} busy={busy}
        onDetails={setDetailTask} onEdit={(task) => showDialog('edit', task)} onMove={move}
        onDelete={(task) => showDialog('delete', task)} />)}
    </div>
    {dialog && <TaskDialog dialog={dialog} busy={busy} error={formError} onCancel={() => setDialog(null)}
      onSubmit={(fields) => mutate(dialog.kind, dialog.task, fields)} />}
    {detailTask && <Suspense fallback={<p role="status">Loading task details…</p>}>
      <TaskDetailsPanel task={detailTask} onClose={() => setDetailTask(null)} />
    </Suspense>}
  </>;
}

function TaskDialog({ dialog, busy, error, onCancel, onSubmit }) {
  const ref = useRef(null);
  useEffect(() => {
    const modal = ref.current;
    modal.showModal();
    return () => modal.close();
  }, []);
  const deleting = dialog.kind === 'delete';
  return <dialog ref={ref} className="task-dialog" aria-labelledby="task-dialog-title" onCancel={(event) => {
    event.preventDefault();
    if (!busy) onCancel();
  }}>
    <h3 id="task-dialog-title">{{ create: 'Create task', edit: 'Edit task', delete: 'Delete task' }[dialog.kind]}</h3>
    {error && <p role="alert" className="board-error">{error.message}</p>}
    {deleting ? <>
      <p>Delete “{dialog.task.title}”? This permanently removes the task from this project.</p>
      <div className="board-actions">
        <button type="button" disabled={busy} onClick={onCancel}>Cancel</button>
        <button type="button" className="board-danger" disabled={busy} onClick={() => onSubmit()}>
          {busy ? 'Deleting…' : 'Delete task'}
        </button>
      </div>
    </> : <TaskForm task={dialog.task} busy={busy} serverErrors={error?.fields} onCancel={onCancel} onSubmit={onSubmit} />}
  </dialog>;
}
