---
tags: [typ/story, status/idee, bereich/frontend, rolle/vater, rolle/sohn]
aliases: [Lesefehler Ternär-Form, ListenAusfall hinter Lade-Platzhalter]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Review zu B-183 in Lauf R, Teil 2 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-369 · Lesefehler in Ternär-Form und unter nacktem Namen bieten noch keinen erneuten Versuch

Abgeteilt von [B-183](B-183-gescheitertes-lesen-ohne-erneuten-versuch.md) in Lauf R, Teil 2. B-183 hat die 29 Lesefehler in der
`&&`-Form auf `ListenAusfall` umgestellt; der `frontend-reviewer` fand **weitere** in einer Form, die die Zählung nicht sah:

- **22 Stellen im Vater-Web in Ternär-Form** `x.loading && x.data === null ? „Lade…" : x.error ? <div className="banner err">` –
  `VaterClassTests.tsx:30/116/194/256`, `VaterShop.tsx:97/255/355/399/425/445`, `VaterRewards.tsx:45/118/203`,
  `VaterDashboard.tsx:51/76`, `VaterKonto.tsx:24/64`, `VaterPlaene.tsx:74`, `VaterVocab.tsx:305`, `VaterZiele.tsx:216`,
  `VaterAnmerkungen.tsx:147`, `PlanPositions.tsx:53` (dort steht das Banner eine Zeile unter `positions.error ? (` – die
  zeilenweise Regex sah es nicht; mehrzeilig nachgezählt in der Nachschau von Lauf S).
- **Frühe Rückgabe mit Banner** (acht, gezählt per `return .*banner err`): `PlanPositions.tsx:738`, `VaterPlanDetail.tsx:32`,
  `VaterKind.tsx:46`, `VaterLernstand.tsx:165/216/280/312/346`.
- **Ein Suchfehler über `StatusBanner`, ohne `reload`:** `MediaPickers.tsx:139` (keine frühe Rückgabe).
- **Drei Stellen unter nacktem Namen:** `CatalogAdmin.tsx:179` (`categories.error` als Prop, ohne `reload`/`loading`),
  `ExercisePreviewModal.tsx:116`, `VaterProfil.tsx:56`.
- **Die Sohn-Arcade** (mindestens acht): `SohnKonto.tsx:29`, `SohnShop.tsx:188`, die `error-box` in `SohnHome.tsx:30/64`,
  `SohnProgress.tsx:16/30`, `SohnInterests.tsx:145` und `SohnSkins.tsx:64` (`banner err` unter `loadError`) – `ListenAusfall` ist
  fest deutsch, die Arcade läuft zweisprachig. Nicht gezählt, weil kein reines Lesen und schon mit Ausweg: die `error-box` beim
  gescheiterten Start in `SohnPractice.tsx:212` (Knopf „zur Startseite“) und `SohnTest.tsx:170` (Knopf „fortsetzen“) – beim Bau zu
  entscheiden, ob sie dazugehören (Nachschau Lauf S, Teil 2).
- **Lücken des Wächters** (`src/lesefehler.test.ts`, Nachschau Lauf S, 🟢): er sieht nur `{x.error && …`; durch rutschen dagegen
  `{!x.loading && x.error && …}`, `{x.error != null && …}`, `<span className="banner err">` und `className="err banner"`.

**Warum nicht mechanisch in B-183:** In der Ternär-Form steht der Platzhalter **vor** dem Ausfall. Nach einer gescheiterten
ersten Ladung würde „Erneut laden" `loading` setzen und den Kasten samt Knopf gegen „Lade…" tauschen – der Fokus fiele unter dem
Finger weg (B-216, im Browser gemessen). Wie Platzhalter und Ausfall dort zusammenspielen (Kasten über den Platzhalter ziehen?
Platzhalter an `showPlaceholder` statt `data === null`?), ist eine Gestaltungsfrage. Dazu: ob der Wächter die nackten Namen
sehen soll – `error` heißt auch der Anmeldefehler (`VaterLogin.tsx:99/185`), ein Wächter darauf meldete falsch.

## Verlauf

- **2026-11-03** — angelegt in Lauf R, Teil 2 (Teilen von B-183). Die **Entscheidung des Nutzers vom 2026-10-23** zu B-183 („`ListenAusfall`
  ist Hausregel für jeden Lesefehler, wo das `reload` eindeutig ist") gilt für diese Stellen ebenso; eine Freigabe für den
  autonomen Bau dieser Story ist damit **nicht** erteilt (Freigabe 9 gilt nur für die Story, in der sie steht) – offen sind die
  zwei Gestaltungsfragen oben.
