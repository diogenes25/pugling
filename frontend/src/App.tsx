import { useState } from "react";
import { SohnView } from "./views/SohnView";
import { VaterView } from "./views/VaterView";

export default function App() {
  const [view, setView] = useState<"sohn" | "vater">("sohn");

  return (
    <div className="app">
      <h1>🐶 Pugling</h1>
      <div className="row" style={{ marginBottom: 16 }}>
        <button className={view === "sohn" ? "" : "secondary"} onClick={() => setView("sohn")}>
          Lernen
        </button>
        <button className={view === "vater" ? "" : "secondary"} onClick={() => setView("vater")}>
          Dashboard
        </button>
      </div>
      {view === "sohn" ? <SohnView /> : <VaterView />}
    </div>
  );
}
