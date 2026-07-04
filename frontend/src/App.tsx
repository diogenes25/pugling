import { Navigate, Route, Routes, useNavigate } from "react-router-dom";
import { useAuth } from "./lib/auth";
import { SohnApp } from "./sohn/SohnApp";
import { VaterApp } from "./vater/VaterApp";

/** Rollen-Weiche: /sohn (mobile PWA) und /vater (Web-Admin). */
export default function App() {
  const { session } = useAuth();

  return (
    <Routes>
      <Route path="/" element={<RoleChooser />} />
      <Route path="/sohn/*" element={<SohnApp />} />
      <Route path="/vater/*" element={<VaterApp />} />
      <Route
        path="*"
        element={<Navigate to={session ? (session.role === "Vater" ? "/vater" : "/sohn") : "/"} replace />}
      />
    </Routes>
  );
}

/** Startseite: bereits angemeldete Rolle direkt weiterleiten, sonst Rollenwahl. */
function RoleChooser() {
  const { session } = useAuth();
  const nav = useNavigate();
  if (session) return <Navigate to={session.role === "Vater" ? "/vater" : "/sohn"} replace />;

  return (
    <div className="app-sohn">
      <div className="center-col" style={{ textAlign: "center" }}>
        <div>
          <div className="pill gold" style={{ display: "inline-block" }}>Pugling</div>
          <h1 className="screen-title" style={{ fontSize: 34, marginTop: 10 }}>Wer bist du?</h1>
          <p className="sub">Wähle deinen Zugang.</p>
        </div>
        <button className="btn gold" onClick={() => nav("/sohn")}>🎮 Ich bin der Sohn</button>
        <button className="btn ghost" onClick={() => nav("/vater")}>🛠️ Ich bin der Vater</button>
      </div>
    </div>
  );
}
