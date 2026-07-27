import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
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
  const action = useAction();

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) { action.fail("Der Name darf nicht leer sein."); return; }
    if (pin !== pin2) { action.fail("Die beiden PINs stimmen nicht überein."); return; }

    // Nur Geändertes schicken – ein leeres PIN-Feld heißt „PIN unverändert", nicht „PIN löschen".
    const dto: UpdateFatherDto = {};
    if (name.trim() !== father.name) dto.name = name.trim();
    if (email.trim() !== (father.email ?? "")) dto.email = email.trim() || null;
    if (pin.trim()) dto.pin = pin;
    if (Object.keys(dto).length === 0) { action.succeed("Nichts zu speichern."); return; }

    const ok = await action.run(() => api.updateFather(father.id, dto),
      dto.pin ? "Gespeichert. Die neue PIN gilt ab der nächsten Anmeldung." : "Gespeichert.");
    if (!ok) return;
    setPin(""); setPin2("");
    onSaved();
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
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>
          {action.busy ? "Speichere…" : "Speichern"}
        </button>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </section>
  );
}
