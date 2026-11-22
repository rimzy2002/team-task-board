import { initializeApp } from 'firebase/app';
import { getAuth } from 'firebase/auth';

const config = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
};

if (Object.values(config).some((value) => !value?.trim())) {
  throw new Error('Missing Firebase web config. Check frontend/.env.local.');
}

export const firebaseApp = initializeApp(config);
export const auth = getAuth(firebaseApp);
