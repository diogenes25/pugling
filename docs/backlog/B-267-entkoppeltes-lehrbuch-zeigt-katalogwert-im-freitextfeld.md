---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/frontend, rolle/supervisor]
aliases: [Freitext beim Entkoppeln, angezeigt ist nicht gespeichert]
status: abgenommen
prio: P3
art: Frage
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: Review Sprint 11 zu B-64 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-267 · Nach dem Entkoppeln zeigt das Freitextfeld den Katalogwert – gespeichert wird er nicht

Seit [B-64](B-64-textbook-vs-series.md) liefert `TextbookResponse` bei verknüpftem Buch Verlag und Klasse aus dem
Katalog; der gespeicherte Freitext bleibt Rückfall, steht aber nicht in der Antwort (Entscheidung 3: „ohne neues
Contract-Feld“). Das Formular (`textbookFormValues`, `frontend/src/vater/textbookPatch.ts`) übernimmt darum den
**Katalogwert** als Ladezustand. Löst der Vater im Formular Reihe oder Unit (oder wechselt auf eine Reihe ohne
Verlag bzw. eine Unit ohne Band), erscheint das Freitextfeld wieder – mit dem Katalogwert vorbelegt:

- Speichert er unverändert, wird der Wert nicht gesendet (gleich dem Ladezustand); danach steht der alte Freitext
  da. Angezeigt war nicht, was gespeichert ist.
- Leert er das Feld, geht `clearGrade` bzw. ein leerer Verlag raus und löscht einen gespeicherten Wert, den er nie
  gesehen hat.

Datenverlust ohne Zutun gibt es nicht (AK 5 von B-64 hält: Integrationstest
`Lehrbuch_Entkoppeln_BringtDenFreitextZurueck`, E2E `lehrbuch-katalog-zuerst.spec.ts`).

**Offene Entscheidung (darum `Frage`, kein Nachtlauf-Bau):**

- (a) Die Antwort trägt den gespeicherten Freitext additiv mit (etwa `PublisherFreeText`/`GradeFreeText`), und das
  Formular belegt das wieder erscheinende Feld damit vor. Das widerspricht der Kostenangabe von B-64
  Entscheidung 3 („kein neues Contract-Feld“).
- (b) Rein im Formular: ein beim Laden abgeleitetes Feld wird beim Wiedererscheinen immer gesendet (angezeigt =
  gespeichert). Das überschreibt aber den Freitext, den AK 5 retten soll.
- (c) Das Feld erscheint leer mit Hinweis, und leer heißt dort „nicht angegeben“.

## Entscheidungen

1. **Weg (a)** (Nutzer, 2026-10-26): `TextbookResponse` trägt zusätzlich `PublisherFreeText` und `GradeFreeText`
   (additiv, kein Vertragsbruch), und das Formular füllt seine Freitextfelder daraus statt aus dem abgeleiteten
   Wert. **Kosten:** zwei Felder im Vertrag, entgegen der Kostenangabe in B-64 Entscheidung 3.

## Akzeptanzkriterien

1. Die Antwort trägt neben `publisher`/`grade` den gespeicherten Freitext unverändert mit, auch wenn der Katalog ihn
   übersteuert.
2. Erscheint das Freitextfeld wieder (Reihe gelöst, Reihe ohne Verlag, Unit ohne Band), zeigt es den gespeicherten
   Freitext, nicht den bisherigen Katalogwert.
3. Unverändert gespeichert, geht kein Verlag und keine Klasse raus. Geleert, geht genau der Wert weg, den das Feld
   gezeigt hat.

## Schätzung

**S**, `wo: beides`, keine Migration (die Spalten gibt es), kein Vertragsbruch (additiv, mit Vorgabewert).
**Testweg:** Integrationstest `Lehrbuch_Entkoppeln_BringtDenFreitextZurueck` mit verschiedenen Katalog- und
Freitextwerten; Vitest für `textbookFormValues` und das Formular (Reihe lösen, Unit ohne Band).

## Verlauf

- **2026-10-26** — angelegt aus dem `pugling-reviewer` (Fund 4) und dem `frontend-reviewer` zu B-64 im sechsten
  Nachtlauf; nicht gebaut, weil jede Behebung eine Entscheidung verlangt, die B-64 nicht hergibt (Freigabe 8).
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): **Weg (a):** `TextbookResponse` trägt den gespeicherten Freitext **additiv** mit (etwa `PublisherFreeText`/`GradeFreeText`, kein Vertragsbruch), und das Formular belegt das wieder erscheinende Feld damit vor. Angezeigt ist dann, was gespeichert ist; AK 5 von B-64 hält weiter. Die Kostenangabe von B-64 Entscheidung 3 („kein neues Contract-Feld“) war eine Schätzung, kein Verbot. `wartet_auf` geleert. **Freigabe des Nutzers (Autonomer Modus, Opt-in):** ein Nachtlauf darf diese Story auf dieser Grundlage selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an.
- **2026-10-26** — ausformuliert und geschätzt (S) unter Freigabe 9 auf Grundlage der Entscheidung (a), dann gebaut ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf B, Sprint *Lehrbuch-Formular*). Backend: zwei Felder am Ende von `TextbookResponse`, Projektion `t.Publisher`/`t.Grade`; der Integrationstest prüft `publisherFreeText = "Freitext-Verlag"` gegen den Katalogverlag, `gradeFreeText` 9 gegen 7 und 8 nach dem Schreiben im verknüpften Zustand. Frontend: `textbookFormValues` liest den Freitext; ein neuer Fall „Reihe lösen → Feld zeigt ‚Klett‘, nicht ‚Cornelsen‘“, der Unit-Fall prüft jetzt den Feldwert 9 statt 7. Rote Proben: Projektion mit dem abgeleiteten Wert → **2 failed** (darunter `Lehrbuch_Entkoppeln…`: „Katalog-Verlag 1“ statt „Freitext-Verlag“); `textbookFormValues` mit `publisher`/`grade` → **3 failed** (je ein Fall in `textbookPatch.test.ts` und zwei im Formular). Grün: Solution-Build 0 Warnungen, Backend **918/918 `exit=0`**, Vitest **441/441**, Lehrbuch-Specs **3 passed**.
- **2026-10-26** — Review ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf B, Sprint *Lehrbuch-Formular*): `pugling-reviewer` Fund 1 (gezählt 1): Kein Fall prüfte ein verknüpftes Buch *ohne* Freitext, ein Rückfall auf den Katalogwert bliebe grün. Behoben mit dem Fall `bare` (beide Felder `null`), Mutante `t.Publisher ?? Katalog` → **1 failed** (`Null` erwartet, `String` erhalten). `frontend-reviewer` Fund 1 (gezählt 2): dasselbe im Frontend (`publisherFreeText ?? publisher`). Behoben mit einem Nullfall in `textbookPatch.test.ts`, Mutante → **1 failed**. Doppelter Testkommentar ohne Zählung zusammengezogen. Zurückgestellt: der Platzhalter „Cornelsen“ im leeren Feld → [B-278](B-278-platzhalter-sieht-aus-wie-gespeicherter-wert.md). Kosmetisch und bekannt: Der OpenAPI-Generator klebt den zweiten `<para>` ohne Leerzeichen an. Rollengang ohne Browser (Extension nicht verbunden) als E2E: `lehrbuch-katalog-zuerst.spec.ts` (Vater: Katalog zuerst, Reihe inline anlegen, Entkoppeln bringt den Freitext zurück), `reihe-anlegen-scheitert-fokus.spec.ts` (Fehlerweg per Tastatur). Sohn und Creator als Regressionszeugen: volle Suiten grün. Verifikation: Solution-Build 0 Warnungen, Backend **918/918 `exit=0`**, Vitest **443/443**, Playwright voll nach dem letzten Code-Edit **95 passed**. Zähler des Sprints am Ende **5** (Grenze erreicht, nicht überschritten), darum kein weiteres Delta-Review. **Abgenommen**; Commit: `4a25d47`.
