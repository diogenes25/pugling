import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "../lib/auth";
import { VaterLogin } from "./VaterLogin";
import { VaterDashboard } from "./VaterDashboard";
import { VaterVocab } from "./VaterVocab";
import { VaterPlanCreate } from "./VaterPlanCreate";
import { VaterPlanDetail } from "./VaterPlanDetail";

export function VaterApp() {
  const { session, signOut } = useAuth();
  if (!session || session.role !== "Vater") return <VaterLogin />;

  return (
    <div className="app-vater">
      <header className="vater-top">
        <span className="brand">🛠️ Pugling · Vater</span>
        <nav>
          <NavLink to="/vater" end>Übersicht</NavLink>
          <NavLink to="/vater/vocab">Vokabeln</NavLink>
          <NavLink to="/vater/plan/new">Neuer Plan</NavLink>
        </nav>
        <span className="spacer" />
        <span className="muted" style={{ fontSize: 14 }}>{session.name} (#{session.id})</span>
        <button type="button" className="btn ghost inline-btn" onClick={signOut} style={{ width: "auto" }}>Abmelden</button>
      </header>

      <main className="vater-main">
        <Routes>
          <Route index element={<VaterDashboard />} />
          <Route path="vocab" element={<VaterVocab />} />
          <Route path="plan/new" element={<VaterPlanCreate />} />
          <Route path="plan/:planId" element={<VaterPlanDetail />} />
          <Route path="*" element={<Navigate to="/vater" replace />} />
        </Routes>
      </main>
    </div>
  );
}
