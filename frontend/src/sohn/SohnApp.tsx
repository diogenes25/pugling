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
import { SohnKonto } from "./SohnKonto";
import { Mascot } from "../components/Mascot";
import { CelebrationLayer, useCelebration } from "../components/Celebration";
import { isMuted, setMuted } from "../lib/feedback";

interface SohnContextValue {
  childId: number;
  coins: number;
  gems: number;
  refreshWallet: () => void;
  skin: Skin;
  setSkin: (s: Skin) => void;
  planId: number | null;
  setPlanId: (id: number) => void;
  streak: number;
  setStreak: (n: number) => void;
  /** Löst eine Feier (Overlay + Ton + Haptik) aus; das Overlay liegt zentral in der Shell. */
  celebrate: ReturnType<typeof useCelebration>["celebrate"];
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
  const [coins, setCoins] = useState(0);
  const [gems, setGems] = useState(0);
  const [skin, setSkin] = useState<Skin>(DEFAULT_SKIN);
  const [planId, setPlanIdState] = useState<number | null>(() => {
    const raw = localStorage.getItem(PLAN_KEY(childId));
    return raw ? Number(raw) : null;
  });
  const [streak, setStreak] = useState(0);
  const [muted, setMutedState] = useState(isMuted());
  const { celebration, celebrate } = useCelebration();

  const refreshWallet = useCallback(() => {
    api.wallet().then((w) => { setCoins(w.coins); setGems(w.gems); }).catch(() => { /* Wallet ist Beiwerk */ });
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
    childId, coins, gems, refreshWallet, skin, setSkin, planId, setPlanId, streak, setStreak, celebrate,
  }), [childId, coins, gems, refreshWallet, skin, planId, setPlanId, streak, celebrate]);

  const toggleMute = useCallback(() => {
    setMutedState((m) => {
      const next = !m;
      setMuted(next);
      return next;
    });
  }, []);

  return (
    <SohnContext.Provider value={value}>
      <div className="app-sohn">
        <div className="sohn-hud">
          <div className="avatar-mini" style={{ background: skin.gradient }} aria-hidden="true">{skin.emoji}</div>
          <div className="chip" aria-live="polite"><span aria-hidden="true">🪙</span><b className="tabnum" aria-label={`${coins} Münzen`}>{coins}</b></div>
          <div className="chip" aria-live="polite"><span aria-hidden="true">💎</span><b className="tabnum" aria-label={`${gems} Gems`}>{gems}</b></div>
          <div className="chip flame" aria-live="polite"><span aria-hidden="true">🔥</span><b className="tabnum" aria-label={`${streak} Tage Streak`}>{streak}</b></div>
          <button
            type="button"
            className="chip mute-toggle"
            onClick={toggleMute}
            aria-pressed={muted ? "true" : "false"}
            aria-label={muted ? "Ton einschalten" : "Ton ausschalten"}
            title={muted ? "Ton einschalten" : "Ton ausschalten"}
          >
            {muted ? "🔇" : "🔊"}
          </button>
        </div>

        <CelebrationLayer celebration={celebration} />

        <Routes>
          <Route index element={<SohnHome />} />
          <Route path="practice/:positionId" element={<SohnPractice />} />
          <Route path="test/:positionId" element={<SohnTest />} />
          <Route path="progress" element={<SohnProgress />} />
          <Route path="skins" element={<SohnSkins />} />
          <Route path="konto" element={<SohnKonto />} />
          <Route path="*" element={<Navigate to="/sohn" replace />} />
        </Routes>

        <nav className="sohn-nav">
          <NavLink to="/sohn" end><span className="ic" aria-hidden="true">🏠</span>Basis</NavLink>
          <NavLink to="/sohn/progress"><span className="ic" aria-hidden="true">🗺️</span>Weg</NavLink>
          <NavLink to="/sohn/konto"><span className="ic" aria-hidden="true">💰</span>Konto</NavLink>
          <NavLink to="/sohn/skins"><span className="ic" aria-hidden="true">🎭</span>Skins</NavLink>
        </nav>
      </div>
    </SohnContext.Provider>
  );
}

/** Kleiner Helfer, den mehrere Screens für den HUD-Avatar nutzen. */
export function HeaderMascot({ skin, mood }: { skin: Skin; mood?: "happy" | "hyped" | "sleepy" }) {
  return <Mascot skin={skin} mood={mood} size={92} />;
}
