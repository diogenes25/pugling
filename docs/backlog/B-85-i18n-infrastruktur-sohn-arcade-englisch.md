---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [i18n Sohn-Arcade, Erste Teilstory Mehrsprachigkeit, Englisch Arcade]
status: geschaetzt
prio: P3
art: Wunsch
groesse: L
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-38 (geteilt)
ersetzt_durch: []
---

# B-85 · i18n-Infrastruktur + Sohn-Arcade auf Englisch (erste Teilstufe der Mehrsprachigkeit)

Erste, realistisch schätzbare Teilstufe aus dem geteilten [B-38](B-38-mehrsprachige-oberflaeche.md)
(Entscheidung 8 dort: „ein Programm, keine Story" — der volle Umfang, Vater-Web und Französisch
eingeschlossen, sprengt jeden Anker dieses Bereichs). Diese Story liefert die Laufzeit-Infrastruktur
(i18n-Bibliothek, Sprachumschalter, `<html lang>`) und übersetzt **nur** die Sohn-Arcade ins Englische —
als Machbarkeitsnachweis, auf dem die restlichen Teile (Vater-Web, Französisch, Server-Sprachfeld,
Übungstyp-Manifest-Schlüssel) später aufbauen.

## User Story

> Als **Kind, das Englisch lernt**, möchte ich meine Arcade **auf Englisch** stellen können, damit die
> Sprache im Alltag vorkommt und nicht nur in der Übung — Immersion als Lerneffekt, nicht als Einstellung.

## Ist-Stand am Code

Übernommen aus der Recherche zu B-38 (dort mit vollen Belegen, hier die für diese Teilstufe relevanten):

- Keine i18n-Bibliothek eingebunden (`frontend/package.json`, geprüft per grep auf
  `react-i18next`/`i18next`/`react-intl`/`formatjs`/`lingui` — null Treffer, auch nicht transitiv).
- `frontend/index.html:2` trägt `lang="de"` hart codiert, kein Mechanismus führt das mit einer Sprachwahl mit.
- Kein Sprachfeld an `Adult`/`Child` (`backend/Pugling.Api/Models/AdminEntities.cs`) — für diese Teilstufe
  auch nicht nötig (Sprachwahl bleibt client-seitig, siehe Entscheidung 2).
- Sohn-Arcade: 12 `.tsx`-Dateien unter `frontend/src/sohn/`, dazu die von ihr genutzten geteilten
  Komponenten aus `frontend/src/components/` (20 Dateien, teils auch vom Vater-Web verwendet — nur die
  tatsächlich von der Arcade eingebundenen sind hier im Scope).
- `frontend/src/lib/fieldHelp.ts` (305 Zeilen Fließtext) ist laut B-38-Recherche überwiegend Vater-Web-Text
  — betrifft diese Teilstufe kaum bis gar nicht.
- Playwright-Specs unter `frontend/e2e/`, die Sohn-Arcade-Text prüfen (`full-flow.spec.ts` u. a.), hängen
  heute an deutschem Wortlaut.

## Die echte Lücke

Zwei unabhängige Bausteine, die zusammen den Machbarkeitsnachweis ergeben: die Laufzeit-Infrastruktur
(Bibliothek, Sprachumschalter, `<html lang>`-Kopplung) existiert überhaupt nicht; und der Sohn-Arcade-Text
liegt vollständig als deutsches Literal im JSX statt als übersetzbarer Schlüssel.

## Offene Punkte

Alle aus B-38 für diesen Teilscope relevanten Punkte sind bereits dort entschieden worden (Entscheidungen
1–3, 6, 7 aus B-38 gelten unverändert); hier folgen sie noch einmal als Entscheidungen dieser Story, damit
sie eigenständig lesbar bleibt.

## Entscheidungen

1. **i18n-Bibliothek: `react-i18next` (mit ICU-Plural-Support), kein Eigenbau.** Begründung: Französisch
   (spätere Teilstufe) hat andere Pluralregeln als Deutsch/Englisch, eine selbstgebaute Ersetzung würde
   das falsch machen — ein etabliertes Werkzeug jetzt einzuführen erspart eine zweite Migration später.
   Kosten: eine neue Laufzeit-Abhängigkeit; die Peer-Konflikt-Historie mit `vite-plugin-pwa`/`vite@8`
   ([B-25](B-25-vite-pwa-peer-konflikt.md)) muss vor dem `npm install` geprüft werden (analoger Check wie
   dort dokumentiert).
2. **Sprachwahl lebt client-seitig (`localStorage`), kein Server-Feld.** Begründung: ein Feld an
   `Adult`/`Child` wäre eine Migration und Vertragsänderung, bevor auch nur eine Zeile Text übersetzt ist.
   Kosten: die Wahl geht bei Geräte-/Profilwechsel verloren; „Vater Deutsch, Kind Englisch am selben
   Gerät" läuft über getrennte Logins, nicht über ein Server-Feld.
3. **Nur UI-Chrome wird übersetzt, Nutzerdaten (Missions-/Auszeichnungstitel, Shop-Artikel, Plan-/
   Übungstitel) bleiben unverändert.** Begründung: der Vater gibt diese Texte selbst ein und erwartet sein
   eigenes Wort wieder. Kosten: keine automatische Erkennung — jede Komponente muss einzeln zwischen
   Chrome und Nutzerdaten unterschieden werden.
4. **Übungstyp-Anzeigenamen bleiben in dieser Teilstufe Deutsch.** Begründung: die Umstellung auf
   Vertrags-Schlüssel ist ein eigener Vertragsbruch mit Streuwirkung in drei weiteren Projekten (siehe
   [B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md)) und nicht Teil dieses Machbarkeitsnachweises.
   Kosten: ein benannter, kein übersehener Bruch — ein englischsprachiges Kind sieht „Leseverständnis"
   mitten in einer sonst englischen Arcade; wird in den Akzeptanzkriterien als bekannte Lücke geführt.
5. **`<html lang>` wechselt mit der Sprachwahl.** Begründung: Barrierefreiheit — ein Screenreader liest
   sonst englischen Text mit deutscher Aussprache vor. Kosten: klein, ein `useEffect` in der
   App-Wurzel der Sohn-Arcade.
6. **Test-Strategie: deutsche Locale bleibt der E2E-Standard, ein neuer Fall prüft den Umschalter.**
   Begründung: alle zehn bestehenden Playwright-Specs bleiben grün, ohne 236 Assertions über den ganzen
   Korpus anzufassen (das beträfe ohnehin größtenteils das Vater-Web, außerhalb dieser Teilstufe). Kosten:
   die Arcade-eigenen Specs, die Sohn-Text prüfen, müssen die Locale explizit auf Deutsch pinnen, damit ein
   späterer Sprachwechsel sie nicht heimlich bricht.

## Akzeptanzkriterien

1. `react-i18next` (oder eine gleichwertige, gegen die Peer-Konflikt-Historie geprüfte Bibliothek mit
   Plural-Unterstützung) ist eingebunden; `npm install` läuft ohne `--legacy-peer-deps`.
2. Ein rein client-seitiger Sprachumschalter (Deutsch/Englisch) ist in der Sohn-Arcade sichtbar und
   bedienbar, Auswahl bleibt über `localStorage` erhalten.
3. Jeder UI-Chrome-Text unter `frontend/src/sohn/` sowie der von der Arcade tatsächlich eingebundenen
   geteilten Komponenten liegt vollständig auf Deutsch **und** Englisch vor.
4. `<html lang>` wechselt mit der gewählten Sprache mit.
5. Nutzerdaten (Missions-/Auszeichnungstitel, Shop-Artikel, Plan-/Übungstitel) bleiben unübersetzt.
6. Die zehn bestehenden Playwright-Specs unter `frontend/e2e/` bleiben grün (Deutsch explizit gepinnt);
   ein neuer E2E-Fall schaltet die Arcade auf Englisch um und prüft mindestens einen übersetzten Text.
7. Die Übungstyp-Anzeigenamen aus dem Server-Manifest bleiben ausdrücklich Deutsch — benannt in der UI als
   bekannte, nicht als übersehene Lücke (kein stiller Bruch der sonst englischen Oberfläche ohne Hinweis
   in dieser Story).

## Schätzung

**Größe: L** — Bibliotheks-Integration (inkl. Peer-Konflikt-Prüfung) + Extraktion über ~32 Dateien (12
Sohn-Arcade + eingebundene geteilte Komponenten) + Testumstellung für die betroffenen E2E-Specs.
Vergleichbar mit dem L-Anker (eine einzelne DB-Umbau-Etappe), aber ohne Backend-Anteil.

- **wo**: frontend
- **migration**: nein — keine Schemaänderung, Sprachwahl bleibt client-seitig.
- **vertragsbruch**: nein — kein DTO ändert sich; das Übungstyp-Manifest bleibt bewusst unangetastet
  (Entscheidung 4, eigene Story B-86).
- **Risiken**: die Peer-Konflikt-Historie (`vite-plugin-pwa`↔`vite@8`) kann die Bibliothekswahl
  einschränken; `fieldHelp.ts`-Fließtext-Anteile, die doch in die Arcade durchschlagen (z. B. über geteilte
  Komponenten), verlangen echte Übersetzungsqualität statt mechanischer Ersetzung — teurer pro Zeile als
  der Rest des Korpus.
- **Angriffsplan**: (1) Peer-Konflikt-Check + Bibliothek einbinden, (2) Sprachumschalter + `<html lang>`-
  Kopplung, (3) Sohn-Arcade-Textkorpus extrahieren und übersetzen, (4) betroffene E2E-Specs auf gepinnte
  Locale umstellen, (5) neuer E2E-Fall für den Sprachwechsel.
- **Testweg**: bestehende zehn Playwright-Specs unter `frontend/e2e/` (grün, Deutsch gepinnt) + ein neuer
  Fall für den Englisch-Umschalter; kein Backend-Test nötig (kein Server-Anteil).

## Verlauf

- **2026-08-03** — angelegt beim Teilen von [B-38](B-38-mehrsprachige-oberflaeche.md) (Entscheidung 8
  dort), direkt als `geschaetzt` übernommen: Ist-Stand, Entscheidungen und Akzeptanzkriterien waren im
  „Empfohlener erster Schnitt" von B-38 bereits vollständig recherchiert und belegt, hier nur auf eine
  eigenständige Story-Datei aufgeteilt. Autonom getroffen, Nutzerauftrag 2026-08-04.
