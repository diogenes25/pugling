import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { api } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import type { CategoryResponse, SubjectResponse } from "../lib/types";

/*
 * Katalog-Verwaltung: Fach und „Art" anlegen, umbenennen und löschen.
 *
 * Lesen und verwenden darf jeder Creator jedes Fach; **umbenennen und löschen nur, wer es angelegt hat**
 * (B-13). Ein Fach aus dem Grundbestand hat keinen Eigentümer und ist damit für *niemanden* änderbar.
 * Das Löschen warnt trotzdem deutlich – nicht wegen der Sichtbarkeit, sondern wegen der Reichweite: fünf
 * Zuordnungen verlieren ihren Bezug (B-144).
 *
 * Die „Art" (Kategorie) ist fachabhängig und dient der Vorfilterung im Katalog – sie ist der einzige
 * Ordnungsbegriff, den der Vater selbst erfinden darf.
 *
 * Seit B-106 hängt jede Übung an einer Lehrwerk-Unit statt an einem Kapitel (entfernt) – die Reihen/Units
 * werden auf der eigenen Seite `/vater/lehrwerke` verwaltet (VaterLehrwerke.tsx), nicht mehr hier.
 *
 * Der Bereich lag früher **eingeklappt** auf der Übungen-Seite und war darum kaum zu finden; er hat jetzt
 * eine eigene Route (`/vater/katalog`, siehe docs/vater-informationsarchitektur-plan.md). Ein eigener Ort
 * muss sich nicht aufklappen – der Einklapper und sein „Schließen" sind entfallen.
 */
export function CatalogAdmin({ subjects, onCatalogChanged }: {
  subjects: SubjectResponse[];
  /** Wird nach **jeder** Änderung gerufen – die Auswahllisten der Seite zeigen sonst gelöschte Fächer weiter. */
  onCatalogChanged: () => void;
}) {
  const [subjectId, setSubjectId] = useState<number | "">("");
  const action = useAction();

  const subject = subjects.find((s) => s.id === subjectId);
  const categories = useAsync<CategoryResponse[]>(
    () => (subjectId === "" ? Promise.resolve([]) : api.categories(Number(subjectId))), [subjectId]);

  /**
   * Mutation ausführen und danach die betroffene Liste **und** die Seite drumherum auffrischen. Liefert,
   * ob es geklappt hat – `NewName` leert sein Feld nur dann (wie `TagAdder.onAdd`).
   */
  async function act(fn: () => Promise<unknown>, okText: string, reload?: () => void) {
    const ok = await action.run(fn, okText);
    if (!ok) return false;
    reload?.();
    onCatalogChanged();
    return true;
  }

  /** Neues Fach anlegen und **gleich auswählen** – Kapitel und Arten hängen daran, der Weg geht weiter. */
  async function createSubject(name: string) {
    const created = await action.runFor(() => api.createSubject(name), "Fach angelegt.");
    if (!created) return false;
    setSubjectId(created.id);
    onCatalogChanged();
    return true;
  }

  return (
    <section className="card">
      <h3 style={{ marginTop: 0 }}>Fächer</h3>
      <p className="muted" style={{ fontSize: 13 }}>
        Die Fächer sind <strong>gemeinsamer Katalog</strong> – verwenden darf sie jeder,
        umbenennen und löschen nur, wer ein Fach angelegt hat.
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
          {/* `key` ist Pflicht, nicht Kosmetik: die `NameRow` unter `SubjectRow` hält den Namen in eigenem
              `useState` (Startwert einmalig). Ohne Schlüssel gilt die Zeile beim Fachwechsel als
              *dieselbe*, behält den alten Namen im Feld – und „OK" schriebe den Namen des vorigen Fachs
              in den geteilten Katalog. Er sitzt hier, weil ein Schlüssel an dem Element hängen muss, das
              der Aufrufer rendert; der zustandshaltende Baustein liegt seit B-154 eine Ebene tiefer. */}
          <SubjectRow key={subject.id} subject={subject} busy={action.busy}
            onSave={(name) => act(() => api.updateSubject(subject.id, name), "Fach umbenannt.")}
            onDelete={() => {
              /* Der Text nennt alle fünf Zuordnungen statt nur zwei (B-144). Bewusst ohne Zahl: die
                 hätte eine eigene Route gekostet und `confirmAction` asynchron gemacht – und sie ändert
                 die Entscheidung nicht. Was ein Kind besitzt (Meilensteine, Stundenplan), taucht hier
                 gar nicht auf: das sperrt der Server mit 409, statt zu warnen. */
              if (!confirmAction(
                `Fach „${subject.name}" wirklich löschen? Lehrwerk-Reihen, Lehrbücher, Fachlehrer-Profile, `
                + "Lehrpläne und Klassenarbeiten behalten ihren Inhalt, verlieren aber die Zuordnung zu "
                + "diesem Fach. Seine Übungs-Kategorien werden gelöscht – die Übungen darin bleiben.")) return;
              act(() => api.deleteSubject(subject.id), "Fach gelöscht.", () => setSubjectId(""));
            }} />

          <p className="muted" style={{ fontSize: 13 }}>
            Lehrwerk-Reihen und ihre Units verwaltest du auf der Seite <a href="/vater/lehrwerke">📕 Lehrwerke</a> –
            jede Übung hängt seit Kurzem an einer Unit statt an einem Kapitel.
          </p>

          <CategorySection subjectId={subject.id} categories={categories.data} error={categories.error}
            busy={action.busy}
            onSave={(c, name) => act(() => api.updateCategory(subject.id, c.id, name), "Art umbenannt.", categories.reload)}
            onDelete={(c) => {
              if (!confirmAction(`Art „${c.name}" löschen? Übungen behalten ihren Inhalt, verlieren aber die Zuordnung.`)) return;
              act(() => api.deleteCategory(subject.id, c.id), "Art gelöscht.", categories.reload);
            }}
            onCreate={(name) => act(() => api.createCategory(subject.id, name), "Art angelegt.", categories.reload)} />
        </>
      )}

      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </section>
  );
}

/**
 * Der Arten-Abschnitt des gewählten Fachs: Überschrift, Erklärung, die Zeilen – und das Anlege-Formular.
 *
 * **Das Formular liegt hier und ausdrücklich NICHT in `CategoryRows`.** Anlegen bleibt für jeden Creator
 * frei (B-157, Entscheidung 2), auch an einem fremden und an einem Fach aus dem Grundbestand: Wer es mit in
 * die Eigentums-Bedingung zieht, friert die Art-Achse aller Seed-Fächer ein — und das sind die einzigen, die
 * ein normaler Nutzer hat.
 *
 * **Seit B-170 ist das strukturell gesichert, nicht mehr nur getestet:** Dieser Abschnitt bekommt das Fach
 * nicht mehr, sondern nur noch dessen **Id** — er *kann* also nicht nach Eigentum gaten, weil eine Zahl
 * kein `isOwn` trägt. Vorher hielten drei Fälle in `CatalogAdmin.test.tsx` die Zusage, einer je
 * Fach-Zustand; ohne das `SubjectResponse` wären sie drei identische Renderings und damit drei Kopien einer
 * Zusicherung, von denen keine anders fallen kann. Einer ist geblieben (das Formular ist überhaupt da), die
 * Zusage selbst hängt jetzt an der Signatur.
 *
 * Eigener exportierter Baustein aus demselben Grund wie `SubjectRow`: `CatalogAdmin` lädt beim Fachwechsel
 * nach und hängt am Netz, dieser Abschnitt nicht.
 */
export function CategorySection({ subjectId, categories, error, busy, onSave, onDelete, onCreate }: {
  /**
   * Das Fach, zu dem dieser Abschnitt gehört – als **Id**, nicht als `SubjectResponse` (siehe oben: eine
   * Zahl kann nicht als Eigentums-Bedingung missbraucht werden). Sie ist nötig, weil `useAsync` seine
   * `data` über einen **Wechsel der Abhängigkeit** behält, nicht nur über ein `reload`: Beim Sprung von
   * Fach A auf Fach B stehen im Ladefenster noch A's Arten da, während die Rückrufe oben schon B's Id
   * binden – ein Klick schickte `DELETE {B}/{Art aus A}`. Ein `disabled` würde nur die Folge sperren; hier
   * ist die Zeile schon **nicht renderbar**, weil sie ihr eigenes Fach mitbringt und es nicht passt.
   */
  subjectId: number;
  categories: CategoryResponse[] | null | undefined;
  /**
   * Als Prop und nicht im Baustein aus `useAsync` gelesen: Ein gescheitertes Laden und ein leeres Fach
   * sahen identisch aus, weil `CategoryRows` ohne Daten gar nichts rendert (B-174). Der Abschnitt ist
   * der richtige Ort, weil er in allen drei Fach-Zuständen erscheint — vor B-170 hing unter einem fremden
   * Fach auch der Erklärsatz an den Arten, dort wurde die Fläche also vollständig stumm.
   */
  error: string | null;
  busy: boolean;
  onSave: (category: CategoryResponse, name: string) => void;
  onDelete: (category: CategoryResponse) => void;
  onCreate: (name: string) => Promise<boolean>;
}) {
  // Nur die Zeilen dieses Fachs – die Zählung in der Überschrift eingeschlossen, sonst nennt sie im
  // Ladefenster die Anzahl des vorigen Fachs.
  const arten = categories?.filter((c) => c.subjectId === subjectId);
  return (
    <>
      <h4 className="h-section" style={{ fontSize: 15, marginTop: 14 }}>
        Arten {arten ? `(${arten.length})` : ""}
      </h4>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        Freie Ordnungsbegriffe innerhalb des Fachs (z. B. „Grammatik", „Vokabeln") – sie filtern die
        Übungssuche beim Planbau.
      </p>
      {/* `role="alert"` wie in `SohnShop.tsx:329`: Der Banner entsteht erst beim Fachwechsel, während
          der Nutzer schon auf der Seite steht – ohne Rolle bemerkt ein Screenreader ihn nicht, und die
          Einfärbung allein ist keine Rückmeldung. */}
      {error && <div className="banner err" role="alert">Arten: {error}</div>}
      <CategoryRows categories={arten} busy={busy} onSave={onSave} onDelete={onDelete} />
      <NewName fieldId="ca-new-category" label="Neue Art" placeholder="z. B. Grammatik"
        busy={busy} onCreate={onCreate} />
    </>
  );
}

/**
 * Die Art-Zeilen des gewählten Fachs – bearbeitbar **pro Zeile** (B-170).
 *
 * Bis B-170 entschied `subject.isMine` über die ganze Liste, und der Kommentar hier begründete das damit,
 * die Art habe keinen eigenen Eigentümer; ein Flag am `CategoryResponse` wäre „eine zweite Kopie derselben
 * Wahrheit". Beides ist überholt: Die Art hat seit B-170 einen eigenen Eigentümer, und `isOwn` ist keine
 * Kopie, sondern die **Antwort** auf eine Frage mit zwei Trägern (Eigentümer der Art *oder* des Fachs oder
 * Admin) – die der Client sonst nachrechnen müsste, mit einem Feld, das er nicht hat.
 *
 * Die Folge für den Bildschirm: In **einem** Fach stehen jetzt beide Sorten Zeilen nebeneinander. In
 * „Englisch" aus dem Grundbestand trägt „Grammatik" (geseedet) keine Knöpfe, eine selbst angelegte
 * „Grammtik" beide. Ein Satz über die ganze Liste kann das nicht mehr sagen – er ist darum weg, und der
 * Vorspann der Seite nennt stattdessen die Regel.
 *
 * **Kein deaktivierter Knopf mit `title`:** Wo man nicht darf, fehlen die Bedienelemente. Ein
 * `disabled`-Knopf bliebe im Fokusbaum und nennt seinen Grund nicht – der `title` erscheint gar nicht
 * (B-160). Die Namen bleiben **lesbar**: eine Seite, die „du kannst sie zum Filtern verwenden" sagt und
 * dann keine zeigt, hält ihr eigenes Versprechen nicht.
 */
export function CategoryRows({ categories, busy, onSave, onDelete }: {
  categories: CategoryResponse[] | null | undefined;
  busy: boolean;
  onSave: (category: CategoryResponse, name: string) => void;
  onDelete: (category: CategoryResponse) => void;
}) {
  if (!categories?.length) return null;
  return (
    <>
      {categories.map((c) => (c.isOwn ? (
        <NameRow key={c.id} busy={busy} fieldId={`ca-category-${c.id}`} label="Art"
          srName={`Art „${c.name}"`} value={c.name}
          onSave={(name) => onSave(c, name)} onDelete={() => onDelete(c)} />
      ) : (
        /* Nur lesbar. `aria-hidden` wäre falsch: der Name ist Inhalt, nur nicht bedienbar. */
        <p key={c.id} className="muted" style={{ fontSize: 13, margin: "6px 0" }}>
          {c.name}
        </p>
      )))}
    </>
  );
}

/**
 * Die Fach-Zeile – bearbeitbar nur für den Eigentümer, sonst ein Satz, der den Grund nennt.
 *
 * An einem fremden oder ownerlosen Fach **fehlen** Feld und Knöpfe, statt später mit `403 not_owner` zu
 * scheitern – dieselbe Wahl wie bei der Lehrwerk-Reihe (`VaterLehrwerke.tsx`). Ein `disabled`-Knopf wäre
 * schlechter: er bleibt im Fokusbaum und nennt keinen Grund. Stumm zu bleiben wäre es auch – eine Seite
 * ohne Knöpfe und ohne Erklärung liest sich als Fehler (B-150).
 *
 * Eigener exportierter Baustein, damit der Test die **Bindung** an `isOwn` prüfen kann: `CatalogAdmin`
 * selbst lädt beim Fachwechsel die Arten nach und hängt damit am Netz (siehe `frontend/CLAUDE.md` –
 * Bausteine hier, Wege durch die App bei Playwright).
 */
export function SubjectRow({ subject, busy, onSave, onDelete }: {
  subject: SubjectResponse;
  busy: boolean;
  onSave: (name: string) => void; onDelete: () => void;
}) {
  if (subject.isOwn) {
    return <NameRow busy={busy} fieldId={`ca-subject-${subject.id}`} label="Fach umbenennen"
      srName={`Fach „${subject.name}"`} value={subject.name} onSave={onSave} onDelete={onDelete} />;
  }
  /* Die zwei Fälle werden unterschieden, weil „gehört jemand anderem" bei einem Fach aus dem Grundbestand
     einfach falsch wäre – es gehört niemandem, und niemand kann es ändern. `== null` deckt `null` und ein
     fehlendes Feld gleichermaßen (der Vertrag gibt `ownerAdultId` optional heraus). */
  return (
    <p className="muted" style={{ fontSize: 13, marginTop: 8 }}>
      {subject.ownerAdultId == null
        ? `„${subject.name}" gehört zum Grundbestand – du kannst es verwenden, `
          + "aber niemand kann es umbenennen oder löschen."
        : `„${subject.name}" hat jemand anderes angelegt – du kannst es verwenden, aber nicht ändern.`}
    </p>
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
export function NameRow({ fieldId, label, srName, value, busy, onSave, onDelete }: {
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
  /** Liefert, ob es geklappt hat – nur dann wird das Eingabefeld geleert (wie `TagAdder.onAdd`). */
  onCreate: (name: string) => Promise<boolean>;
}) {
  const [name, setName] = useState("");
  return (
    <form className="row" style={{ gap: 6, alignItems: "flex-end", marginTop: 8 }}
      onSubmit={async (e) => {
        e.preventDefault();
        const trimmed = name.trim();
        if (trimmed && await onCreate(trimmed)) setName("");
      }}>
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
