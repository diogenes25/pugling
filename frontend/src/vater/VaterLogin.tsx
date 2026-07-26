import { useState } from "react";
import { api, ApiError, errorMessage } from "../lib/api";
import { useAuth } from "../lib/auth";

/**
 * Der Einstieg für den Vater – bewusst mit **zwei** Modi.
 *
 * Anmelden geht über die fachliche Vater-Id (so schneidet der Server den Login), Registrieren über den
 * einen anonymen Schreibpfad der API. Beides gehört auf denselben Schirm: ohne Registrierung könnte ein
 * Vater nur per Seed entstehen, und wer sich gerade registriert hat, kennt seine Id noch nicht.
 * Deshalb loggt die Registrierung direkt ein und nennt die neue Id danach unübersehbar – sie ist der
 * Schlüssel für jede weitere Anmeldung.
 */

/** Die zuletzt benutzte Vater-Id vorbelegen; sie ist der Login-Name und wird sonst leicht vergessen. */
const LAST_ID_KEY = "pugling.lastFatherId";

export function VaterLogin() {
  const [mode, setMode] = useState<"login" | "register">("login");
  // Die Id überlebt die Registrierung: nach dem Auto-Login zeigt der Vater-Bereich sie weiter an.
  const [registeredId, setRegisteredId] = useState<number | null>(null);

  return (
    <div className="app-vater" style={{ justifyContent: "center", alignItems: "center" }}>
      <div className="card" style={{ width: 380, maxWidth: "90vw", display: "flex", flexDirection: "column", gap: 14 }}>
        <div className="brand" style={{ fontFamily: "var(--font-display)", fontSize: 22 }}>🛠️ Pugling · Vater</div>

        <div className="row" style={{ gap: 8 }} role="radiogroup" aria-label="Zugang">
          {([["login", "Anmelden"], ["register", "Neu registrieren"]] as const).map(([value, label]) => (
            <button
              key={value} type="button" className={`pill toggle-pill ${mode === value ? "lime" : ""}`}
              role="radio" aria-checked={mode === value} onClick={() => setMode(value)}
            >{label}</button>
          ))}
        </div>

        {registeredId !== null && (
          <div className="banner ok" role="status" aria-live="polite">
            Konto angelegt. Deine <strong>Vater-Id ist {registeredId}</strong> – die brauchst du bei jeder
            Anmeldung. Notiere sie.
          </div>
        )}

        {mode === "login"
          ? <LoginForm />
          : <RegisterForm onRegistered={setRegisteredId} />}
      </div>
    </div>
  );
}

function LoginForm() {
  const { signIn } = useAuth();
  const [fatherId, setFatherId] = useState(() => localStorage.getItem(LAST_ID_KEY) ?? "");
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const res = await api.loginFather(Number(fatherId), pin);
      localStorage.setItem(LAST_ID_KEY, fatherId);
      signIn(res);
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401 ? "Vater-Id oder PIN falsch." : "Login fehlgeschlagen.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form style={{ display: "flex", flexDirection: "column", gap: 14 }} onSubmit={submit}>
      <p className="sub" style={{ margin: 0 }}>Melde dich mit deiner Vater-Id und PIN an.</p>
      <div className="field">
        <label htmlFor="fid">Vater-Id</label>
        <input id="fid" name="fatherId" inputMode="numeric" autoComplete="username" value={fatherId}
          onChange={(e) => setFatherId(e.target.value.replace(/\D/g, ""))} placeholder="z.B. 1" />
      </div>
      <div className="field">
        <label htmlFor="pin">PIN</label>
        <input id="pin" name="pin" type="password" autoComplete="current-password" value={pin} onChange={(e) => setPin(e.target.value)} />
      </div>
      {error && <div className="banner err" role="alert">{error}</div>}
      <button type="submit" className="btn" disabled={busy}>{busy ? "…" : "Anmelden"}</button>
    </form>
  );
}

/**
 * Registrierung samt Auto-Login. Der zweite PIN-Eingang ist kein Zierrat: eine vertippte PIN wäre ohne
 * ihn erst beim nächsten Login bemerkt worden – und dann ohne Weg zurück, weil die PIN nur über das
 * eigene, angemeldete Konto änderbar ist.
 */
function RegisterForm({ onRegistered }: { onRegistered: (id: number) => void }) {
  const { signIn } = useAuth();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [pin, setPin] = useState("");
  const [pin2, setPin2] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!name.trim()) { setError("Bitte einen Namen eingeben."); return; }
    if (!pin.trim()) { setError("Bitte eine PIN setzen – ohne sie kommst du nicht wieder herein."); return; }
    if (pin !== pin2) { setError("Die beiden PINs stimmen nicht überein."); return; }
    setBusy(true);
    try {
      const created = await api.registerFather({ name: name.trim(), email: email.trim() || null, pin });
      onRegistered(created.id);
      localStorage.setItem(LAST_ID_KEY, String(created.id));
      // Direkt anmelden: die Registrierung liefert kein Token, und ein Zwischenschritt „jetzt einloggen"
      // wäre genau die Stelle, an der die frisch vergebene Id verloren geht.
      signIn(await api.loginFather(created.id, pin));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form style={{ display: "flex", flexDirection: "column", gap: 14 }} onSubmit={submit}>
      <p className="sub" style={{ margin: 0 }}>
        Lege dein Vater-Konto an. Du steuerst damit die Lernpläne deiner Kinder und erstellst Übungen.
      </p>
      <div className="field">
        <label htmlFor="reg-name">Name</label>
        <input id="reg-name" name="name" autoComplete="name" value={name} onChange={(e) => setName(e.target.value)} placeholder="z.B. Thomas" />
      </div>
      <div className="field">
        <label htmlFor="reg-email">E-Mail <span className="muted">(optional)</span></label>
        <input id="reg-email" name="email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
      </div>
      <div className="field">
        <label htmlFor="reg-pin">PIN</label>
        <input id="reg-pin" name="newPin" type="password" autoComplete="new-password" value={pin} onChange={(e) => setPin(e.target.value)} />
      </div>
      <div className="field">
        <label htmlFor="reg-pin2">PIN wiederholen</label>
        <input id="reg-pin2" name="newPin2" type="password" autoComplete="new-password" value={pin2} onChange={(e) => setPin2(e.target.value)} />
      </div>
      {error && <div className="banner err" role="alert">{error}</div>}
      <button type="submit" className="btn" disabled={busy}>{busy ? "…" : "Konto anlegen"}</button>
      <p className="sub" style={{ margin: 0 }}>
        Angemeldet wird danach mit der <strong>Vater-Id</strong>, die du beim Anlegen bekommst – nicht mit der E-Mail.
      </p>
    </form>
  );
}
