---
name: creator
description: >-
  Drive the Pugling REST API from the CREATOR seat (api/v1/creator/*) to build catalog content —
  subjects, chapters, typed exercises, the vocabulary store, items and tags — AND, in the same pass,
  smoke-test that the Creator surface actually works and (re)write the verified Creator tutorial.
  Use this whenever the user wants to exercise/validate the Creator API, author catalog content against
  the running app, or refresh docs/tutorial-creator.md, e.g. "run the creator skill", "test the creator
  API", "creator", "Katalog-API prüfen", "Creator-Tutorial aktualisieren". This is NOT the file-based
  Lehrplan format (that is `lehrplan-autor`/`lehrplan-lerner`) — it drives the real product API.
---

# creator — der Inhalts-Ersteller (technische Rolle „Creator")

Du bist der **Creator**: du baust den **gemeinsamen, kindneutralen Lern-Katalog** (Fächer → Kapitel →
typisierte Übungen), pflegst den **Vokabel-Store** als „single source of truth" und taggst Übungen.
Im Produkt hält der **Vater** diese Rolle (zusammen mit Supervisor); der reine Creator-Archetyp ist der
**Lehrer** (Seed: *Herr Schmidt*, `fatherId=2`, PIN `9999`), der Inhalte kuratiert, ohne ein Kind zu betreuen.

Zwei Ziele in einem Lauf: **(1)** die Creator-Endpunkte end-to-end **verifizieren** und **(2)** daraus das
verifizierte Tutorial **`docs/tutorial-creator.md`** schreiben/aktualisieren. Entdeckte API-Mängel werden
**berichtet**, nicht stillschweigend im Backend gefixt (das ist eine separate Entscheidung).

## Ablauf

Alles läuft gegen eine **Wegwerf-Instanz** (Port 5280, Temp-DB) — die echte `pugling.db` bleibt unberührt,
die Seed-IDs sind stabil. Der gemeinsame Helfer `.claude/scripts/tutorial-api.sh` kapselt Start/Stop/Login.

1. **Hochfahren** (nicht abweichen — Windows-Fallstricke stecken im Helfer):
   - `bash .claude/scripts/tutorial-api.sh build`
   - `bash .claude/scripts/tutorial-api.sh stop` (vor-aufräumen)
   - `bash .claude/scripts/tutorial-api.sh serve` **im Hintergrund** (`run_in_background: true`)
   - `bash .claude/scripts/tutorial-api.sh wait`
   - Läuft eine Dev-Instanz und blockiert der Build den Datei-Lock: den `Pugling.Api`-Prozess stoppen.
2. **Einloggen** als Creator: `source .claude/scripts/tutorial-api.sh; TOK=$(login_father 2 9999)`.
   Kontrolle: `api_get /api/v1/auth/me` → `roles:["Creator","Supervisor"]`, `fatherId:2`.
3. **Katalog-Flow durchspielen und jede Antwort prüfen** (Status + Schlüsselfelder). Reihenfolge:
   - `POST /api/v1/creator/subjects` `{"name":"…"}` → merke `id`.
   - `POST …/subjects/{id}/chapters` `{"name":"…","orderIndex":1}` → merke `id`.
   - `GET /api/v1/creator/vocabulary?take=2` → Store-Einträge; Refs sind **ID-basiert** (`vocabularyId`).
   - `POST …/chapters/{id}/vocabulary` mit `config.refs=[{"vocabularyId":…}]`. **Fallstrick:** Sollen später
     inline `{"front","back"}`-Items angelegt werden, muss die Config `sourceLang`+`targetLang` tragen —
     sonst liefert der Item-POST `400 validation_error` („Provide an existing vocabularyId, or front and
     back plus the exercise's sourceLang/targetLang"). Zeige **beide** Wege: Item per `{"vocabularyId":…}`
     und (auf einer sprachtragenden Übung) inline `{"front":"…","back":"…"}`.
   - `GET …/vocabulary/{exerciseId}/items` → materialisierte Items (Tabelle, `_self`-Links).
   - `GET /api/v1/creator/exercises?type=Vocabulary&take=3` → kindneutrale Katalogsuche (Metadaten,
     `isOwn` nach `authorFatherId`).
   - `GET /api/v1/creator/exercises/{id}/preview` → **Testmodus**: Aufgaben ohne Lösung (bzw. `reveal` je
     Stufe), nebenwirkungsfrei — der Creator probiert die Übung selbst durch.
   - Optional: ein zweiter Übungstyp (`…/cloze`, `…/matching`, `…/arithmetic`) und `/api/v1/creator/tags`.
   - `GET /api/v1/creator/exercise-types` → Typ-Manifest (12 Typen; auch für Student lesbar).
4. **Tutorial schreiben/aktualisieren**: `docs/tutorial-creator.md` mit den **echt beobachteten** Requests
   und den für den nächsten Schritt relevanten Response-Feldern (volle Bodies per Link auf
   `docs/api-examples/catalog.md`). Konvention aus `docs/obsidian.md`: Frontmatter-Tags
   `[typ/tutorial, bereich/katalog, rolle/creator, lerntechnik/vokabeln]`, **nur relative** MD-Links,
   „Verwandt"-Footer. Erklär einmal die Brücke: *Der Vater hält Creator+Supervisor; der Lehrer ist reiner
   Creator.* Für die Tiefe auf `wiki/03-uebungstypen.md` verweisen (nicht duplizieren).
5. **Herunterfahren & Bericht**: `bash .claude/scripts/tutorial-api.sh stop`. Danach knapp: welche Schritte
   grün/rot, welche API-Mängel aufgefallen sind (z. B. der inline-Item-Sprachen-Fallstrick), und dass
   `pugling.db` unberührt blieb.

## Regeln

- **Rolle sauber halten**: nur `api/v1/creator/*` (+ `auth`, `exercise-types`). Plan/Shop/Spielen gehören
  Supervisor/Student — die haben eigene Skills.
- **Verifizieren, nicht behaupten**: kein Schritt gilt als „grün", ohne die Antwort geprüft zu haben.
- **ASCII in `-d`-Bodies** (Git-Bash verstümmelt Umlaute), relative Temp-DB, Server als Hintergrund-Task.
- **Mängel melden, nicht heimlich fixen.** Ein Backend-Fix ist eine eigene, bewusste Aufgabe.
