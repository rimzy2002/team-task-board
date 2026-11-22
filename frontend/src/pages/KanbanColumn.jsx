import { useEffect, useState } from 'react';
import { listTasks } from '../services/api.js';
import { taskStatuses } from './TaskForm.jsx';

export default function KanbanColumn({ projectId, status, label, priority, search, recent, busy,
  onDetails, onEdit, onMove, onDelete }) {
  const [page, setPage] = useState(1);
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState(null);
  const [showRecent, setShowRecent] = useState(true);
  const current = result?.page === page && result.attempt === attempt ? result : null;
  const data = current?.data;

  useEffect(() => {
    const request = new AbortController();
    listTasks(projectId, { status, priority, search, page }, { signal: request.signal })
      .then((data) => {
        if (request.signal.aborted) return;
        const lastPage = Math.max(1, Math.ceil(data.totalCount / data.pageSize));
        if (page > lastPage) setPage(lastPage);
        else setResult({ page, attempt, data });
      })
      .catch((error) => {
        if (!request.signal.aborted) setResult({ page, attempt, error: error.message });
      });
    return () => request.abort();
  }, [projectId, status, priority, search, page, attempt]);

  function changePage(nextPage) {
    setShowRecent(false);
    setPage(nextPage);
  }

  const extraTask = showRecent && recent?.status === status && !data?.items.some((task) => task.id === recent.id)
    ? recent : null;
  const pages = data && Math.max(1, Math.ceil(data.totalCount / data.pageSize));
  const actions = { busy, onDetails, onEdit, onMove, onDelete };
  return <section className={`kanban-column kanban-${status}`} aria-labelledby={`column-${status}`} aria-busy={!current}>
    <header className="column-heading">
      <h3 id={`column-${status}`}>{label}</h3>
      {data && <span aria-label={`${data.totalCount} tasks`}>{data.totalCount}</span>}
    </header>
    {!current && <p role="status">Loading {label} tasks…</p>}
    {current?.error && <div>
      <p role="alert" className="board-error">{current.error}</p>
      <button type="button" onClick={() => setAttempt((previous) => previous + 1)}>Retry {label}</button>
    </div>}
    {data && <>
      {extraTask && <div className="recent-task">
        <p>Recently changed</p>
        <TaskCard task={extraTask} {...actions} />
      </div>}
      {!data.items.length && <p className="column-empty">{search || priority ? 'No matching tasks.' : 'No tasks yet.'}</p>}
      <ul className="kanban-list">
        {data.items.map((task) => <li key={task.id}><TaskCard task={task} {...actions} /></li>)}
      </ul>
      {pages > 1 && <nav className="column-pages" aria-label={`${label} pages`}>
        <button type="button" disabled={page <= 1 || busy} onClick={() => changePage(page - 1)}>Previous</button>
        <span>Page {page} of {pages}</span>
        <button type="button" disabled={page >= pages || busy} onClick={() => changePage(page + 1)}>Next</button>
      </nav>}
    </>}
  </section>;
}

function TaskCard({ task, busy, onDetails, onEdit, onMove, onDelete }) {
  return <article className="task-card">
    <h4>{task.title}</h4>
    <dl className="task-meta">
      <div><dt>Priority</dt><dd className={`priority priority-${task.priority}`}>{task.priority}</dd></div>
      {task.dueDate && <div><dt>Due</dt><dd><time dateTime={task.dueDate}>{task.dueDate}</time></dd></div>}
      {task.assignee && <div><dt>Assignee</dt><dd>{task.assignee}</dd></div>}
    </dl>
    <div className="board-actions task-actions">
      <button type="button" disabled={busy} aria-label={`Details for ${task.title}`} onClick={() => onDetails(task)}>Details</button>
      <button type="button" disabled={busy} aria-label={`Edit ${task.title}`} onClick={() => onEdit(task)}>Edit</button>
      <button type="button" disabled={busy} aria-label={`Delete ${task.title}`} onClick={() => onDelete(task)}>Delete</button>
    </div>
    <form className="task-move" onSubmit={(event) => {
      event.preventDefault();
      onMove(task, new FormData(event.currentTarget).get('status'));
    }}>
      <select name="status" aria-label={`Move ${task.title} to`} defaultValue={task.status} disabled={busy}>
        {taskStatuses.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
      </select>
      <button type="submit" disabled={busy} aria-label={`Move ${task.title}`}>Move</button>
    </form>
  </article>;
}
