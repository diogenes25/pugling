import { Link, Navigate } from "react-router-dom";
import { useAuth } from "./lib/auth";

/*
 * Produkt-/Landingpage: erklärt Pugling (zwei Rollen, Lernkreislauf, Verfahren) und leitet in die
 * passende Rolle. Bewusst dark, im Arcade-Look der Sohn-App, aber mit ruhigen, breiten Sektionen –
 * damit sie sowohl den steuernden Vater als auch den spielenden Sohn abholt.
 */
export function Landing() {
  const { session } = useAuth();
  if (session) return <Navigate to={session.role === "Supervisor" ? "/vater" : "/sohn"} replace />;

  return (
    <div className="lp">
      <div className="lp-wrap">
        <header className="lp-top">
          <span className="brand"><span aria-hidden="true">🐶</span> Pugling</span>
          <span className="spacer" />
          <Link to="/vater" className="btn ghost inline-btn" style={{ width: "auto", textDecoration: "none" }}><span aria-hidden="true">🛠️</span> Vater-Login</Link>
          <Link to="/sohn" className="btn gold inline-btn" style={{ width: "auto", textDecoration: "none" }}><span aria-hidden="true">🎮</span> Sohn-App</Link>
        </header>

        {/* Hero */}
        <section className="lp-hero">
          <div>
            <span className="pill gold" style={{ display: "inline-block" }}>Lern-App mit Punktesystem</span>
            <h1>Lernen wie ein Match.</h1>
            <p className="lead">
              Pugling verbindet zwei Rollen zu einem Ziel: Der <strong>Vater</strong> stellt den Lernplan
              zusammen und erzwingt den Lernerfolg, der <strong>Sohn</strong> übt mit Punkten, Combos und
              Belohnungen – nach dem bewährten Karteikasten-Prinzip (Leitner).
            </p>
            <div className="lp-cta">
              <Link to="/vater" className="btn" style={{ width: "auto", textDecoration: "none" }}>Als Vater starten</Link>
              <Link to="/sohn" className="btn gold" style={{ width: "auto", textDecoration: "none" }}>Als Sohn spielen</Link>
            </div>
          </div>
          <div className="lp-orb" aria-hidden>
            <span className="face">🐶</span>
            <span className="spark" style={{ top: "16%", left: "18%" }}>⭐</span>
            <span className="spark" style={{ bottom: "18%", right: "16%" }}>🏆</span>
            <span className="spark" style={{ top: "22%", right: "20%" }}>🔥</span>
          </div>
        </section>

        {/* Zwei Rollen */}
        <section className="lp-section">
          <span className="kicker">Das Prinzip</span>
          <h2>Zwei Rollen, ein Ziel</h2>
          <p className="intro">
            Getrennte Zugänge, aufeinander abgestimmt: Der Vater steuert im Web, der Sohn lernt in einer
            spielerischen App auf dem Handy. Die Regeln setzt der Server durch – nicht das Gerät des Kindes.
          </p>
          <div className="lp-grid2">
            <div className="card lp-role vater">
              <div className="emoji" aria-hidden="true">🛠️</div>
              <h3>Vater · steuert &amp; erzwingt</h3>
              <ul>
                <li>Kind anlegen (Name, Klasse, Schulart)</li>
                <li>Lehrplan bauen – geführt per Assistent oder von Hand</li>
                <li>Übungszeit, Test-Hürde &amp; Punkte festlegen</li>
                <li>Fortschritt, Streak und Klassenarbeiten im Blick</li>
              </ul>
            </div>
            <div className="card lp-role sohn">
              <div className="emoji" aria-hidden="true">🎮</div>
              <h3>Sohn · lernt mit Spaß</h3>
              <ul>
                <li>Tagesmission: üben und den Test bestehen</li>
                <li>Punkte, Combos &amp; Zeitfenster-Bonus sammeln</li>
                <li>Missionen, Auszeichnungen und Skins freischalten</li>
                <li>Karteikasten steigt Box für Box auf</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Lernkreislauf */}
        <section className="lp-section">
          <span className="kicker">Der Ablauf</span>
          <h2>So funktioniert der Lernkreislauf</h2>
          <div className="lp-loop">
            <LoopNode ic="🧩" t="1 · Plan" d="Vater stellt Inhalte & Regeln zusammen" />
            <LoopNode ic="✍️" t="2 · Üben" d="Sohn trainiert die fälligen Karten" />
            <LoopNode ic="✅" t="3 · Test" d="Täglicher Abschlusstest, serverbewertet" />
            <LoopNode ic="📈" t="4 · Fortschritt" d="Punkte, Streak & Auswertung für den Vater" />
          </div>
        </section>

        {/* Vater-Tutorial */}
        <section className="lp-section">
          <span className="kicker">Tutorial · Vater</span>
          <h2>In 5 Schritten zum Lehrplan</h2>
          <p className="intro">
            Der <strong>Lehrplan-Assistent</strong> im Vater-Bereich führt genau hier durch. Beispiel:
            ein 14-jähriger Sohn mit Schwächen in Französisch.
          </p>
          <div className="card lp-steps">
            <Step no="1" t="Anmelden" d="Mit Vater-Id und PIN einloggen (Demo: Id 1, PIN 0000)." />
            <Step no="2" t="Kind wählen oder anlegen" d="Name, Geburtsjahr, Klasse (z.B. 8.) und Schulart – die Klasse filtert später passende Übungen." />
            <Step no="3" t="Problemfeld eingrenzen" d="Fach (Französisch), Thema, Ziel (Klassenarbeit / Aufholen / Regelmäßig) und Intensität – daraus schlägt der Assistent Pensum und Test-Stufe vor." />
            <Step no="4" t="Inhalte auswählen" d="Vorhandene Vokabeln aus dem Store übernehmen – passende Katalog-Übungen (z.B. „Découvertes 1, Unité 2“) werden angezeigt." />
            <Step no="5" t="Feinschliff & Erstellen" d="Dauer, Minuten/Tag und Bestehensgrenze anpassen, fertig – der Plan erscheint sofort in der Sohn-App." />
          </div>
          <div className="lp-cta">
            <Link to="/vater" className="btn inline-btn" style={{ width: "auto", textDecoration: "none" }}>Assistent öffnen →</Link>
          </div>
        </section>

        {/* Sohn-Tutorial */}
        <section className="lp-section">
          <span className="kicker">Tutorial · Sohn</span>
          <h2>So spielt der Sohn</h2>
          <div className="card lp-steps">
            <Step gold no="1" t="Einloggen" d="Charakter wählen und PIN eintippen (Demo: Kind 1, PIN 1111)." />
            <Step gold no="2" t="Tagesmission öffnen" d="Die Startseite zeigt, was heute zu tun ist: üben und den Test bestehen." />
            <Step gold no="3" t="Karten üben" d="Vokabel-Karten umdrehen, Antwort geben – richtig steigt im Karteikasten auf, Combos geben Bonus." />
            <Step gold no="4" t="Test bestehen" d="Den täglichen Abschlusstest schaffen, um den Tag zu vollenden und die Streak zu halten." />
            <Step gold no="5" t="Belohnungen holen" d="Punkte einlösen, Missionen abschließen, Auszeichnungen und neue Skins freischalten." />
          </div>
          <div className="lp-cta">
            <Link to="/sohn" className="btn gold inline-btn" style={{ width: "auto", textDecoration: "none" }}>Zur Sohn-App →</Link>
          </div>
        </section>

        {/* Features */}
        <section className="lp-section">
          <span className="kicker">Warum es wirkt</span>
          <h2>Bewährte Mechanik, motivierend verpackt</h2>
          <div className="lp-grid3">
            <Feature emoji="🗂️" t="Leitner-Kasten" d="Fällige Karten steigen bei Erfolg in die nächste Box – Wiederholung genau dann, wenn nötig." />
            <Feature emoji="⏱️" t="Punkte & Zeitfenster" d="Server-autoritative Punkte mit Multiplikator je Tageszeit, Combo- und Schnell-Bonus." />
            <Feature emoji="📅" t="Klassenarbeiten" d="Arbeiten planen, Übungen taggen, gezielt vorbereiten und schwache Themen wiederholen." />
            <Feature emoji="🏅" t="Missionen & Badges" d="Tages- und Wochenziele, Auszeichnungen und Skins halten die Motivation hoch." />
            <Feature emoji="🔒" t="Kein Selbstbetrug" d="Stufe, Zeit und Bewertung erzwingt der Server – das Kind kann sich nicht selbst gutschreiben." />
            <Feature emoji="🧩" t="Viele Verfahren" d="Vokabeln, Lückentext, Zuordnung, Rechnen u.v.m. – ein Katalog mit typisierten Übungen." />
          </div>
        </section>

        {/* Footer-CTA */}
        <section className="lp-foot">
          <h2>Bereit, den Lernkreislauf zu starten?</h2>
          <div className="lp-cta" style={{ justifyContent: "center" }}>
            <Link to="/vater" className="btn" style={{ width: "auto", textDecoration: "none" }}><span aria-hidden="true">🛠️</span> Ich bin der Vater</Link>
            <Link to="/sohn" className="btn gold" style={{ width: "auto", textDecoration: "none" }}><span aria-hidden="true">🎮</span> Ich bin der Sohn</Link>
          </div>
          <p className="muted">Pugling · API-First Lern-App · Vater steuert, Sohn lernt.</p>
        </section>
      </div>
    </div>
  );
}

function LoopNode({ ic, t, d }: { ic: string; t: string; d: string }) {
  return (
    <div className="card node">
      <div className="ic" aria-hidden="true">{ic}</div>
      <div className="t">{t}</div>
      <div className="d">{d}</div>
    </div>
  );
}

function Step({ no, t, d, gold }: { no: string; t: string; d: string; gold?: boolean }) {
  return (
    <div className={`lp-step${gold ? " gold" : ""}`}>
      <span className="no">{no}</span>
      <div>
        <div className="t">{t}</div>
        <div className="d">{d}</div>
      </div>
    </div>
  );
}

function Feature({ emoji, t, d }: { emoji: string; t: string; d: string }) {
  return (
    <div className="card lp-feature">
      <div className="emoji" aria-hidden="true">{emoji}</div>
      <h3>{t}</h3>
      <p>{d}</p>
    </div>
  );
}
