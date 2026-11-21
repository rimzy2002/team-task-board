import { Navigate, Outlet } from 'react-router';
import { useAuth } from '../contexts/AuthContext.jsx';

export default function ProtectedRoute() {
  const { user, loading } = useAuth();

  if (loading) return <p role="status">Checking your session...</p>;
  if (!user) return <Navigate to="/login" replace />;

  return <Outlet />;
}
