import { AppBar, Alert, Box, Button, Container, Stack, Toolbar, Typography } from '@mui/material';
import { useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { email, signOut } = useAuth();
  const queryClient = useQueryClient();
  const [signOutError, setSignOutError] = useState<string | null>(null);

  const handleSignOut = async () => {
    setSignOutError(null);
    try {
      await signOut();
      queryClient.clear();
    } catch (error: unknown) {
      setSignOutError(error instanceof Error ? error.message : 'Unable to sign out.');
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', background: 'linear-gradient(180deg, #f8fbff 0%, #eef3ff 100%)' }}>
      <AppBar position="sticky" color="transparent" elevation={0} sx={{ borderBottom: '1px solid rgba(148,163,184,0.25)', backdropFilter: 'blur(8px)' }}>
        <Toolbar>
          <Container maxWidth="xl" sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="h6" sx={{ fontWeight: 800, color: '#0f172a' }}>
              CrudOps Cloud
            </Typography>

            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <Button component={NavLink} to="/" sx={{ color: '#475569', fontWeight: 600 }}>
                Dashboard
              </Button>
              {email ? <Typography variant="body2" color="text.secondary">{email}</Typography> : null}
              <Button onClick={() => { void handleSignOut(); }} color="inherit">
                Sign out
              </Button>
            </Stack>
          </Container>
        </Toolbar>
      </AppBar>

      <Container maxWidth="xl" sx={{ py: 4 }}>
        {signOutError ? <Alert severity="error" sx={{ mb: 2 }}>{signOutError}</Alert> : null}
        <Outlet />
      </Container>
    </Box>
  );
}
