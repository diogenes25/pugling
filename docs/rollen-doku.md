---
tags: [typ/referenz, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Rollen-Doku, Doku nach Rollen, Creator Supervisor Student]
---

# Doku nach Rollen: Creator, Supervisor, Student

Die API und der Code sind bereits nach den drei Ebenen geschnitten (`api/v1/creator`,
`api/v1/supervisor`, `api/v1/student`). Diese Seite ordnet die vorhandene Doku genauso ein: Wer aus
einer Rolle kommt, findet hier den direkten Einstieg und die angrenzenden Themen.

← [Zurück zum Wiki-Index](../README.md) · Fachliches Modell: [grundprinzip.md](grundprinzip.md)

---

## Creator — Inhalte und Katalog

Der Creator baut den globalen Lern-Katalog: Fächer, Kapitel, Übungen, Stores und Metadaten. Heute trägt
der Vater technisch meist auch diese Rolle, fachlich bleibt sie aber getrennt vom Supervisor.

| Einstieg | Wofür |
| --- | --- |
| **[Creator-Tutorial](tutorial-creator.md)** | Verifizierter Durchlauf: Fach → Kapitel → typisierte Übung → Items → Katalogsuche → Testmodus. |
| [wiki/03 · Übungstypen](../wiki/03-uebungstypen.md) | Katalogmodell, alle Übungstypen, Configs, Stores und Checks. |
| [wiki/08 · Erweitern](../wiki/08-erweitern.md) | Neue Übungstypen oder Lernverfahren im bestehenden Katalogmuster ergänzen. |
| [docs/uebungs-meta-und-versionierung.md](uebungs-meta-und-versionierung.md) | Metadaten, Versionierung und Vorfilterung von Katalog-Übungen. |
| [docs/vokabel-funktionalitaeten-entwickler-tutorial.md](vokabel-funktionalitaeten-entwickler-tutorial.md) | Vokabel-Store, `ExerciseItem`s und Authoring-Flows. |
| [docs/api-examples/catalog.md](api-examples/catalog.md) | Verifizierte Beispiel-Requests für Katalog-Endpunkte. |

Typische Creator-Frage: "Welche Übung existiert, wie wird sie gespeichert, und wie kann sie später in
Plänen wiederverwendet werden?"

## Supervisor — Steuerung, Kontrolle, Belohnung

Der Supervisor macht aus Katalog-Inhalten verbindliche Aufgaben für ein Kind: Study-Pläne, Positionen,
Ziele, Punkte, Shop, Missionen, Klassenarbeiten und Auswertung.

| Einstieg | Wofür |
| --- | --- |
| **[Supervisor-Tutorial](tutorial-supervisor.md)** | Verifizierter Durchlauf: Plan-Container → Positionen (Ziel/Punkte) → Lernziele → Shop → Missionen → Kontrolle. |
| [wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md) | Punkte, Coins/Gems, Missionen, Auszeichnungen, Angebote und Shop. |
| [docs/endpunkt-beziehungen.md](endpunkt-beziehungen.md) | Datenfluss Übung → Lehrplan → Kind → Auswertung. |
| [docs/klassenarbeiten-tagging.md](klassenarbeiten-tagging.md) | Klassenarbeiten planen, Übungen taggen und gezielt wiederholen. |
| [docs/api-examples/study-plans.md](api-examples/study-plans.md) | Verifizierte Study-Plan-Beispiele. |
| [docs/api-examples/children.md](api-examples/children.md) | Kind-Verwaltung, Fortschritt und supervisornahe Kind-Endpunkte. |
| [docs/api-examples/shop.md](api-examples/shop.md) | Shop, Angebote, Käufe und Aktivierungen. |
| [docs/api-examples/class-tests.md](api-examples/class-tests.md) | Klassenarbeiten-Endpunkte. |

Typische Supervisor-Frage: "Was muss dieses Kind bis wann erledigen, wie wird es belohnt, und woran
sehe ich den Lernstand?"

## Student — Lernen, Fortschritt, Einlösen

Der Student sieht nur die eigenen Pläne und erledigt die Aufgaben. Der Server bewertet Antworten,
bucht Punkte, schützt vor Selbstbetrug und zeigt Fortschritt, Missionen und verfügbare Belohnungen.

| Einstieg | Wofür |
| --- | --- |
| **[Student-Tutorial](tutorial-student.md)** | Verifizierter Durchlauf: Tagesmission → Üben → Abschlusstest → Fortschritt → Münzen einlösen. |
| [docs/tutorial.md](tutorial.md) | Rollen-Tutorial-Index (Creator/Supervisor/Student) + gemeinsame Grundlagen. |
| [docs/api-examples/me.md](api-examples/me.md) | Student-`me`-Endpunkte für Punkte, Missionen, Auszeichnungen und Angebote. |
| [docs/api-examples/vocabulary.md](api-examples/vocabulary.md) | Vokabel-Lernstand und Wiederholungsdaten aus Student-/Kind-Sicht. |
| [docs/api-examples/auth.md](api-examples/auth.md) | Login und Token als Voraussetzung für Student-Flows. |

Typische Student-Frage: "Was ist heute dran, wie gebe ich Antworten ab, was habe ich verdient, und was
kann ich einlösen?"

## Übergreifende Orientierung

Diese Seiten betreffen alle drei Rollen oder erklären die Doku selbst:

| Einstieg | Wofür |
| --- | --- |
| [docs/grundprinzip.md](grundprinzip.md) | Fachliches Drei-Ebenen-Modell. |
| [wiki/01 · Überblick & Architektur](../wiki/01-ueberblick-architektur.md) | Technische Landkarte über Rollen, Datenmodell, Services und End-to-End-Loop. |
| [wiki/02 · Authentifizierung](../wiki/02-authentifizierung.md) | Accounts, Rollenclaims, Ownership und Anti-Schummel-Regeln. |
| [wiki/07 · API-Referenz](../wiki/07-api-referenz.md) | Endpunkt-Index, jetzt zusätzlich nach Rollen gegliedert. |
| [wiki/09 · LLM-Kochbuch](../wiki/09-llm-kochbuch.md) | Prompt → fertiger Lernplan über die API. |
| [docs/obsidian.md](obsidian.md) | Doku-Konventionen, Tags und Wissenskarte. |
| [docs/architektur-entscheidung.md](architektur-entscheidung.md) · [docs/architektur-resumee.md](architektur-resumee.md) | Architekturentscheidungen und aktuelles Systembild. |

## Konvention für neue Doku

Neue Doku bekommt neben `typ/...` und `bereich/...` mindestens einen Rollen-Tag:

```yaml
tags: [typ/tutorial, bereich/training, rolle/supervisor]
```

- `rolle/creator` für Katalog, Übungstypen, Stores, Inhalts-Authoring.
- `rolle/supervisor` für Study-Pläne, Ziele, Kontrolle, Punkte, Shop, Klassenarbeiten.
- `rolle/student` für Üben, Tests, Fortschritt, Punkte-Sicht und Einlösen.
- Rollenübergreifende Seiten können mehrere Rollen-Tags tragen.

---

**Verwandt:** [grundprinzip.md](grundprinzip.md) · [wiki/01 · Überblick](../wiki/01-ueberblick-architektur.md) ·
[wiki/07 · API-Referenz](../wiki/07-api-referenz.md) · [obsidian.md](obsidian.md)
