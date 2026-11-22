import { useState } from 'react';
import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
} from 'firebase/auth';
import { Link, Navigate } from 'react-router';
import { auth } from '../config/firebase.js';
import { useAuth } from '../contexts/AuthContext.jsx';
import './ui-theme.css';

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

  if (loading) {
    return (
      <main className="auth-page">
        <p role="status" className="auth-status">
          Checking your session...
        </p>
      </main>
    );
  }
  if (user) return <Navigate to="/" replace />;

  return (
    <main className="auth-page">
      <div className="auth-brand">
        <span className="brand-mark" aria-hidden="true">T</span>
        <span>Team Task Board</span>
      </div>

      <div className="auth-card">
        <h1>{title}</h1>
        <p className="auth-subtitle">
          {isSignup
            ? 'Create an account to start organizing your team’s work.'
            : 'Welcome back. Log in to continue.'}
        </p>

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="email">Email</label>
            <input id="email" name="email" type="email" autoComplete="email" required />
          </div>

          <div className="field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete={isSignup ? 'new-password' : 'current-password'}
              minLength={isSignup ? 6 : undefined}
              required
            />
          </div>

          {error && (
            <p role="alert" className="notice notice-error">
              {error}
            </p>
          )}

          <button type="submit" className="btn btn-primary" disabled={pending}>
            {pending ? 'Please wait...' : title}
          </button>
        </form>

        <p className="auth-switch">
          {isSignup ? 'Already have an account? ' : 'New to Team Task Board? '}
          <Link to={isSignup ? '/login' : '/signup'}>
            {isSignup ? 'Log in' : 'Create an account'}
          </Link>
        </p>
      </div>
    </main>
  );
}
