import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { setToken } from "./api";
import type { LoginResponse, Role } from "./types";

interface Session {
  token: string;
  role: Role;
  id: number;
  name: string;
  expiresAt: string;
}

interface AuthContextValue {
  session: Session | null;
  signIn: (login: LoginResponse) => void;
  signOut: () => void;
}

const SESSION_KEY = "pugling.session";
const AuthContext = createContext<AuthContextValue | null>(null);

function load(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const s = JSON.parse(raw) as Session;
    // Abgelaufene Tokens gar nicht erst annehmen.
    if (new Date(s.expiresAt).getTime() < Date.now()) return null;
    // Sessions mit einer nicht mehr gültigen Rolle (z. B. altes "Vater"/"Sohn" vor der Ebenen-Umstellung)
    // verwerfen → sauberer Re-Login, statt den Nutzer an einem Guard in den falschen Login zu werfen.
    if (s.role !== "Supervisor" && s.role !== "Student") return null;
    return s;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(load);

  useEffect(() => {
    setToken(session?.token ?? null);
  }, [session]);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    signIn: (login) => {
      const s: Session = {
        token: login.token, role: login.role, id: login.id, name: login.name, expiresAt: login.expiresAt,
      };
      localStorage.setItem(SESSION_KEY, JSON.stringify(s));
      setToken(s.token);
      setSession(s);
    },
    signOut: () => {
      localStorage.removeItem(SESSION_KEY);
      setToken(null);
      setSession(null);
    },
  }), [session]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth muss innerhalb von <AuthProvider> genutzt werden.");
  return ctx;
}
