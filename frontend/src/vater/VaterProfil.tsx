import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { FatherResponse, UpdateFatherDto } from "../lib/types";

/**
 * Das eigene Konto. Zwei Dinge stehen hier, die sonst nirgends erreichbar wären:
 *
 * 1. **Die Vater-Id.** Sie ist der Login-Name; wer sie vergisst, kommt nicht mehr herein.
 * 2. **Die PIN.** Sie lässt sich nur vom angemeldeten Konto aus ändern – es gibt bewusst keinen
 *    Zurücksetzen-Weg über E-Mail, weil die App keine Mails verschickt.
 */
export function VaterProfil() {
  const { session, signOut } = useAuth();
  const fatherId = session!.id;
  const me = useAsync<FatherResponse>(() => api.father(fatherId), [fatherId]);

  return (
    <>
      <h2 className="h-section">Mein Konto</h2>

      {me.loading ? <div className="loading">Lade…</div>
        : me.error ? <div className="banner err">{me.error}</div>
        : me.data && (
          <>
            <section className="vater-grid">
              <div className="card">
                <div className="muted">Vater-Id <span className="muted">(dein Login-Name)</span></div>
                <div className="h-section">#{me.data.id}</div>
              </div>
              <div className="card"><div className="muted">Betreute Kinder</div><div className="h-section">{me.data.childrenCount}</div></div>
              <div className="card">
                <div className="muted">Konto seit</div>
                <div className="h-section" style={{ fontSize: 20 }}>{new Date(me.data.createdAt).toLocaleDateString()}</div>
              </div>
            </section>

            <ProfileForm father={me.data} onSaved={me.reload} />
          </>
        )}

      <section>
        <h3 className="h-section">Abmelden</h3>
        <p className="muted">Beendet die Sitzung auf diesem Gerät. Dein Kind bleibt in seiner App angemeldet.</p>
        <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={signOut}>Abmelden</button>
      </section>
    </>
  );
}

function ProfileForm({ father, onSaved }: { father: FatherResponse; onSaved: () => void }) {
  const [name, setName] = useState(father.name);
  const [email, setEmail] = useState(father.email ?? "");
  const [pin, setPin] = useState("");
  const [pin2, setPin2] = useState("");
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setMsg(null);
    if (!name.trim()) { setMsg({ ok: false, text: "Der Name darf nicht leer sein." }); return; }
    if (pin !== pin2) { setMsg({ ok: false, text: "Die beiden PINs stimmen nicht überein." }); return; }

    // Nur Geändertes schicken – ein leeres PIN-Feld heißt „PIN unverändert", nicht „PIN löschen".
    const dto: UpdateFatherDto = {};
    if (name.trim() !== father.name) dto.name = name.trim();
    if (email.trim() !== (father.email ?? "")) dto.email = email.trim() || null;
    if (pin.trim()) dto.pin = pin;
    if (Object.keys(dto).length === 0) { setMsg({ ok: true, text: "Nichts zu speichern." }); return; }

    setBusy(true);
    try {
      await api.updateFather(father.id, dto);
      setPin(""); setPin2("");
      setMsg({ ok: true, text: dto.pin ? "Gespeichert. Die neue PIN gilt ab der nächsten Anmeldung." : "Gespeichert." });
      onSaved();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <section>
      <h3 className="h-section">Stammdaten</h3>
      <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
        <div className="form-grid" style={{ alignItems: "end" }}>
          <div className="field"><label htmlFor="prof-name">Name</label>
            <input id="prof-name" value={name} onChange={(e) => setName(e.target.value)} /></div>
          <div className="field"><label htmlFor="prof-email">E-Mail <span className="muted">(optional)</span></label>
            <input id="prof-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} /></div>
          <div className="field"><label htmlFor="prof-pin">Neue PIN <span className="muted">(leer = unverändert)</span></label>
            <input id="prof-pin" type="password" autoComplete="new-password" value={pin} onChange={(e) => setPin(e.target.value)} /></div>
          <div className="field"><label htmlFor="prof-pin2">Neue PIN wiederholen</label>
            <input id="prof-pin2" type="password" autoComplete="new-password" value={pin2} onChange={(e) => setPin2(e.target.value)} /></div>
        </div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Speichern"}</button>
      </form>
      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }} role="status" aria-live="polite">{msg.text}</div>}
    </section>
  );
}
