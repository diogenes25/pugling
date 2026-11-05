---
tags: [typ/protokoll, bereich/pm]
aliases: [PM-Sitzung 2026-11-05, Lauf T]
---

# PM-Sitzung: Backlog-Lauf nach der Delegation (Lauf T)

**Datum:** 2026-11-05  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** die 37 autonom bearbeitbaren Stories aus der [Backlog-Sichtung vom selben Tag](pm-sitzung-2026-11-05-delegation.md)
in Sprints abarbeiten, bis ein Limit greift.

**Auftrag:** im Dialog („Ja, loslegen"), nach der Delegation „übernimm meine Position und entscheide nach bestem Wissen und
Gewissen". Produktfragen, die ein Bau aufwirft, entscheidet Claude und protokolliert sie als „Entscheidung im Auftrag des
Nutzers". Sonst gelten die Freigaben 2–10 aus [nachtlauf.md](nachtlauf.md): Fünfer-Zähler je Sprint, Retro schlägt nur vor,
Commits selbst, Push beim Nutzer.

## Sprint 1

**Sprint-Ziel (Creator und Sohn):** „Wer auf einer Liste oder in einem Formular etwas auslöst, sieht danach, was passiert ist,
und verliert seinen Platz nicht – kein stummer Knopf, keine unsichtbare Bestätigung, kein ‚keine' während noch geladen wird."

**Umfang:** B-231, B-232, B-233, B-234 (Paket NP-14, alle am 2026-10-23 vom Nutzer entschieden und freigegeben). Verfeinert in
diesem Sprint von `idee` bis `geschaetzt`; jede XS bis S, reines Frontend, keine Migration, kein Vertragsbruch.

**Entwickler-Brief:** Quelle der Wahrheit ist jeweils die Oberfläche selbst (keine API-Änderung). B-234 bekommt einen
`pending`-Zustand an `CategorySection` (`data === null || stale`); B-232 lässt die Bedingung `!meldung.ok` am Scroll-Effekt
fallen; B-233 sperrt die Speichern-Knöpfe von `SeriesForm`/`UnitForm` bei leerem Pflichtfeld und streicht die lokale
Meldung; B-231 legt den Fokus beim gesperrten „Mehr laden" ohne Fehlerkasten auf die Liste. Testweg: Vitest je Baustein mit
Mutanten, eine neue E2E für den Sichtbereich (B-232), die volle E2E-Suite als Rollengang.

**Entscheidung im Auftrag des Nutzers (beim Bau aufgetreten):** B-233 zieht die Regel nur auf der Lehrwerke-Seite nach;
die übrigen zehn Formulare mit demselben Muster gehen als [B-380](backlog/B-380-pflichtfeld-sperre-in-den-uebrigen-formularen.md)
in den Backlog. Begründung: das Sprint-Ziel ist ohne sie erfüllt, und das Nachziehen trifft elf exakte Label-Abfragen in
fremden Tests.

### Iteration 1 — umgesetzt

- **B-234** (`CatalogAdmin.tsx`): `pending` an `CategorySection`, Verdrahtung in `artenPending`; drei sichtbare Zustände.
- **B-232** (`VaterLehrwerke.tsx:155`): Scroll zum Meldungsplatz auch bei Erfolg; neue Spec `bestaetigung-scrollt-zum-platz.spec.ts`.
- **B-233** (`VaterLehrwerke.tsx`): `SeriesForm`/`UnitForm` sperren bei leerem Pflichtfeld, „(Pflicht)" am Label, stiller Riegel vor
  dem Räumen; vier E2E auf das neue Label, `bestaetigung-ueberlebt.spec.ts` Schritt 2 umgestellt; Hausregel in `frontend/CLAUDE.md`
  (Datei danach 14 B kleiner).
- **B-231** (`SohnShop.tsx`, `index.css`): Fokus auf die Liste beim gesperrten „Mehr laden" ohne Fehlerkasten.

**Verifikation:** Vitest 648/648, `tsc -b` sauber, Playwright 110 passed (volle Suite, 6,7 min; vor den zwei Review-Fixes, die nur
eine Funktion herauslösen und Tests ergänzen). Jede neue Zusicherung mit benanntem Mutanten rot geprüft (Zahlen in den Stories).
**Review** `frontend-reviewer`: 🟡 1 Verdrahtung von `pending` ungetestet, 🟡 2 `tabIndex={-1}` ungetestet (happy-dom fokussiert
alles) – beide behoben, **Zähler Sprint 1: 2 von 5**; 🟢 3 (Rerender ohne Fallmöglichkeit, entfernt), 🟢 4 (Regel-Satz vermischt,
getrennt) ohne Zählung.

### Runde 1 — Re-Review / Abnahme

- **Creator:** „Ich sehe jetzt nach jedem Speichern im Verlags-Vokabular die Bestätigung, ohne hochzuscrollen, und ein leerer
  Reihen-Name lässt den Knopf grau – ich weiß, warum." Belegt durch die neue Spec und die vier nachgezogenen Lehrwerke-Specs im
  echten Browser. Den Arten-Ladezustand sieht er nur kurz; belegt über Vitest, nicht im Browser (ein Ladefenster wäre nur mit
  künstlich langsamem Server herstellbar).
- **Sohn:** abgenommen mit **benanntem Gerätecheck** (B-231 `wartet_auf`): „Mehr laden" per Tastatur, Fokusring an der Liste. Die
  Sohn-Specs liefen grün als Regressionszeugen.
- **Vater:** Regressionszeuge – Suite grün, kein Weg von ihm vom Diff berührt.

## Retrospektive (Sprint 1)

**Nachschau:** Lauf S, Teil 3 (`46e3871`) – B-372 per frischem `pugling-reviewer` (Suite 1073/1073), B-373 selbst per `grep`;
`nachgeschaut: 2026-11-05` an beiden. **Entgangen:** B-372s Entscheidung 2 behauptet die Vorrangfolge „Name vor PIN", kein Test trägt sie
→ [B-379](backlog/B-379-name-vor-pin-vorrang-ungetestet.md) (`entgangen_bei: [B-372]`). Index danach: Nachgeschaut 309 von 313 (die
vier offenen sind die dieses Sprints).

**Was die eigenen Tore durchgelassen haben:** zweimal dieselbe Klasse – eine Zusicherung am **Baustein**, nicht an seiner
**Verdrahtung**. In B-234 testete jeder Fall `CategorySection` mit gesetztem `pending`, keiner, wie der Aufrufer es berechnet; in
B-231 prüfte der Fokus-Fall in happy-dom, das jedes Element fokussiert, das Attribut also nicht brauchte. Beide fand der Reviewer,
kein Tor. B-379 aus der Nachschau ist eine Verwandte: eine Entscheidung nennt zwei Zweige, der Test trägt einen.

**Vorschlag (nicht gelandet, Freigabe 3):** (BG) In Step 5 von `pm-loop` ein Satz: „Wird ein Prop am Aufrufer **berechnet**, braucht
die Berechnung einen eigenen Fall (reine Funktion), sonst testet der Baustein-Fall einen Wert, den niemand liefert." Ein Tor dafür
wäre eine Regel über Prop-Ausdrücke in JSX – zu unscharf, um es zu mechanisieren; darum Prosa.
