import { createTheme } from '@mui/material/styles';

export const appTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#2563EB'
    },
    secondary: {
      main: '#7C3AED'
    },
    success: {
      main: '#10B981'
    },
    error: {
      main: '#EF4444'
    },
    background: {
      default: '#F5F7FB',
      paper: '#FFFFFF'
    }
  },
  shape: {
    borderRadius: 16
  },
  typography: {
    fontFamily: 'Inter, system-ui, sans-serif',
    h4: {
      fontWeight: 700
    },
    h5: {
      fontWeight: 700
    }
  }
});
