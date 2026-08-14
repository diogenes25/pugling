---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [Schluessel der Antwort, Batching-Annahme im Assistenten]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-169 (Nachtlauf 2026-08-14, Fund ausserhalb des Schadens)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-177 · Der Seitenschlüssel hängt an einer Batching-Annahme, die kein Tor hält

B-169 sperrt die Zeilen, solange sie zum vorigen Filter gehören. Der Vergleich stützt sich auf eine
Annahme, die **im Code nur als Kommentar** steht: dass eine Antwort und ein Kriterienwechsel nie im
*selben* React-Batch committen.

## Behauptung (ungeprüft, aus dem Review)

`frontend/src/vater/VaterWizard.tsx` — der Effekt hängt an `[exercises.data]`, liest aber `filterKey`. Er
unterstellt damit, dass beim Eintreffen einer Antwort der Schlüssel noch der ist, mit dem sie abgeschickt
wurde.

Der Gegenfall: `children.data` speist über `effectiveGrade`/`effectiveSchoolType` den `filterKey`. Landen
`children.data` und eine `exercises`-Antwort im **selben** Batch, setzt der Effekt `seitenSchluessel` auf den
**neuen** Schlüssel über den Zeilen der **alten** Abfrage → das Gate ist offen, die Zeilen sind veraltet,
und kein Signal sagt es.

**Warum das heute praktisch nicht eintritt** (und die Story darum P3 ist): Es braucht `mode === "existing"`,
ein Kind mit gesetzter Klasse oder Schulart, **und** eine Kinderliste, die später antwortet als die erste
Katalogabfrage — die aber erst nach der Fachwahl startet, also lange nach dem Laden der Kinder.

## Warum es trotzdem eine Story ist

Es ist die gemessene Fehlerfamilie dieses Repos in ihrer leisesten Form: eine Bedingung, die zwei Zeitpunkte
zusammenzieht („der Schlüssel jetzt" und „der Schlüssel beim Abschicken"). Und sie hängt an **nichts**
Mechanischem: das Projekt hat kein `lint`-Skript (`frontend/package.json`), die `eslint-disable`-Zeile im
Effekt ist reine Dokumentation. Die Annahme lebt allein im Kommentar.

## Offene Punkte

1. Ist der Fall konstruierbar? Empfehlung: **erst versuchen** — ein Kind mit Klasse anlegen, die Kinderliste
   per `page.route` verzögern und beobachten. Lässt er sich nicht herstellen, ist das ein gutes Ergebnis und
   die Story wird mit Grund verworfen, nicht gebaut.
2. Wie härten? Empfehlung: den Schlüssel **beim Abschicken** in eine Ref schreiben (im `fn`-Closure von
   `useAsync`) und im Effekt `setSeitenSchluessel(keyDerAntwort.current)`. Kosten: eine Ref mehr, und der
   Schlüssel steht dann an zwei Stellen — dafür hängt die Zuordnung nicht mehr an einer Reihenfolge.
3. Gilt dasselbe für `geltenderFilterKey`? Empfehlung: nachsehen, nicht annehmen. Er wird im Effekt auf
   `[filterKey]` gesetzt, also aus derselben Quelle wie sein Vergleichswert — vermutlich unbetroffen, aber
   das ist die Art Vermutung, die diese Story gerade behandelt.

## Verlauf

- 2026-08-14 · Aufgenommen aus dem Review von B-169 im Nachtlauf. Der Reviewer hat ihn selbst als praktisch
  unerreichbar eingeordnet; er steht hier, weil die Annahme an keinem Tor hängt und beim nächsten Umbau des
  Assistenten still brechen kann. Bleibt `unverifiziert`, bis jemand versucht hat, den Fall herzustellen.
