import { useEffect, useState } from "react";
import { useChromeT } from "../lib/chromeText";

/*
 * Die Stimme des Geräts für eine Sprache – oder `null`, solange keine passende da ist. Die Stimmen laden asynchron (Chrome
 * liefert beim ersten `getVoices()` oft eine leere Liste), darum wird auf `voiceschanged` neu gelesen.
 */
function useVoiceFor(lang: string): SpeechSynthesisVoice | null {
  const [voices, setVoices] = useState<SpeechSynthesisVoice[]>([]);
  useEffect(() => {
    const synth = typeof window !== "undefined" ? window.speechSynthesis : undefined;
    if (!synth) return;
    const load = () => setVoices(synth.getVoices());
    load();
    synth.addEventListener?.("voiceschanged", load);
    return () => synth.removeEventListener?.("voiceschanged", load);
  }, []);
  const wanted = lang.toLowerCase();
  // „fr“ trifft „fr“, „fr-FR“ und „fr_CA“, aber nicht eine Sprache, deren Code nur mit denselben Buchstaben beginnt.
  return voices.find((v) => {
    const l = v.lang.toLowerCase();
    return l === wanted || l.startsWith(`${wanted}-`) || l.startsWith(`${wanted}_`);
  }) ?? null;
}

/**
 * Vorlese-Knopf (B-252): liest den Text der Karte mit der Sprachausgabe des Geräts in `lang` vor – nur auf Antippen,
 * nie von selbst (eine plötzlich sprechende Stimme im Klassenzimmer wäre eine Störung). Ob und in welcher Sprache
 * vorgelesen werden darf, entscheidet der Server (`speechLang`): nur das Gefragte, nie die Lösung.
 *
 * Ohne passende Stimme gibt es keinen Knopf – eine deutsche Stimme, die „la gare" liest, lehrt eine falsche Aussprache.
 * Wie beim `AudioButton` ist der sichtbare Text der zugängliche Name (WCAG 2.5.3).
 */
export function SpeakButton({ text, lang }: { text: string; lang: string }) {
  const t = useChromeT();
  const voice = useVoiceFor(lang);
  // Eine laufende Äußerung endet mit der Karte: sonst spräche das alte Wort über die nächste Karte hinweg.
  useEffect(() => () => window.speechSynthesis?.cancel(), [text]);
  if (!voice) return null;

  function speak() {
    const synth = window.speechSynthesis;
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = lang;
    utterance.voice = voice;
    // Ein zweites Antippen beginnt von vorn, statt sich hinter die laufende Äußerung zu reihen.
    synth.cancel();
    synth.speak(utterance);
  }

  return (
    <div className="row" style={{ gap: 8, alignItems: "center" }}>
      <button type="button" className="btn ghost" style={{ width: "auto" }} onClick={speak}>{t("play.speak")}</button>
    </div>
  );
}
