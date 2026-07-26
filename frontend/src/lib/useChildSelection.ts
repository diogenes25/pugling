import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import type { ChildResponse } from "./types";

/**
 * Welches Kind eine kind-bezogene Seite gerade zeigt.
 *
 * Die Vorauswahl kommt aus `?childId=` – so führen die Links vom Kind-Hub (Lernstand, Shop, Konto …) beim
 * richtigen Kind hinein. Ohne den Parameter bleibt es beim ersten Kind. Eine Auswahl im Pulldown wandert
 * zurück in die URL, damit Zurück-Taste und Neuladen sie behalten.
 *
 * Eine Id aus der URL wird gegen die geladene Liste geprüft: eine fremde oder gelöschte Id würde sonst
 * Anfragen auslösen, die der Server (zu Recht) mit 403/404 abweist.
 */
export function useChildSelection(children: ChildResponse[] | null | undefined) {
  const [params, setParams] = useSearchParams();
  const [picked, setPicked] = useState<number | null>(null);

  const fromUrl = Number(params.get("childId")) || null;
  const known = (id: number | null) => (id != null && children?.some((c) => c.id === id) ? id : null);

  const activeChild = known(picked) ?? known(fromUrl) ?? children?.[0]?.id ?? null;

  function select(id: number) {
    setPicked(id);
    const next = new URLSearchParams(params);
    next.set("childId", String(id));
    // `replace`, damit der Kindwechsel keine Historie aufbaut – „zurück" soll zur vorigen Seite führen.
    setParams(next, { replace: true });
  }

  return { activeChild, select };
}
