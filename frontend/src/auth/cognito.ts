import { Amplify } from 'aws-amplify';
import { fetchAuthSession } from 'aws-amplify/auth';

const userPoolId = import.meta.env.VITE_COGNITO_USER_POOL_ID;
const userPoolClientId = import.meta.env.VITE_COGNITO_USER_POOL_CLIENT_ID;
const region = import.meta.env.VITE_AWS_REGION;

export const cognitoConfigured = Boolean(userPoolId && userPoolClientId && region);

if (cognitoConfigured) {
  Amplify.configure({
    Auth: {
      Cognito: {
        userPoolId,
        userPoolClientId,
        loginWith: { email: true }
      }
    }
  });
}

export async function getIdToken(): Promise<string> {
  const session = await fetchAuthSession();
  const token = session.tokens?.idToken?.toString();
  if (!token) {
    throw new Error('Your session has expired. Sign in again to continue.');
  }

  return token;
}
