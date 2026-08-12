import { Link } from "react-router-dom";
import { api } from "../lib/api";
import { useAuth } from "../lib/auth";
import { useAsync } from "../lib/useAsync";
import type { Paged, ExerciseSummary, SubjectResponse } from "../lib/types";

/**
 * Startseite der Perspektive **Erstellen** – die Werkstatt des Lehrers.
 *
 * Sie existiert, weil der Creator vorher keinen Ausgangspunkt hatte: die sieben Autoren-Bereiche standen
 * gleichrangig in der Navigation, ohne zu verraten, welcher der Einstieg ist und wie sie zusammenhängen
 * (eine Übung *braucht* Katalog und Store, ein Fachlehrer-Profil ist optional). Siehe
 * docs/vater-perspektiven-plan.md.
 *
 * Bewusst **ohne Kind-Bezug**: wer hier arbeitet, baut Stoff auf Vorrat. Das Zuweisen ist eine eigene
 * Perspektive – sonst schleicht sich das Betreuen in die Autorenarbeit zurück.
 *
 * **Kein kontextfreier Weg zum Anlegen.** Seit B-106 hängt jede Übung zwingend an einer Lehrwerk-Unit, und
 * ein „+ Neue Übung" von hier führte in ein Formular, dessen erstes Pflichtfeld leer war – die Auswahl traf
 * man dann im Formular statt dort, wo man den Stoff ohnehin sieht. Der Einstieg führt darum über die Unit
 * (📕 Lehrwerke → „+ Übung"); die Bestandsliste behält ihren eigenen Knopf, weil sie den Fach/Reihe/Unit-
 * Filter schon mitreicht.
 */
export function VaterInhalte() {
  // Ein Lehrer-Konto hat keine Zuweisen-Perspektive – ein Link dorthin würde ihn zurückwerfen.
  const { session } = useAuth();
  const isTeacher = session?.role === "Creator";
  // Zwei Zahlen genügen als Standortbestimmung („habe ich überhaupt schon etwas?"). Mehr wäre eine
  // Auswertung, und die gehört nicht in eine Werkstatt.
  const subjects = useAsync<SubjectResponse[]>(() => api.subjects(), []);
  const mine = useAsync<Paged<ExerciseSummary>>(() => api.searchExercises({ mineOnly: true, take: 1 }), []);

  return (
    <>
      <h2 className="h-section">Werkstatt</h2>
      <p className="sub">
        {isTeacher
          ? "Hier entsteht der Stoff – unabhängig von einem Kind. Zuweisen tun die Eltern in ihren eigenen Lehrplänen; du gibst dein Material nur frei."
          : <>Hier entsteht der Stoff – unabhängig von einem Kind. Zugewiesen wird er später unter{" "}
            <Link to="/vater/plaene">Zuweisen</Link>.</>}
      </p>

      <section className="card">
        <div className="row" style={{ gap: 18, flexWrap: "wrap" }}>
          <span><strong>{mine.data?.total ?? "…"}</strong> <span className="muted">eigene Übungen</span></span>
          <span><strong>{subjects.data?.length ?? "…"}</strong> <span className="muted">Fächer im Katalog</span></span>
          <Link to="/vater/exercises" style={{ marginLeft: "auto" }}>alle Übungen verwalten →</Link>
        </div>
      </section>

      {/* Die Reihenfolge ist der Arbeitsweg, nicht das Alphabet: erst das Werkstück, dann seine Bausteine,
          dann das Beiwerk. */}
      <h3 className="h-section" style={{ fontSize: 16 }}>Das Werkstück</h3>
      <div className="vater-grid">
        {/* Die Lehrwerke stehen hier und nicht mehr unter „Materialkunde": eine Unit ist keine Zutat, die
            man auch weglassen kann – sie ist der Ort, an dem eine Übung entsteht. */}
        <HubCard to="/vater/lehrwerke" icon="📕" title="Neue Übung: erst die Unit"
          text="Reihe aufklappen, Unit wählen, Übung anlegen – so startet das Formular schon im richtigen Stoff." />
        <HubCard to="/vater/exercises" icon="📚" title="Übungen"
          text="Der Bestand: suchen, ausprobieren, bearbeiten, löschen – und mit gesetztem Filter auch anlegen." />
      </div>

      <h3 className="h-section" style={{ fontSize: 16 }}>Die Bausteine</h3>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        Sie tragen <strong>mehrere</strong> Übungen. Einmal gepflegt, überall nutzbar – darum liegen sie
        neben dem Anlegen und nicht darin.
      </p>
      <div className="vater-grid">
        <HubCard to="/vater/katalog" icon="🗂️" title="Katalog"
          text="Fach → Art. Der Rahmen jeder Übung – und er ist unter allen Vätern geteilt." />
        <HubCard to="/vater/vocab" icon="🔤" title="Vokabeln"
          text="Der Wortspeicher. Dieselbe Vokabel bleibt über Übungen hinweg verknüpft, samt Lernstand." />
        <HubCard to="/vater/lueckentexte" icon="📄" title="Lückentexte"
          text="Trägertexte als Lerngrundlage – ein Satz, mehrere Übungen." />
        <HubCard to="/vater/media" icon="🖼️" title="Bilder"
          text="Ein Motiv, viele Bilder. Das Kind sieht durchgehend dasselbe – die Bildkonstanz ist der Merkeffekt." />
      </div>

      <h3 className="h-section" style={{ fontSize: 16 }}>Materialkunde</h3>
      <p className="muted" style={{ marginTop: 0, fontSize: 13 }}>
        Optional, aber der Unterschied zwischen „irgendeine Übung" und „passt zu Unit 3": Themen, Grammatik
        und Wortschatz stehen an der Unit, dein Unterrichtsstil im Profil.
      </p>
      <div className="vater-grid">
        <HubCard to="/vater/fachlehrer" icon="🎓" title="Fachlehrer"
          text="Dein Profil als Fachlehrer: Fach, Schulart, Klassenstufen – und wie du unterrichtest." />
      </div>
    </>
  );
}

/** Eine Kachel der Werkstatt: Ziel, Symbol, Name und der eine Satz, der sagt, wofür es da ist. */
function HubCard({ to, icon, title, text }: { to: string; icon: string; title: string; text: string }) {
  return (
    // Die ganze Kachel ist der Link (ein Ziel, eine Trefferfläche) – ein zweiter Link darin wäre eine
    // Falle für Tastatur und Screenreader.
    <Link to={to} className="card hub-card" style={{ textDecoration: "none" }}>
      <span className="hub-card-title"><span aria-hidden="true">{icon}</span> {title}</span>
      <span className="muted" style={{ fontSize: 13 }}>{text}</span>
    </Link>
  );
}
