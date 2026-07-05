import { useEffect, useRef, useState } from "react";

/**
 * Abspielknopf für die Aussprache-Audioquelle einer Vokabel (Hör-Stufe „Hören → tippen"): Das Kind hört das
 * Wort und tippt die Übersetzung – der Wort-Text bleibt daher bewusst verborgen. Beim Erscheinen wird einmal
 * automatisch abgespielt (Browser blocken Autoplay ggf.; der Knopf bleibt der verlässliche Weg). Rein clientseitig,
 * ohne Bewertung – die bleibt serverseitig.
 */
export function AudioButton({ url, label = "🔊 Anhören", autoPlay = true }: {
  url: string;
  label?: string;
  autoPlay?: boolean;
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

  return (
    <div className="row" style={{ gap: 8, alignItems: "center" }}>
      <audio ref={audio} src={url} preload="auto" onError={() => setError(true)} />
      <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={play} aria-label="Vokabel anhören">
        {label}
      </button>
      {error && <span className="muted" style={{ fontSize: 12 }}>Audio nicht abspielbar</span>}
    </div>
  );
}
