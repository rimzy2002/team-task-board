import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../contexts/AuthContext.jsx';
import { maxCommentLength, postTaskComment, subscribeToTaskComments } from '../services/taskComments.js';
import { taskStatuses } from './TaskForm.jsx';

function commentError(error, fallback) {
  return error?.code === 'permission-denied' ?
    'Comments are unavailable for this account or task.' : fallback;
}

export default function TaskDetailsPanel({ task, onClose }) {
  const { user } = useAuth();
  const dialog = useRef(null);
  useEffect(() => {
    const modal = dialog.current;
    modal.showModal();
    return () => modal.close();
  }, []);

  const status = taskStatuses.find(([value]) => value === task.status)?.[1];
  return <dialog className="task-dialog task-details" ref={dialog} aria-labelledby="task-details-title"
    onCancel={(event) => { event.preventDefault(); onClose(); }}>
    <div className="task-details-heading">
      <h3 id="task-details-title">{task.title}</h3>
      <button type="button" onClick={onClose}>Close</button>
    </div>
    {task.description && <p className="task-details-text">{task.description}</p>}
    <dl className="task-meta">
      <div><dt>Status</dt><dd>{status}</dd></div>
      <div><dt>Priority</dt><dd className={`priority priority-${task.priority}`}>{task.priority}</dd></div>
      {task.dueDate && <div><dt>Due date</dt><dd><time dateTime={task.dueDate}>{task.dueDate}</time></dd></div>}
      {task.assignee && <div><dt>Assignee</dt><dd>{task.assignee}</dd></div>}
    </dl>
    {user && <TaskComments key={`${user.uid}:${task.id}`} uid={user.uid} taskId={task.id} />}
  </dialog>;
}

function TaskComments({ uid, taskId }) {
  const [attempt, setAttempt] = useState(0);
  const [snapshot, setSnapshot] = useState(null);
  const [loadError, setLoadError] = useState('');
  const [postError, setPostError] = useState('');
  const [draft, setDraft] = useState('');
  const [posting, setPosting] = useState(false);
  const write = useRef(null);
  useEffect(() => () => { write.current = null; }, []);

  useEffect(() => {
    let active = true;
    function fail(error) {
      if (!active) return;
      setSnapshot(null);
      setLoadError(commentError(error, 'Could not load comments. Check your connection and try again.'));
    }
    let unsubscribe;
    try {
      unsubscribe = subscribeToTaskComments(uid, taskId, (result) => {
        if (active) setSnapshot(result);
      }, fail);
    } catch (error) {
      fail(error);
    }
    return () => {
      active = false;
      unsubscribe?.();
    };
  }, [uid, taskId, attempt]);

  const awaitingPost = posting || snapshot?.comments.some((comment) => comment.pending);
  async function submit(event) {
    event.preventDefault();
    if (write.current || awaitingPost || !snapshot || loadError) return;
    const text = draft.trim();
    if (!text || text.length > maxCommentLength) {
      setPostError(`Enter a comment of 1 to ${maxCommentLength} characters.`);
      return;
    }

    const pending = postTaskComment(uid, taskId, text);
    write.current = pending;
    setPosting(true);
    setPostError('');
    try {
      await pending;
      if (write.current === pending) setDraft('');
    } catch (error) {
      if (write.current === pending) setPostError(commentError(error, 'Could not post the comment. Try again.'));
    } finally {
      if (write.current === pending) {
        write.current = null;
        setPosting(false);
      }
    }
  }

  return <section className="task-comments" aria-labelledby="comments-heading">
    <h4 id="comments-heading">Comments</h4>
    {!snapshot && !loadError && <p role="status">Loading comments…</p>}
    {loadError && <div>
      <p className="board-error" role="alert">{loadError}</p>
      <button type="button" onClick={() => { setLoadError(''); setAttempt((value) => value + 1); }}>
        Retry comments
      </button>
    </div>}
    {snapshot?.fromCache && <p role="status">Waiting for Firestore to connect. Comments may be out of date.</p>}
    {snapshot && !snapshot.fromCache && !snapshot.comments.length && <p>No comments yet.</p>}
    {Boolean(snapshot?.comments.length) && <ol className="task-comment-list" aria-live="polite">
      {snapshot.comments.map(({ id, text, createdAt, pending }) => <li key={id}>
        <p className="task-details-text">{text}</p>
        {pending ? <span>Sending…</span> : createdAt && (
          <time dateTime={createdAt.toISOString()}>{createdAt.toLocaleString()}</time>
        )}
      </li>)}
    </ol>}
    <form className="task-form" onSubmit={submit} aria-busy={Boolean(awaitingPost)}>
      <div className="task-field">
        <label htmlFor="task-comment">Comment</label>
        <textarea id="task-comment" rows={3} maxLength={maxCommentLength} value={draft}
          disabled={Boolean(awaitingPost) || !snapshot || Boolean(loadError)}
          aria-describedby={postError ? 'task-comment-error' : undefined}
          onChange={(event) => { setDraft(event.target.value); setPostError(''); }} />
        {postError && <p className="board-error" role="alert" id="task-comment-error">{postError}</p>}
      </div>
      <button className="board-primary" type="submit"
        disabled={Boolean(awaitingPost) || !snapshot || Boolean(loadError)}>
        {awaitingPost ? 'Posting…' : 'Post comment'}
      </button>
    </form>
  </section>;
}
