---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-27, Lauf E]
---

# PM-Sitzung: Nachtlauf 2026-10-27 – Nacharbeit B-05, dann NP-9 und Folgepakete

**Datum:** 2026-10-27  ·  **Moderation:** PM (unbeaufsichtigt nach der Morgen-Entscheidung)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Lauf D (B-05) abschließen, danach die Pakete NP-9, NP-10 und NP-11 nacheinander fahren.

Auftrag: der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10. Ausgangslage: HEAD `a4b712c`,
Baum sauber, nichts gepusht. Lauf-Reihenfolge nach der [Morgen-Entscheidung vom
2026-10-26](pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26): A–D sind gelaufen
([Protokoll](pm-sitzung-2026-10-26-tag.md)), Lauf D endete an Freigabe 3 mit Zähler 6.

## Morgen-Entscheidung 2026-10-27

Der Nutzer hat die beiden offenen Punkte aus [Lauf D](pm-sitzung-2026-10-26-tag.md#wiederaufnahme--offene-entscheidungen-für-den-nutzer)
im Dialog entschieden:

1. **Lauf D wird als Nacharbeit mit neuem Zähler (0) fortgesetzt**: Fund 6 (Tastaturfokus fällt nach jeder Karte auf
   `body`) und die übrigen Punkte (a)–(e) aus dem Verlauf von B-05, dazu die Testlücken aus B-283. Danach Abnahme von
   B-05, dann NP-9, NP-10, NP-11.
2. **🟢-Hinweise zählen nicht mehr auf den Fünfer-Zähler.** Gezählt wird ein Fund nur noch, wenn er 🟡 oder 🔴 ist
   *und* seine Behebung Code oder Tests ändert. 🟢-Hinweise werden behoben, wenn das billig ist, sonst als Story
   zurückgestellt – beides ohne Zählung, im Protokoll weiter als „ohne Zählung“ ausgewiesen. Festgehalten als
   Freigabe 10 im Auftragstext ([nachtlauf.md](nachtlauf.md)).

Die sieben Retro-Vorschläge des 2026-10-26 (A, A2, B–F) bleiben unentschieden; dieser Lauf schlägt weiter nur vor.

## Lauf E, Teil 1 — Nacharbeit B-05 (Buchstaben ordnen), Zähler 0

**Sprint-Ziel (unverändert aus NP-7):** *Der Sohn kann ein Wort aus vertauschten Buchstaben zusammensetzen, per Klick
oder Tastatur* – und jetzt auch **mehrere Karten hintereinander nur mit der Tastatur**, ohne dass der Fokus verloren
geht. Umfang: [B-05](backlog/B-05-buchstaben-tausch.md) (offene Funde a–e) und [B-283](backlog/B-283-buchstaben-ordnen-testluecken.md)
(`Defekt`, Testlücken, Freigabe 1). **Zähler: 0.**

**Entwickler-Brief:** Der Fokus gehört in die Komponente, nicht in die Aufrufer: `LetterTiles` setzt ihren Zustand
selbst zurück, wenn sie neue Kacheln bekommt (bisher per `key` von außen erzwungen), und holt den Fokus auf die erste
Kachel, wenn er vorher im Block stand oder verloren ging (`body`). Stand er woanders (etwa auf „Prüfen“ nach einem
Mausklick), bleibt er dort. Strg/Alt/Cmd-Kombinationen gehören dem Browser. Die Feldhilfe und der Plan-Assistent
nennen die Stufe. Backend: nur Tests (B-283), kein Verhalten ändert sich, keine Migration.

### Iteration

- **(a) Fokus** – `LetterTiles` ohne `key`: Zustand im Render zurückgesetzt, wenn `tiles` wechselt; ein Effekt auf `tiles`
  holt den Fokus auf die erste Kachel, wenn er im Block stand oder auf `body` lag (Kästchen-Karte davor), sonst bleibt er.
  Vitest: vier neue Fälle, Mutanten „nie“/„immer“/„kein Reset“/„kein Modifier-Wächter“ → je **1 failed**, danach **10/10**.
  E2E mit vier Karten, zweite Tastatur-Karte ohne Fokussieren: gegen die alte Komponente **1 failed**
  (`toBeFocused`: expected focused, received inactive), danach **1 passed**.
- **(c)** Strg/Alt/Cmd ignoriert. **(d)** Feldhilfe nennt „gemischte Buchstaben ordnen“. **(e)** Assistent bietet
  „6 · Auswahl“/„7 · Ordnen“ (Kurznamen-Familie wie `STAGE_LABEL`; Langnamen in `VOCAB_FORMS`).
- **B-283** – Prüfungstest mit zweitem Versuch auf zweiter Position (Mutante Seed `attemptId` → 1: **1 failed**),
  Vorschau-Test `stage=7` (Mutante `LetterTiles` → null: **1 failed**); die Spec wertet jede Antwort über `wasCorrect`.
  Filterlauf **13/13 `exit=0`**, Vitest gesamt **454/454**.

### Review (nacheinander, je Bereich)

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| – | `pugling-reviewer` | Testkommentar begründet die zweite Position falsch; Summary als Umbau-Erzählung | zwei Kommentare | nein (nur Kommentar) |
| 1 | `frontend-reviewer` 🟡 | Der kommentierte Maus-Weg „Fokus bleibt auf ‚Prüfen‘“ entsteht nicht: der gesperrte Knopf gibt den Fokus ab | Spec auf `Serial` umgestellt, `toBeFocused` nach jedem der drei Übergänge; gemessen **1 passed** (Fokus auf der Kachel); Kommentare berichtigt | ja (Test) |
| – | `frontend-reviewer` 🟢 | Reset-Vertrag nur implizit; dritte Handkopie der Stufenliste; Fokus-Sprung im Vorschau-Dialog; Aufgabe wird nach Kartenwechsel nicht angesagt | JSDoc; Assistent liest `VOCAB_FORMS`; notiert; [B-286](backlog/B-286-kartenwechsel-sagt-die-aufgabe-nicht-an.md) | nein (Freigabe 10) |

**Zähler: 1.** Nach den Behebungen: Build grün, Vitest **454/454**, `assistent.spec` + `feldhilfe.spec` **6 passed**,
Backend **949/949 `exit=0`**.

### Rollengang / Abnahme

Kein Browser verbunden (Extension nicht erreichbar), darum E2E als Rollengang (Freigabe 6):

- **Sohn:** `buchstaben-ordnen.spec.ts` fährt vier Karten in serieller Reihenfolge – Kästchen per Enter, Kacheln per
  Tastatur ohne erneutes Fokussieren, Kacheln per Klick und „Prüfen“, Kacheln per Tastatur – und prüft je Antwort die
  Server-Wertung. *„Ich tippe einfach weiter, ohne erst irgendwo hinzuklicken.“* **1 passed.**
- **Vater:** Assistent bietet alle sieben Stufen aus derselben Liste wie das Anlege-Formular; Feldhilfe nennt die
  Form. Regressionszeugen `assistent.spec`, `feldhilfe.spec` **6 passed**; Playwright voll **96 passed** vor der
  Spec-Umstellung.
- **Creator:** nur Tests und Vorschau-Test berührt, kein Weg geändert; Suite **949/949 `exit=0`**.

B-05 und B-283 `abgenommen`.

### Retrospektive

**Nachschau:** NP-3 (`b644081`: B-245, B-28, B-230) von einem frischen Prüfer gegen den Commit geprüft, je Behauptung
eine Prüfzeile in den Stories. **Ein Code-Entgleiten** (B-245): löscht ein Vater sich selbst, bleiben die Konten seiner
verwaisenden Kinder als leere Hülle, der Kind-Token bleibt gültig → [B-285](backlog/B-285-kind-konto-bleibt-nach-vater-loeschung.md)
(`entgangen_bei: B-245`). Ein Doku-Entgleiten (`MeResponse.AccountId` „null for a legacy token“) sofort berichtigt.
B-28 und B-230 ohne Befund, `nachgeschaut` an allen drei gesetzt. Index vor der Abnahme: **Nachgeschaut: 198 von 198**, danach 198 von 200 (B-05, B-283 stehen für die nächste Nachschau an).

**Was die eigenen Tore durchgelassen haben:** (1) Eine Behauptung über Browser-Fokus stand als JSDoc und als
Vitest-Fall („Maus auf ‚Prüfen‘ behält den Fokus“), und beide waren über einen Zustand formuliert, den die App nicht
erzeugt – der Knopf ist in dem Moment gesperrt. jsdom/happy-dom kennen die Fokus-Abgabe eines gesperrten Knopfs
nicht; nur die E2E hat es gezeigt. Das ist Vorschlag **(D)** vom 2026-10-26 ein zweites Mal gemessen: *Fokus-
Zusicherungen, die `disabled` oder `body` berühren, gehören in eine E2E-Zeile, nicht in Vitest.* (2) Die B-245-Lücke
ist eine **Asymmetrie zweier Löschpfade** (Adult vs. Child), die der Reviewer des Sprints nicht gesehen hat, weil er
den Diff las und der zweite Pfad außerhalb lag.

**Vorschlag, nicht gelandet (Freigabe 3):** **(G)** eine suite-weite Invariante in `PuglingWebAppFactory` –
*nach dem Lauf existiert kein `Account` ohne `AccountProfile`* – als Tor; sie hätte B-285 an jedem Test gezeigt, der
einen Vater mit Kind löscht, ohne dass jemand den zweiten Pfad kennen muss. Bewusst ungedeckt bleibt die Frage,
welche der Review-Kommentare wahr sind: ein Kommentar ist nicht ausführbar.

**Commit:** folgt unten.
