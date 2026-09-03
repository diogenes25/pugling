import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, errorMessage } from "../lib/api";
import { useAction } from "../lib/useAction";
import { StatusBanner } from "../components/StatusBanner";
import { interestFacetLabel } from "../lib/labels";
import { useSohn } from "./SohnApp";
import type { InterestFacet, InterestTagOption, MyInterestProfile } from "../lib/types";

/**
 * Der geführte Prozess, in dem das Kind sich selbst beschreibt (B-50).
 *
 * Die Mechanik ist bewusst eine **Kachel-Mehrfachauswahl** und kein Paarvergleich (Entscheidung 8 der
 * Story): fachlich zählt am Ende nur ein gewichtetes Schlagwort, und eine Ranking-Engine hätte dafür
 * eigenen Zustand und eigene Tests gebraucht. Getippt wird nur im letzten, freiwilligen Schritt.
 *
 * **Nichts wird unterwegs geschickt.** Erst „Fertig" speichert – damit ist ein Verlassen des Bildschirms
 * folgenlos (abbrechbar), und ein erneuter Besuch zeigt den zuletzt gespeicherten Stand (fortsetzbar).
 */

/** Der Schlüssel, unter dem die App sich merkt, dass der Prozess schon einmal angeboten wurde. */
export const INTRO_SEEN_KEY = (childId: number) => `pugling.interests.seen.${childId}`;

/**
 * `needsIntro` leitet der Server aus dem **leeren** Bestand ab (kein neues Feld, keine Migration). Ein Kind,
 * das bewusst „nichts davon" wählt, bliebe damit für immer neu – darum merkt sich das Gerät den Besuch.
 */
export function markIntroSeen(childId: number) {
  try { localStorage.setItem(INTRO_SEEN_KEY(childId), "1"); } catch { /* privater Modus: dann eben nochmal */ }
}

/** Ob der Prozess dem Kind von selbst angeboten werden soll. */
export function shouldOfferIntro(childId: number, needsIntro: boolean): boolean {
  if (!needsIntro) return false;
  try { return localStorage.getItem(INTRO_SEEN_KEY(childId)) !== "1"; } catch { return true; }
}

/** Ein Gewicht der groben Skala, wie die Kacheln es durchlaufen. */
const LIKE_STEPS = [0, 2, 3] as const;

/**
 * Die Zuneigungs-Kachel im Weiterschalten: aus → mag ich → Lieblingsthema → aus. Eine Kachel, die auf
 * „Abneigung" steht, springt zuerst auf „mag ich" – sonst liefe das Kind erst durch die negativen Werte.
 */
export function nextLikeWeight(current: number): number {
  const index = LIKE_STEPS.indexOf(current as 0 | 2 | 3);
  return index < 0 ? LIKE_STEPS[1] : LIKE_STEPS[(index + 1) % LIKE_STEPS.length];
}

/** Abneigung an/aus. Sie überschreibt eine Zuneigung – beides zugleich ergäbe keinen Sinn. */
export function toggledDislikeWeight(current: number): number {
  return current < 0 ? 0 : -3;
}

const STEPS = [
  { title: "Was magst du?", hint: "Einmal tippen = mag ich. Nochmal tippen = Lieblingsthema. Und noch einmal nimmt es wieder weg." },
  { title: "Wie sollen die Bilder aussehen?", hint: "Comic, Foto, Pixel-Art – such dir aus, was dir gefällt." },
  { title: "Was geht gar nicht?", hint: "Alles, was du hier antippst, kommt dir nie als Bild unter." },
  { title: "Fehlt noch was?", hint: "Wenn dein Ding nicht dabei war, schreib es hier auf – dein Vater sieht es." },
] as const;

export function SohnInterests() {
  const { childId, celebrate } = useSohn();
  const navigate = useNavigate();
  const [options, setOptions] = useState<InterestTagOption[] | null>(null);
  const [weights, setWeights] = useState<Record<number, number>>({});
  const [wishes, setWishes] = useState<string[]>([]);
  const [wish, setWish] = useState("");
  const [step, setStep] = useState(0);
  // Eigener Fehler fürs erste Laden, getrennt von `action`: die gilt dem Speichern, nicht dem Lesen.
  const [loadError, setLoadError] = useState<string | null>(null);
  const action = useAction();

  useEffect(() => {
    // Der Bildschirm ist das Angebot – ab jetzt gilt er als gesehen, auch wenn das Kind nichts wählt.
    markIntroSeen(childId);
    Promise.all([api.interestOptions(), api.myInterests()])
      .then(([tags, profile]: [InterestTagOption[], MyInterestProfile]) => {
        setOptions(tags);
        setWishes(profile.wishes);
        // Der gespeicherte Stand ist der Ausgangspunkt – genau das macht den Prozess fortsetzbar.
        setWeights(Object.fromEntries(profile.interests.map((i) => [i.tagId, i.weight])));
      })
      .catch((e) => setLoadError(errorMessage(e)));
  }, [childId]);

  const byFacet = useMemo(() => {
    const topics = (options ?? []).filter((t) => t.facet !== "Style");
    const styles = (options ?? []).filter((t) => t.facet === "Style");
    return { topics, styles };
  }, [options]);

  function cycleLike(tagId: number) {
    setWeights((w) => ({ ...w, [tagId]: nextLikeWeight(w[tagId] ?? 0) }));
  }

  function toggleDislike(tagId: number) {
    setWeights((w) => ({ ...w, [tagId]: toggledDislikeWeight(w[tagId] ?? 0) }));
  }

  async function save() {
    const interests = Object.entries(weights)
      .filter(([, weight]) => weight !== 0)
      .map(([tagId, weight]) => ({ tagId: Number(tagId), weight }));

    const ok = await action.run(async () => {
      await api.setMyInterests(interests);
      const text = wish.trim();
      if (text) {
        const profile = await api.addMyInterestWish(text);
        setWishes(profile.wishes);
        setWish("");
      }
    });
    if (!ok) return; // der Fehler steht im Banner dieses Bildschirms – hier bleiben

    // Die Bestätigung darf nicht in dem Element leben, dessen Verschwinden sie bestätigt (B-151): der
    // `StatusBanner` ginge mit dem Bildschirm unter. Die Feier liegt in der Shell und überlebt darum den
    // Sprung zurück – und ist zugleich die Sprache, die die Arcade sonst auch spricht.
    celebrate("medium", "🙂", "GESPEICHERT!", "Deine Übungen passen sich an.");
    navigate("/sohn");
  }

  if (loadError) return <div className="sohn-body"><div className="error-box">{loadError}</div></div>;
  if (!options) return <div className="sohn-body"><div className="loading">Lade…</div></div>;

  const current = STEPS[step];
  const isLast = step === STEPS.length - 1;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Das bin ich</span>
        <span className="chip" style={{ marginLeft: "auto" }}>Schritt <b className="tabnum">{step + 1}</b>/{STEPS.length}</span>
      </div>
      <p className="sub">
        Was du hier auswählst, ändert <b>nicht</b>, was du lernst – nur, wie es aussieht und worum es in den
        Beispielen geht.
      </p>

      <h3 className="screen-title" style={{ fontSize: 18 }}>{current.title}</h3>
      <p className="sub">{current.hint}</p>

      {step === 0 && <TagTiles tags={byFacet.topics} weights={weights} onPick={cycleLike} disabled={action.busy} />}
      {step === 1 && <TagTiles tags={byFacet.styles} weights={weights} onPick={cycleLike} disabled={action.busy} />}
      {step === 2 && <TagTiles tags={options} weights={weights} onPick={toggleDislike} mode="dislike" disabled={action.busy} />}
      {step === 3 && (
        <>
          <div className="field">
            <label htmlFor="wish">Dein Wunsch</label>
            <input
              id="wish" value={wish} maxLength={60} disabled={action.busy}
              onChange={(e) => setWish(e.target.value)}
              placeholder="z.B. Wellensittiche"
            />
          </div>
          {wishes.length > 0 && (
            <p className="sub" style={{ marginTop: 8 }}>Schon aufgeschrieben: {wishes.join(", ")}</p>
          )}
        </>
      )}

      <div className="row" style={{ marginTop: 12, gap: 8 }}>
        <button
          type="button" className="btn ghost" style={{ flex: 1 }}
          onClick={() => (step === 0 ? navigate("/sohn") : setStep(step - 1))}
          disabled={action.busy}
        >
          {step === 0 ? "Abbrechen" : "Zurück"}
        </button>
        {isLast ? (
          <button type="button" className="btn lime" style={{ flex: 1 }} onClick={save} disabled={action.busy}>
            Fertig
          </button>
        ) : (
          <button type="button" className="btn" style={{ flex: 1 }} onClick={() => setStep(step + 1)} disabled={action.busy}>
            Weiter
          </button>
        )}
      </div>

      <StatusBanner message={action.message} />
    </div>
  );
}

/**
 * Die Kacheln eines Schritts. `aria-pressed` statt `role="tab"` – es sind Umschalter, keine Reiter
 * (Konvention aus dem A11y-Durchgang); der zugesagte Zustand steht zusätzlich als Text auf der Kachel,
 * damit „mag ich" und „Lieblingsthema" nicht allein an der Farbe hängen.
 */
export function TagTiles({ tags, weights, onPick, mode = "like", disabled }: {
  tags: InterestTagOption[];
  weights: Record<number, number>;
  onPick: (tagId: number) => void;
  mode?: "like" | "dislike";
  disabled: boolean;
}) {
  if (tags.length === 0) {
    return <p className="sub">Hier gibt es noch nichts zur Auswahl – dein Vater legt die Stichwörter an.</p>;
  }

  return (
    <div className="skin-grid">
      {tags.map((t) => {
        const weight = weights[t.id] ?? 0;
        const on = mode === "dislike" ? weight < 0 : weight > 0;
        return (
          <button
            key={t.id}
            type="button"
            className={`skin${on ? " on" : ""}`}
            aria-pressed={on ? "true" : "false"}
            onClick={() => onPick(t.id)}
            disabled={disabled}
          >
            <div className="nm">{t.label}</div>
            <div className="sub" style={{ fontSize: 11 }}>{interestFacetLabel(t.facet as InterestFacet)}</div>
            {mode === "dislike"
              ? <span className={`pill ${weight < 0 ? "red" : "cyan"}`}>{weight < 0 ? "geht gar nicht" : "ist okay"}</span>
              : <span className={`pill ${weight === 3 ? "gold" : weight === 2 ? "lime" : "cyan"}`}>
                  {weight === 3 ? "Lieblingsthema" : weight === 2 ? "mag ich" : "auswählen"}
                </span>}
          </button>
        );
      })}
    </div>
  );
}

/**
 * Der Anstoß beim ersten Mal (Akzeptanzkriterium 7): Er erscheint nur, solange das Kind sich noch nie
 * beschrieben hat, und verschwindet, sobald der Prozess einmal geöffnet war – dauerhaft erreichbar bleibt
 * er über den Menüpunkt im Konto. Ein Fehler beim Laden bleibt hier **stumm**: der Hinweis ist ein
 * Angebot, kein Inhalt, und eine rote Meldung auf der Startseite wäre die falsche Antwort darauf.
 */
export function InterestIntroCard() {
  const { childId } = useSohn();
  const [offer, setOffer] = useState(false);

  useEffect(() => {
    let alive = true;
    api.myInterests()
      .then((p) => { if (alive) setOffer(shouldOfferIntro(childId, p.needsIntro)); })
      .catch(() => { /* stumm: der Hinweis ist ein Angebot, kein Inhalt */ });
    return () => { alive = false; };
  }, [childId]);

  if (!offer) return null;
  return (
    <div className="card" style={{ textAlign: "center" }}>
      <div className="screen-title" style={{ fontSize: 18 }}>Erzähl mal von dir!</div>
      <p className="sub">
        Sag uns, was du magst – dann passen die Bilder und Beispiele zu dir. Dauert eine Minute, ohne Tippen.
      </p>
      <Link to="/sohn/ich" className="btn gold">Los geht's</Link>
    </div>
  );
}
