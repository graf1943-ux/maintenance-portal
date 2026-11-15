import {
  type AccountInfo,
  type Configuration,
  type PublicClientApplication,
} from '@azure/msal-browser';
import { reactive } from 'vue';

type MsalModule = typeof import('@azure/msal-browser');

let msalModulePromise: Promise<MsalModule> | undefined;

function loadMsal(): Promise<MsalModule> {
  if (!msalModulePromise) msalModulePromise = import('@azure/msal-browser');
  return msalModulePromise;
}

export type PortalRole = 'staff' | 'customer';

type IdentitySettings = {
  clientId: string;
  authority: string;
  scope: string;
  knownAuthority: string;
};

const sessionKey = 'roadbike-care-auth-role';
const qrSessionKey = 'roadbike-care-qr-session';
let qrAccessToken: string | null = null;
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
  const { PublicClientApplication } = await loadMsal();
  const application = new PublicClientApplication(msalConfig);
  await application.initialize();
  applications.set(role, application);
  return application;
}

export async function restoreAuthentication(): Promise<boolean> {
  const qrCode = new URL(window.location.href).searchParams.get('qr');
  if (qrCode) {
    const response = await fetch(
      `${import.meta.env.VITE_API_BASE_URL ?? '/api'}/qr-access/redeem`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code: qrCode }),
      }
    );
    const body = await response.json();
    window.history.replaceState({}, '', window.location.pathname);
    if (!response.ok) throw new Error(body.message ?? 'QRコードを確認できませんでした。');
    qrAccessToken = body.accessToken;
    sessionStorage.setItem(qrSessionKey, qrAccessToken!);
    authState.role = 'customer';
    authState.account = null;
    return true;
  }
  const savedQrSession = sessionStorage.getItem(qrSessionKey);
  if (savedQrSession) {
    qrAccessToken = savedQrSession;
    authState.role = 'customer';
    return true;
  }
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
  if (qrAccessToken) return qrAccessToken;
  const role = authState.role;
  const account = authState.account;
  if (!role || !account) throw new Error('サインインしてください。');
  const application = await getApplication(role);
  const { InteractionRequiredAuthError } = await loadMsal();
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
  const qrToken = qrAccessToken;
  if (qrToken) {
    try {
      await fetch(`${import.meta.env.VITE_API_BASE_URL ?? '/api'}/qr-access/session`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${qrToken}` },
      });
    } catch {
      // Local sign-out still clears the session if the API is unavailable.
    }
  }
  sessionStorage.removeItem(sessionKey);
  sessionStorage.removeItem(qrSessionKey);
  qrAccessToken = null;
  authState.role = null;
  authState.account = null;
  if (!role || !account) return;
  const application = await getApplication(role);
  await application.logoutRedirect({ account, postLogoutRedirectUri: window.location.origin });
}
