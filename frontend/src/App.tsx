import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./lib/auth";
import { Landing } from "./Landing";
import { SohnApp } from "./sohn/SohnApp";
import { VaterApp } from "./vater/VaterApp";

/** Rollen-Weiche: / (Produktseite), /sohn (mobile PWA) und /vater (Web-Admin). */
export default function App() {
  const { session } = useAuth();

  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/sohn/*" element={<SohnApp />} />
      <Route path="/vater/*" element={<VaterApp />} />
      <Route
        path="*"
        element={<Navigate to={session ? (session.role === "Vater" ? "/vater" : "/sohn") : "/"} replace />}
      />
    </Routes>
  );
}
