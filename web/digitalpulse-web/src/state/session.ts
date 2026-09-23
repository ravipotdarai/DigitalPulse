import { create } from "zustand";
import { api, getStoredToken, storeToken, type AuthResponse, type MeResponse } from "../lib/api";

type SessionState = {
  token: string | null;
  profile: MeResponse | null;
  applyAuth: (auth: AuthResponse) => void;
  hydrate: () => Promise<void>;
  clear: () => void;
};

export const useSession = create<SessionState>((set) => ({
  token: null,
  profile: null,
  applyAuth: (auth) => {
    storeToken(auth.accessToken);
    set({
      token: auth.accessToken,
      profile: {
        userId: auth.userId,
        email: auth.email,
        displayName: auth.displayName,
        tenantId: auth.tenantId,
        tenantName: auth.tenantName,
        tenantType: auth.tenantType
      }
    });
  },
  hydrate: async () => {
    const token = getStoredToken();
    if (!token) {
      set({ token: null, profile: null });
      return;
    }
    set({ token });
    try {
      const me = await api.me();
      set({ profile: me });
    } catch {
      storeToken(null);
      set({ token: null, profile: null });
    }
  },
  clear: () => {
    storeToken(null);
    set({ token: null, profile: null });
  }
}));
