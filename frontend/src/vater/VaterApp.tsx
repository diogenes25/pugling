import { useEffect, useRef, type ReactNode } from "react";
import { Link, NavLink, Navigate, Route, Routes, useLocation, useNavigate } from "react-router-dom";
import { RemarkWidget } from "../components/RemarkWidget";
import { useAuth } from "../lib/auth";
import {
  homeFor, isNeutralPath, perspective, perspectiveOfPath, perspectivesFor, rememberPerspective,
  rememberedPerspective,
  type Perspective,
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
  // „Erwachsener" = Supervisor (Vater) oder Creator (Lehrer-Konto). Der Unterschied steckt in `role`.
  const adultRole = session?.role === "Supervisor" || session?.role === "Creator" ? session.role : null;
  const wasSignedIn = useRef(adultRole !== null);
  useEffect(() => {
    const signedIn = adultRole !== null;
    const justSignedIn = signedIn && !wasSignedIn.current;
    wasSignedIn.current = signedIn;
    if (!justSignedIn || pathname !== "/vater") return;
    // Ein Lehrer hat nur eine Perspektive – er gehört immer in die Werkstatt, unabhängig vom Gemerkten.
    if (adultRole === "Creator") { navigate(homeFor("Creator"), { replace: true }); return; }
    const remembered = rememberedPerspective();
    if (remembered && remembered !== "betreuen") navigate(perspective(remembered).home, { replace: true });
    // Absichtlich nur an `session` gekoppelt: der Effekt beschreibt den Anmelde-Übergang, nicht jeden Pfadwechsel.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session]);

  if (!session || adultRole === null) return <VaterLogin />;

  /*
   * Ein Lehrer-Konto hat nur die Erstellen-Perspektive. Ruft es eine Betreuungs-Seite auf – per Lesezeichen
   * oder weil es `/vater` eintippt –, führt der Weg in die Werkstatt statt in einen Bereich, dessen
   * Endpunkte ihn ohnehin mit 403 abweisen. Die Rechteprüfung bleibt beim Server; das hier ist nur
   * Höflichkeit gegenüber dem Nutzer.
   *
   * Ausgenommen sind die perspektivlosen Seiten (Konto, Anmerkungen): sie gehören keiner Ebene, und
   * `perspectiveOfPath` fällt für sie auf „Betreuen" zurück – ohne diese Ausnahme käme ein Lehrer nicht
   * an sein eigenes Profil.
   */
  const allowed = perspectivesFor(adultRole);
  const pathKey = perspectiveOfPath(pathname);
  const neutral = isNeutralPath(pathname);
  if (!neutral && !allowed.some((p) => p.key === pathKey)) {
    return <Navigate to={homeFor(adultRole)} replace />;
  }
  // Auf einer perspektivlosen Seite zeigt die Navigation die Heimat des Kontos – nicht „Betreuen" für einen
  // Lehrer, der dort nichts zu suchen hat.
  const active = allowed.some((p) => p.key === pathKey)
    ? perspective(pathKey)
    : perspective(adultRole === "Creator" ? "erstellen" : "betreuen");

  return (
    <div className="app-vater">
      <header className="vater-top">
        {/* Kopfzeile und Bereichs-Navigation liegen in **eigenen** Zeilen: lagen beide in einem Flex,
            schob jeder Umbruch der Navigation den rechten Block (Profil, Abmelden) mit. */}
        <div className="vater-top-row">
          {/* Die Marke nennt die Rolle: ein Lehrer-Konto ist kein Vater-Bereich, auch wenn die Routen
              (bewusst) unter /vater liegen – ein Umzug wäre Kosmetik gegen den Preis aller Lesezeichen. */}
          <span className="brand">{adultRole === "Creator" ? "🎓 Pugling · Lehrer" : "🛠️ Pugling · Vater"}</span>
          <span className="spacer" />
          {/* Die Id ist der Login-Name – sie steht hier, damit sie nicht verloren geht. */}
          {/*
            Der Profil-Link nur für den Vater: `FathersController` ist Supervisor-gegated, ein Lehrer bekäme
            dort 403. Lieber den Namen ohne Link zeigen als eine Tür, die sich nicht öffnet – die
            Selbstverwaltung des Lehrer-Kontos ist noch offen (siehe docs/lehrer-konto-plan.md).
          */}
          {adultRole === "Supervisor"
            ? <NavLink to="/vater/profil" className="muted" style={{ fontSize: 14 }}>👤 {session.name} (#{session.id})</NavLink>
            : <span className="muted" style={{ fontSize: 14 }}>🎓 {session.name} (#{session.id})</span>}
          <button type="button" className="btn ghost inline-btn" onClick={signOut} style={{ width: "auto" }}>Abmelden</button>
        </div>
        {/*
          Der Perspektiven-Umschalter steht ÜBER der Bereichs-Navigation, weil er sie auswechselt: Betreuen,
          Zuweisen, Erstellen (siehe navigation.ts und docs/vater-perspektiven-plan.md). Vorher lagen alle
          zwölf Bereiche beider Rollen gleichzeitig da – „Belohnungen" neben „Lehrwerke".

          Die aktive Perspektive kommt aus dem **Pfad**, nicht aus einem State: sonst öffnete ein
          Lesezeichen die richtige Seite in der falschen Perspektive.
        */}
        <PerspectiveSwitch available={allowed} />
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
function PerspectiveSwitch({ available }: { available: Perspective[] }) {
  const { pathname } = useLocation();
  const activeKey = perspectiveOfPath(pathname);
  // Ein Schalter mit einer Stellung ist Dekoration – ein Lehrer-Konto hat nur die Werkstatt.
  if (available.length < 2) return null;
  return (
    <nav className="perspective-switch" aria-label="Perspektive">
      {available.map((p) => {
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
