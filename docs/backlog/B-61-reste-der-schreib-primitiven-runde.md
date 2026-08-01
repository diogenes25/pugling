---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [CatalogAdmin leert unbedingt, Kinderliste flackert]
status: idee
prio: P3
art: Defekt
quelle: docs/backlog/B-54-objectivecard-schreib-primitive.md
unverifiziert: true
---

# B-61 · Zwei Reste aus der Schreib-Primitiven-Runde

Beim Bauen von [B-54](B-54-objectivecard-schreib-primitive.md) sind zwei Stellen aufgefallen, die zur
selben Familie gehören, aber auf anderen Bildschirmen liegen. Sie sind **nicht** in B-54 mitgemacht worden,
weil sie deren Zuschnitt gesprengt hätten – und nicht in einen „offen:"-Vermerk gewandert, weil dafür
dieser Bereich existiert.

1. **`CatalogAdmin.tsx:180` leert sein Eingabefeld unbedingt.** `onCreate(name.trim()); setName("")` –
   `onCreate` ist `void`, der Erfolg wird also nicht abgewartet. Lehnt der Server ab (Fach heißt schon so),
   ist der getippte Name weg und muss neu geschrieben werden. Genau dieser Defekt ist in B-54 am `TagAdder`
   des Vokabel-Stores repariert worden (dort liefert `onAdd` jetzt `boolean` und geleert wird nur bei
   Erfolg). Der Knopf selbst trägt korrekt `disabled={busy}`; es geht **nur** um den verlorenen Text.
2. **`VaterDashboard.tsx:48` und `:73` benutzen weiter `{loading ? "Lade…" : …}`.** Die
   [in frontend/CLAUDE.md beschriebene `useAsync`-Falle](../../frontend/CLAUDE.md) beißt dort nicht hart –
   die Tabellen haben keine aufklappbaren Zeilen, deren Zustand verloren gehen könnte –, aber beide Listen
   **flackern** nach jedem Anlegen eines Kindes durch die Ladeanzeige. In B-54 sind die zwei Stellen
   reparariert worden, an denen die Falle echten Zustand kostete (`VaterZiele`, `VaterVocab`).

**Ungeprüft** ist an beiden Punkten nur der Umfang: ob es im Vater-Web weitere `{loading ? …}`-Stellen
gibt, an denen die Falle harmlos, aber sichtbar ist, und ob es weitere „leert unbedingt"-Formulare gibt.
Das gehört ins Ausformulieren – die zwei Stellen oben sind am Code belegt.

## Verlauf

- **2026-08-01** — angelegt aus zwei Funden beim Bauen von B-54 (einer davon aus dem
  `frontend-reviewer`-Lauf), je mit `Datei:Zeile` belegt. Bewusst **nicht** in B-54 mitgemacht: andere
  Bildschirme, und der Vollständigkeits-Beweis dieser Story galt den mutierenden Knöpfen, nicht dem
  Aufräumen jedes Formulars.
