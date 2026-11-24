---
tags: [typ/story, status/idee, rolle/supervisor, rolle/student, bereich/training]
aliases: [Berechtigt ohne Korrektur]
status: idee
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "docs/pm-sitzung-2026-11-24-k.md (frontend-reviewer, Sprint 151)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-970 · „Berechtigt“ an einer im Test richtig gewerteten Antwort: was soll es heißen?

Seit B-767 zeigt das Vater-Web „Berechtigt“ an jeder Meldung mit Test-Antwort, auch wenn der Test die Antwort **richtig**
gewertet hat (etwa Grund „zu schwer“). Der Server nimmt das an, rechnet nichts nach (`FeedbackCorrectionService`, keine
berichtigte Antwort) und macht die Meldung trotzdem endgültig. Die Rückfrage im Vater-Web und der Hinweis an das Kind
sagen dann „der Test ist nachgerechnet“, obwohl nichts zu rechnen war.

Offene Frage: Soll „berechtigt“ dort heißen „du hattest recht, die Karte ist zu schwer“ (dann andere Texte, kein
„nachgerechnet“), oder soll das Urteil nur an einer falsch gewerteten Antwort angeboten werden (dann `409` oder kein
Knopf)? Gefunden im Review von Sprint 151, bewusst nicht im Sprint entschieden (Produktfrage).

## Verlauf

- **2026-11-24** — angelegt (Nachtlauf K, Sprint 151, Review-Hinweis 🟢 ohne Zählung).
