import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { setToken, setUnauthorizedHandler } from "./api";
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
  /**
   * Übernimmt die Anmeldung. Gibt `false` zurück, wenn der Server eine Rolle liefert, die diese App nicht
   * kennt – der Aufrufer **muss** das melden, sonst sieht ein gültiger Login wie „nichts passiert" aus.
   */
  signIn: (login: LoginResponse) => boolean;
  signOut: () => void;
}

const SESSION_KEY = "pugling.session";
const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Ist die Rolle eine, die diese Oberfläche kennt? Nötig an **zwei** Stellen, und aus zwei Gründen: aus dem
 * `localStorage` kommt eine womöglich veraltete Sitzung, und aus dem Login kommt laut Vertrag ein nackter
 * `string` (`LoginResponse.Role` ist im Backend kein Enum, sondern eine Konstante). Statt zu casten wird
 * geprüft – eine unbekannte Rolle führt zum Re-Login, nicht zu einer Sitzung ohne Zuhause.
 */
function isRole(value: string): value is Role {
  return value === "Supervisor" || value === "Creator" || value === "Student";
}

function load(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const s = JSON.parse(raw) as Session;
    // Abgelaufene Tokens gar nicht erst annehmen.
    if (new Date(s.expiresAt).getTime() < Date.now()) return null;
    // Sessions mit einer nicht mehr gültigen Rolle (z. B. altes "Vater"/"Sohn" vor der Ebenen-Umstellung)
    // verwerfen → sauberer Re-Login, statt den Nutzer an einem Guard in den falschen Login zu werfen.
    if (!isRole(s.role)) return null;
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

  /*
   * Ein 401 auf einer beliebigen Anfrage heißt: das Token gilt nicht mehr (abgelaufen, Konto geändert).
   * Die Sitzung hier zu beenden ist die einzige Stelle, die das zentral kann – sonst müsste jedes Panel
   * den Fall selbst behandeln, und der Nutzer bliebe auf einer Seite voller Fehlermeldungen sitzen.
   */
  useEffect(() => {
    setUnauthorizedHandler(() => {
      localStorage.removeItem(SESSION_KEY);
      setToken(null);
      setSession(null);
    });
    return () => setUnauthorizedHandler(null);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    session,
    signIn: (login) => {
      if (!isRole(login.role)) return false;
      const s: Session = {
        token: login.token, role: login.role, id: login.id, name: login.name, expiresAt: login.expiresAt,
      };
      localStorage.setItem(SESSION_KEY, JSON.stringify(s));
      setToken(s.token);
      setSession(s);
      return true;
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
