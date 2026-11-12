import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo,
  type Configuration,
} from '@azure/msal-browser';
import { reactive } from 'vue';

export type PortalRole = 'staff' | 'customer';

type IdentitySettings = {
  clientId: string;
  authority: string;
  scope: string;
  knownAuthority: string;
};

const sessionKey = 'roadbike-care-auth-role';
const settings: Record<PortalRole, IdentitySettings> = {
  customer: {
    clientId: import.meta.env.VITE_ENTRA_CUSTOMER_CLIENT_ID ?? '',
    authority: import.meta.env.VITE_ENTRA_CUSTOMER_AUTHORITY ?? '',
    scope: import.meta.env.VITE_ENTRA_CUSTOMER_API_SCOPE ?? '',
    knownAuthority: import.meta.env.VITE_ENTRA_CUSTOMER_KNOWN_AUTHORITY ?? '',
  },
  staff: {
    clientId: import.meta.env.VITE_ENTRA_STAFF_CLIENT_ID ?? '',
    authority: import.meta.env.VITE_ENTRA_STAFF_AUTHORITY ?? '',
    scope: import.meta.env.VITE_ENTRA_STAFF_API_SCOPE ?? '',
    knownAuthority: import.meta.env.VITE_ENTRA_STAFF_KNOWN_AUTHORITY ?? '',
  },
};

const applications = new Map<PortalRole, PublicClientApplication>();
export const authState = reactive({
  role: null as PortalRole | null,
  account: null as AccountInfo | null,
  loading: false,
});

export function isAuthConfigured(role: PortalRole): boolean {
  const config = settings[role];
  return Boolean(config.clientId && config.authority && config.scope);
}

async function getApplication(role: PortalRole): Promise<PublicClientApplication> {
  if (!isAuthConfigured(role)) throw new Error(`Entra IDの${role}向け設定が不足しています。`);
  const existing = applications.get(role);
  if (existing) return existing;

  const config = settings[role];
  const msalConfig: Configuration = {
    auth: {
      clientId: config.clientId,
      authority: config.authority,
      redirectUri: window.location.origin,
      knownAuthorities: config.knownAuthority ? [config.knownAuthority] : [],
    },
    cache: { cacheLocation: 'sessionStorage' },
  };
  const application = new PublicClientApplication(msalConfig);
  await application.initialize();
  applications.set(role, application);
  return application;
}

export async function restoreAuthentication(): Promise<boolean> {
  const role = sessionStorage.getItem(sessionKey);
  if (role !== 'customer' && role !== 'staff') return false;
  if (!isAuthConfigured(role)) {
    sessionStorage.removeItem(sessionKey);
    return false;
  }

  const application = await getApplication(role);
  const redirectResult = await application.handleRedirectPromise();
  const account =
    redirectResult?.account ?? application.getActiveAccount() ?? application.getAllAccounts()[0];
  if (!account) {
    sessionStorage.removeItem(sessionKey);
    return false;
  }
  application.setActiveAccount(account);
  authState.role = role;
  authState.account = account;
  return true;
}

export async function signIn(role: PortalRole): Promise<void> {
  authState.loading = true;
  try {
    const application = await getApplication(role);
    sessionStorage.setItem(sessionKey, role);
    try {
      await application.loginRedirect({
        scopes: [settings[role].scope],
        prompt: 'select_account',
      });
    } catch (error) {
      sessionStorage.removeItem(sessionKey);
      throw error;
    }
  } finally {
    authState.loading = false;
  }
}

export async function getAccessToken(): Promise<string> {
  const role = authState.role;
  const account = authState.account;
  if (!role || !account) throw new Error('サインインしてください。');
  const application = await getApplication(role);
  try {
    const result = await application.acquireTokenSilent({
      account,
      scopes: [settings[role].scope],
    });
    return result.accessToken;
  } catch (error) {
    if (!(error instanceof InteractionRequiredAuthError)) throw error;
    await application.acquireTokenRedirect({ account, scopes: [settings[role].scope] });
    throw new Error('再認証のためページを移動しています。');
  }
}

export async function signOut(): Promise<void> {
  const role = authState.role;
  const account = authState.account;
  sessionStorage.removeItem(sessionKey);
  authState.role = null;
  authState.account = null;
  if (!role || !account) return;
  const application = await getApplication(role);
  await application.logoutRedirect({ account, postLogoutRedirectUri: window.location.origin });
}
