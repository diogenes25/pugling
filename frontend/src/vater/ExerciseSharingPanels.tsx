import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { GRANT_PERMISSIONS, grantPermissionLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { AssetThumb, MediaSearch } from "./MediaPickers";
import type { ExerciseGrant, GrantPermission, MediaLinkResponse } from "../lib/types";

/*
 * Die beiden Seiten einer Übung, die über ihren Inhalt hinausgehen.
 *
 * **Rechte** machen die geteilte Bibliothek erst benutzbar: der Katalog ist global lesbar, aber wer eine
 * fremde Übung *ändern* oder *zuweisen* darf, entscheidet ein Grant. Ohne diese Oberfläche war das Modell
 * vorhanden, aber unbedienbar – ein Lehrer konnte einem Vater nichts freigeben.
 *
 * **Titelbild** ist bewusst getrennt von der Bebilderung der Vokabeln: es schmückt die Übungskachel und
 * hat auf die Karten keinen Einfluss (dort gilt die Kaskade Item → Vokabel).
 */

/**
 * Wer darf was mit dieser Übung. Nur ein **Owner** darf Rechte vergeben – deshalb ist das Formular an
 * `isOwner` gebunden und nicht an das bloße Schreibrecht.
 */
export function GrantsSection({ exerciseId, isOwner }: { exerciseId: number; isOwner: boolean }) {
  const grants = useAsync<ExerciseGrant[]>(() => api.exerciseGrants(exerciseId), [exerciseId]);
  const [creatorId, setCreatorId] = useState("");
  const [permission, setPermission] = useState<GrantPermission>("Execute");
  const action = useAction();

  async function add(e: React.FormEvent) {
    e.preventDefault();
    const id = Number(creatorId);
    if (!id) { action.fail("Bitte die Vater-Id des Creators angeben."); return; }
    if (!await action.run(() => api.addExerciseGrant(exerciseId, id, permission), "Recht vergeben.")) return;
    // Die eingegebene Id bleibt stehen, wenn der Server ablehnt – sonst müsste sie neu getippt werden.
    setCreatorId("");
    grants.reload();
  }

  async function revoke(g: ExerciseGrant) {
    const what = grantPermissionLabel(g.permission);
    if (!confirmAction(`${g.creatorName} das Recht „${what}" entziehen?`)) return;
    if (await action.run(() => api.removeExerciseGrant(exerciseId, g.creatorId, g.permission), "Recht entzogen.")) {
      grants.reload();
    }
  }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>Rechte {grants.data ? `(${grants.data.length})` : ""}</h4>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        <strong>Lesen darf jeder</strong> – der Katalog ist gemeinsam. Hier vergibst du mehr: Zuweisen,
        Bearbeiten oder Verwalten. Gedacht für „ein Lehrer erstellt, mehrere Väter nutzen".
      </p>

      {grants.error && <div className="banner err">{grants.error}</div>}
      {grants.data && (
        <table className="table">
          <thead><tr><th>Creator</th><th>Recht</th><th>Vergeben von</th><th /></tr></thead>
          <tbody>
            {grants.data.map((g) => (
              <tr key={`${g.creatorId}-${g.permission}`}>
                <td>{g.creatorName} <span className="muted">(#{g.creatorId})</span></td>
                <td>{grantPermissionLabel(g.permission)}</td>
                <td className="muted">{g.grantedByFatherId != null ? `#${g.grantedByFatherId}` : "beim Anlegen"}</td>
                <td style={{ textAlign: "right" }}>
                  {isOwner && (
                    <button type="button" className="btn ghost small" style={{ width: "auto" }} disabled={action.busy}
                      aria-label={`${grantPermissionLabel(g.permission)} für ${g.creatorName} entziehen`}
                      onClick={() => revoke(g)}>Entziehen</button>
                  )}
                </td>
              </tr>
            ))}
            {grants.data.length === 0 && <tr><td colSpan={4} className="muted">Keine zusätzlichen Rechte vergeben.</td></tr>}
          </tbody>
        </table>
      )}

      {isOwner ? (
        <form className="row" style={{ gap: 10, alignItems: "flex-end", flexWrap: "wrap", marginTop: 10 }} onSubmit={add}>
          <div className="field" style={{ maxWidth: 160 }}>
            <label htmlFor="grant-creator">Vater-Id des Creators</label>
            <input id="grant-creator" inputMode="numeric" autoComplete="off" value={creatorId}
              onChange={(e) => setCreatorId(e.target.value.replace(/\D/g, ""))} placeholder="z. B. 4" />
          </div>
          <div className="field" style={{ maxWidth: 190 }}>
            <label htmlFor="grant-permission">Recht</label>
            <select id="grant-permission" value={permission}
              onChange={(e) => setPermission(e.target.value as GrantPermission)}>
              {GRANT_PERMISSIONS.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
            </select>
          </div>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>Vergeben</button>
          <span className="sub" style={{ flex: 1, minWidth: 200 }}>
            {GRANT_PERMISSIONS.find((p) => p.value === permission)?.hint}
          </span>
        </form>
      ) : (
        <p className="sub">Rechte vergeben darf nur, wer die Übung <strong>verwaltet</strong> (Owner).</p>
      )}
      <StatusBanner message={action.message} />
    </section>
  );
}

/**
 * Das Titelbild der Übung – Schmuck der Kachel, **nicht** Teil der Karten. Für die Bebilderung eines
 * Wortes gilt weiter die Kaskade Item → Vokabel; hier hineinzumischen würde beides verwischen.
 */
export function ExerciseCoverSection({ exerciseId, canWrite }: { exerciseId: number; canWrite: boolean }) {
  const linked = useAsync<MediaLinkResponse[]>(() => api.exerciseMedia(exerciseId), [exerciseId]);
  const action = useAction();

  async function link(assetId: number) {
    if (await action.run(() => api.linkExerciseMedia(exerciseId, assetId))) linked.reload();
  }

  async function unlink(linkId: number) {
    if (await action.run(() => api.unlinkExerciseMedia(exerciseId, linkId))) linked.reload();
  }

  return (
    <section>
      <h4 className="h-section" style={{ fontSize: 16 }}>Titelbild</h4>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        Schmückt die Übungskachel. Welches Bild dein Kind <em>zu einem Wort</em> sieht, entscheidet weiterhin
        die Vokabel (Reiter <strong>Vokabeln</strong>) bzw. eine übungslokale Übersteuerung.
      </p>

      {linked.error && <div className="banner err">{linked.error}</div>}
      {linked.data && (linked.data.length === 0
        ? <p className="muted">Kein Titelbild.</p>
        : (
          <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
            {linked.data.map((l) => (
              <AssetThumb key={l.id} asset={l.asset}
                action={canWrite
                  ? { label: "Entfernen", disabled: action.busy, onClick: () => unlink(l.id) }
                  : undefined} />
            ))}
          </div>
        ))}

      {canWrite && <MediaSearch busy={action.busy} linkedIds={new Set((linked.data ?? []).map((l) => l.asset.id))}
        onPick={link} />}
      <StatusBanner message={action.message} />
    </section>
  );
}
