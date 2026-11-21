import { auth } from '../config/firebase.js';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() || 'http://127.0.0.1:5080';

function getApiUrl(path) {
  try {
    const base = new URL(apiBaseUrl);
    const url = new URL(path, base);
    const isLoopback = ['localhost', '127.0.0.1', '[::1]'].includes(base.hostname);
    const isSafeProtocol = base.protocol === 'https:' || (base.protocol === 'http:' && isLoopback);

    if (isSafeProtocol && !base.username && !base.password &&
        !url.username && !url.password && url.origin === base.origin) {
      return url;
    }
  } catch {
    // Keep configuration errors free of URLs and credentials.
  }

  throw new Error('Check VITE_API_BASE_URL. Use HTTPS or a local HTTP address.');
}

export function getProtectedJson(path, options) {
  return requestProtectedJson(path, options);
}

async function requestProtectedJson(path, { method = 'GET', body, signal } = {}) {
  const url = getApiUrl(path);
  await auth.authStateReady();
  const user = auth.currentUser;

  function ensureSameSession() {
    signal?.throwIfAborted();
    if (!user || auth.currentUser !== user) {
      throw new Error('Sign in again to check the API connection.');
    }
  }

  async function send(forceRefresh) {
    ensureSameSession();

    let token;
    try {
      token = await user.getIdToken(forceRefresh);
    } catch {
      throw new Error('Could not get a Firebase session token. Check your connection or sign in again.');
    }

    ensureSameSession();
    const timeout = AbortSignal.timeout(10000);
    try {
      return await fetch(url, {
        method,
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${token}`,
          ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        },
        body: body === undefined ? undefined : JSON.stringify(body),
        credentials: 'omit',
        redirect: 'error',
        cache: 'no-store',
        signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
      });
    } catch {
      signal?.throwIfAborted();
      throw new Error('Could not reach the API. Check that it is running and that the API URL is correct.');
    }
  }

  let response = await send(false);
  if (response.status === 401) {
    response = await send(true);
  }

  ensureSameSession();
  if (response.status === 401) {
    throw new Error('The API rejected your session. Check the API Firebase project ID or sign in again.');
  }
  if (response.status === 403) {
    throw new Error('Your account does not have access to this API request.');
  }
  if (response.status === 400 && method === 'POST') {
    const problem = await response.json().catch(() => null);
    ensureSameSession();
    const error = new Error('Check the project details and try again.');
    const keys = Object.keys(problem?.errors ?? {}).map((key) => key.toLowerCase());
    error.fields = {
      name: keys.includes('name') ? 'Enter a project name of 200 characters or fewer.' : '',
      description: keys.includes('description') ? 'Use 2,000 characters or fewer for the description.' : '',
    };
    throw error;
  }
  if (!response.ok) {
    throw new Error('The API request failed. Check the API and MySQL, then try again.');
  }

  let data;
  try {
    data = await response.json();
  } catch {
    signal?.throwIfAborted();
    throw new Error('The API returned an unreadable response.');
  }
  ensureSameSession();
  return data;
}

export async function getCurrentUser(options) {
  const user = await getProtectedJson('/api/auth/me', options);
  if (!Number.isSafeInteger(user?.id) || user.id <= 0) {
    throw new Error('The API returned an invalid local user ID.');
  }
  return user;
}

export function listProjects(options) {
  return getProtectedJson('/api/projects', options);
}

export function createProject({ name, description }, options) {
  return requestProtectedJson('/api/projects', { ...options, method: 'POST', body: { name, description } });
}
