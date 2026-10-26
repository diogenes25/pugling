---
tags: [typ/story, status/idee, bereich/test, bereich/backend]
aliases: [AtomaresSchreiben_KeineLeseFehler flackert wieder]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Test-Tor (Stop-Hook) in Lauf B, docs/pm-sitzung-2026-10-26-tag.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-165]
wartet_auf: ""
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

## Verlauf

- **2026-10-26** — angelegt in Lauf B ([Protokoll](../pm-sitzung-2026-10-26-tag.md)). Liegt außerhalb des
  Sprint-Diffs (Freigabe 3: nicht gezählt).
