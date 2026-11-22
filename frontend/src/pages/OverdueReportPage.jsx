import { useEffect, useState } from 'react';
import { getOverdueReport } from '../services/api.js';
import './overdue-report.css';

export default function OverdueReportPage() {
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState(null);
  const current = result?.attempt === attempt ? result : null;

  useEffect(() => {
    const request = new AbortController();
    getOverdueReport({ signal: request.signal }).then((rows) => {
      if (!request.signal.aborted) setResult({ attempt, rows });
    }).catch((error) => {
      if (!request.signal.aborted) setResult({ attempt, error: error.message });
    });
    return () => request.abort();
  }, [attempt]);

  const refresh = () => setAttempt((previous) => previous + 1);

  return <section className="overdue-report" aria-labelledby="overdue-heading">
    <header className="overdue-report-heading">
      <h2 id="overdue-heading">Overdue report</h2>
      <button type="button" onClick={refresh}>{current?.error ? 'Retry report' : 'Refresh report'}</button>
    </header>
    <p id="overdue-definition">Unfinished tasks in your projects with due dates before today (UTC).</p>
    <div aria-busy={!current}>
      {!current && <p role="status">Loading overdue report…</p>}
      {current?.error && <p className="overdue-error" role="alert">{current.error}</p>}
      {current?.rows?.length === 0 && <p role="status">No overdue tasks in your projects.</p>}
      {current?.rows?.length > 0 && <table aria-describedby="overdue-definition">
        <caption>Overdue tasks by assignee</caption>
        <thead>
          <tr><th scope="col">Assignee</th><th scope="col">Overdue tasks</th></tr>
        </thead>
        <tbody>
          {current.rows.map(({ assigneeName, overdueTaskCount }) => <tr key={assigneeName === null ? 'unassigned' : `assigned:${assigneeName}`}>
            <th scope="row">{assigneeName ?? <em>Unassigned</em>}</th>
            <td>{overdueTaskCount}</td>
          </tr>)}
        </tbody>
      </table>}
    </div>
  </section>;
}
