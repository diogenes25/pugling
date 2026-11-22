---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: [Aufwärmhinweis verspricht mehr]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Review Sprint 108 (B-608, frontend-reviewer 🟢) im Nachtlauf 2026-11-21, docs/pm-sitzung-2026-11-21.md"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-657]
---

# B-785 · Der Aufwärm-Hinweis verspricht „ab der Auswahl gibt es mehr“, die Auswahl zahlt aber gleich viel

Der Hinweis über einer Karteikarten-Runde im Bonus-Training (`bonus.hintSelf`, `frontend/src/i18n/de.ts`) sagt „jedes
gewusste Wort bringt {{coins}} 🪙, ab der Auswahl gibt es mehr“. Seit B-657 zahlt die Karteikarte 1, die Auswahl aber
ebenfalls 1 (`ExtraTrainingOptions.cs`: `CoinsPerSelfAssessHit = 1`, `CoinsPerChoiceHit = 1`, `CoinsPerTypedHit = 3`;
`appsettings.json:38-40`). Mehr gibt es erst beim Tippen. Dieselbe Art Übertreibung wie [B-608](B-608-bonus-kopf-nennt-zu-hohen-satz.md),
nur im festen Text statt in einer Zahl.

Am Code nachgelesen, nicht im Browser gesehen. Gefunden vom `frontend-reviewer` in Sprint 108 als 🟢 außerhalb des Diffs;
bewusst nicht dort behoben (anderer Satz, fremder Text).

Mögliche Reparatur (zu entscheiden): den Text auf „ab dem Tippen“ ändern, oder ihn aus den Sätzen des Servers ableiten,
damit eine geänderte Konfiguration ihn nicht wieder falsch macht. Rote Probe: Vitest, der den Hinweis gegen die Sätze
des Vorrats hält.

## User Story

Als Sohn möchte ich, dass der Hinweis über einer Aufwärm-Runde nur verspricht, was die nächste Stufe wirklich zahlt.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf A 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **Der Text sagt „beim Tippen gibt es mehr“ (en: „typing pays more“).** Das ist das Wort, mit dem die Liste derselben
   Seite die Form nennt („Tippen · 3 🪙“). Verworfen: den Satz aus den Sätzen des Servers ableiten – das
   bräuchte ein neues Vertragsfeld („nächst höherer Satz“) für einen festen Text. Kosten: ändert jemand die Sätze, kann der
   Text wieder lügen – dagegen Entscheidung 2.
2. **Ein Server-Test hält die Voraussetzung des Texts:** Schreiben zahlt mehr als Karteikarte und Auswahl, in
   `appsettings.json` und in den Vorgaben von `ExtraTrainingOptions`. Kosten: ein Test über Konfiguration; wer die Sätze
   bewusst umstellt, muss den Text mitziehen – genau das soll er merken.

## Akzeptanzkriterien

1. Eine Karteikarten-Runde zu 100 % zeigt „… bringt 1 🪙, beim Tippen gibt es mehr.“ und nicht „ab der Auswahl“. Rot:
   der alte Text.
2. `ExtraTrainingRatesTests`: `CoinsPerTypedHit` > `CoinsPerSelfAssessHit` und > `CoinsPerChoiceHit`, gebunden und als
   Vorgabe. Rot: ein Tipp-Satz, der auf die anderen fällt.

## Verlauf

- **2026-11-21** — angelegt aus dem Review von Sprint 108 (`idee`, `entgangen_bei: [B-657]`).
- **2026-11-22** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt (S, frontend) im Nachtlauf A; in-arbeit in Sprint 113.
  - AK 1 → `SohnBonus.test.tsx` „verspricht mehr erst beim Schreiben“, R1 (alter Text) → rot.
  - AK 2 → `ExtraTrainingRatesTests`, R2 (`appsettings.json` Tippen 1) → rot, R3 (Vorgabe Tippen 1) → rot. Messung 00:56 nach der letzten Änderung, Grundlinie grün.
- **2026-11-22** — Review `frontend-reviewer` (gemeinsam mit B-778): 🟢 Wortlaut übernommen – „beim Tippen“ statt „beim Schreiben“, weil die Liste derselben Seite die Form „Tippen“ nennt (Entscheidung 1 damit angepasst); kein 🟡/🔴. Mutanten nach der letzten Änderung (R1 neu gefasst) rot.
- **2026-11-22** — abgenommen (`b2081837`, Sprint 113): Rollengang Sohn per E2E `bonus` (Hinweistext im Aufwärmen) gegen frisch gestarteten Server; Vater/Creator Regressionszeugen. Backend 2023/2023 (Release).
