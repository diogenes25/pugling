/**
 * Wie der Server vergleicht (`StageMechanics.Normalize`): ein Apostroph (B-440), Schlusszeichen `.?!…` weg, außer die
 * Antwort besteht nur daraus (B-457), dann trimmen, klein, Mehrfach-Leerzeichen zusammen. Gehalten von der gemeinsamen
 * Fallliste `docs/fallliste-antwortvergleich.json` (B-493); gelesen vom Vater-Web und von der Lernkarte (B-598).
 */
export const normalizeAnswer = (s: string) => {
  // U+FEFF fällt weg, bevor `\s` es als Trenner sähe – der Server entfernt es (B-501, Fallliste B-493).
  const folded = s.split(String.fromCharCode(0xfeff)).join("").replace(/[’‘ʼ´`]/g, "'").trim();
  const stripped = folded.replace(/[\s.?!…]+$/, "");
  return (stripped || folded).toLowerCase().split(/\s+/).filter(Boolean).join(" ");
};
