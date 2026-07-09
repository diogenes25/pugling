import { NavLink, Navigate, Route, Routes } from "react-router-dom";
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

export function VaterApp() {
  const { session, signOut } = useAuth();
  if (!session || session.role !== "Vater") return <VaterLogin />;

  return (
    <div className="app-vater">
      <header className="vater-top">
        <span className="brand">🛠️ Pugling · Vater</span>
        <nav>
          <NavLink to="/vater" end>Übersicht</NavLink>
          <NavLink to="/vater/wizard">🧭 Assistent</NavLink>
          <NavLink to="/vater/exercises">📚 Übungen</NavLink>
          <NavLink to="/vater/vocab">Vokabeln</NavLink>
          <NavLink to="/vater/rewards">🏆 Belohnungen</NavLink>
          <NavLink to="/vater/shop">🛒 Shop</NavLink>
          <NavLink to="/vater/konto">💰 Konto</NavLink>
          <NavLink to="/vater/class-tests">📝 Klassenarbeiten</NavLink>
          <NavLink to="/vater/plan/new">Neuer Plan</NavLink>
        </nav>
        <span className="spacer" />
        <span className="muted" style={{ fontSize: 14 }}>{session.name} (#{session.id})</span>
        <button type="button" className="btn ghost inline-btn" onClick={signOut} style={{ width: "auto" }}>Abmelden</button>
      </header>

      <main className="vater-main">
        <Routes>
          <Route index element={<VaterDashboard />} />
          <Route path="wizard" element={<VaterWizard />} />
          <Route path="exercises" element={<VaterExercises />} />
          <Route path="vocab" element={<VaterVocab />} />
          <Route path="rewards" element={<VaterRewards />} />
          <Route path="shop" element={<VaterShop />} />
          <Route path="konto" element={<VaterKonto />} />
          <Route path="class-tests" element={<VaterClassTests />} />
          <Route path="plan/new" element={<VaterPlanCreate />} />
          <Route path="plan/:planId" element={<VaterPlanDetail />} />
          <Route path="*" element={<Navigate to="/vater" replace />} />
        </Routes>
      </main>
    </div>
  );
}
