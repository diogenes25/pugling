import { useState } from "react";
import { LearningDad, type DadAction } from "../components/LearningDad";
import { useSohn } from "./SohnApp";

export function DadShowcase() {
  const { celebrate, encourage } = useSohn();
  const [activeMode, setActiveMode] = useState<"webp" | "video">("webp");

  return (
    <div className="sohn-body dad-showcase" style={{ maxWidth: 840, margin: "0 auto", paddingBottom: 60 }}>
      <div style={{ textAlign: "center", marginBottom: 24 }}>
        <h1 style={{ fontFamily: "var(--font-display)", color: "var(--gold)", margin: "0 0 6px" }}>
          Learning Dad 🌟
        </h1>
        <p style={{ color: "var(--muted)", margin: 0, fontSize: 16 }}>
          Der offizielle Puckling-Begleiter: Motivations- und Feedback-Animationen
        </p>
      </div>

      {/* Interaktive Trigger-Buttons */}
      <div className="card" style={{ marginBottom: 24 }}>
        <h3 style={{ marginTop: 0, color: "var(--cyan)", fontFamily: "var(--font-display)" }}>
          ⚡ Live-Animationen auslösen
        </h3>
        <p style={{ fontSize: 14, color: "var(--ink)", opacity: 0.85 }}>
          Teste die Overlay-Animationen, wie sie im echten Lernbetrieb bei Erfolg oder Fehlern erscheinen:
        </p>

        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 12 }}>
          <button
            type="button"
            className="btn lime"
            style={{ fontSize: 14, padding: "12px 10px" }}
            onClick={() => celebrate("big", "🏆", "JA! STARK GEMACHT!", "+10 🪙 & 5er Streak")}
          >
            👍 Erfolg: Daumen hoch (Groß)
          </button>

          <button
            type="button"
            className="btn"
            style={{ fontSize: 14, padding: "12px 10px" }}
            onClick={() => celebrate("medium", "🎯", "COMBO ×3!", "+3 🪙 Bonus")}
          >
            👍 Erfolg: Daumen hoch (Combo)
          </button>

          <button
            type="button"
            className="btn gold"
            style={{ fontSize: 14, padding: "12px 10px" }}
            onClick={() => celebrate("small", "⭐", undefined, "+1 🪙")}
          >
            ⭐ Kleiner Erfolg (Pop)
          </button>

          <button
            type="button"
            className="btn red"
            style={{ fontSize: 14, padding: "12px 10px" }}
            onClick={() =>
              encourage
                ? encourage("Kein Problem!", "Das kriegen wir beim nächsten Mal hin.")
                : celebrate("encourage", "💪", "Kein Problem!", "Das kriegen wir hin.", "facepalm")
            }
          >
            🤦‍♂️ Fehler: Aufmunterung
          </button>
        </div>

        <div style={{ marginTop: 16, display: "flex", gap: 12, alignItems: "center" }}>
          <span style={{ fontSize: 13, color: "var(--muted)" }}>Wiedergabe-Modus:</span>
          <button
            type="button"
            className={`pill ${activeMode === "webp" ? "gold" : ""}`}
            onClick={() => setActiveMode("webp")}
          >
            Transparente WebP-Animation
          </button>
          <button
            type="button"
            className={`pill ${activeMode === "video" ? "gold" : ""}`}
            onClick={() => setActiveMode("video")}
          >
            HTML5 WebM / MP4 Video
          </button>
        </div>
      </div>

      {/* Galerie der Posen */}
      <h3 style={{ fontFamily: "var(--font-display)", color: "var(--cyan)", marginBottom: 12 }}>
        🎭 Alle Charakter-Animationen
      </h3>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(240px, 1fr))", gap: 16 }}>
        {/* Karte 1: Daumen hoch 1 */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
          <b style={{ color: "var(--gold)", marginBottom: 8 }}>Daumen hoch 1 (Erfolg)</b>
          <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <LearningDad action="thumbs-up" size="md" mode={activeMode} />
          </div>
          <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>
            Wird gezeigt bei gelösten Aufgaben, Meilensteinen und Belohnungen.
          </span>
        </div>

        {/* Karte 2: Daumen hoch 2 */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
          <b style={{ color: "var(--gold)", marginBottom: 8 }}>Daumen hoch 2 (Variante)</b>
          <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <LearningDad action="thumbs-up-alt" size="md" mode={activeMode} />
          </div>
          <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>
            Abwechslungsreiche Variante mit breitem Lächeln.
          </span>
        </div>

        {/* Karte 3: Facepalm */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
          <b style={{ color: "var(--red)", marginBottom: 8 }}>Facepalm (Aufmunterung)</b>
          <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <LearningDad action="facepalm" size="md" mode={activeMode} />
          </div>
          <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>
            Verständnisvolles Kopfschütteln bei Fehlern: „Kein Problem – weiter so!“
          </span>
        </div>

        {/* Karte 4: Uhr */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
          <b style={{ color: "var(--cyan)", marginBottom: 8 }}>Uhr / Zeit</b>
          <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <LearningDad action="clock" size="md" mode={activeMode} />
          </div>
          <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>
            Zeigt auf die Armbanduhr: Countdown, Zeitdruck oder Erinnerung.
          </span>
        </div>

        {/* Karten 5–7: Boxen & Wut – einmal abspielende Clips, per Knopf neu gestartet */}
        <ReplayCard action="punch" title="Treffer! (Jab + Cross)" hint="Combo-Treffer: „You hit it“. Spielt einmal." />
        <ReplayCard action="boxing-win" title="Boxsieg" hint="Aufgabe abgeschlossen: Boxer-Siegesserie, spielt einmal." />
        <ReplayCard action="angry" title="Wütend" hint="Knurren mit geballten Fäusten, läuft als Schleife." />
        <ReplayCard action="boxing-complete" title="Boxserie" hint="Lange Boxsequenz (8 s), spielt einmal." />
        <ReplayCard action="applause" title="Applaus" hint="Klatscht begeistert, läuft als Schleife." />

        {/* Tagesziele vom Vortag verfehlt: die vier Clips laufen nacheinander (wütend → Monster → Kampf) */}
        <ReplayCard action="become-angry" title="Verfehlt 1: wird wütend" hint="Ruhig, dann Fäuste und Wutschrei. 5 s, einmal." />
        <ReplayCard action="become-monster" title="Verfehlt 2: Monster" hint="Verwandlung in ein Monster. 10 s, einmal." />
        <ReplayCard action="starts-fight" title="Verfehlt 3: Kampfansage" hint="Zeigt auf dich, ballt die Fäuste. 10 s, einmal." />

        {/* Karte 5: Avatar & Stills */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
          <b style={{ color: "var(--ink)", marginBottom: 8 }}>Avatar & Profil-Badge</b>
          <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center", gap: 12 }}>
            <LearningDad action="avatar" size={80} variant="circle" />
            <LearningDad action="still-thumbs-up" size="sm" />
          </div>
          <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>
            Kompakter kreisförmiger Avatar und Fallback für reduzierte Bewegung.
          </span>
        </div>

        {/* Karte 6: Sprechblasen-Vorschau */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
          <b style={{ color: "var(--lime)", marginBottom: 8 }}>Mit Sprechblase</b>
          <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center" }}>
            <LearningDad
              action="thumbs-up"
              size="sm"
              caption="Ich bin stolz auf dich!"
              subCaption="+2 🪙 verdient"
              mode={activeMode}
            />
          </div>
          <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>
            Integrierte Arcade-Sprechblase mit dynamischem Text.
          </span>
        </div>
      </div>
    </div>
  );
}

/** One preview card; "Nochmal" bumps `playKey` so a one-shot clip restarts from frame 0. */
function ReplayCard({ action, title, hint }: { action: DadAction; title: string; hint: string }) {
  const [run, setRun] = useState(0);
  return (
    <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", textAlign: "center" }}>
      <b style={{ color: "var(--cyan)", marginBottom: 8 }}>{title}</b>
      <div style={{ minHeight: 220, display: "flex", alignItems: "center", justifyContent: "center" }}>
        <LearningDad action={action} size="md" playKey={run} />
      </div>
      <button type="button" className="pill" onClick={() => setRun((n) => n + 1)}>▶ Nochmal</button>
      <span style={{ fontSize: 12, color: "var(--muted)", marginTop: 8 }}>{hint}</span>
    </div>
  );
}
