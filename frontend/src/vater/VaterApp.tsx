import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { RemarkWidget } from "../components/RemarkWidget";
import { useAuth } from "../lib/auth";
import { VaterLogin } from "./VaterLogin";
import { VaterDashboard } from "./VaterDashboard";
import { VaterVocab } from "./VaterVocab";
import { VaterRewards } from "./VaterRewards";
import { VaterShop } from "./VaterShop";
import { VaterKonto } from "./VaterKonto";
import { VaterClassTests } from "./VaterClassTests";
import { VaterExercises } from "./VaterExercises";
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
  if (!session || session.role !== "Supervisor") return <VaterLogin />;

  return (
    <div className="app-vater">
      <header className="vater-top">
        <span className="brand">🛠️ Pugling · Vater</span>
        <nav>
          <NavLink to="/vater" end>Übersicht</NavLink>
          <NavLink to="/vater/wizard">🧭 Assistent</NavLink>
          <NavLink to="/vater/exercises">📚 Übungen</NavLink>
          <NavLink to="/vater/lehrwerke">📕 Lehrwerke</NavLink>
          <NavLink to="/vater/fachlehrer">🎓 Fachlehrer</NavLink>
          <NavLink to="/vater/vocab">Vokabeln</NavLink>
          <NavLink to="/vater/media">🖼️ Bilder</NavLink>
          <NavLink to="/vater/rewards">🏆 Belohnungen</NavLink>
          <NavLink to="/vater/shop">🛒 Shop</NavLink>
          <NavLink to="/vater/konto">💰 Kontostand</NavLink>
          <NavLink to="/vater/class-tests">📝 Klassenarbeiten</NavLink>
          <NavLink to="/vater/plan/new">Neuer Plan</NavLink>
          {/* Werkzeug für die Entwicklung, wie das Erfassungs-Widget: im Prod-Bundle wegoptimiert. */}
          {import.meta.env.DEV && <NavLink to="/vater/anmerkungen">📝 Anmerkungen</NavLink>}
        </nav>
        <span className="spacer" />
        {/* Die Id ist der Login-Name – sie steht hier, damit sie nicht verloren geht. */}
        <NavLink to="/vater/profil" className="muted" style={{ fontSize: 14 }}>👤 {session.name} (#{session.id})</NavLink>
        <button type="button" className="btn ghost inline-btn" onClick={signOut} style={{ width: "auto" }}>Abmelden</button>
      </header>

      <main className="vater-main">
        <Routes>
          <Route index element={<VaterDashboard />} />
          <Route path="wizard" element={<VaterWizard />} />
          <Route path="exercises" element={<VaterExercises />} />
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
