---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test]
aliases: [Tor nennt ungeprüfte Zeile]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf Q, Teil 1 in Lauf Q, Teil 2 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-259]
wartet_auf: ""
---

# B-366 · Das Klebestellen-Tor nennt eine Zeile, die kein Test prüft

B-259 AK 2 verspricht eine Meldung „mit Datei und Zeile". Der Selbsttest `Leerraum_Zwischen_Elementen_Wird_Erkannt`
vergleicht nur die **Anzahl**; die Mutante „ohne Versatz im Lauf" (`yield return start + 1`) oder „um eins daneben" bliebe
grün, und die Meldung zeigte auf den falschen Kommentar.

## User Story

Als **Entwickler**, den das Tor rot meldet, möchte ich die genannte Zeile direkt öffnen können, damit ich die Klebestelle
nicht im ganzen Kommentarblock suchen muss.

## Ist-Stand am Code (Stand `1067b55`)

- `backend/Pugling.Api.Tests/ConventionGuardTests.cs`, `GluedElements`: Zeile = Laufbeginn + 1 + Zeilenumbrüche bis zum
  Treffer; die Theory prüft `.Count()`.

## Die echte Lücke

Eine Hälfte des AK ohne Zusicherung.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein Fall, der die Zeile vergleicht**, mit dem Treffer auf der dritten Zeile eines Laufs, der nicht in Zeile 1 beginnt.

## Akzeptanzkriterien

1. „Ohne Versatz im Lauf" und „um eins daneben" machen einen Fall rot.

## Schätzung

`XS`, nur Test. Testweg: `ConventionGuardTests`.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf Q, Teil 1 (frischer `pugling-reviewer` über `1067b55`, 🟡). Im selben
  Sprint bearbeitet (Freigabe 3). Testlücke: neuer Fall grün; Mutanten „ohne Versatz im Lauf" und „um eins daneben" je **1
  failed**. Mit ihm aus derselben Nachschau, ohne Zählung (🟢): Fall `</item>` links (Mutante „`item` links" **1 failed**),
  B-260-Fall „Fett am Zeilenanfang ist kein Listenpunkt" (Mutante „jedes `*`" **1 failed**), der überflüssige Term
  `previousIndented` im Normalisierer gestrichen, zwei Sätze in B-259 berichtigt.
- **2026-11-03** — Review `frontend-reviewer` und `pugling-reviewer`: kein 🔴; ein 🟡 (InfoHint noch nicht gestaged – AK 1 von B-362 gilt erst nach dem Commit, dort per `git ls-files --eol` geprüft, siehe Protokoll; kein Code geändert, ohne Zählung); drei 🟢 (Umbruch, zwei Kommentar-Formulierungen) ohne Zählung behoben. Abnahme Lauf Q, Teil 2: volle Suite **1059/1059**, Vitest **621/621**, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (die Tor-Meldung); Creator, Vater, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf Q](../pm-sitzung-2026-11-03-nacht.md)).
