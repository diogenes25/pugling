---
tags: [typ/tutorial, bereich/training, rolle/creator, rolle/supervisor, rolle/student]
---

# Pugling — Tutorials nach Rolle (API)

Pugling ist **API-First**: Die REST-API (`api/v1`, Swagger unter `/swagger`) ist das Produkt. Die
Anleitung folgt den **drei Ebenen** — **Creator** (Inhalte), **Supervisor** (Steuerung), **Student**
(Lernen). Jedes Tutorial ist eigenständig, mit **verifizierten** Requests/Responses (gegen die laufende
API geprüft, nicht von Hand erfunden).

**Rollen vs. Produkt-Metapher:** Technisch gibt es Creator/Supervisor/Student. Im Produkt hält der **Vater**
zugleich **Creator + Supervisor** (Login `auth/father`), der **Sohn** ist der **Student** (Login `auth/child`).
Der reine Creator-Archetyp ist der **Lehrer** (Seed: *Herr Schmidt*), der nur Katalog kuratiert.

## Die drei Tutorials

| Ebene | Wer im Produkt | Worum es geht | Tutorial |
| --- | --- | --- | --- |
| **Creator** | Vater / Lehrer | Lern-Katalog bauen: Fächer, Kapitel, typisierte Übungen, Vokabel-Store, Tags | [tutorial-creator.md](tutorial-creator.md) |
| **Supervisor** | Vater | Steuern & belohnen: Study-Pläne, Positionen, Ziele, Shop, Missionen, Kontrolle | [tutorial-supervisor.md](tutorial-supervisor.md) |
| **Student** | Sohn | Lernen & einlösen: Tagesmission, Üben, Abschlusstest, Fortschritt, Münzen | [tutorial-student.md](tutorial-student.md) |

Der natürliche End-to-End-Loop läuft in dieser Reihenfolge: **Creator baut → Supervisor weist zu & belohnt
→ Student lernt & löst ein.** Die drei Tutorials verlinken einander an den Übergabepunkten.

## Grundlagen (für alle drei)

- Basis-URL der Beispiele: `http://localhost:5200` (siehe [Backend starten](../CLAUDE.md)). Geschützte Aufrufe
  brauchen den JWT im Header `Authorization: Bearer <token>`.
- **Seed-Zugänge:** Vater `fatherId=1` PIN `0000` (Creator+Supervisor) · Lehrer `fatherId=2` PIN `9999`
  (reiner Creator) · Sohn `childId=1` PIN `1111` (Student). Login-Antwort trägt `role` = `Supervisor`
  bzw. `Student`.
- Volle, verifizierte Response-Bodies liegen unter [api-examples/](api-examples/index.md) (aus echten
  Aufrufen erzeugt von `DocsCaptureTests`).

## Selbst prüfen / neu erzeugen

Die drei Tutorials werden von den gleichnamigen **Rollen-Skills** `creator`/`supervisor`/`student`
geschrieben, die die API von ihrem Rollen-Sitz aus gegen eine **Wegwerf-Instanz** (Port 5280, Temp-DB;
Helfer `.claude/scripts/tutorial-api.sh`) treiben und dabei die Funktionalität mit-testen. Ein schneller
Kern-Check läuft über `/smoke-test`.

---

**Verwandt:** [Rollen-Doku (Einstieg je Ebene)](rollen-doku.md) · [Grundprinzip: drei Ebenen](grundprinzip.md) ·
[Endpunkt-Beziehungen](endpunkt-beziehungen.md) · [wiki/01 · Überblick](../wiki/01-ueberblick-architektur.md) ·
[wiki/07 · API-Referenz](../wiki/07-api-referenz.md)
