import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { api } from "../lib/api";
import { useAuth } from "../lib/auth";
import { DEFAULT_SKIN, skinById, type Skin } from "../lib/skins";
import { SohnLogin } from "./SohnLogin";
import { SohnHome } from "./SohnHome";
import { SohnPractice } from "./SohnPractice";
import { SohnTest } from "./SohnTest";
import { SohnProgress } from "./SohnProgress";
import { SohnSkins } from "./SohnSkins";
import { Mascot } from "../components/Mascot";

interface SohnContextValue {
  childId: number;
  balance: number;
  refreshWallet: () => void;
  skin: Skin;
  setSkin: (s: Skin) => void;
  planId: number | null;
  setPlanId: (id: number) => void;
  streak: number;
  setStreak: (n: number) => void;
}

const SohnContext = createContext<SohnContextValue | null>(null);
export function useSohn(): SohnContextValue {
  const ctx = useContext(SohnContext);
  if (!ctx) throw new Error("useSohn außerhalb der Sohn-App genutzt.");
  return ctx;
}

const PLAN_KEY = (childId: number) => `pugling.plan.${childId}`;

export function SohnApp() {
  const { session } = useAuth();
  if (!session || session.role !== "Sohn") return <SohnLogin />;
  return <SohnShell childId={session.id} />;
}

function SohnShell({ childId }: { childId: number }) {
  const [balance, setBalance] = useState(0);
  const [skin, setSkin] = useState<Skin>(DEFAULT_SKIN);
  const [planId, setPlanIdState] = useState<number | null>(() => {
    const raw = localStorage.getItem(PLAN_KEY(childId));
    return raw ? Number(raw) : null;
  });
  const [streak, setStreak] = useState(0);

  const refreshWallet = useCallback(() => {
    api.wallet().then((w) => setBalance(w.balance)).catch(() => { /* Wallet ist Beiwerk */ });
  }, []);

  useEffect(() => { refreshWallet(); }, [refreshWallet]);

  // Ausgerüsteten Skin server-autoritativ laden (gilt geräteübergreifend); bis dahin Starter.
  useEffect(() => {
    api.skins().then((s) => setSkin(skinById(s.selected))).catch(() => { /* Fallback: Starter */ });
  }, [childId]);

  const setPlanId = useCallback((id: number) => {
    localStorage.setItem(PLAN_KEY(childId), String(id));
    setPlanIdState(id);
  }, [childId]);

  const value = useMemo<SohnContextValue>(() => ({
    childId, balance, refreshWallet, skin, setSkin, planId, setPlanId, streak, setStreak,
  }), [childId, balance, refreshWallet, skin, planId, setPlanId, streak]);

  return (
    <SohnContext.Provider value={value}>
      <div className="app-sohn">
        <div className="sohn-hud">
          <div className="avatar-mini" style={{ background: skin.gradient }}>{skin.emoji}</div>
          <div className="chip">🪙<b className="tabnum">{balance}</b></div>
          <div className="chip flame">🔥<b className="tabnum">{streak}</b></div>
        </div>

        <Routes>
          <Route index element={<SohnHome />} />
          <Route path="practice" element={<SohnPractice />} />
          <Route path="test" element={<SohnTest />} />
          <Route path="progress" element={<SohnProgress />} />
          <Route path="skins" element={<SohnSkins />} />
          <Route path="*" element={<Navigate to="/sohn" replace />} />
        </Routes>

        <nav className="sohn-nav">
          <NavLink to="/sohn" end><span className="ic">🏠</span>Basis</NavLink>
          <NavLink to="/sohn/progress"><span className="ic">🗺️</span>Weg</NavLink>
          <NavLink to="/sohn/skins"><span className="ic">🎭</span>Skins</NavLink>
        </nav>
      </div>
    </SohnContext.Provider>
  );
}

/** Kleiner Helfer, den mehrere Screens für den HUD-Avatar nutzen. */
export function HeaderMascot({ skin, mood }: { skin: Skin; mood?: "happy" | "hyped" | "sleepy" }) {
  return <Mascot skin={skin} mood={mood} size={92} />;
}
