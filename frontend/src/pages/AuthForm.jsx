import { useState } from 'react';
import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
} from 'firebase/auth';
import { Link, Navigate } from 'react-router';
import { auth } from '../config/firebase.js';
import { useAuth } from '../contexts/AuthContext.jsx';

export default function AuthForm({ mode }) {
  const { user, loading } = useAuth();
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);

  const isSignup = mode === 'signup';
  const title = isSignup ? 'Create account' : 'Log in';

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setPending(true);

    const data = new FormData(event.currentTarget);
    const email = String(data.get('email')).trim();
    const password = String(data.get('password'));
    const submit = isSignup
      ? createUserWithEmailAndPassword
      : signInWithEmailAndPassword;

    try {
      await submit(auth, email, password);
    } catch (firebaseError) {
      if (firebaseError.code === 'auth/network-request-failed') {
        setError('Could not reach Firebase. Check your connection and try again.');
      } else if (
        isSignup &&
        ['auth/weak-password', 'auth/password-does-not-meet-requirements'].includes(
          firebaseError.code,
        )
      ) {
        setError('Choose a stronger password and try again.');
      } else {
        setError(
          isSignup
            ? 'Could not create an account. Check your details or try logging in.'
            : 'Could not log in. Check your email and password and try again.',
        );
      }
    } finally {
      setPending(false);
    }
  }

  if (loading) return <p role="status">Checking your session...</p>;
  if (user) return <Navigate to="/" replace />;

  return (
    <main>
      <h1>{title}</h1>

      <form onSubmit={handleSubmit}>
        <p>
          <label htmlFor="email">Email</label>
          <br />
          <input id="email" name="email" type="email" autoComplete="email" required />
        </p>

        <p>
          <label htmlFor="password">Password</label>
          <br />
          <input
            id="password"
            name="password"
            type="password"
            autoComplete={isSignup ? 'new-password' : 'current-password'}
            minLength={isSignup ? 6 : undefined}
            required
          />
        </p>

        {error && <p role="alert">{error}</p>}

        <button type="submit" disabled={pending}>
          {pending ? 'Please wait...' : title}
        </button>
      </form>

      <p>
        {isSignup ? 'Already have an account? ' : 'New to Team Task Board? '}
        <Link to={isSignup ? '/login' : '/signup'}>
          {isSignup ? 'Log in' : 'Create an account'}
        </Link>
      </p>
    </main>
  );
}
