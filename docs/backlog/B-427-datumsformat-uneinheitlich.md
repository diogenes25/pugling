---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [Datumsformat, toLocaleDateString ohne Locale]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-427 · Datumsformat: zwölf Stellen formatieren ohne Locale, daneben stehen ISO-Strings

Im Rollengang vom 2026-11-08 (Browser-Locale en-US) stand im Kind-Hub „Betreuer seit **11/8/2026**" und im
Konto „**11/8/2026**", während die Plan-Seite „**2026-11-08 – 2026-11-21**" und der Tagesverlauf ISO-Daten
zeigen. Drei Schreibweisen für dasselbe Datum in einer deutschen Oberfläche; welche erscheint, hängt vom
Browser ab.

## User Story

Als *Vater* möchte ich jedes Datum in **einer** deutschen Schreibweise sehen („08.11.2026"), unabhängig von der
Browser-Einstellung, damit ich Termine nicht umdenken muss.

## Ist-Stand am Code

- Zwölf Aufrufe `toLocaleDateString()` ohne Locale: `ChildCarePanels.tsx:68`, `VaterKonto.tsx:71`,
  `VaterLernstand.tsx:183/224/359`, `VaterProfil.tsx:78`, `VaterShop.tsx:408/454`,
  `VaterAnmerkungen.tsx:202/224/295`, `RemarkWidget.tsx:317`.
- ISO-Rohstrings in der Anzeige: `VaterPlanDetail.tsx:63` (`{p.startDate} – {p.endDate}`),
  `VaterPlaene.tsx:83`, der Tagesverlauf auf der Plan-Seite; der Assistent-Überblick zeigt „2026-11-08 · 14
  Tage" (Rollengang).
- Keine gemeinsame Formatfunktion (kein Treffer für `formatDate`/`datum(` in `frontend/src/lib`).
- `DateOnly`-Werte kommen vom Server als `YYYY-MM-DD` (UTC-Tagesregel, `CLAUDE.md` „Zeit/UTC"); Zeitstempel
  als ISO-8601 mit Uhrzeit.

## Die echte Lücke

Kein gemeinsamer Formatierer, darum zwölf implizite Entscheidungen und eine Handvoll gar nicht formatierter
Werte. Aufräumen: kein Verhalten ändert sich außer der Schreibweise.

## Offene Punkte

Keine Produktfrage; Aufräumen, autonom grillbar. Empfehlung: `lib/datum.ts` mit `formatDate(iso)` →
`Intl.DateTimeFormat("de-DE", { day: "2-digit", month: "2-digit", year: "numeric" })` und
`formatDateRange(a, b)`; `DateOnly`-Strings ohne `new Date()`-Umweg parsen (sonst springt der Tag in
westlichen Zeitzonen). Rohes ISO bleibt nur in `<input type="date">`-Werten. Die Arcade (`t()`) gleich
mitnehmen, falls dort Daten stehen (prüfen: `grep -n "toLocaleDateString" frontend/src/sohn`).

## Akzeptanzkriterien (Entwurf)

1. Kein `toLocaleDateString(`/`toLocaleString(` ohne Locale mehr in `frontend/src` (Tor: Vitest-Fall, der die
   Quellen greppt, wie `lesefehler.test.ts`).
2. Plan-Zeitraum und Tagesverlauf zeigen „08.11.2026 – 21.11.2026".
3. Ein `DateOnly` „2026-11-08" wird in jeder Zeitzone als 08.11.2026 gezeigt (Test mit `TZ`-Variation oder
   reiner String-Parse).
4. `old-phrases.py` findet keine Spec mehr, die auf das alte Format zusichert.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Aufräumen, Freigabe 1).
