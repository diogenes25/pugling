---
tags: [typ/story, status/idee, bereich/katalog, bereich/frontend, rolle/supervisor]
aliases: [Freitext beim Entkoppeln, angezeigt ist nicht gespeichert]
status: idee
prio: P3
art: Frage
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: Review Sprint 11 zu B-64 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Nutzer: Entscheidung, ob die Antwort den gespeicherten Freitext getrennt mitliefert"
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

## Verlauf

- **2026-10-26** — angelegt aus dem `pugling-reviewer` (Fund 4) und dem `frontend-reviewer` zu B-64 im sechsten
  Nachtlauf; nicht gebaut, weil jede Behebung eine Entscheidung verlangt, die B-64 nicht hergibt (Freigabe 8).
