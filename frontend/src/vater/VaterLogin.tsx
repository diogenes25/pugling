import { useState } from "react";
import { api, ApiError } from "../lib/api";
import { useAuth } from "../lib/auth";

export function VaterLogin() {
  const { signIn } = useAuth();
  const [fatherId, setFatherId] = useState("");
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const res = await api.loginFather(Number(fatherId), pin);
      signIn(res);
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401 ? "Vater-Id oder PIN falsch." : "Login fehlgeschlagen.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="app-vater" style={{ justifyContent: "center", alignItems: "center" }}>
      <form className="card" style={{ width: 360, maxWidth: "90vw", display: "flex", flexDirection: "column", gap: 14 }} onSubmit={submit}>
        <div className="brand" style={{ fontFamily: "var(--font-display)", fontSize: 22 }}>🛠️ Pugling · Vater</div>
        <p className="sub">Melde dich mit deiner Vater-Id und PIN an.</p>
        <div className="field">
          <label htmlFor="fid">Vater-Id</label>
          <input id="fid" inputMode="numeric" value={fatherId} onChange={(e) => setFatherId(e.target.value.replace(/\D/g, ""))} placeholder="z.B. 1" />
        </div>
        <div className="field">
          <label htmlFor="pin">PIN</label>
          <input id="pin" type="password" value={pin} onChange={(e) => setPin(e.target.value)} />
        </div>
        {error && <div className="banner err">{error}</div>}
        <button type="submit" className="btn" disabled={busy}>{busy ? "…" : "Anmelden"}</button>
      </form>
    </div>
  );
}
