import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ChildMaterialSection } from "./ChildMaterialSection";
import { SupervisorsSection, TimetableSection } from "./ChildCarePanels";
import { StatusBanner } from "../components/StatusBanner";
import { FieldLabel } from "../components/InfoHint";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { interestSlug } from "../lib/interests";
import { GENDERS, INTEREST_FACETS, SCHOOL_TYPES, interestFacetLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { useAsync } from "../lib/useAsync";
import type {
  ChildInterestResponse, ChildResponse, ContentRating, Gender, InterestFacet,
  InterestTagResponse, SchoolType, SubjectResponse, UpdateChildDto,
} from "../lib/types";

/**
 * Das Profil eines Kindes – bis hierher nur über die API pflegbar, obwohl es die Grundlage der
 * Individualisierung ist. Zwei Dinge stehen hier, die anderswo nicht hingehören:
 *
 * 1. **Gewichtete Interessen.** Sie sind kein Freitext, sondern Verweise auf dieselbe Taxonomie, mit der
 *    auch die Bilder getaggt sind – nur deshalb kann der Server „welches Bild passt zu diesem Kind"
 *    überhaupt rechnen. Das Vorzeichen trägt die Hauptaussage: **negativ = Abneigung**, und die schließt
 *    Bilder hart aus, statt sie nur schlechter zu ranken.
 * 2. **Die Bild-Freigabe.** Sie ist die Achse, auf der die Zielgruppen-Trennung ruht; nur der Vater darf
 *    sie heben, und der Standard ist die strengste Stufe.
 */
export function VaterKind() {
  const childId = Number(useParams().childId);
  const child = useAsync<ChildResponse>(() => api.child(childId), [childId]);
  const interests = useAsync<ChildInterestResponse[]>(() => api.childInterests(childId), [childId]);
  const tags = useAsync<InterestTagResponse[]>(() => api.interestTags(), []);
  // Für den Stundenplan: die Fächer des Katalogs.
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);

  const me = child.data;

  /*
   * Auf `data` prüfen, nicht auf `loading` – aus demselben Grund wie beim Interessen-Editor weiter unten:
   * nach dem Speichern lädt der Datensatz neu, und ein Platzhalter nähme die Formulare für einen Moment
   * aus dem DOM. Sie verlören dabei ihren Zustand samt der Bestätigung, die der Nutzer gerade lesen soll.
   */
  if (child.error) return <div className="banner err">{child.error}</div>;
  if (!me) return child.loading
    ? <div className="loading">Lade…</div>
    : <div className="banner err">Kind nicht gefunden.</div>;

  return (
    <>
      <div className="row" style={{ marginBottom: 8 }}>
        <h2 className="h-section">{me.name}</h2>
        <span className="pill">🪙 {me.coins}</span>
        <span className="pill">💎 {me.gems}</span>
        <Link to="/vater" className="btn ghost small" style={{ marginLeft: "auto", textDecoration: "none" }}>← Übersicht</Link>
      </div>

      <ChildNav child={me} />

      <CoreDataSection child={me} onSaved={child.reload} />

      {/*
        Direkt hinter den Stammdaten: Klasse und Schulart von oben entscheiden mit, welcher Fachlehrer
        zu diesem Kind passt – die Wirkung soll auf demselben Blick sichtbar sein.
      */}
      <ChildMaterialSection childId={childId} childName={me.name} subjects={subjects.data ?? []} />

      {/* Wer betreut das Kind (gemeinsames Wallet) und wann welches Fach ansteht – beides Profilwissen. */}
      <SupervisorsSection childId={childId} childName={me.name} />
      <TimetableSection childId={childId} subjects={subjects.data ?? []} />

      <RatingSection child={me} onSaved={child.reload} />

      <section>
        <h3 className="h-section">Interessen</h3>
        <p className="sub">
          Steuern, welches Bild dein Kind zu einer Vokabel sieht. <strong>Minus-Werte sind Abneigungen</strong> –
          passende Bilder werden dann gar nicht erst gezeigt.
        </p>
        {interests.error && <div className="banner err">{interests.error}</div>}
        {/*
          Auf `data` prüfen, nicht auf `loading`: nach dem Speichern lädt die Liste neu, und ein
          Platzhalter würde den Editor kurz aus dem DOM nehmen. Er verlöre dabei seinen Zustand –
          samt der Bestätigung, die der Nutzer gerade lesen soll.
        */}
        {interests.data === null ? <div className="loading">Lade…</div> : (
          <InterestEditor
            childId={childId}
            current={interests.data}
            known={tags.data ?? []}
            onSaved={() => { interests.reload(); tags.reload(); }}
          />
        )}
      </section>
    </>
  );
}

/**
 * Alles, was zu *diesem* Kind gehört, von einer Stelle aus erreichbar. Ohne diese Leiste beginnt jeder
 * Weg im Hauptmenü und endet an einem Kind-Pulldown – bei mehreren Kindern eine ständige Fehlerquelle,
 * weil dort immer das erste vorausgewählt ist. Die Ziel-Seiten übernehmen das Kind aus `?childId=`.
 */
function ChildNav({ child }: { child: ChildResponse }) {
  const links: [string, string][] = [
    [`/vater/kind/${child.id}/lernstand`, "📈 Lernstand"],
    [`/vater/kind/${child.id}/ziele`, "🎯 Ziele"],
    [`/vater?childId=${child.id}`, "🗂️ Lehrpläne"],
    [`/vater/class-tests?childId=${child.id}`, "📝 Klassenarbeiten"],
    [`/vater/rewards?childId=${child.id}`, "🏆 Belohnungen"],
    [`/vater/shop?childId=${child.id}`, "🛒 Shop"],
    [`/vater/konto?childId=${child.id}`, "💰 Kontostand"],
  ];
  return (
    <nav className="row" style={{ gap: 8, flexWrap: "wrap", marginBottom: 14 }} aria-label={`Bereiche für ${child.name}`}>
      {links.map(([to, label]) => (
        <Link key={to} to={to} className="btn ghost small" style={{ width: "auto", textDecoration: "none" }}>{label}</Link>
      ))}
    </nav>
  );
}

/**
 * Die Stammdaten des Kindes. Sie gehören hierher, weil sie nach dem Anlegen sonst unerreichbar wären –
 * und zwei von ihnen sind mehr als Beschriftung:
 *
 * * **Die PIN** ist der Login des Kindes. Wird sie beim Anlegen weggelassen, kommt das Kind nicht in
 *   seine App; hier ist die einzige Stelle, an der sie nachgetragen oder geändert werden kann.
 * * **Klasse und Schulart** filtern die Übungssuche im Katalog und im Assistenten – eine falsche Klasse
 *   versteckt also passende Übungen.
 *
 * Die Freitext-Interessen sind absichtlich getrennt von den gewichteten Interessen weiter unten: sie sind
 * die Sprache des KI-Creators (Prosa fürs Modell), nicht das Vokabular der Bildauswahl.
 */
/**
 * Der Formular-Zustand aus dem Server-Stand. Auch die Rücksetz-Quelle nach dem Speichern: der `PATCH`
 * ignoriert `null` bei `birthYear`/`grade` (`HasValue`), ein geleertes Zahlenfeld ist also **kein** Löschen.
 * Ohne das Zurücksetzen stünde danach ein leeres Feld über einem gespeicherten Wert – die Anzeige würde lügen.
 */
function formStateOf(child: ChildResponse) {
  return {
    name: child.name,
    birthYear: child.birthYear?.toString() ?? "",
    grade: child.grade?.toString() ?? "",
    // Der Server kann bei Kindern eine Flags-Kombination liefern ("Realschule, Gymnasium"); für die
    // Auswahl zählt nur ein Einzelwert – Kombinationen fallen auf "None" zurück.
    schoolType: (SCHOOL_TYPES.includes(child.schoolType as SchoolType) ? child.schoolType : "None") as SchoolType | "None",
    gender: child.gender,
    interests: child.interests.join(", "),
    profileNotes: child.profileNotes ?? "",
  };
}

function CoreDataSection({ child, onSaved }: { child: ChildResponse; onSaved: () => void }) {
  const nav = useNavigate();
  const [form, setForm] = useState(() => formStateOf(child));
  const [pin, setPin] = useState("");
  const action = useAction();

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name.trim()) { action.fail("Der Name darf nicht leer sein."); return; }

    // Nur Geändertes senden (PATCH-Semantik); ein leeres PIN-Feld heißt „unverändert", nicht „löschen".
    const initial = formStateOf(child);
    const dto: UpdateChildDto = {};
    if (form.name.trim() !== child.name) dto.name = form.name.trim();
    // Ein geleertes Zahlenfeld ist ein ausdrücklicher Löschwunsch – im PATCH gilt `null` als
    // „nicht angegeben", also braucht es den Clear-Schalter.
    const year = form.birthYear.trim() === "" ? null : Number(form.birthYear);
    if (year !== child.birthYear) { if (year === null) dto.clearBirthYear = true; else dto.birthYear = year; }
    const grade = form.grade.trim() === "" ? null : Number(form.grade);
    if (grade !== child.grade) { if (grade === null) dto.clearGrade = true; else dto.grade = grade; }
    /*
     * Gegen den **Anzeigewert** vergleichen, nicht gegen `child.schoolType`: bei einer Flags-Kombination
     * zeigt die Auswahl "None", und ein Vergleich mit dem Rohwert wäre immer „geändert" – jedes Speichern
     * (auch ein reines Umbenennen) hätte die Kombination auf None zurückgesetzt.
     */
    if (form.schoolType !== initial.schoolType) dto.schoolType = form.schoolType as SchoolType;
    if (form.gender !== child.gender) dto.gender = form.gender;
    const interests = form.interests.split(",").map((s) => s.trim()).filter(Boolean);
    if (interests.length !== child.interests.length
      || interests.some((x, i) => x !== child.interests[i])) dto.interests = interests;
    // Leerer Text als "" senden, nicht als null: `null` ignoriert der Server, "" löscht die Notiz wirklich.
    const notes = form.profileNotes.trim();
    if (notes !== (child.profileNotes ?? "")) dto.profileNotes = notes;
    if (pin.trim()) dto.pin = pin.trim();
    if (Object.keys(dto).length === 0) { action.succeed("Nichts zu speichern."); return; }

    const saved = await action.runFor(() => api.updateChild(child.id, dto),
      dto.pin ? "Gespeichert. Die neue PIN gilt ab dem nächsten Login des Kindes." : "Gespeichert.");
    if (!saved) return;
    setPin("");
    // Formular aus der Server-Antwort setzen, nicht aus der Eingabe: so zeigt es immer den echten Stand
    // (etwa einen getrimmten Namen), statt dem Nutzer seine eigene Eingabe als „gespeichert" zu bestätigen.
    setForm(formStateOf(saved));
    onSaved();
  }

  async function remove() {
    if (!confirmAction(
      `„${child.name}" wirklich löschen? Lehrpläne, Fortschritt, Punktekonto, Käufe und Auswertungen `
      + "dieses Kindes gehen mit verloren – auch für alle anderen Betreuer (Mutter, Oma …), die es "
      + "mitbetreuen. Die Übungen im Katalog bleiben erhalten.")) return;
    if (await action.run(() => api.deleteChild(child.id))) nav("/vater");
  }

  return (
    <section>
      <h3 className="h-section">Stammdaten</h3>
      <form onSubmit={submit} style={{ display: "flex", flexDirection: "column", gap: 10 }}>
        <div className="form-grid" style={{ alignItems: "end" }}>
          <div className="field"><label htmlFor="kd-name">Name</label>
            <input id="kd-name" value={form.name} onChange={(e) => up("name", e.target.value)} /></div>
          <div className="field"><label htmlFor="kd-year">Geburtsjahr</label>
            <input id="kd-year" type="number" min={1990} max={2030} value={form.birthYear} onChange={(e) => up("birthYear", e.target.value)} /></div>
          <div className="field"><label htmlFor="kd-grade">Klasse</label>
            <input id="kd-grade" type="number" min={1} max={13} value={form.grade} onChange={(e) => up("grade", e.target.value)} /></div>
          <div className="field"><label htmlFor="kd-school">Schulart</label>
            <select id="kd-school" value={form.schoolType} onChange={(e) => up("schoolType", e.target.value as SchoolType)}>
              <option value="None">– keine Angabe –</option>
              {SCHOOL_TYPES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select></div>
          <div className="field"><label htmlFor="kd-gender">Geschlecht</label>
            <select id="kd-gender" value={form.gender} onChange={(e) => up("gender", e.target.value as Gender)}>
              {GENDERS.map((g) => <option key={g.value} value={g.value}>{g.label}</option>)}
            </select></div>
          <div className="field"><label htmlFor="kd-pin">PIN <span className="muted">(leer = unverändert)</span></label>
            <input id="kd-pin" value={pin} onChange={(e) => setPin(e.target.value)} placeholder="z.B. 1111" /></div>
        </div>
        <div className="field">
          <label htmlFor="kd-interests">Interessen als Freitext <span className="muted">(kommagetrennt)</span></label>
          <input id="kd-interests" value={form.interests} onChange={(e) => up("interests", e.target.value)}
            placeholder="Fußball, Minecraft, Hunde" />
          <span className="sub">Damit kleidet der KI-Creator Aufgaben ein. Für die Bildauswahl zählen die gewichteten Interessen unten.</span>
        </div>
        <div className="field">
          <label htmlFor="kd-notes">Notizen zum Kind <span className="muted">(optional)</span></label>
          <textarea id="kd-notes" rows={2} value={form.profileNotes} onChange={(e) => up("profileNotes", e.target.value)}
            placeholder="z.B. Schwächen bei unregelmäßigen Verben, braucht kurze Einheiten." />
        </div>
        <div className="row" style={{ gap: 8 }}>
          <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>{action.busy ? "Speichere…" : "Speichern"}</button>
          <button type="button" className="btn ghost inline-btn" style={{ width: "auto", marginLeft: "auto" }} disabled={action.busy} onClick={remove}>
            Kind löschen
          </button>
        </div>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </section>
  );
}

const RATINGS: { value: ContentRating; label: string; hint: string }[] = [
  { value: "Everyone", label: "Für alle", hint: "Standard – nur uneingeschränkt kindgerechte Bilder." },
  { value: "Teen", label: "Ab ca. 12", hint: "Zusätzlich mildere Grusel-/Konflikt-Motive." },
  { value: "Mature", label: "Erwachsene", hint: "Auch drastische oder freizügige Darstellungen." },
];

/** Die Bild-Freigabe. Bewusst eine eigene, erklärte Auswahl statt eines beiläufigen Häkchens. */
function RatingSection({ child, onSaved }: { child: ChildResponse; onSaved: () => void }) {
  const [value, setValue] = useState<ContentRating>(child.allowedContentRating);
  const action = useAction();

  async function save(next: ContentRating) {
    setValue(next);
    if (await action.run(() => api.updateChild(child.id, { allowedContentRating: next }), "Freigabe gespeichert.")) {
      onSaved();
      return;
    }
    // Zurück auf den Serverstand: die Auswahl zeigte sonst eine Freigabe, die gar nicht gilt.
    setValue(child.allowedContentRating);
  }

  return (
    <section>
      <h3 className="h-section">Bild-Freigabe</h3>
      <div className="field" style={{ maxWidth: 320 }}>
        <label htmlFor="rating">Welche Bilder darf {child.name} sehen?</label>
        <select id="rating" value={value} disabled={action.busy} onChange={(e) => save(e.target.value as ContentRating)}>
          {RATINGS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
        </select>
      </div>
      <p className="sub">{RATINGS.find((r) => r.value === value)?.hint}</p>
      <StatusBanner message={action.message} style={{ marginTop: 0 }} />
    </section>
  );
}

/** Skala grob halten: sie wird von einem Menschen gepflegt, nicht kalibriert. */
const WEIGHTS = [
  { value: 3, label: "★★★ Lieblingsthema" },
  { value: 2, label: "★★ mag es" },
  { value: 1, label: "★ ganz nett" },
  { value: -1, label: "✕ eher nicht" },
  { value: -3, label: "✕✕ auf keinen Fall" },
];

/**
 * Bearbeitet die Menge als Ganzes und schickt sie per PUT – genauso, wie der Server sie versteht.
 * Ein Eintrag lässt sich sonst nicht loswerden (PATCH kennt kein „weg").
 */
function InterestEditor({ childId, current, known, onSaved }: {
  childId: number;
  current: ChildInterestResponse[];
  known: InterestTagResponse[];
  onSaved: () => void;
}) {
  const [rows, setRows] = useState<ChildInterestResponse[]>(current);
  const [label, setLabel] = useState("");
  const [facet, setFacet] = useState<InterestFacet>("Franchise");
  const [weight, setWeight] = useState(2);
  const action = useAction();

  // Nach dem Speichern liefert der Server die kanonische Menge (Slugs, aufgelöste Synonyme) – übernehmen.
  useEffect(() => { setRows(current); }, [current]);

  function add(e: React.FormEvent) {
    e.preventDefault();
    const text = label.trim();
    if (!text) return;
    // Nach DERSELBEN Regel wie der Server (interestSlug spiegelt InterestSlug.From): „Brawl Stars" und
    // „brawl-stars" sind ein Eintrag. Ein reiner toLowerCase-Vergleich ließ beide durch – der PUT lief
    // dann in den Unique-Index, und der Slug diente hier zugleich als React-Key und als Griff der
    // Ändern-/Entfernen-Handler, die damit zwei Zeilen als eine behandelten.
    const slug = interestSlug(text);
    if (!slug) {
      action.fail(`„${text}" ergibt kein verwertbares Schlagwort – nimm einen Namen mit Buchstaben oder Zahlen.`);
      return;
    }
    if (rows.some((r) => r.slug === slug || interestSlug(r.label) === slug)) {
      action.fail(`„${text}" steht schon in der Liste.`);
      return;
    }
    // tagId 0 = noch nicht angelegt; der Server legt beim Speichern an (create-if-missing).
    setRows([...rows, { tagId: 0, slug, label: text, facet, weight, createdAt: "" }]);
    setLabel(""); action.clear();
  }

  async function save() {
    const ok = await action.run(() => api.setChildInterests(childId, rows.map((r) => ({
      weight: r.weight,
      // Bekannte Tags per Id (eindeutig), neue per Label – der Server legt sie an bzw. trifft ein Synonym.
      tagId: r.tagId > 0 ? r.tagId : null,
      label: r.tagId > 0 ? null : r.label,
      facet: r.tagId > 0 ? null : r.facet,
    }))), "Interessen gespeichert.");
    if (ok) onSaved();
  }

  const likes = rows.filter((r) => r.weight > 0).sort((a, b) => b.weight - a.weight);
  const dislikes = rows.filter((r) => r.weight < 0);
  // Vorschläge aus dem gemeinsamen Vokabular: was schon an Bildern hängt, führt garantiert zu Treffern.
  const suggestions = known
    .filter((t) => t.mediaCount > 0 && !rows.some((r) => r.slug === t.slug))
    .slice(0, 8);

  return (
    <>
      <form className="form-grid" style={{ alignItems: "end" }} onSubmit={add}>
        <div className="field">
          <label htmlFor="int-label">Interesse</label>
          <input id="int-label" value={label} onChange={(e) => setLabel(e.target.value)} placeholder="z.B. Pokémon" />
        </div>
        <div className="field">
          <FieldLabel htmlFor="int-facet" topic="interestFacet">Art</FieldLabel>
          <select id="int-facet" value={facet} onChange={(e) => setFacet(e.target.value as InterestFacet)}>
            {INTEREST_FACETS.map((f) => <option key={f.value} value={f.value}>{f.label}</option>)}
          </select>
        </div>
        <div className="field">
          <FieldLabel htmlFor="int-weight" topic="interestWeight">Wie sehr?</FieldLabel>
          <select id="int-weight" value={weight} onChange={(e) => setWeight(Number(e.target.value))}>
            {WEIGHTS.map((w) => <option key={w.value} value={w.value}>{w.label}</option>)}
          </select>
        </div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }}>Hinzufügen</button>
      </form>

      {suggestions.length > 0 && (
        <p className="sub" style={{ marginTop: 8 }}>
          Dafür gibt es schon Bilder:{" "}
          {suggestions.map((t) => (
            <button
              key={t.id} type="button" className="btn ghost small" style={{ width: "auto", marginRight: 6 }}
              onClick={() => setRows([...rows, { tagId: t.id, slug: t.slug, label: t.label, facet: t.facet, weight: 2, createdAt: "" }])}
            >+ {t.label}</button>
          ))}
        </p>
      )}

      <InterestList title="Mag" empty="Noch nichts eingetragen." rows={likes} setRows={setRows} all={rows} />
      <InterestList title="Mag nicht" empty="Nichts ausgeschlossen." rows={dislikes} setRows={setRows} all={rows} />

      <div className="row" style={{ marginTop: 12, gap: 8 }}>
        <button type="button" className="btn" style={{ width: "auto" }} disabled={action.busy} onClick={save}>
          {action.busy ? "Speichere…" : "Interessen speichern"}
        </button>
      </div>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />
    </>
  );
}

function InterestList({ title, empty, rows, setRows, all }: {
  title: string; empty: string;
  rows: ChildInterestResponse[];
  setRows: (r: ChildInterestResponse[]) => void;
  all: ChildInterestResponse[];
}) {
  return (
    <div style={{ marginTop: 12 }}>
      <h4 className="h-section" style={{ fontSize: "1rem" }}>{title}</h4>
      {rows.length === 0 ? <p className="muted">{empty}</p> : (
        <table className="table">
          <thead><tr><th>Thema</th><th>Art</th><th>Wie sehr</th><th /></tr></thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.slug}>
                <td>{r.label}</td>
                <td className="muted">{interestFacetLabel(r.facet)}</td>
                <td>
                  <select
                    aria-label={`Gewichtung für ${r.label}`}
                    value={r.weight}
                    onChange={(e) => setRows(all.map((x) => x.slug === r.slug ? { ...x, weight: Number(e.target.value) } : x))}
                  >
                    {WEIGHTS.map((w) => <option key={w.value} value={w.value}>{w.label}</option>)}
                  </select>
                </td>
                <td style={{ textAlign: "right" }}>
                  <button
                    type="button" className="btn ghost small" style={{ width: "auto" }}
                    aria-label={`${r.label} entfernen`}
                    onClick={() => setRows(all.filter((x) => x.slug !== r.slug))}
                  >Entfernen</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
