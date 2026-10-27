import { useState } from "react";
import { useTranslation } from "react-i18next";
import { api, ApiError } from "../lib/api";
import { useAuth } from "../lib/auth";
import { SKINS } from "../lib/skins";
import { Mascot } from "../components/Mascot";
import { useSkinText } from "../lib/skinText";

const LAST_ID = "pugling.lastChildId";

export function SohnLogin() {
  const { signIn } = useAuth();
  // Der Login folgt der gespeicherten Sprache (B-304); umschalten kann erst das HUD der angemeldeten Arcade.
  const { t } = useTranslation();
  const skinText = useSkinText();
  const [childId, setChildId] = useState<string>(() => localStorage.getItem(LAST_ID) ?? "");
  const [pin, setPin] = useState("");
  const [heroIdx, setHeroIdx] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const key = (d: string) => setPin((p) => (p.length >= 6 ? p : p + d));
  const del = () => setPin((p) => p.slice(0, -1));

  async function submit() {
    setError(null);
    const id = Number(childId);
    if (!id) { setError(t("login.errNoId")); return; }
    if (pin.length < 1) { setError(t("login.errNoPin")); return; }
    setBusy(true);
    try {
      const res = await api.loginChild(id, pin);
      localStorage.setItem(LAST_ID, String(id));
      if (!signIn(res)) setError(t("login.errWrongAccount"));
    } catch (e) {
      setError(e instanceof ApiError && e.status === 401 ? t("login.errWrongPin") : t("login.errFailed"));
      setPin("");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="app-sohn">
      <div className="center-col">
        <div style={{ textAlign: "center" }}>
          <div className="pill gold" style={{ display: "inline-block" }}>Pugling</div>
          <h1 className="screen-title" style={{ fontSize: 30, marginTop: 8 }}>{t("login.title")}</h1>
        </div>

        <Mascot skin={SKINS[heroIdx]} mood="happy" size={110} />

        <div>
          <div className="sub" style={{ textAlign: "center", marginBottom: 6 }}>{t("login.chooseHero")}</div>
          <div className="heroes">
            {SKINS.slice(0, 3).map((s, i) => (
              <button
                type="button"
                key={s.id}
                className={`hero${i === heroIdx ? " sel" : ""}`}
                onClick={() => setHeroIdx(i)}
                aria-label={skinText.name(s)}
              >
                {s.emoji}
              </button>
            ))}
          </div>
        </div>

        <div className="field">
          <label htmlFor="childId">{t("login.heroNumber")}</label>
          <input
            id="childId" name="childId" inputMode="numeric" autoComplete="username" value={childId}
            onChange={(e) => setChildId(e.target.value.replace(/\D/g, ""))}
            placeholder={t("login.heroNumberPlaceholder")}
          />
        </div>

        <div className="dots">
          {[0, 1, 2, 3].map((i) => <i key={i} className={i < pin.length ? "f" : ""} />)}
        </div>

        {error && <div className="error-box" style={{ padding: 0 }} role="alert">{error}</div>}

        <div className="keys">
          {["1", "2", "3", "4", "5", "6", "7", "8", "9"].map((d) => (
            <button type="button" key={d} onClick={() => key(d)}>{d}</button>
          ))}
          <button type="button" className="ghost" onClick={del} style={{ boxShadow: "0 4px 0 #0b0d2c" }} aria-label={t("login.deleteDigit")}>✕</button>
          <button type="button" onClick={() => key("0")}>0</button>
          <button type="button" onClick={submit} disabled={busy} style={{ color: "var(--lime)" }} aria-label={t("login.submit")}>⏎</button>
        </div>

        <button type="button" className="btn gold" onClick={submit} disabled={busy}>
          {busy ? "…" : t("login.go")}
        </button>
      </div>
    </div>
  );
}
