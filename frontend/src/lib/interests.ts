/**
 * Der Slug eines Interessen-Schlagworts – ein **Spiegel** der Server-Regel
 * (`backend/Pugling.Api/Data/InterestSlug.cs`). Der Server bleibt die Autorität: er legt an, löst
 * Synonyme auf und antwortet mit der kanonischen Menge. Diese Kopie existiert nur, damit das Vater-UI
 * eine Dublette *vor* dem Speichern erkennt.
 *
 * Ohne sie verglich der Editor nur `toLowerCase()` – „Brawl Stars" und „brawl-stars" galten damit als
 * zwei Einträge, die serverseitig auf denselben Tag fallen. Der PUT lief dann in den Unique-Index und
 * der Vater sah einen nackten Fehler statt eines Hinweises.
 *
 * Regel: Kleinschreibung, ß→ss, Diakritika weg, alles Nicht-Alphanumerische zu einem Bindestrich
 * verdichtet („Brawl Stars!" → `brawl-stars`).
 */
export function interestSlug(text: string): string {
  const normalized = text
    .toLowerCase()
    .replace(/ß/g, "ss")
    .normalize("NFD")
    .replace(/\p{Mn}/gu, "");

  let slug = "";
  for (const ch of normalized) {
    if (/[\p{L}\p{N}]/u.test(ch)) slug += ch;
    // Trennzeichen nur anhängen, wenn schon Inhalt da ist – verhindert führende und doppelte Bindestriche.
    else if (slug.length > 0 && !slug.endsWith("-")) slug += "-";
  }
  return slug.replace(/-+$/, "");
}
