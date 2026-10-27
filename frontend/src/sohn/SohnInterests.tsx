import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Trans, useTranslation } from "react-i18next";
import { api, errorMessage } from "../lib/api";
import { useAction } from "../lib/useAction";
import { StatusBanner } from "../components/StatusBanner";
import { useSohn } from "./SohnApp";
import type { InterestTagOption, MyInterestProfile } from "../lib/types";

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
  { title: "interests.likeTitle", hint: "interests.likeHint" },
  { title: "interests.styleTitle", hint: "interests.styleHint" },
  { title: "interests.dislikeTitle", hint: "interests.dislikeHint" },
  { title: "interests.wishTitle", hint: "interests.wishHint" },
] as const;

export function SohnInterests() {
  const { t } = useTranslation();
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
    Promise.all([api.interestOptions(), api.myInterests()])
      .then(([tags, profile]: [InterestTagOption[], MyInterestProfile]) => {
        // Erst jetzt gilt das Angebot als eingelöst – auch wenn das Kind nichts wählt. Vor dem Laden
        // wäre es bei einem Fehlschlag (offline) verbraucht, ohne dass je etwas zu sehen war.
        markIntroSeen(childId);
        setOptions(tags);
        setWishes(profile.wishes);
        // Der gespeicherte Stand ist der Ausgangspunkt – genau das macht den Prozess fortsetzbar.
        setWeights(Object.fromEntries(profile.interests.map((i) => [i.tagId, i.weight])));
      })
      .catch((e) => setLoadError(errorMessage(e)));
  }, [childId]);

  // Der Schrittwechsel tauscht Titel, Hinweis und die ganze Kachelmenge aus. Ohne Fokuswechsel hört
  // niemand, dass jetzt „Was geht gar nicht?" dran ist – bei einem Bildschirm, der Bedienbarkeit ohne
  // Tippen zusagt, ist das die halbe Zusage. Der übliche Wizard-Griff: die Überschrift fokussieren.
  const headingRef = useRef<HTMLHeadingElement>(null);
  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) { firstRender.current = false; return; }
    headingRef.current?.focus();
  }, [step]);

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

    const text = wish.trim();
    // Die beiden Schreibwege sind unabhängig, und der Wunsch ist der freiwillige. Er läuft darum als
    // eigener Schritt: Scheitert er (etwa weil die Wunschliste voll ist), sind die Interessen trotzdem
    // gespeichert – und genau das muss die Meldung sagen, statt nur den Wunsch-Fehler zu zeigen.
    const ok = await action.run(() => api.setMyInterests(interests));
    if (!ok) return; // der Fehler steht im Banner dieses Bildschirms – hier bleiben

    if (text) {
      const profile = await action.runFor(() => api.addMyInterestWish(text));
      if (profile === null) {
        action.fail(t("interests.wishFailed", { text }));
        return;
      }
      setWishes(profile.wishes);
      setWish("");
    }

    // Die Bestätigung darf nicht in dem Element leben, dessen Verschwinden sie bestätigt (B-151) – der
    // `StatusBanner` dieses Bildschirms ginge mit ihm unter. Sie reist deshalb mit dem Sprung zur
    // Meldungsstelle der Shell (`SavedNotice`). Die Feier kommt obendrauf: sie ist `aria-hidden` und bei
    // „Bewegung reduzieren" gar nicht da, taugt also als Schmuck, nie als einziger Beleg.
    celebrate("medium", "🙂", t("interests.savedTitle"), t("interests.savedSub"));
    navigate("/sohn", { state: { saved: t("interests.savedNotice") } });
  }

  if (loadError) return <div className="sohn-body"><div className="error-box">{loadError}</div></div>;
  if (!options) return <div className="sohn-body"><div className="loading">{t("interests.loading")}</div></div>;

  const current = STEPS[step];
  const isLast = step === STEPS.length - 1;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>{t("interests.title")}</span>
        <span className="chip" style={{ marginLeft: "auto" }}>
          <Trans i18nKey="interests.step" values={{ step: step + 1, total: STEPS.length }} components={{ b: <b className="tabnum" /> }} />
        </span>
      </div>
      <p className="sub"><Trans i18nKey="interests.note" components={{ b: <b /> }} /></p>

      <h3 className="screen-title" style={{ fontSize: 18 }} tabIndex={-1} ref={headingRef}>{t(current.title)}</h3>
      <p className="sub">{t(current.hint)}</p>

      {step === 0 && <TagTiles tags={byFacet.topics} weights={weights} onPick={cycleLike} disabled={action.busy} />}
      {step === 1 && <TagTiles tags={byFacet.styles} weights={weights} onPick={cycleLike} disabled={action.busy} />}
      {step === 2 && <TagTiles tags={options} weights={weights} onPick={toggleDislike} mode="dislike" disabled={action.busy} />}
      {step === 3 && (
        <>
          <div className="field">
            <label htmlFor="wish">{t("interests.wishLabel")}</label>
            <input
              id="wish" value={wish} maxLength={60} disabled={action.busy}
              onChange={(e) => setWish(e.target.value)}
              placeholder={t("interests.wishPlaceholder")}
            />
          </div>
          {wishes.length > 0 && (
            <p className="sub" style={{ marginTop: 8 }}>{t("interests.wishesSoFar", { list: wishes.join(", ") })}</p>
          )}
        </>
      )}

      <div className="row" style={{ marginTop: 12, gap: 8 }}>
        <button
          type="button" className="btn ghost" style={{ flex: 1 }}
          onClick={() => (step === 0 ? navigate("/sohn") : setStep(step - 1))}
          disabled={action.busy}
        >
          {step === 0 ? t("interests.cancel") : t("interests.back")}
        </button>
        {isLast ? (
          <button type="button" className="btn lime" style={{ flex: 1 }} onClick={save} disabled={action.busy}>
            {t("interests.done")}
          </button>
        ) : (
          <button type="button" className="btn" style={{ flex: 1 }} onClick={() => setStep(step + 1)} disabled={action.busy}>
            {t("interests.next")}
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
  const { t } = useTranslation();
  if (tags.length === 0) {
    return <p className="sub">{t("interests.noTags")}</p>;
  }

  return (
    <div className="skin-grid">
      {tags.map((tag) => {
        const weight = weights[tag.id] ?? 0;
        const on = mode === "dislike" ? weight < 0 : weight > 0;
        return (
          <button
            key={tag.id}
            type="button"
            className={`skin${on ? " on" : ""}`}
            aria-pressed={on ? "true" : "false"}
            onClick={() => onPick(tag.id)}
            disabled={disabled}
          >
            <div className="nm">{tag.label}</div>
            <div className="sub" style={{ fontSize: 11 }}>{t(`interests.facet.${tag.facet}`, { defaultValue: tag.facet })}</div>
            {/* Aus dem VORZEICHEN abgeleitet, nicht aus der Gleichheit: Der Vater kann am selben Kind
                „ganz nett" (1) und „eher nicht" (-1) setzen. Ein `weight === 2`-Vergleich beschriftete eine
                leuchtende, `aria-pressed`-gedrückte Kachel dann mit „auswählen" – der Text sagte das
                Gegenteil von Farbe und Zustand. Umgerechnet wird hier bewusst nichts: eine Normalisierung
                schriebe die feinere Angabe des Vaters still auf die grobe Kind-Skala um. */}
            {mode === "dislike"
              ? <span className={`pill ${weight < 0 ? "red" : "cyan"}`}>{weight < 0 ? t("interests.pillDislike") : t("interests.pillOkay")}</span>
              : <span className={`pill ${weight >= 3 ? "gold" : weight > 0 ? "lime" : "cyan"}`}>
                  {weight >= 3 ? t("interests.pillFavourite") : weight > 0 ? t("interests.pillLike") : t("interests.pillPick")}
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
  const { t } = useTranslation();
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
      <div className="screen-title" style={{ fontSize: 18 }}>{t("interests.introTitle")}</div>
      <p className="sub">{t("interests.introText")}</p>
      <Link to="/sohn/ich" className="btn gold" style={{ textDecoration: "none" }}>{t("interests.introGo")}</Link>
    </div>
  );
}
