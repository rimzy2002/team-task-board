import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router';
import { createProject, listProjects } from '../services/api.js';
import './projects.css';

export default function ProjectsPage() {
  const [projects, setProjects] = useState([]);
  const [listState, setListState] = useState({ loading: true });
  const [attempt, setAttempt] = useState(0);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [fields, setFields] = useState({});
  const [submitError, setSubmitError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const submission = useRef(null);

  useEffect(() => {
    const request = new AbortController();
    listProjects({ signal: request.signal }).then((result) => {
      if (request.signal.aborted) return;
      setProjects(result);
      setListState({ loading: false });
    }).catch((error) => {
      if (!request.signal.aborted) setListState({ error: error.message });
    });
    return () => request.abort();
  }, [attempt]);

  useEffect(() => () => submission.current?.abort(), []);

  function reloadProjects() {
    setListState({ loading: true });
    setAttempt((value) => value + 1);
  }

  async function submit(event) {
    event.preventDefault();
    if (submission.current || listState.loading || listState.error) return;

    const projectName = name.trim();
    const details = description.trim();
    const errors = {
      name: !projectName ? 'Enter a project name.' : projectName.length > 200 ? 'Use 200 characters or fewer for the name.' : '',
      description: details.length > 2000 ? 'Use 2,000 characters or fewer for the description.' : '',
    };
    setFields(errors);
    setSubmitError('');
    if (errors.name || errors.description) return;

    const request = new AbortController();
    submission.current = request;
    setSubmitting(true);
    try {
      const created = await createProject({ name: projectName, description: details || null }, { signal: request.signal });
      if (request.signal.aborted) return;
      setProjects((current) => [created, ...current]);
      setName('');
      setDescription('');
    } catch (error) {
      if (!request.signal.aborted) {
        setFields(error.fields ?? {});
        setSubmitError(error.fields ? error.message : `${error.message} Review your projects before retrying.`);
        if (!error.fields) reloadProjects();
      }
    } finally {
      submission.current = null;
      if (!request.signal.aborted) setSubmitting(false);
    }
  }

  return (
    <section className="projects" aria-labelledby="projects-heading">
      <h2 id="projects-heading">Projects</h2>
      <div className="projects-layout">
        <form className="project-form" onSubmit={submit} noValidate aria-labelledby="create-project-heading">
          <h3 id="create-project-heading">New project</h3>
          <label htmlFor="project-name">Project name</label>
          <input id="project-name" value={name} onChange={(event) => setName(event.target.value)}
            required disabled={submitting} aria-invalid={Boolean(fields.name)} aria-describedby={fields.name ? 'name-error' : undefined} />
          {fields.name && <p id="name-error" role="alert">{fields.name}</p>}
          <label htmlFor="project-description">Description (optional)</label>
          <textarea id="project-description" value={description} onChange={(event) => setDescription(event.target.value)}
            rows={4} disabled={submitting} aria-invalid={Boolean(fields.description)} aria-describedby={fields.description ? 'description-error' : undefined} />
          {fields.description && <p id="description-error" role="alert">{fields.description}</p>}
          {submitError && <p role="alert">{submitError}</p>}
          <button type="submit" disabled={submitting || listState.loading || Boolean(listState.error)}>
            {submitting ? 'Creating project…' : 'Create project'}
          </button>
        </form>

        <section aria-label="Your projects" aria-busy={Boolean(listState.loading)}>
          {listState.loading && <p role="status">Loading projects…</p>}
          {listState.error && (
            <div>
              <p role="alert">{listState.error}</p>
              <button type="button" onClick={reloadProjects}>
                Retry loading projects
              </button>
            </div>
          )}
          {!listState.loading && !listState.error && (projects.length === 0 ? <p>No projects yet.</p> : (
            <ul className="project-list">
              {projects.map((project) => (
                <li key={project.id}>
                  <h3><Link to={`/projects/${project.id}/board`}>{project.name}</Link></h3>
                  {project.description && <p>{project.description}</p>}
                </li>
              ))}
            </ul>
          ))}
        </section>
      </div>
    </section>
  );
}
