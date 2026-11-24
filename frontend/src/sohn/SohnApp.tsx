import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { isLanguage, setLanguage } from "../lib/i18n";
import { ArcadeScope } from "../lib/chromeText";
import { api } from "../lib/api";
import { useAuth } from "../lib/auth";
import { DEFAULT_SKIN, skinById, type Skin } from "../lib/skins";
import { SohnLogin } from "./SohnLogin";
import { SohnHome } from "./SohnHome";
import { SohnPractice } from "./SohnPractice";
import { SohnTest } from "./SohnTest";
import { SohnProgress } from "./SohnProgress";
import { SohnSkins } from "./SohnSkins";
import { SohnShop } from "./SohnShop";
import { SohnKonto } from "./SohnKonto";
import { SohnInterests } from "./SohnInterests";
import { SohnBonus, SohnBonusRun } from "./SohnBonus";
import { SohnMischrunde } from "./SohnMischrunde";
import { DadShowcase } from "./DadShowcase";
import { Mascot } from "../components/Mascot";
import { CelebrationLayer, useCelebration } from "../components/Celebration";
import { RemarkWidget } from "../components/RemarkWidget";
import { SavedNotice } from "../components/SavedNotice";
import { StatusBanner } from "../components/StatusBanner";
import { isMuted, setMuted } from "../lib/feedback";
import { useKopierSperre } from "./kopierSperre";

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
  /** Löst eine aufmunternde Reaktion bei Fehlern (Learning Dad Facepalm + Ton) aus. */
  encourage?: ReturnType<typeof useCelebration>["encourage"];
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
  const { i18n } = useTranslation();
  const lang = isLanguage(i18n.language) ? i18n.language : "de";

  // `<html lang>` folgt der Wahl (B-85, Entscheidung 5): ein Screenreader liest englischen Text sonst mit deutscher
  // Aussprache. An der Wurzel, nicht in der Shell, damit auch der Login sein `lang` trägt (B-304). Beim Verlassen
  // der Arcade zurück auf Deutsch – das Vater-Web ist (noch) nicht übersetzt.
  useEffect(() => {
    document.documentElement.lang = lang;
    return () => { document.documentElement.lang = "de"; };
  }, [lang]);

  // Der Scope lässt die geteilten Spiel-Bausteine der Arcade-Sprache folgen (B-305); die Vater-Vorschau liegt
  // außerhalb und bleibt deutsch.
  return (
    <ArcadeScope>
      {!session || session.role !== "Student" ? <SohnLogin /> : <SohnShell childId={session.id} />}
    </ArcadeScope>
  );
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
  const { celebration, celebrate, encourage } = useCelebration();
  const { t, i18n } = useTranslation();
  // Kein Kopieren aus der Arcade, kein Einfügen hinein: sonst schreibt man das Wort ab, solange es zu sehen ist, und
  // fügt es im Gedächtnis-Schritt wieder ein. Der Login liegt bewusst außerhalb.
  // Ein gesperrtes ganzes Wort (iPhone-Wischtippen, Diktat) sagt, warum es nicht ankam – sonst verschwände es stumm.
  const wurzel = useRef<HTMLDivElement>(null);
  const [tippHinweis, setTippHinweis] = useState(false);
  const hinweisUhr = useRef<number | undefined>(undefined);
  useKopierSperre(wurzel, () => {
    setTippHinweis(true);
    window.clearTimeout(hinweisUhr.current);
    hinweisUhr.current = window.setTimeout(() => setTippHinweis(false), 3000);
  });
  useEffect(() => () => window.clearTimeout(hinweisUhr.current), []);
  const lang = isLanguage(i18n.language) ? i18n.language : "de";

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
    childId, coins, gems, refreshWallet, skin, setSkin, planId, setPlanId, streak, setStreak, celebrate, encourage,
  }), [childId, coins, gems, refreshWallet, skin, planId, setPlanId, streak, celebrate, encourage]);

  const toggleMute = useCallback(() => {
    setMutedState((m) => {
      const next = !m;
      setMuted(next);
      return next;
    });
  }, []);

  return (
    <SohnContext.Provider value={value}>
      <div className="app-sohn" ref={wurzel}>
        <div className="sohn-hud">
          <div className="avatar-mini" style={{ background: skin.gradient, overflow: "hidden" }} aria-hidden="true">
            {skin.id === "pug"
              ? <img src="/character/dad/dad_head_happy.png" alt="" width={32} height={32} style={{ objectFit: "cover" }} />
              : skin.emoji}
          </div>
          <div className="chip" aria-live="polite"><span aria-hidden="true">🪙</span><b className="tabnum" aria-label={t("hud.coins", { count: coins })}>{coins}</b></div>
          <div className="chip" aria-live="polite"><span aria-hidden="true">💎</span><b className="tabnum" aria-label={t("hud.gems", { count: gems })}>{gems}</b></div>
          <div className="chip flame" aria-live="polite"><span aria-hidden="true">🔥</span><b className="tabnum" aria-label={t("hud.streak", { count: streak })}>{streak}</b></div>
          <button
            type="button"
            className="chip mute-toggle"
            onClick={toggleMute}
            aria-pressed={muted ? "true" : "false"}
            aria-label={muted ? t("hud.soundOn") : t("hud.soundOff")}
            title={muted ? t("hud.soundOn") : t("hud.soundOff")}
          >
            {muted ? "🔇" : "🔊"}
          </button>
          {/* Der Sprachumschalter (B-294): rein client-seitig, die Wahl überlebt im `localStorage`. Der sichtbare
              Text nennt das ZIEL („EN“ auf Deutsch) und steht am Anfang des Vorlesenamens (WCAG 2.5.3, „Label in
              Name“ – Sprachsteuerung „Klick EN“ trifft ihn). Kein `aria-pressed`: eine Aktion, kein Zustand. */}
          <button
            type="button"
            className="chip mute-toggle"
            onClick={() => { void setLanguage(lang === "de" ? "en" : "de"); }}
            aria-label={t("hud.language")}
            title={t("hud.language")}
          >
            <span aria-hidden="true">🌐 </span>{t("hud.languageShort")}
          </button>
        </div>

        <CelebrationLayer celebration={celebration} />
        {/* Die Bestätigung einer Aktion, die ihren Bildschirm verlässt. Sie liegt hier und nicht in
            der Feier: die ist `aria-hidden` und fällt bei „Bewegung reduzieren" ganz weg (B-151). */}
        <SavedNotice />
        <StatusBanner message={tippHinweis ? { ok: false, text: t("hud.typeLetters") } : null}
          style={{ position: "fixed", left: 12, right: 12, bottom: 96, zIndex: 20 }} />

        <Routes>
          <Route index element={<SohnHome />} />
          <Route path="practice/:positionId" element={<SohnPractice />} />
          <Route path="test/:positionId" element={<SohnTest />} />
          <Route path="progress" element={<SohnProgress />} />
          <Route path="shop" element={<SohnShop />} />
          <Route path="skins" element={<SohnSkins />} />
          <Route path="konto" element={<SohnKonto />} />
          <Route path="ich" element={<SohnInterests />} />
          <Route path="dad" element={<DadShowcase />} />
          <Route path="bonus" element={<SohnBonus />} />
          <Route path="bonus/misch" element={<SohnMischrunde />} />
          <Route path="bonus/:exerciseId" element={<SohnBonusRun />} />
          <Route path="*" element={<Navigate to="/sohn" replace />} />
        </Routes>

        <nav className="sohn-nav">
          <NavLink to="/sohn" end><span className="ic" aria-hidden="true">🗺️</span>{t("nav.home")}</NavLink>
          <NavLink to="/sohn/progress"><span className="ic" aria-hidden="true">🏆</span>{t("nav.trophies")}</NavLink>
          <NavLink to="/sohn/konto"><span className="ic" aria-hidden="true">💰</span>{t("nav.account")}</NavLink>
          <NavLink to="/sohn/shop"><span className="ic" aria-hidden="true">🛒</span>{t("nav.shop")}</NavLink>
          <NavLink to="/sohn/skins"><span className="ic" aria-hidden="true">🎭</span>{t("nav.skins")}</NavLink>
        </nav>

        {/* Anmerkungen beim Testen – nur im Dev-Modus, im Prod-Bundle wegoptimiert. Der Abstand hält
            die Arcade-Navigation frei: Bei 12px läge das Widget darüber und finge deren Klicks ab. */}
        {import.meta.env.DEV && <RemarkWidget bottomOffset={96} />}
      </div>
    </SohnContext.Provider>
  );
}

/** Kleiner Helfer, den mehrere Screens für den HUD-Avatar nutzen. */
export function HeaderMascot({ skin, mood }: { skin: Skin; mood?: "happy" | "hyped" | "sleepy" }) {
  return <Mascot skin={skin} mood={mood} size={92} />;
}
