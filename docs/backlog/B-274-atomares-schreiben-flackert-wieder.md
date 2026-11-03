---
tags: [typ/story, status/abgenommen, bereich/test, bereich/backend]
aliases: [AtomaresSchreiben_KeineLeseFehler flackert wieder]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Test-Tor (Stop-Hook) in Lauf B, docs/pm-sitzung-2026-10-26-tag.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-165]
wartet_auf: ""
nachgeschaut: "2026-11-03"
---

# B-274 · `AtomaresSchreiben_KeineLeseFehler` flackert wieder

Am 2026-10-26 war `OpenApiExampleCatalogConcurrencyTests.AtomaresSchreiben_KeineLeseFehler` im Test-Tor (Stop-Hook,
`dotnet test Pugling.sln -c Release`) rot: **1 failed / 917 passed**. Unmittelbar davor und danach war derselbe Stand
grün: die volle Suite zweimal **918/918**, der Test einzeln **3 von 3**. Zur selben Zeit lief ein Reviewer-Agent im
selben Arbeitsbaum, die Maschine war also zusätzlich belastet.

[B-165](B-165-backend-suite-flackert.md) hat genau dieses Flackern als behoben abgenommen. Der Test duldet seither
gesperrte Lesezugriffe, verlangt aber `ok > gesperrt` je Durchgang. **Unbekannt ist, welche der beiden Zusicherungen
fiel**, denn das Test-Tor gibt nur den Testnamen weiter. Naheliegend, aber ungemessen: Unter Last überwiegen in
einem Fenster die gesperrten Lesezugriffe (Virenscanner, B-181), und `ok > gesperrt` fällt, ohne dass das Schreiben
unatomar ist.

Nächster Schritt: die Meldung beim nächsten Auftreten festhalten (z. B. `--logger "console;verbosity=detailed"` im
Hook oder die `.trx` aus `TestResults/`) und erst dann entscheiden.

## User Story

Als **Entwickler** möchte ich, dass `AtomaresSchreiben_KeineLeseFehler` nur dann rot wird, wenn das Schreiben nicht mehr
atomar ist – nicht, weil ein einzelnes Fenster unter Last überwiegend gesperrt war.

## Ist-Stand (Lauf O, am Code belegt, Stand `cc19020`)

- Die Probe verlangte `ok > gesperrt` **je Fenster** (`OpenApiExampleCatalogConcurrencyTests.cs:298`). Zweimal im
  Test-Tor rot, beide Male unter Zusatzlast (Reviewer bzw. Playwright parallel); einzeln und unmittelbar danach grün.
  Welche Zusicherung fiel, war nicht festgehalten.
- **Gemessen in Lauf O:** der unsichere Schreiber ergibt über zehn Fenster 925 ok gegen 13 639 gesperrt – nicht in jedem
  Fenster null Erfolge (Lesezugriffe vor und nach der Pause gelingen), aber in jedem Fenster eine klare Mehrheit gesperrt.

## Die echte Lücke

Ein Kriterium je Fenster misst die Last der Maschine mit; die Eigenschaft „atomar" ist eine über die Probe.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **„Gehalten" wird über alle zehn Fenster beurteilt** (`ok gesamt > gesperrt gesamt`), zerrissener Inhalt weiter je
   Fenster und schon bei einem Fund. *Kosten:* ein Schreiber, der nur in wenigen Fenstern hält, fiele nicht mehr auf – das
   ist nicht die Fehlerklasse, die B-57 behoben hat (das unsichere Schreiben hält in jedem Fenster).
2. **Das Flackern selbst ist nicht reproduzierbar** und darum nicht als rote Probe zu haben. Belegt ist, dass die
   Mutante weiter fällt und die Meldung jetzt sagt, welcher Fall vorlag; ob das Flackern weg ist, zeigt erst die Zeit.
   Tritt es wieder auf, steht die Diagnose in der Meldung.

## Akzeptanzkriterien

1. Die Mutante „unsicherer Schreiber" bleibt rot.
2. Die Meldung nennt jedes Fenster.

## Schätzung

`XS`, nur Tests. Testweg: `OpenApiExampleCatalogConcurrencyTests`.

## Verlauf

- **2026-10-26** — angelegt in Lauf B ([Protokoll](../pm-sitzung-2026-10-26-tag.md)). Liegt außerhalb des
  Sprint-Diffs (Freigabe 3: nicht gezählt).
- **2026-10-26** — zweites Auftreten im Test-Tor (Lauf D, 946/947): Zur selben Zeit lief Playwright voll im Hintergrund (Backend-Build, Dev-Server, Browser). Beide Male war die Maschine also zusätzlich belastet, das stützt die Vermutung „unter Last überwiegen gesperrte Lesezugriffe“. Gemessen ist sie damit noch nicht, denn die Assertion-Meldung fehlt weiter.
- **2026-11-03** — Lauf O, Teil 1: gebaut (mit B-265). AK 1 → Mutante „unsicherer Schreiber" **1 failed** an der
  Summen-Zusicherung (nicht an der Verhungern-Zeile, Meldung gelesen); AK 2 → dieselbe Meldung mit allen zehn Fenstern.
  Probe grün **2/2**. Keine rote Probe für das Flackern selbst – nicht reproduzierbar (Entscheidung 2).
- **2026-11-03** — Review `pugling-reviewer`: Entscheidung 1 als vertretbare Abschwächung bestätigt (ein Schreiber, der nur in 1–5 von 10 Fenstern hält, bliebe unter Windows grün; der echte Schreiber ist deterministisch, unter Linux fängt die Zerrissen-Regel jedes Fenster). 🟡 der Kommentar in der Schleife beschrieb noch die alte Regel und „1867 von 1867" → gekürzt, Begründung mit den Lauf-O-Zahlen an die Summen-Zusicherung verschoben (nur Kommentar, ohne Zählung).
- **2026-11-03** — Abnahme Lauf O, Teil 1: volle Suite **1048/1048**, Build ohne Warnung, E2E **103 passed** (`exit=0`, nach dem letzten Edit; der Dev-Server lädt den Beispielkatalog über `Load`). Rollengang: der Entwickler ist die Rolle – die Proben selbst sind sein Weg, ihre Mutanten je benannt; Creator, Vater und Sohn Regressionszeugen (kein Pfad von ihnen im Diff, ihre Specs grün). **Abgenommen**, Commit siehe Protokoll ([Lauf O](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-03** — Nachschau in Lauf P, Teil 1 (frischer `pugling-reviewer`, read-only): **sauber**. Geprüft: die Summe über zehn Fenster nimmt das Ein-Fenster-Verhungern aus dem Flackern; ein falsches Rot verlangt, dass über alle Fenster die Mehrzahl der Lesezugriffe gesperrt ist (Entscheidung 2 nimmt das an); `:297` nennt alle zehn Fenster.
