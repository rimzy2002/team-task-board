import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router';
import { AuthProvider } from './contexts/AuthContext.jsx';
import LoginPage from './pages/LoginPage.jsx';
import SignupPage from './pages/SignupPage.jsx';
import SignedInPage from './pages/SignedInPage.jsx';
import ProjectsPage from './pages/ProjectsPage.jsx';
import BoardPage from './pages/BoardPage.jsx';
import OverdueReportPage from './pages/OverdueReportPage.jsx';
import ProtectedRoute from './routes/ProtectedRoute.jsx';

createRoot(document.getElementById('root')).render(
  <BrowserRouter>
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/signup" element={<SignupPage />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<SignedInPage />}>
            <Route index element={<ProjectsPage />} />
            <Route path="projects" element={<ProjectsPage />} />
            <Route path="projects/:projectId/board" element={<BoardPage />} />
            <Route path="reports/overdue" element={<OverdueReportPage />} />
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  </BrowserRouter>,
);
