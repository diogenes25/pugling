import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type {
  ChildInterestResponse, ChildResponse, ContentRating, InterestFacet, InterestTagResponse,
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
  const child = useAsync<ChildResponse[]>(() => api.children(), [childId]);
  const interests = useAsync<ChildInterestResponse[]>(() => api.childInterests(childId), [childId]);
  const tags = useAsync<InterestTagResponse[]>(() => api.interestTags(), []);

  const me = child.data?.find((c) => c.id === childId);

  if (child.loading) return <div className="loading">Lade…</div>;
  if (child.error) return <div className="banner err">{child.error}</div>;
  if (!me) return <div className="banner err">Kind nicht gefunden.</div>;

  return (
    <>
      <div className="row" style={{ marginBottom: 8 }}>
        <h2 className="h-section">{me.name}</h2>
        <Link to="/vater" className="btn ghost small" style={{ marginLeft: "auto", textDecoration: "none" }}>← Übersicht</Link>
      </div>

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

const RATINGS: { value: ContentRating; label: string; hint: string }[] = [
  { value: "Everyone", label: "Für alle", hint: "Standard – nur uneingeschränkt kindgerechte Bilder." },
  { value: "Teen", label: "Ab ca. 12", hint: "Zusätzlich mildere Grusel-/Konflikt-Motive." },
  { value: "Mature", label: "Erwachsene", hint: "Auch drastische oder freizügige Darstellungen." },
];

/** Die Bild-Freigabe. Bewusst eine eigene, erklärte Auswahl statt eines beiläufigen Häkchens. */
function RatingSection({ child, onSaved }: { child: ChildResponse; onSaved: () => void }) {
  const [value, setValue] = useState<ContentRating>(child.allowedContentRating);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  async function save(next: ContentRating) {
    setValue(next); setBusy(true); setMsg(null);
    try {
      await api.updateChild(child.id, { allowedContentRating: next });
      setMsg("Freigabe gespeichert.");
      onSaved();
    } catch (e) { setMsg(errorMessage(e)); setValue(child.allowedContentRating); }
    finally { setBusy(false); }
  }

  return (
    <section>
      <h3 className="h-section">Bild-Freigabe</h3>
      <div className="field" style={{ maxWidth: 320 }}>
        <label htmlFor="rating">Welche Bilder darf {child.name} sehen?</label>
        <select id="rating" value={value} disabled={busy} onChange={(e) => save(e.target.value as ContentRating)}>
          {RATINGS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
        </select>
      </div>
      <p className="sub">{RATINGS.find((r) => r.value === value)?.hint}</p>
      {msg && <div className="banner ok" role="status" aria-live="polite">{msg}</div>}
    </section>
  );
}

const FACETS: { value: InterestFacet; label: string }[] = [
  { value: "Franchise", label: "Marke/Serie" }, { value: "Sport", label: "Sport" },
  { value: "Animal", label: "Tiere" }, { value: "Vehicle", label: "Fahrzeuge" },
  { value: "Music", label: "Musik" }, { value: "Hobby", label: "Hobby" },
  { value: "Nature", label: "Natur" }, { value: "Style", label: "Stil" },
  { value: "Other", label: "Sonstiges" },
];

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
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  // Nach dem Speichern liefert der Server die kanonische Menge (Slugs, aufgelöste Synonyme) – übernehmen.
  useEffect(() => { setRows(current); }, [current]);

  function add(e: React.FormEvent) {
    e.preventDefault();
    const text = label.trim();
    if (!text) return;
    // Der Slug entsteht serverseitig; hier reicht ein grober Vergleich gegen offensichtliche Dubletten.
    const rough = text.toLowerCase();
    if (rows.some((r) => r.label.toLowerCase() === rough || r.slug === rough)) {
      setMsg(`„${text}" steht schon in der Liste.`);
      return;
    }
    // tagId 0 = noch nicht angelegt; der Server legt beim Speichern an (create-if-missing).
    setRows([...rows, { tagId: 0, slug: rough, label: text, facet, weight, createdAt: "" }]);
    setLabel(""); setMsg(null);
  }

  async function save() {
    setBusy(true); setMsg(null);
    try {
      await api.setChildInterests(childId, rows.map((r) => ({
        weight: r.weight,
        // Bekannte Tags per Id (eindeutig), neue per Label – der Server legt sie an bzw. trifft ein Synonym.
        tagId: r.tagId > 0 ? r.tagId : null,
        label: r.tagId > 0 ? null : r.label,
        facet: r.tagId > 0 ? null : r.facet,
      })));
      setMsg("Interessen gespeichert.");
      onSaved();
    } catch (e) { setMsg(errorMessage(e)); }
    finally { setBusy(false); }
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
          <label htmlFor="int-facet">Art</label>
          <select id="int-facet" value={facet} onChange={(e) => setFacet(e.target.value as InterestFacet)}>
            {FACETS.map((f) => <option key={f.value} value={f.value}>{f.label}</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="int-weight">Wie sehr?</label>
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
        <button type="button" className="btn" style={{ width: "auto" }} disabled={busy} onClick={save}>
          {busy ? "Speichere…" : "Interessen speichern"}
        </button>
      </div>
      {msg && <div className="banner ok" style={{ marginTop: 10 }} role="status" aria-live="polite">{msg}</div>}
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
                <td className="muted">{FACETS.find((f) => f.value === r.facet)?.label ?? r.facet}</td>
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
