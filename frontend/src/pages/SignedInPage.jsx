import { useEffect, useRef, useState } from 'react';
import { signOut } from 'firebase/auth';
import { auth } from '../config/firebase.js';
import { useAuth } from '../contexts/AuthContext.jsx';
import { getCurrentUser } from '../services/api.js';
import { NavLink, Outlet } from 'react-router';
import './projects.css';
import './ui-theme.css';

export default function SignedInPage() {
  const { user } = useAuth();
  return <SignedInAccount key={user.uid} user={user} />;
}

function SignedInAccount({ user }) {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [apiCheck, setApiCheck] = useState(null);
  const apiRequest = useRef(null);

  useEffect(() => () => apiRequest.current?.abort(), []);

  async function checkApi() {
    const request = new AbortController();
    apiRequest.current = request;
    setApiCheck({ pending: true });

    try {
      const localUser = await getCurrentUser({ signal: request.signal });
      if (!request.signal.aborted) setApiCheck({ id: localUser.id });
    } catch (apiError) {
      if (!request.signal.aborted) setApiCheck({ error: apiError.message });
    }
  }

  async function handleLogout() {
    setError('');
    setPending(true);

    try {
      await signOut(auth);
    } catch (firebaseError) {
      if (firebaseError.code === 'auth/network-request-failed') {
        setError('Could not reach Firebase. Check your connection and try again.');
      } else {
        setError('Could not log out. Please try again.');
      }
    } finally {
      setPending(false);
    }
  }

  return (
    <main className="workspace app-shell">
      <header className="app-header">
        <div className="app-brand">
          <span className="brand-mark" aria-hidden="true">T</span>
          <h1>Team Task Board</h1>
        </div>

        <div className="app-account">
          <p className="app-user">
            Signed in as <strong>{user.email}</strong>
          </p>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={checkApi}
            disabled={pending || apiCheck?.pending}
          >
            {apiCheck?.pending ? 'Checking API connection...' : 'Check API connection'}
          </button>
          <button
            type="button"
            className="btn btn-ghost"
            onClick={handleLogout}
            disabled={pending}
          >
            {pending ? 'Logging out...' : 'Log out'}
          </button>
        </div>
      </header>

      <div className="app-feedback">
        {apiCheck?.id && (
          <p role="status" className="notice notice-success">
            API connected. Local user ID: {apiCheck.id}
          </p>
        )}
        {apiCheck?.error && (
          <p role="alert" className="notice notice-error">
            {apiCheck.error}
          </p>
        )}
        {error && (
          <p role="alert" className="notice notice-error">
            {error}
          </p>
        )}
      </div>

      <nav className="workspace-nav" aria-label="Workspace">
        <NavLink to="/projects">Projects</NavLink>
        <NavLink to="/reports/overdue">Overdue report</NavLink>
      </nav>
      <Outlet />
    </main>
  );
}
