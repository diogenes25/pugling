import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { CategoryResponse, ChapterResponse, SubjectResponse } from "../lib/types";

/*
 * Katalog-Verwaltung: Fach, Kapitel und „Art" anlegen, umbenennen und löschen.
 *
 * Korrigieren ging lange nicht – ein Tippfehler im Fachnamen blieb für alle sichtbar, denn der Katalog ist
 * **global**: Fächer und Kapitel teilen sich alle Väter. Genau deshalb warnt das Löschen hier deutlich und
 * nennt die Kaskade.
 *
 * Die „Art" (Kategorie) ist fachabhängig und dient der Vorfilterung im Katalog – sie ist der einzige
 * Ordnungsbegriff, den der Vater selbst erfinden darf.
 *
 * Der Bereich lag früher **eingeklappt** auf der Übungen-Seite und war darum kaum zu finden; er hat jetzt
 * eine eigene Route (`/vater/katalog`, siehe docs/vater-informationsarchitektur-plan.md). Ein eigener Ort
 * muss sich nicht aufklappen – der Einklapper und sein „Schließen" sind entfallen. Weil das Anlegen von
 * Fach und Kapitel bisher nur am Anlege-Formular hing, steht es jetzt **auch hier**: sonst wäre der
 * Katalog eine Seite, auf der man nur umbenennen und löschen kann.
 */
export function CatalogAdmin({ subjects, onCatalogChanged }: {
  subjects: SubjectResponse[];
  /** Wird nach **jeder** Änderung gerufen – die Auswahllisten der Seite zeigen sonst gelöschte Kapitel weiter. */
  onCatalogChanged: () => void;
}) {
  const [subjectId, setSubjectId] = useState<number | "">("");
  const action = useAction();

  const subject = subjects.find((s) => s.id === subjectId);
  const chapters = useAsync<ChapterResponse[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.chapters(Number(subjectId))), [subjectId]);
  const categories = useAsync<CategoryResponse[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.categories(Number(subjectId))), [subjectId]);

  /** Mutation ausführen und danach die betroffene Liste **und** die Seite drumherum auffrischen. */
  async function act(fn: () => Promise<unknown>, okText: string, reload?: () => void) {
    if (!await action.run(fn, okText)) return;
    reload?.();
    // Auch die Seite drumherum: ein hier gelöschtes Kapitel stünde sonst weiter im Kapitel-Pulldown.
    onCatalogChanged();
  }

  /** Neues Fach anlegen und **gleich auswählen** – Kapitel und Arten hängen daran, der Weg geht weiter. */
  async function createSubject(name: string) {
    const created = await action.runFor(() => api.createSubject(name), "Fach angelegt.");
    if (!created) return;
    setSubjectId(created.id);
    onCatalogChanged();
  }

  return (
    <section className="card">
      <h3 style={{ marginTop: 0 }}>Fächer</h3>
      <p className="muted" style={{ fontSize: 13 }}>
        Fächer und Kapitel sind <strong>gemeinsamer Katalog</strong> – deine Änderungen sehen alle Väter.
      </p>

      <div className="field" style={{ maxWidth: 280, marginTop: 8 }}>
        <label htmlFor="ca-subject">Fach</label>
        <select id="ca-subject" value={subjectId}
          onChange={(e) => setSubjectId(e.target.value === "" ? "" : Number(e.target.value))}>
          <option value="">– wählen –</option>
          {subjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
      </div>
      <NewName fieldId="ca-new-subject" label="Neues Fach" placeholder="z. B. Französisch"
        busy={action.busy} onCreate={createSubject} />

      {subject && (
        <>
          {/* `key` ist Pflicht, nicht Kosmetik: `NameRow` hält den Namen in eigenem `useState` (Startwert
              einmalig). Ohne Schlüssel gilt die Zeile beim Fachwechsel als *dieselbe*, behält den alten
              Namen im Feld – und „OK" schriebe den Namen des vorigen Fachs in den geteilten Katalog. */}
          <NameRow key={subject.id} busy={action.busy}
            fieldId={`ca-subject-${subject.id}`} label="Fach umbenennen"
            srName={`Fach „${subject.name}"`} value={subject.name}
            onSave={(name) => act(() => api.updateSubject(subject.id, name), "Fach umbenannt.")}
            onDelete={() => {
              if (!confirmAction(
                `Fach „${subject.name}" wirklich löschen? Alle ${subject.chaptersCount} Kapitel und deren `
                + "Übungen gehen mit. Übungen, die in einem Lehrplan stecken, verhindern das Löschen.")) return;
              act(() => api.deleteSubject(subject.id), "Fach gelöscht.", () => setSubjectId(""));
            }} />

          <h4 className="h-section" style={{ fontSize: 15, marginTop: 14 }}>
            Kapitel {chapters.data ? `(${chapters.data.length})` : ""}
          </h4>
          {chapters.data?.map((c) => (
            <NameRow key={c.id} busy={action.busy} fieldId={`ca-chapter-${c.id}`} label={`Kapitel #${c.orderIndex}`}
              srName={`Kapitel „${c.name}"`} value={c.name}
              onSave={(name) => act(() => api.updateChapter(subject.id, c.id, { name }), "Kapitel umbenannt.", chapters.reload)}
              onDelete={() => {
                if (!confirmAction(`Kapitel „${c.name}" samt seinen Übungen löschen? `
                  + "Übungen, die in einem Lehrplan stecken, verhindern das Löschen.")) return;
                act(() => api.deleteChapter(subject.id, c.id), "Kapitel gelöscht.", chapters.reload);
              }} />
          ))}
          {chapters.data?.length === 0 && <p className="muted">Noch keine Kapitel.</p>}
          {/* `orderIndex` ans Ende: das Kapitel folgt dem Stoff, und die Reihenfolge trägt Bedeutung. */}
          <NewName fieldId="ca-new-chapter" label="Neues Kapitel" placeholder="z. B. Unit 1"
            busy={action.busy} onCreate={(name) => act(
              () => api.createChapter(subject.id, name, (chapters.data?.length ?? 0) + 1),
              "Kapitel angelegt.", chapters.reload)} />

          <h4 className="h-section" style={{ fontSize: 15, marginTop: 14 }}>
            Arten {categories.data ? `(${categories.data.length})` : ""}
          </h4>
          <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
            Freie Ordnungsbegriffe innerhalb des Fachs (z. B. „Grammatik", „Vokabeln") – sie filtern die
            Übungssuche beim Planbau.
          </p>
          {categories.data?.map((c) => (
            <NameRow key={c.id} busy={action.busy} fieldId={`ca-category-${c.id}`} label="Art"
              srName={`Art „${c.name}"`} value={c.name}
              onSave={(name) => act(() => api.updateCategory(subject.id, c.id, name), "Art umbenannt.", categories.reload)}
              onDelete={() => {
                if (!confirmAction(`Art „${c.name}" löschen? Übungen behalten ihren Inhalt, verlieren aber die Zuordnung.`)) return;
                act(() => api.deleteCategory(subject.id, c.id), "Art gelöscht.", categories.reload);
              }} />
          ))}
          <NewName fieldId="ca-new-category" label="Neue Art" placeholder="z. B. Grammatik"
            busy={action.busy} onCreate={(name) => act(() => api.createCategory(subject.id, name), "Art angelegt.", categories.reload)} />
        </>
      )}

      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </section>
  );
}

/**
 * Eine Zeile „Name ändern / löschen". Der OK-Knopf erscheint erst bei echter Änderung.
 *
 * `fieldId` kommt von außen und ist aus der **Datensatz-Id** gebaut, nicht aus dem Namen: ein Name enthält
 * Leerzeichen (in einer DOM-`id` unzulässig) und ist nicht eindeutig – zwei gleichnamige Kapitel hätten
 * sonst dieselbe `id`, und das `label` zeigte auf das falsche Feld. `srName` benennt für Screenreader und
 * Sprachsteuerung, *welche* Zeile ein Knopf betrifft; „OK" allein sagt bei zehn Zeilen nichts.
 */
function NameRow({ fieldId, label, srName, value, busy, onSave, onDelete }: {
  fieldId: string; label: string; srName: string; value: string;
  /** Läuft schon eine Mutation? Dann sperren – ein zweiter Klick auf „Löschen" landete als 404. */
  busy: boolean;
  onSave: (name: string) => void; onDelete: () => void;
}) {
  const [name, setName] = useState(value);
  const dirty = name.trim() !== "" && name.trim() !== value;
  return (
    <div className="row" style={{ gap: 6, alignItems: "flex-end", marginTop: 6, flexWrap: "wrap" }}>
      <div className="field" style={{ flex: 1, minWidth: 200 }}>
        <label htmlFor={fieldId}>{label}</label>
        <input id={fieldId} value={name} onChange={(e) => setName(e.target.value)} />
      </div>
      {dirty && <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}
        aria-label={`${srName} speichern`} onClick={() => onSave(name.trim())}>OK</button>}
      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
        aria-label={`${srName} löschen`} onClick={onDelete}>Löschen</button>
    </div>
  );
}

/**
 * Einen neuen Katalog-Eintrag anlegen. Eigenes `<form>`, damit die Eingabetaste ihn abschickt.
 *
 * `fieldId` kommt von außen, weil es diese Zeile inzwischen **dreimal** auf der Seite gibt (Fach, Kapitel,
 * Art): eine feste DOM-`id` wäre dreifach vergeben, und jedes `label` zeigte auf dasselbe – das erste – Feld.
 */
function NewName({ fieldId, label, placeholder, busy, onCreate }: {
  fieldId: string; label: string; placeholder: string;
  /** Läuft schon eine Mutation? Dann sperren – wie in `NameRow`, hier fehlte es. */
  busy: boolean;
  onCreate: (name: string) => void;
}) {
  const [name, setName] = useState("");
  return (
    <form className="row" style={{ gap: 6, alignItems: "flex-end", marginTop: 8 }}
      onSubmit={(e) => { e.preventDefault(); if (name.trim()) { onCreate(name.trim()); setName(""); } }}>
      <div className="field" style={{ flex: 1, minWidth: 200 }}>
        <label htmlFor={fieldId}>{label}</label>
        <input id={fieldId} value={name} onChange={(e) => setName(e.target.value)} placeholder={placeholder} />
      </div>
      {/* „Anlegen" steht dreimal auf der Seite (Fach, Kapitel, Art). Der sichtbare Text bleibt kurz, der
          zugängliche Name nennt die Sache – sonst sagt Sprachsteuerung „Anlegen" und trifft irgendeinen. */}
      <button type="submit" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
        aria-label={`${label} anlegen`}>Anlegen</button>
    </form>
  );
}
