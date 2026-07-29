---
tags: [typ/referenz, bereich/doku]
aliases: [Anmerkungen-Export]
---

# Anmerkungen – Exporte

Hier landen die Markdown-Schnappschüsse aus `GET api/v1/remarks/export`, geschrieben vom Skill
`anmerkungen`. **Nicht von Hand bearbeiten** – die Quelle ist die Datenbank; Änderungen wären beim
nächsten Export weg. Status und Antworten ändert der Skill über die API.

Wozu der Umweg über eine Datei, wenn es die API gibt? Zwei Gründe:

1. **Die Test-Skills kommen nicht an die Datenbank.** `creator`, `supervisor`, `student` und
   `/smoke-test` laufen bewusst gegen eine Wegwerf-DB (`pugling_smoke.db`, wird nach dem Lauf gelöscht).
   Die echten Anmerkungen sehen sie **nur** als Datei im Repo.
2. **Nacharbeiten ohne laufenden Server.** Der Export ist ein eingefrorener, versionierter Stand.

Erzeugen:

```bash
export TUTORIAL_API_BASE=http://localhost:5200
source .claude/scripts/tutorial-api.sh
TOK=$(login_adult 1 0000)
# Ohne status-Filter: liefert offene UND eingeplante Einträge. Ein `?status=Open` wäre hier falsch –
# die Rollen-Skills sollen ausdrücklich auch `eingeplant` sehen, und genau die tragen schon eine Analyse.
curl -sf "$TUTORIAL_API_BASE/api/v1/remarks/export" -H "Authorization: Bearer $TOK" \
  -o docs/anmerkungen/aktuell.md || echo "Export fehlgeschlagen – Datei unveraendert gelassen"
```

`curl -sf` statt des Helfers `api_get`: Der schluckt Fehler nicht, sondern bricht ab. Sonst landete bei
einem 401/403 die ProblemDetails-JSON **in der Exportdatei**, und die Rollen-Skills läsen einen
Fehlerkörper als Anmerkungsliste.

## Für die Rollen-Skills: der Startschritt

`creator`, `supervisor` und `student` lesen diese Exporte, **bevor** sie ihren Durchlauf beginnen. Damit
wird aus einer Beobachtung des Nutzers eine gezielte Testanweisung: Statt nur den Standardpfad abzuspulen,
prüft der Skill genau das nach, was aufgefallen war.

Der Schritt ist **optional und darf nie blockieren**:

1. Gibt es hier keine Datei (oder ist sie leer), überspringen – ohne Kommentar, ohne Fehler.
2. Sonst die Einträge lesen und die zum **eigenen Bereich** heraussuchen (Zuordnung über die Zeile
   `- **Wo:** \`/route\` (bereich)` je Eintrag; welche Routen zu welcher Rolle gehören, steht im
   jeweiligen Skill).
3. Nur `offen` und `eingeplant` sind interessant – `erledigt`/`verworfen` überspringen.
4. Im Durchlauf gezielt nachtesten und im Abschlussbericht sagen, was dabei herauskam: bestätigt,
   nicht reproduzierbar, oder inzwischen behoben.

**Der Skill ändert den Status nicht.** Er läuft gegen eine Wegwerf-DB und sähe die echte Anmerkung gar
nicht; sein Befund geht in den Bericht, das Nachtragen erledigt der Skill `anmerkungen` gegen die echte
Instanz. Ein Export ist außerdem ein *eingefrorener* Stand – er kann veraltet sein, und genau das ist Teil
des Nachtestens.

Verwandt: [Plan](../anmerkungen-plan.md) · [Skill](../../.claude/skills/anmerkungen/SKILL.md)
