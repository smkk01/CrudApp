import { LockOutlined } from '@mui/icons-material';
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Stack,
  TextField,
  Typography
} from '@mui/material';
import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

type AuthMode = 'sign-in' | 'sign-up' | 'confirm';

export function AuthPage() {
  const { configurationError, confirmSignUp, isAuthenticated, signIn, signUp } = useAuth();
  const [mode, setMode] = useState<AuthMode>('sign-in');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setNotice(null);
    setIsSubmitting(true);

    try {
      if (mode === 'sign-in') {
        await signIn(email, password);
      } else if (mode === 'sign-up') {
        const needsConfirmation = await signUp(email, password);
        if (needsConfirmation) {
          setMode('confirm');
          setNotice('Check your email for the verification code.');
        } else {
          setNotice('Account created. Sign in to continue.');
          setMode('sign-in');
        }
      } else {
        await confirmSignUp(email, code);
        setMode('sign-in');
        setNotice('Email verified. Sign in to continue.');
      }
    } catch (submitError: unknown) {
      setError(submitError instanceof Error ? submitError.message : 'Authentication failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const title = mode === 'sign-in' ? 'Welcome back' : mode === 'sign-up' ? 'Create your account' : 'Verify your email';

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', px: 2, background: 'linear-gradient(180deg, #f8fbff 0%, #eef3ff 100%)' }}>
      <Card sx={{ width: '100%', maxWidth: 440, boxShadow: '0 24px 64px rgba(15, 23, 42, 0.12)' }}>
        <CardContent sx={{ p: { xs: 3, sm: 4 } }}>
          <Stack component="form" spacing={2.5} onSubmit={handleSubmit}>
            <Stack spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
              <LockOutlined color="primary" fontSize="large" />
              <Typography variant="h5">{title}</Typography>
              <Typography color="text.secondary" align="center">
                Sign in to manage your product inventory.
              </Typography>
            </Stack>

            {error ? <Alert severity="error">{error}</Alert> : null}
            {notice ? <Alert severity="success">{notice}</Alert> : null}
            {configurationError ? <Alert severity="error">{configurationError}</Alert> : null}

            <TextField
              label="Email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
              fullWidth
            />

            {mode !== 'confirm' ? (
              <TextField
                label="Password"
                type="password"
                autoComplete={mode === 'sign-in' ? 'current-password' : 'new-password'}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                slotProps={{ htmlInput: { minLength: 8 } }}
                required
                fullWidth
              />
            ) : (
              <TextField
                label="Verification code"
                value={code}
                onChange={(event) => setCode(event.target.value)}
                autoComplete="one-time-code"
                required
                fullWidth
              />
            )}

            <Button type="submit" variant="contained" size="large" disabled={isSubmitting || Boolean(configurationError)}>
              {isSubmitting ? <CircularProgress size={24} color="inherit" /> : mode === 'sign-in' ? 'Sign in' : mode === 'sign-up' ? 'Create account' : 'Verify email'}
            </Button>

            {mode === 'sign-in' ? (
              <Button variant="text" onClick={() => { setMode('sign-up'); setError(null); setNotice(null); }}>
                New here? Create an account
              </Button>
            ) : mode === 'sign-up' ? (
              <Button variant="text" onClick={() => { setMode('sign-in'); setError(null); setNotice(null); }}>
                Already registered? Sign in
              </Button>
            ) : (
              <Button variant="text" onClick={() => { setMode('sign-in'); setError(null); setNotice(null); }}>
                Back to sign in
              </Button>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
