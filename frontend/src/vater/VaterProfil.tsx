import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { useAuth } from "../lib/auth";
import type { AdultResponse, TeacherAccount, UpdateMyAccountDto } from "../lib/types";

/**
 * Das eigene Konto – für **beide** Erwachsenen-Arten. Zwei Dinge stehen hier, die sonst nirgends
 * erreichbar wären:
 *
 * 1. **Die eigene Id.** Sie ist der Login-Name; wer sie vergisst, kommt nicht mehr herein.
 * 2. **Die PIN.** Sie lässt sich nur vom angemeldeten Konto aus ändern – es gibt bewusst keinen
 *    Zurücksetzen-Weg über E-Mail, weil die App keine Mails verschickt.
 *
 * Geschrieben wird über `PATCH auth/me` (Selbstverwaltung), nicht über den Vater-Endpunkt: der ist
 * Supervisor-gegated und für ein **Lehrer-Konto** verschlossen. Zusätzlich hält der eine Weg Konto- und
 * fachlichen Namen zusammen – der Vater-PATCH spiegelte nur die PIN, sodass die beiden Namen driften konnten.
 */
export function VaterProfil() {
  const { session, signOut } = useAuth();
  const isTeacher = session!.role === "Creator";

  // Zwei Quellen, weil zwei Rollen: der Vater-Endpunkt kennt betreute Kinder und das Anlege-Datum, das
  // Lehrer-Konto seine Rollen. Für einen Lehrer wäre der Vater-Endpunkt ein 403.
  const adult = useAsync<AdultResponse | null>(
    () => (isTeacher ? Promise.resolve(null) : api.adult(session!.id)), [session!.id, isTeacher]);
  const teacher = useAsync<TeacherAccount | null>(
    () => (isTeacher ? api.teacherAccount(session!.id) : Promise.resolve(null)), [session!.id, isTeacher]);

  const loading = isTeacher ? teacher.loading : adult.loading;
  const error = isTeacher ? teacher.error : adult.error;
  /*
   * Ob überhaupt schon Daten da sind – und nur dann darf der Platzhalter greifen. `onSaved` frischt den
   * Datensatz auf, und ein „Lade…" an dieser Stelle hängt das Formular aus: es verliert die Bestätigung,
   * die der Nutzer gerade lesen soll, und die eingegebenen Werte. Dieselbe Falle wie in VaterKatalog und
   * VaterExercises (siehe frontend/CLAUDE.md) – `useAsync` behält `data` über ein `reload`.
   */
  const hasData = (isTeacher ? teacher.data : adult.data) !== null;
  const name = (isTeacher ? teacher.data?.name : adult.data?.name) ?? "";
  const email = (isTeacher ? teacher.data?.email : adult.data?.email) ?? null;
  const reload = () => { if (isTeacher) teacher.reload(); else adult.reload(); };

  return (
    <>
      <h2 className="h-section">Mein Konto</h2>
      <p className="sub">
        {isTeacher
          ? "Ein Lehrer-Konto: du erstellst Inhalte. Kinder betreuen und Lehrpläne zuweisen tun die Eltern."
          : "Ein Vater-Konto: du betreust deine Kinder und erstellst Inhalte."}
      </p>

      {loading && !hasData ? <div className="loading">Lade…</div>
        : error ? <div className="banner err">{error}</div>
        : (
          <>
            <section className="vater-grid">
              <div className="card">
                <div className="muted">{isTeacher ? "Lehrer-Id" : "Vater-Id"} <span className="muted">(dein Login-Name)</span></div>
                <div className="h-section">#{session!.id}</div>
              </div>
              {isTeacher
                ? (
                  <div className="card">
                    <div className="muted">Rollen</div>
                    <div className="h-section" style={{ fontSize: 20 }}>{teacher.data?.roles.join(", ")}</div>
                  </div>
                )
                : (
                  <>
                    <div className="card"><div className="muted">Betreute Kinder</div>
                      <div className="h-section">{adult.data?.childrenCount}</div></div>
                    <div className="card">
                      <div className="muted">Konto seit</div>
                      <div className="h-section" style={{ fontSize: 20 }}>
                        {adult.data && new Date(adult.data.createdAt).toLocaleDateString()}
                      </div>
                    </div>
                  </>
                )}
            </section>

            <ProfileForm name={name} email={email} onSaved={reload} />
          </>
        )}

      <section>
        <h3 className="h-section">Abmelden</h3>
        <p className="muted">
          Beendet die Sitzung auf diesem Gerät.{isTeacher ? "" : " Dein Kind bleibt in seiner App angemeldet."}
        </p>
        <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={signOut}>Abmelden</button>
      </section>
    </>
  );
}

function ProfileForm({ name: initialName, email: initialEmail, onSaved }: {
  name: string; email: string | null; onSaved: () => void;
}) {
  const [name, setName] = useState(initialName);
  const [email, setEmail] = useState(initialEmail ?? "");
  const [pin, setPin] = useState("");
  const [pin2, setPin2] = useState("");
  const action = useAction();

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) { action.fail("Der Name darf nicht leer sein."); return; }
    if (pin !== pin2) { action.fail("Die beiden PINs stimmen nicht überein."); return; }

    // Nur Geändertes schicken – ein leeres PIN-Feld heißt „PIN unverändert", nicht „PIN löschen".
    const dto: UpdateMyAccountDto = {};
    if (name.trim() !== initialName) dto.name = name.trim();
    if (email.trim() !== (initialEmail ?? "")) {
      // Geleertes Feld heißt hier tatsächlich „löschen" – dafür braucht die API den ausdrücklichen Schalter,
      // weil `null` dort „nicht angegeben" bedeutet.
      if (email.trim()) dto.email = email.trim(); else dto.clearEmail = true;
    }
    if (pin.trim()) dto.pin = pin;
    if (Object.keys(dto).length === 0) { action.succeed("Nichts zu speichern."); return; }

    const ok = await action.run(() => api.updateMyAccount(dto),
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
