import { Alert, Box, CircularProgress } from '@mui/material';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function RequireAuth() {
  const { configurationError, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (configurationError) {
    return (
      <Box sx={{ maxWidth: 640, mx: 'auto', p: 4 }}>
        <Alert severity="error">{configurationError}</Alert>
      </Box>
    );
  }

  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}
