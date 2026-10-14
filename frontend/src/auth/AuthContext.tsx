import {
  confirmSignUp as cognitoConfirmSignUp,
  fetchAuthSession,
  signIn as cognitoSignIn,
  signOut as cognitoSignOut,
  signUp as cognitoSignUp
} from 'aws-amplify/auth';
import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { cognitoConfigured } from './cognito';

interface AuthContextValue {
  email: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  configurationError: string | null;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string) => Promise<boolean>;
  confirmSignUp: (email: string, code: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function getSessionEmail(payload: Record<string, unknown> | undefined): string | null {
  return typeof payload?.email === 'string' ? payload.email : null;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [configurationError, setConfigurationError] = useState<string | null>(
    cognitoConfigured
      ? null
      : 'Set VITE_COGNITO_USER_POOL_ID, VITE_COGNITO_USER_POOL_CLIENT_ID, and VITE_AWS_REGION to enable sign-in.'
  );

  useEffect(() => {
    if (!cognitoConfigured) {
      setIsLoading(false);
      return;
    }

    let active = true;
    void fetchAuthSession()
      .then((session) => {
        if (!active) {
          return;
        }

        const idToken = session.tokens?.idToken;
        setIsAuthenticated(Boolean(idToken));
        setEmail(getSessionEmail(idToken?.payload));
      })
      .catch((error: unknown) => {
        if (active) {
          setConfigurationError(error instanceof Error ? error.message : 'Unable to restore your Cognito session.');
        }
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    email,
    isAuthenticated,
    isLoading,
    configurationError,
    async signIn(userEmail, password) {
      const result = await cognitoSignIn({ username: userEmail, password });
      if (!result.isSignedIn) {
        throw new Error(`Additional sign-in step required: ${result.nextStep.signInStep}.`);
      }

      const session = await fetchAuthSession();
      setEmail(getSessionEmail(session.tokens?.idToken?.payload) ?? userEmail);
      setIsAuthenticated(Boolean(session.tokens?.idToken));
    },
    async signUp(userEmail, password) {
      const result = await cognitoSignUp({
        username: userEmail,
        password,
        options: { userAttributes: { email: userEmail } }
      });

      return result.nextStep.signUpStep === 'CONFIRM_SIGN_UP';
    },
    async confirmSignUp(userEmail, code) {
      await cognitoConfirmSignUp({ username: userEmail, confirmationCode: code });
    },
    async signOut() {
      await cognitoSignOut();
      setEmail(null);
      setIsAuthenticated(false);
    }
  }), [configurationError, email, isAuthenticated, isLoading]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.');
  }

  return context;
}
