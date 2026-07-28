---
tags: [typ/plan, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Perspektiven, Betreuen, Zuweisen, Erstellen, Vater-Rollentrennung]
---

# Umbau: Das Vater-Web trennt Betreuen, Zuweisen und Erstellen

Status: **Umgesetzt** (2026-07-28). `npm run build`, 21 Vitest und 19 Playwright-E2E grün. Baut auf
[vater-informationsarchitektur-plan.md](vater-informationsarchitektur-plan.md) auf – dort wurde die
Navigation gruppiert, hier wird sie **getrennt**.

## Das Problem

Ein Vater-Konto trägt zwei Rollen: **Creator** und **Supervisor** ([grundprinzip.md](grundprinzip.md)).
API und Ordner sind danach geschnitten, das UI war es nicht – es zeigte beide Rollen gleichzeitig.
„Belohnungen" stand neben „Lehrwerke". Wer sein Kind steuern wollte, sah Autorenwerkzeug; wer Inhalte
baute, sah Münz-Verwaltung. Die Gruppierung der Nav (Etappe 1 des Vorgängerplans) hat das gemildert und
damit auch bewiesen, dass eine Gruppe im selben Blickfeld eben doch dieselbe Aufmerksamkeit kostet.

## Die drei Perspektiven

| Perspektive | Die Frage, die sie beantwortet | Startseite |
|---|---|---|
| 👀 **Betreuen** | Wie läuft es bei deinem Kind – und was steuerst du daran? | `/vater` |
| 🎯 **Zuweisen** | Welches Kind lernt welchen Stoff – mit welcher Pflicht? | `/vater/plaene` |
| ✏️ **Erstellen** | Welcher Stoff existiert überhaupt? | `/vater/inhalte` |

Sie sind **keine Rechte** – ein Vater darf alles – sondern eine Antwort auf „woran arbeite ich gerade".

**Warum „Zuweisen" eigenständig ist** und nicht zu „Betreuen" gehört: Es ist die einzige Stelle, an der
sich beide Rollen treffen – es *verbraucht* den Katalog des Creators und *erzeugt* die Pflicht des
Supervisors. Und es hat ein eigenes Vokabular (Position, Rhythmus, Bestehensgrenze, Münz-Malus), das die
anderen zwei nie brauchen. Als Anhängsel des Dashboards war es genau deshalb schlecht aufgehoben.

## Umsetzung

**Die Architektur liegt als Daten in [navigation.ts](../frontend/src/vater/navigation.ts)**, nicht als JSX
in der Kopfzeile: je Perspektive Startseite, Beschriftung, Zweck und Bereiche. Die Kopfzeile rendert nur.

**Die aktive Perspektive kommt aus dem Pfad** (`perspectiveOfPath`), nicht aus einem State. Ein State hätte
bedeutet: Lesezeichen öffnen → richtige Seite, falsche Perspektive, und die Navigation zeigt etwas anderes
als der Inhalt. Aus demselben Grund **wandert keine Route**: kein `/vater/betreuen/...`-Präfix, dessen
Nutzen kosmetisch wäre und dessen Preis alle Links, Lesezeichen und fünf E2E gewesen wären.

Zwei Fallen, die die Zuordnung hat und die `navigation.test.ts` festnagelt: `/vater` ist Präfix von allem
(darf also nur exakt treffen), und `/vater/plaene` sieht `/vater/plan/7` zum Verwechseln ähnlich.

**Der Umschalter besteht aus Links**, nicht aus Knöpfen: jede Perspektive hat eine Startseite, der Wechsel
ist also eine Navigation und gehört in die Historie. Die aktive trägt `aria-current="page"` – abgeleitet aus
dem Pfad, denn auf `/vater/kind/3` ist keine der drei Startseiten aktiv und „Betreuen" muss es doch sein.

**Nach dem Anmelden führt die zuletzt *bewusst gewählte* Perspektive** (nur der Klick auf den Umschalter
zählt, nicht jede Navigation): ein Lehrer landet in seiner Werkstatt statt jedes Mal an der Vater-Sicht
vorbei. Der Sprung greift **nur** beim Anmelde-Übergang und nur auf `/vater` – bei jedem Besuch von
`/vater` wäre der Umschalter unbenutzbar geworden (Klick auf „Betreuen" → sofort zurückgeworfen).

### Neue Startseiten

- **`/vater/plaene`** (`VaterPlaene.tsx`) – die Plan-Liste, vorher der dritte Abschnitt des Dashboards.
  Dazu der Assistent als geführter Weg und die Warnung **„mehrfach aktiv"**: nur ein aktiver, laufender
  Plan je Kind ist spielbar, und das soll der Vater hier sehen statt beim Sohn zu entdecken.
- **`/vater/inhalte`** (`VaterInhalte.tsx`) – die **Werkstatt**. Der Creator hatte vorher keinen
  Ausgangspunkt: sieben Bereiche standen gleichrangig in der Navigation, ohne zu verraten, welcher der
  Einstieg ist und wie sie zusammenhängen. Die Kacheln sind nach dem *Arbeitsweg* sortiert, nicht
  alphabetisch: Werkstück → Bausteine → Materialkunde. Bewusst **ohne Kind-Bezug**.
- **`/vater`** (`VaterDashboard.tsx`) behält Heute + Kinder; die Pläne sind zu einer Zeile mit Zahl und
  Weg geschrumpft („9 Pläne, davon 2 aktiv").

## Was beim Durchspielen als Vater und als Lehrer aufgefallen ist

Der Teil, der nur im Browser sichtbar wird – jeder Punkt war ein echter Fehler in der Handhabung:

1. **„+ Neuer Plan" verlor das gefilterte Kind.** Auf `/vater/plaene?childId=2` führte der Knopf in ein
   Formular, in dem das *erste* Kind vorausgewählt war: wer für Kind 2 einen Plan anlegte, baute ihn
   stillschweigend für Kind 1. Die Auswahl reist jetzt als Query mit (`VaterPlanCreate` liest sie).
2. **„Zum ersten Mal hier?" stand über neun Plänen.** Die Überschrift der Assistenten-Kachel richtet sich
   jetzt nach dem Bestand – eine falsche Aussage macht den Rest der Seite unglaubwürdig.
3. **Der Rückweg aus einem Plan zeigte auf `/vater`**, wo seit der Trennung keine Pläne mehr liegen. Er
   führt jetzt zur Plan-Liste.
4. **Sieben Stellen nannten die Startseite „Kinder & Pläne"** – ein Name, der nach dem Auszug der Pläne
   nicht mehr stimmte.
5. **Kein sichtbarer Fokusring** an Umschalter, Bereichs-Nav und Werkstatt-Kacheln; der Standard-Ring des
   Browsers verschwindet auf der dunklen Fläche zu leicht.

## Tests

`frontend/src/vater/navigation.test.ts` (12 Fälle) nagelt die Pfad-Zuordnung und den gemerkten Wert fest –
inklusive der Prüfung, dass ein verbogener `localStorage`-Eintrag verworfen wird statt in einer Navigation
auf `undefined` zu laufen.

`frontend/e2e/perspektiven.spec.ts` prüft die eigentliche Zusage: dass in einer Perspektive die Bereiche
der anderen **nicht** auftauchen. Ein Test, der nur „Link existiert" prüft, hätte den Zustand vorher
genauso bestanden. Dazu die Landung des Lehrers nach dem Anmelden und die Perspektive beim Direktaufruf
einer Unterseite.

## Offen

Ein **eigenes Lehrer-Konto** gibt es weiterhin nicht: „Lehrersicht" ist heute die Creator-Perspektive
desselben Vater-Kontos (`VaterApp` verlangt `role === "Supervisor"`). Der Entwurf dafür liegt in der
Feature-Idee „Lehrer-Hausaufgaben" (Teacher-Entität, Beitrittscode, Ownership-Umkehr) – erst damit wird aus
der Perspektive eine echte Rollentrennung mit eigenem Login.
