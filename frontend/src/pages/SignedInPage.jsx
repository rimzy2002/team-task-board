import { useEffect, useRef, useState } from 'react';
import { signOut } from 'firebase/auth';
import { auth } from '../config/firebase.js';
import { useAuth } from '../contexts/AuthContext.jsx';
import { getCurrentUser } from '../services/api.js';
import ProjectsPage from './ProjectsPage.jsx';

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
    <main className="workspace">
      <h1>Team Task Board</h1>
      <p>Signed in as {user.email}</p>
      <button type="button" onClick={checkApi} disabled={pending || apiCheck?.pending}>
        {apiCheck?.pending ? 'Checking API connection...' : 'Check API connection'}
      </button>
      {apiCheck?.id && <p role="status">API connected. Local user ID: {apiCheck.id}</p>}
      {apiCheck?.error && <p role="alert">{apiCheck.error}</p>}
      {error && <p role="alert">{error}</p>}
      <button type="button" onClick={handleLogout} disabled={pending}>
        {pending ? 'Logging out...' : 'Log out'}
      </button>
      <ProjectsPage />
    </main>
  );
}
