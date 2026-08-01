import { useState } from "react";
import { api, ApiError, errorMessage } from "../lib/api";
import { useAuth } from "../lib/auth";

/**
 * Der Einstieg für Erwachsene – bewusst mit **zwei** Modi, und die Registrierung mit **zwei Konto-Arten**:
 * Vater (betreut und erstellt) oder Lehrer (erstellt nur). Der Unterschied sind die Rollen des Kontos, und
 * die entstehen beim Anlegen – darum ist es eine Wahl hier und keine Einstellung später.
 *
 * Anmelden geht über die fachliche Id des Erwachsenen (so schneidet der Server den Login), Registrieren über den
 * einen anonymen Schreibpfad der API. Beides gehört auf denselben Schirm: ohne Registrierung könnte ein
 * Vater nur per Seed entstehen, und wer sich gerade registriert hat, kennt seine Id noch nicht.
 * Deshalb loggt die Registrierung direkt ein und nennt die neue Id danach unübersehbar – sie ist der
 * Schlüssel für jede weitere Anmeldung.
 */

/** Die zuletzt benutzte Id vorbelegen; sie ist der Login-Name und wird sonst leicht vergessen. */
const LAST_ID_KEY = "pugling.lastLoginId";
/**
 * Der frühere Schlüssel. Er hält **Nutzerdaten** – wer ihn beim Umbenennen fallen lässt, nimmt jedem die
 * vorbelegte Id. Darum einmalig als Rückfall lesen; die eine Zeile darf bleiben.
 */
const LEGACY_LAST_ID_KEY = "pugling.lastFatherId";

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
            Konto angelegt. Deine <strong>Id ist {registeredId}</strong> – die brauchst du bei jeder
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
  const [adultId, setAdultId] = useState(
    () => localStorage.getItem(LAST_ID_KEY) ?? localStorage.getItem(LEGACY_LAST_ID_KEY) ?? "");
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const res = await api.loginAdult(Number(adultId), pin);
      localStorage.setItem(LAST_ID_KEY, adultId);
      // Unbekannte Rolle: PIN war richtig, aber diese App hat kein Zuhause dafür. Ohne Meldung stünde der
      // Vater vor einem Formular, das auf den richtigen Knopfdruck nicht reagiert.
      if (!signIn(res)) setError("Dieses Konto hat eine Rolle, die diese App nicht kennt.");
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401 ? "Id oder PIN falsch." : "Login fehlgeschlagen.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form style={{ display: "flex", flexDirection: "column", gap: 14 }} onSubmit={submit}>
      <p className="sub" style={{ margin: 0 }}>Melde dich mit deiner Id und PIN an.</p>
      <div className="field">
        {/* „Deine Id" statt „Vater-Id": dasselbe Formular meldet auch ein Lehrer-Konto an. */}
        <label htmlFor="fid">Deine Id</label>
        <input id="fid" name="adultId" inputMode="numeric" autoComplete="username" value={adultId}
          onChange={(e) => setAdultId(e.target.value.replace(/\D/g, ""))} placeholder="z.B. 1" />
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
  /*
   * Zwei Arten Konto, und der Unterschied ist keine Einstellung, die man später umstellt: er steckt in den
   * **Rollen**, und die entstehen beim Anlegen. Ein Vater-Konto trägt Creator + Supervisor, ein Lehrer-Konto
   * nur Creator – ihm fehlt damit der Betreuungsauftrag und alles, was daran hängt.
   */
  const [kind, setKind] = useState<"father" | "teacher">("father");
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
      const dto = { name: name.trim(), email: email.trim() || null, pin };
      // Beide Wege liefern eine fachliche Id, die zugleich der Login-Name ist.
      const id = kind === "teacher"
        ? (await api.registerTeacher(dto)).creatorId
        : (await api.registerAdult(dto)).id;
      onRegistered(id);
      localStorage.setItem(LAST_ID_KEY, String(id));
      // Direkt anmelden: die Registrierung liefert kein Token, und ein Zwischenschritt „jetzt einloggen"
      // wäre genau die Stelle, an der die frisch vergebene Id verloren geht.
      if (!signIn(await api.loginAdult(id, pin)))
        setError("Angelegt, aber die Anmeldung liefert eine unbekannte Rolle – bitte neu anmelden.");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form style={{ display: "flex", flexDirection: "column", gap: 14 }} onSubmit={submit}>
      <div className="row" style={{ gap: 8 }} role="radiogroup" aria-label="Art des Kontos">
        {([["father", "👤 Vater"], ["teacher", "🎓 Lehrer"]] as const).map(([value, label]) => (
          <button
            key={value} type="button" className={`pill toggle-pill ${kind === value ? "lime" : ""}`}
            role="radio" aria-checked={kind === value} onClick={() => setKind(value)}
          >{label}</button>
        ))}
      </div>
      <p className="sub" style={{ margin: 0 }}>
        {kind === "father"
          ? "Lege dein Vater-Konto an. Du steuerst damit die Lernpläne deiner Kinder und erstellst Übungen."
          : "Lege dein Lehrer-Konto an: du erstellst Übungen und Material für andere. Kinder betreuen und "
            + "Lehrpläne zuweisen tun die Eltern – dafür brauchst du kein Konto."}
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
        Angemeldet wird danach mit der <strong>Id</strong>, die du beim Anlegen bekommst – nicht mit der E-Mail.
      </p>
    </form>
  );
}
