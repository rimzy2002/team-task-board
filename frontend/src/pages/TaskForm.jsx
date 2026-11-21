import { useState } from 'react';

export const taskStatuses = [
  ['ToDo', 'To Do'],
  ['InProgress', 'In Progress'],
  ['Done', 'Done'],
];
export const taskPriorities = ['Low', 'Medium', 'High'];

export default function TaskForm({ task, busy, serverErrors, onSubmit, onCancel }) {
  const [values, setValues] = useState({
    title: task?.title ?? '',
    description: task?.description ?? '',
    status: task?.status ?? 'ToDo',
    priority: task?.priority ?? 'Medium',
    dueDate: task?.dueDate ?? '',
    assignee: task?.assignee ?? '',
  });
  const [errors, setErrors] = useState({});
  const [changed, setChanged] = useState({});

  function update(event) {
    const { name, value } = event.target;
    setValues((previous) => ({ ...previous, [name]: value }));
    setErrors((previous) => ({ ...previous, [name]: '' }));
    setChanged((previous) => ({ ...previous, [name]: true }));
  }

  function submit(event) {
    event.preventDefault();
    if (busy) return;
    const title = values.title.trim();
    const description = values.description.trim();
    const assignee = values.assignee.trim();
    const nextErrors = {};
    const today = new Date().toISOString().slice(0, 10);
    if (!title || title.length > 200) nextErrors.title = 'Enter a title of 1 to 200 characters.';
    if (new TextEncoder().encode(description).length > 65535) nextErrors.description = 'Use at most 65,535 UTF-8 bytes.';
    if (assignee.length > 200) nextErrors.assignee = 'Use 200 characters or fewer.';
    if (!taskStatuses.some(([value]) => value === values.status)) nextErrors.status = 'Choose a task status.';
    if (!taskPriorities.includes(values.priority)) nextErrors.priority = 'Choose a priority.';
    if (event.currentTarget.elements.dueDate.validity.badInput ||
        (values.dueDate && (!/^\d{4}-\d{2}-\d{2}$/.test(values.dueDate) ||
        (values.dueDate !== task?.dueDate && values.dueDate < today)))) {
      nextErrors.dueDate = 'Use a valid date. A new due date must be today or later (UTC).';
    }
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length) return;
    setChanged({});
    onSubmit({ ...values, title, description: description || null, assignee: assignee || null,
      dueDate: values.dueDate || null });
  }

  function field(name, label, kind = 'text', options = []) {
    const error = errors[name] || (!changed[name] && serverErrors?.[name]);
    const props = {
      id: `task-${name}`, name, value: values[name], onChange: update,
      'aria-invalid': Boolean(error),
      'aria-describedby': error ? `task-${name}-error` : undefined,
    };
    return <div className="task-field">
      <label htmlFor={props.id}>{label}</label>
      {kind === 'select' ? (
        <select {...props}>{options.map(([value, text]) => <option key={value} value={value}>{text}</option>)}</select>
      ) : kind === 'textarea' ? <textarea {...props} rows={3} /> : (
        <input {...props} type={kind} required={name === 'title'}
          maxLength={kind === 'text' ? 200 : undefined} />
      )}
      {error && <p className="board-error" id={`task-${name}-error`} role="alert">{error}</p>}
    </div>;
  }

  return <form className="task-form" onSubmit={submit} noValidate aria-busy={busy}>
    <fieldset disabled={busy}>
      {field('title', 'Title')}
      {field('description', 'Description (optional)', 'textarea')}
      <div className="task-form-grid">
        {field('status', 'Status', 'select', taskStatuses)}
        {field('priority', 'Priority', 'select', taskPriorities.map((value) => [value, value]))}
        {field('dueDate', 'Due date (optional, UTC)', 'date')}
        {field('assignee', 'Assignee (optional)')}
      </div>
    </fieldset>
    <div className="board-actions">
      <button className="board-primary" type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save task'}</button>
      <button type="button" disabled={busy} onClick={onCancel}>Cancel</button>
    </div>
  </form>;
}
