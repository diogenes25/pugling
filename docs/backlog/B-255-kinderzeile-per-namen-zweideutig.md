---
tags: [typ/story, status/abgenommen, bereich/test, rolle/entwickler]
aliases: [Kinderzeile per Namen, createChild-Rennen]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (dritter Lauf, rote Probe zu B-35)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-03"
---

# B-255 · E2E-Specs suchen die Kinderzeile per Namen, den zwei Tabellen tragen

Die Vater-Startseite (`VaterDashboard.tsx`) zeigt den Namen eines Kindes zweimal: in der Tagesübersicht
(`:54`, Name als Text in der ersten Spalte) und in der Kinder-Tabelle (`:79-81`, Id vorn, Name als Link).
Eine Spec, die `getByRole("row", { name: /<Kind>/ })` nimmt und die erste Zelle als Id liest, bekommt je nach
Ladereihenfolge den Namen statt der Id – gemessen beim Bau von `e2e/karten-umdrehen.spec.ts` (B-35): zwei
Läufe rot an zwei verschiedenen Stellen, mit dem Link-Filter dreimal grün. Dasselbe Muster steht ungefiltert
in `e2e/bilder.spec.ts` (`createChild`); ob es dort je fällt und welche weiteren Specs es teilen, ist nicht
geprüft. Außerhalb des Diffs von Sprint 1, darum eigene Story.

## User Story

Als **Entwickler**, der den Vater→Sohn-Durchstich per E2E fährt, möchte ich, dass jede Spec die Id ihres eben angelegten
Kindes bekommt, damit ein Lauf nicht davon abhängt, welche Tabelle zuerst lädt oder ob der Name schon existiert.

## Ist-Stand (Lauf N, am Code belegt, Stand `b73bcde`)

- `VaterDashboard.tsx:54-58` (Tabelle „Heute", Name als Text) und `:79-82` („Kinder", Id vorn, Name als Link) tragen beide
  den Namen. Nach dem Anlegen lädt nur „Kinder" neu (`:43`); „Heute" zeigt, was beim Seitenaufruf schon da war.
- Zehn Specs lasen die Id als erste Zelle der Zeile, die sie über den Namen fanden – sieben inline, drei als lokale
  `createChild` (`bilder`, `creator-lehrwerk-weg`, `lehrwerke`); drei filterten zusätzlich (`^\d+ Name` bzw. Namens-Link).
- **Gemessen:** ein zweites Kind gleichen Namens → `strict mode violation: getByRole('row', …) resolved to 2 elements`.

## Die echte Lücke

Ein Name ist kein Schlüssel; die Specs nutzten ihn als einen, während der Server die Id längst zurückgibt.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein gemeinsamer Helfer `createChild(page, name, pin)` in `e2e/helpers.ts`** liest die Id aus der Antwort des `POST`
   und wartet danach auf die Zeile mit Namens-Link **und** Id-Zelle. *Kosten:* die Specs hängen am Pfad
   `/api/v1/supervisor/children`; ändert er sich, fällt der Helfer laut statt still.
2. **Alle zehn Stellen** ziehen darauf um, auch die drei schon gefilterten – zwei Muster für dieselbe Handlung driften.

## Akzeptanzkriterien

1. Zwei Kinder gleichen Namens bekommen über den Helfer zwei Ids, und die zweite Id öffnet das zweite Kind.
2. Keine Spec liest die Id mehr aus einer Zeile, die sie über den Namen gefunden hat.

## Schätzung

`S`, nur E2E. Testweg: neue Spec `kind-anlegen-id.spec.ts`, Gesamtlauf.

## Verlauf

- **2026-10-23** — angelegt beim Bau von B-35; ungeprüft für die übrigen Specs.
- **2026-11-02** — Lauf N, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe (Helfer
  zunächst mit dem alten Muster): `kind-anlegen-id.spec.ts` **1 failed** (erwartet 1) mit Strict-Mode-Verstoß beim zweiten
  Anlegen; mit dem Fix **1 passed**. AK 1 → diese Spec; AK 2 → `grep 'getByRole("cell").first()' e2e` ohne Treffer, zehn
  Stellen umgestellt. `tsc` über `e2e/*.ts` sauber.
- **2026-11-02** — Review `frontend-reviewer`: 🟡 AK 2 zu eng gemessen – vier weitere Stellen leiteten die Id aus einem über den Namen gefundenen Link ab (`punkte-empfehlung.spec.ts` dreimal, `vater-von-null.spec.ts`); der grep suchte nur nach der Zellen-Form. Behoben: die drei auf `createChild`, `vater-von-null` (verzögert das Neuladen, setzt eine Klasse) liest die Id ebenfalls aus der POST-Antwort; Prüfweg jetzt `grep "getAttribute(\"href\")\|getByRole(\"cell\").first()" e2e` ohne Kinder-Treffer (zählt). 🟡 der Helfer hing bei einem gescheiterten Anlegen bis zum Test-Timeout ohne Grund → Antwort ohne `ok()`-Filter abwarten und mit Status und Text zusichern, Warten auf 10 s begrenzt; Probe mit PIN „1" (der Client schickt nichts): **1 failed** nach 10 s mit `TimeoutError` an `helpers.ts:42` statt nach 60 s (zählt). 🟢 die Zusicherung der neuen Spec trägt nur über „zwei Ids + eindeutiger Name" – als Kommentar benannt (ohne Zählung). **Zähler Teil 2: 2.**
- **2026-11-02** — Abnahme Lauf N, Teil 2: E2E-Gesamtlauf **103 passed** (`exit=0`, nach dem letzten Edit gestartet), `tsc` über `e2e/*.ts` sauber. Rollengang: die Specs SIND der Weg des Entwicklers; Vater und Sohn fahren ihre Specs über den neuen Helfer (zwölf Specs, alle grün), Creator Regressionszeuge. **Abgenommen**, Commit siehe Protokoll ([Lauf N](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-03** — Nachschau in Lauf O, Teil 1 (frischer `frontend-reviewer`, `git show cc19020 -- frontend`): **sauber** (🟢 → [B-357](B-357-e2e-helfer-restkanten.md)). Geprüft: AK 1 – auf der frisch geladenen Seite tragen „Heute" und zwei „Kinder"-Zeilen den Namen, das alte Muster wirft im Helfer; Mutanten „erste Id zweimal" und „fremde Id" je rot. AK 2 nach der *Handlung* gesucht (jede Stelle, an der in `e2e/*.ts` eine Kind-Id entsteht: Helfer, POST-Antwort in `vater-von-null`, `postId` in `buchstaben-ordnen`, Seed-Konstanten) – keine über einen Namen. Verschobene Stellen gleichwertig (ein zusätzliches `goto`), `vater-von-null`: Warten nach dem `unroute` registriert, Rückhalt nur für GET. Wortlaut: „zwölf Specs über den Helfer" – elf über den Helfer, eine mit demselben Muster inline.
