import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { RequireAuth } from './auth/RequireAuth';
import { Layout } from './components/Layout';
import { AuthPage } from './pages/AuthPage';
import { DashboardPage } from './pages/DashboardPage';
import { appTheme } from './theme';

const queryClient = new QueryClient();

export default function App() {
  return (
    <AuthProvider>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={appTheme}>
          <CssBaseline />
          <BrowserRouter>
            <Routes>
              <Route path="/login" element={<AuthPage />} />
              <Route element={<RequireAuth />}>
                <Route element={<Layout />}>
                  <Route path="/" element={<DashboardPage />} />
                </Route>
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </BrowserRouter>
        </ThemeProvider>
      </QueryClientProvider>
    </AuthProvider>
  );
}
