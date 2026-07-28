import { useEffect, useRef, type ReactNode } from "react";
import { Link, NavLink, Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { RemarkWidget } from "../components/RemarkWidget";
import { useAuth } from "../lib/auth";
import {
  PERSPECTIVES, perspective, perspectiveOfPath, rememberPerspective, rememberedPerspective,
} from "./navigation";
import { VaterLogin } from "./VaterLogin";
import { VaterDashboard } from "./VaterDashboard";
import { VaterPlaene } from "./VaterPlaene";
import { VaterInhalte } from "./VaterInhalte";
import { VaterVocab } from "./VaterVocab";
import { VaterRewards } from "./VaterRewards";
import { VaterShop } from "./VaterShop";
import { VaterKonto } from "./VaterKonto";
import { VaterClassTests } from "./VaterClassTests";
import { VaterExercises } from "./VaterExercises";
import { VaterExerciseCreate } from "./VaterExerciseCreate";
import { VaterKatalog } from "./VaterKatalog";
import { VaterLueckentexte } from "./VaterLueckentexte";
import { VaterPlanCreate } from "./VaterPlanCreate";
import { VaterPlanDetail } from "./VaterPlanDetail";
import { VaterWizard } from "./VaterWizard";
import { VaterMedia } from "./VaterMedia";
import { VaterLehrwerke } from "./VaterLehrwerke";
import { VaterFachlehrer } from "./VaterFachlehrer";
import { VaterKind } from "./VaterKind";
import { VaterProfil } from "./VaterProfil";
import { VaterLernstand } from "./VaterLernstand";
import { VaterZiele } from "./VaterZiele";
import { VaterAnmerkungen } from "./VaterAnmerkungen";

export function VaterApp() {
  const { session, signOut } = useAuth();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  // Vor dem Login gibt es keine Perspektive – die Hooks müssen aber laufen, bevor wir aussteigen.
  const active = perspective(perspectiveOfPath(pathname));

  /*
   * Nach dem **Anmelden** in die zuletzt bewusst gewählte Perspektive führen: ein Lehrer landet in seiner
   * Werkstatt statt jedes Mal in der Vater-Sicht.
   *
   * Bewusst nur bei diesem einen Übergang (`wasSignedIn`) und nur auf `/vater`: ein Sprung bei *jedem*
   * Besuch von `/vater` würde den Umschalter selbst unbenutzbar machen (Klick auf „Betreuen" → sofort
   * zurückgeworfen) und die Rückwege aus dem Kind-Hub kapern.
   *
   * Der Ref startet auf dem **Anfangszustand**, nicht auf `false`: `AuthProvider` stellt die Sitzung
   * synchron aus dem Speicher her (`useState(load)` in lib/auth.tsx), sie ist im ersten Render also schon
   * da. Mit `false` als Startwert hätte jedes *Neuladen* von `/vater` wie eine frische Anmeldung gezählt –
   * ein Vater, der einmal „Erstellen" gewählt hat, käme nie wieder auf seine Startseite und könnte sie
   * auch nicht als Lesezeichen halten.
   */
  const wasSignedIn = useRef(!!session && session.role === "Supervisor");
  useEffect(() => {
    const signedIn = !!session && session.role === "Supervisor";
    const justSignedIn = signedIn && !wasSignedIn.current;
    wasSignedIn.current = signedIn;
    if (!justSignedIn || pathname !== "/vater") return;
    const remembered = rememberedPerspective();
    if (remembered && remembered !== "betreuen") navigate(perspective(remembered).home, { replace: true });
    // Absichtlich nur an `session` gekoppelt: der Effekt beschreibt den Anmelde-Übergang, nicht jeden Pfadwechsel.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session]);

  if (!session || session.role !== "Supervisor") return <VaterLogin />;

  return (
    <div className="app-vater">
      <header className="vater-top">
        {/* Kopfzeile und Bereichs-Navigation liegen in **eigenen** Zeilen: lagen beide in einem Flex,
            schob jeder Umbruch der Navigation den rechten Block (Profil, Abmelden) mit. */}
        <div className="vater-top-row">
          <span className="brand">🛠️ Pugling · Vater</span>
          <span className="spacer" />
          {/* Die Id ist der Login-Name – sie steht hier, damit sie nicht verloren geht. */}
          <NavLink to="/vater/profil" className="muted" style={{ fontSize: 14 }}>👤 {session.name} (#{session.id})</NavLink>
          <button type="button" className="btn ghost inline-btn" onClick={signOut} style={{ width: "auto" }}>Abmelden</button>
        </div>
        {/*
          Der Perspektiven-Umschalter steht ÜBER der Bereichs-Navigation, weil er sie auswechselt: Betreuen,
          Zuweisen, Erstellen (siehe navigation.ts und docs/vater-perspektiven-plan.md). Vorher lagen alle
          zwölf Bereiche beider Rollen gleichzeitig da – „Belohnungen" neben „Lehrwerke".

          Die aktive Perspektive kommt aus dem **Pfad**, nicht aus einem State: sonst öffnete ein
          Lesezeichen die richtige Seite in der falschen Perspektive.
        */}
        <PerspectiveSwitch />
        <nav className="vater-nav" aria-label={`Bereiche: ${active.label}`}>
          {active.entries.map((e) => (
            <NavLink key={e.to} to={e.to} end={e.end}>{e.label}</NavLink>
          ))}
          {/* Werkzeug für die Entwicklung, wie das Erfassungs-Widget: im Prod-Bundle wegoptimiert. Es
              gehört zu keiner Perspektive und steht darum abgesetzt am Ende. */}
          {import.meta.env.DEV && (
            <NavGroup label="Entwicklung">
              <NavLink to="/vater/anmerkungen">🐞 Anmerkungen</NavLink>
            </NavGroup>
          )}
        </nav>
      </header>

      <main className="vater-main">
        <Routes>
          {/* Je Perspektive eine Startseite. Vorher trug das Dashboard alle drei Anliegen: „Heute" und
              „Kinder" (Betreuen) neben „Lehrpläne" (Zuweisen). */}
          <Route index element={<VaterDashboard />} />
          <Route path="plaene" element={<VaterPlaene />} />
          <Route path="inhalte" element={<VaterInhalte />} />
          <Route path="wizard" element={<VaterWizard />} />
          <Route path="exercises" element={<VaterExercises />} />
          {/* Anlegen ist ein abgeschlossener Vorgang, keine Daueraufgabe – eigene Route, aber bewusst
              KEIN Nav-Eintrag: der Weg führt über „+ Neue Übung" in der Verwaltung (Anmerkung 11). */}
          <Route path="exercises/neu" element={<VaterExerciseCreate />} />
          {/* Bausteine des Katalogs – eigene Orte, weil sie mehrere Übungen tragen (Anmerkung 12). */}
          <Route path="katalog" element={<VaterKatalog />} />
          <Route path="lueckentexte" element={<VaterLueckentexte />} />
          <Route path="vocab" element={<VaterVocab />} />
          <Route path="media" element={<VaterMedia />} />
          <Route path="lehrwerke" element={<VaterLehrwerke />} />
          <Route path="fachlehrer" element={<VaterFachlehrer />} />
          <Route path="kind/:childId" element={<VaterKind />} />
          <Route path="kind/:childId/lernstand" element={<VaterLernstand />} />
          <Route path="kind/:childId/ziele" element={<VaterZiele />} />
          <Route path="profil" element={<VaterProfil />} />
          <Route path="rewards" element={<VaterRewards />} />
          <Route path="shop" element={<VaterShop />} />
          <Route path="konto" element={<VaterKonto />} />
          <Route path="class-tests" element={<VaterClassTests />} />
          <Route path="plan/new" element={<VaterPlanCreate />} />
          <Route path="plan/:planId" element={<VaterPlanDetail />} />
          {import.meta.env.DEV && <Route path="anmerkungen" element={<VaterAnmerkungen />} />}
          <Route path="*" element={<Navigate to="/vater" replace />} />
        </Routes>
      </main>

      {/* Anmerkungen beim Testen – nur im Dev-Modus. Datenmodell und API sind produktreif, allein die
          Einblendung ist gegated: Freischalten wäre später das Streichen dieser einen Bedingung. */}
      {import.meta.env.DEV && <RemarkWidget />}
    </div>
  );
}

/**
 * Der Umschalter zwischen den drei Perspektiven.
 *
 * Bewusst **Links**, keine Knöpfe: jede Perspektive hat eine Startseite, also ist der Wechsel eine
 * Navigation und gehört in die Historie (Zurück muss zurückführen). Die aktive trägt `aria-current="page"`
 * – der Vergleich läuft über die aus dem Pfad abgeleitete Perspektive, nicht über den Link selbst, denn
 * auf `/vater/kind/3` ist keine der drei Startseiten aktiv und „Betreuen" muss es trotzdem sein.
 */
function PerspectiveSwitch() {
  const { pathname } = useLocation();
  const activeKey = perspectiveOfPath(pathname);
  return (
    <nav className="perspective-switch" aria-label="Perspektive">
      {PERSPECTIVES.map((p) => {
        const current = p.key === activeKey;
        return (
          <Link key={p.key} to={p.home} title={p.purpose}
            className={`perspective${current ? " active" : ""}`}
            aria-current={current ? "page" : undefined}
            // Nur der Klick hier gilt als Entscheidung – siehe rememberPerspective.
            onClick={() => rememberPerspective(p.key)}>
            <span aria-hidden="true">{p.icon}</span> {p.label}
          </Link>
        );
      })}
    </nav>
  );
}

/**
 * Eine benannte Gruppe der Bereichs-Navigation.
 *
 * Der Name steht sichtbar davor **und** als `aria-label` an der `role="group"`; die sichtbare
 * Beschriftung ist darum `aria-hidden`, sonst nennte ein Screenreader sie zweimal. Die Gruppe ist
 * selbst ein Flex **ohne** Umbruch: so bricht die Navigation zwischen Gruppen um statt mitten in einer.
 */
function NavGroup({ label, children }: { label: string; children: ReactNode }) {
  return (
    <span className="nav-group" role="group" aria-label={label}>
      <span className="nav-group-label" aria-hidden="true">{label}</span>
      {children}
    </span>
  );
}
