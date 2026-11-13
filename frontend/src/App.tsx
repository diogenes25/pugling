import { lazy, Suspense } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./lib/auth";
import { Landing } from "./Landing";
import { RemarkContextProvider } from "./lib/remarkContext";

// Lazy: eine Sitzung braucht nie beide Bündel gleichzeitig (entweder Vater- oder Sohn-Seite).
const SohnApp = lazy(() => import("./sohn/SohnApp").then((m) => ({ default: m.SohnApp })));
const VaterApp = lazy(() => import("./vater/VaterApp").then((m) => ({ default: m.VaterApp })));
// PROTOTYP (Wegwerf, nur DEV): Lernpfad nach Duolingo/Snipwise, siehe src/prototyp/PfadPrototyp.tsx.
const PfadPrototyp = lazy(() => import("./prototyp/PfadPrototyp").then((m) => ({ default: m.PfadPrototyp })));

/** Rollen-Weiche: / (Produktseite), /sohn (mobile PWA) und /vater (Web-Admin). */
export default function App() {
  const { session } = useAuth();

  return (
    // Der Kontext-Speicher der Test-Anmerkungen liegt um die Routen, damit jeder Screen beitragen kann.
    // Er rendert nichts und hält nur Refs – ohne Widget (Etappe 3) ist er wirkungslos.
    <RemarkContextProvider>
      <Suspense fallback={null}>
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/sohn/*" element={<SohnApp />} />
          <Route path="/vater/*" element={<VaterApp />} />
          {import.meta.env.DEV && <Route path="/prototyp/pfad" element={<PfadPrototyp />} />}
          <Route
            path="*"
            element={<Navigate to={session ? (session.role === "Supervisor" ? "/vater" : "/sohn") : "/"} replace />}
          />
        </Routes>
      </Suspense>
    </RemarkContextProvider>
  );
}
