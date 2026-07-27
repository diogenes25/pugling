import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { AuthProvider } from "./lib/auth";
import { installGlobalErrorCapture } from "./lib/remarks";
import "./index.css";

// Fehler-Ringpuffer für Test-Anmerkungen scharfschalten: JS-Fehler landen ab jetzt im Puffer, damit
// eine spätere Bug-Anmerkung sie mitbringt. Rein passiv – der Puffer wirft nie und ändert kein Verhalten.
installGlobalErrorCapture();

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter
      future={{ v7_startTransition: true, v7_relativeSplatPath: true }}
    >
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>,
);
