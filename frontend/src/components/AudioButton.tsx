import { useEffect, useRef, useState } from "react";

/**
 * Abspielknopf für eine Aufnahme – die Aussprache einer Vokabel oder der Mitschnitt einer
 * Hörverstehen-Übung. Rein clientseitig, ohne Bewertung; die bleibt serverseitig.
 *
 * Zwei Betriebsarten, weil die Aufnahme zweierlei sein kann:
 *
 * - **Sie ist die Aufgabe** (Vokabel-Hörstufe): kurz, wird beim Erscheinen einmal angespielt, der Knopf
 *   genügt zum Wiederholen. Der Server schickt dort bewusst keinen Text mit.
 * - **Sie ist das Material** (Hörverstehen): kann Minuten laufen. Dann gehören die Browser-Bedienelemente
 *   dazu (`withControls`) – ein Knopf, der nur „von vorn" kann, lässt das Kind eine laufende Aufnahme nicht
 *   anhalten.
 *
 * Kein eigenes `aria-label`: Der sichtbare Text **ist** der zugängliche Name. Ein fest verdrahtetes Label
 * („Vokabel anhören") wich davon ab, sobald der Aufrufer etwas anderes beschriftete – dann greift
 * Spracheingabe ins Leere (WCAG 2.5.3), und bei einem Hörverstehen stimmte das Wort „Vokabel" ohnehin nicht.
 */
export function AudioButton({ url, label = "🔊 Anhören", autoPlay = true, withControls = false }: {
  url: string;
  label?: string;
  autoPlay?: boolean;
  withControls?: boolean;
}) {
  const audio = useRef<HTMLAudioElement | null>(null);
  const [error, setError] = useState(false);

  function play() {
    setError(false);
    const el = audio.current;
    if (!el) return;
    el.currentTime = 0;
    el.play().catch(() => setError(true));
  }

  // Beim Wechsel der Quelle einmal automatisch anspielen (best effort – Autoplay kann geblockt sein).
  useEffect(() => {
    if (autoPlay) play();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [url]);

  if (withControls) {
    return (
      <div className="row" style={{ gap: 8, alignItems: "center", flexWrap: "wrap" }}>
        <audio ref={audio} src={url} preload="auto" controls style={{ width: "100%" }}
          onError={() => setError(true)} aria-label={label.replace(/^\W+\s*/, "")} />
        {error && <span className="muted" style={{ fontSize: 12 }} role="alert">Audio nicht abspielbar</span>}
      </div>
    );
  }

  return (
    <div className="row" style={{ gap: 8, alignItems: "center" }}>
      <audio ref={audio} src={url} preload="auto" onError={() => setError(true)} />
      <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={play}>
        {label}
      </button>
      {error && <span className="muted" style={{ fontSize: 12 }} role="alert">Audio nicht abspielbar</span>}
    </div>
  );
}
