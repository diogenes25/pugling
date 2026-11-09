---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Fachlehrer-Typen roh, Bevorzugte Übungstypen ohne Manifest]
status: in-arbeit
wartet_auf: Reparatur des Fund 6 aus Sprint 28 (Lauf AG an Freigabe 3 beendet)
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-creator
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-426 · Fachlehrer-Profil: „Bevorzugte Übungstypen" zeigt vier rohe Schlüssel aus einer Frontend-Liste

Im Rollengang vom 2026-11-08 sah der Lehrer unter `/vater/fachlehrer` die Knöpfe **VOCABULARY · CLOZE ·
TRANSLATION · GRAMMAR** – englische Schlüssel statt „Vokabelkarten", und nur vier von zwölf Typen. Die
Hausregel des Frontends sagt: Übungstypen kommen aus dem Server-Manifest, nie aus einer Tabelle im Frontend
(`frontend/CLAUDE.md`).

## User Story

Als *Creator* möchte ich im Fachlehrer-Profil **alle** Übungstypen mit ihrem deutschen Namen anklicken können,
damit der KI-Creator meine Vorlieben vollständig kennt und ich nicht raten muss, was „CLOZE" ist.

## Ist-Stand am Code

- `VaterFachlehrer.tsx:24` – `const TYPES = ["Vocabulary", "Cloze", "Translation", "Grammar"] as const;`
  `:431-436` rendert `{t}` roh als `toggle-pill`.
- Das Manifest liegt bereit: `GET creator/exercise-types` liefert `type` + `label` für zwölf Typen (Rollengang:
  Vokabelkarten … Birkenbihl); `exerciseTypes.ts:52` `useExerciseTypes()` ist der vorgesehene Zugang und wird
  im Anlege-Formular schon benutzt.
- Das Profil speichert `DefaultTypes` als JSON-Liste von Schlüsseln (`backend/Pugling.Api/CLAUDE.md`,
  „Creator-Profile": „JSON-Liste → neu zuweisen"); ob der Server unbekannte Schlüssel abweist, ist nicht
  belegt – im Nachtlauf prüfen (`CreatorProfilesController`), ggf. als Teil der Reparatur ergänzen.

## Die echte Lücke

Eine dritte handgepflegte Typliste, obwohl das Manifest und der Hook existieren – genau der Drift, den die
Hausregel verhindern soll.

## Offene Punkte

Keine Produktfrage; Defekt, autonom grillbar. Empfehlung: `useExerciseTypes()`; Knopf zeigt `label`, speichert
`type`; Typen, die der Agent nicht erzeugen kann, nicht ausfiltern (das weiß der Agent selbst). Server:
unbekannter Schlüssel → `400 validation_error`, falls heute nicht so.

## Akzeptanzkriterien (Entwurf)

1. Die Knöpfe zeigen die zwölf Manifest-Namen (deutsch) und speichern die Manifest-Schlüssel.
2. Ein gespeichertes Profil mit `["Cloze"]` zeigt „Lückentext" gedrückt.
3. `PATCH creator/profiles/{id}` mit einem unbekannten Typ-Schlüssel → `400 validation_error` (prüfen, ggf.
   herstellen, Regressionstest).
4. Vitest `VaterFachlehrer.test.tsx`: Fälle 1–2; Mutante „Label durch Schlüssel ersetzen" → rot.

## Entscheidungen

1. **Typ-Knöpfe aus dem Manifest** (Name anzeigen, Schlüssel speichern), alle Typen, nicht gefiltert. Das Manifest kommt als Prop von der
   Seite, damit `ProfileForm` netzfrei bleibt (B-67). *Entscheidung im Lauf (Defekt), getroffen von Claude.*
2. **Server weist unbekannte Schlüssel ab** (`400 validation_error`, exakt wie die Registry vergleicht), im Create und im PATCH vor dem
   Speichern. Ein gespeicherter Altlast-Schlüssel bleibt im Formular als Rohwert sichtbar und abwählbar (mit Hinweis im `title`).

## Schätzung

**Größe: S** — ein Prüfhelfer, zwei Aufrufstellen, Knopfliste aus dem Manifest. Testweg: `CreatorProfileTypeKeyTests`,
`VaterFachlehrer.test.tsx`, E2E `creator-katalog-ueberblick.spec.ts`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Creator ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Defekt, Freigabe 1).
- **2026-11-09** — gegrillt (Defekt, Freigabe 1), geschätzt und gebaut in Lauf AG, Sprint 28. **Rote Probe mit Zahl:** die drei neuen
  Integrationstests gegen den Ausgangsstand → 2 von 3 rot (201 bzw. 200 statt 400); der dritte („alle Manifest-Typen") ist eine
  *Gegenprobe* und war vorher grün. `lehrwerke.spec.ts` klickt jetzt „Vokabelkarten" statt „Vocabulary".
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `a14961d + Arbeitsbaum`, 2026-11-09 17:17:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 3 Create | `CreatorProfileTypeKeyTests.Anlegen_MitUnbekanntemTyp_Liefert400` | MB5 Prüfung aus (`CreatorProfilesController.cs:91`) | red, am Fall |
  | AK 3 Update | `…Aendern_MitUnbekanntemTyp_Liefert400_UndLaesstDasProfil` | MB6 Prüfung aus (`:138`) | red, am Fall |
  | AK 1 | `VaterFachlehrer.test.tsx` „zeigt jeden Typ des Manifests …" | MF1 Schlüssel statt Name (`VaterFachlehrer.tsx:448`) | red, am Fall |
  | Altlast abwählbar | „lässt einen unbekannten gespeicherten Schlüssel abwählbar" | MF2 (`:442`) | red, am Fall |
  | Review: Manifest-Ausfall | `e2e/creator-katalog-ueberblick.spec.ts` „Ausfälle sagen, was fehlt" | ME7 `ListenAusfall` weg (`:73`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün. AK 2 („Cloze" gedrückt als „Lückentext") hält der Vitest „zeigt einen gespeicherten
  Schlüssel als gedrückten Namen"; seine Mutante fällt mit MF1 zusammen.
- **2026-11-09** — Review: `pugling-reviewer` 🟢 Kommentare (Prüfung „vor SaveChanges", nicht „vor jedem Feld"; der Agent liest
  `DefaultTypes` heute nicht) → berichtigt (ohne Zählung). `frontend-reviewer` 🟡 Manifest-Ausfall auf der Fachlehrer-Seite stumm →
  `ListenAusfall` (**zählt, Sprint 28**); 🟢 Altlast-Knopf ohne Hinweis → `title`; 🟢 E2E-Kommentar zu „Birkenbihl" präzisiert.
- **2026-11-09** — **nicht abgenommen: Lauf AG an Freigabe 3 beendet.** Volle Suite nach dem letzten Edit: Backend 1187/1187, Vitest 723/723, `tsc -b` sauber, Playwright **1 failed, 129 passed** – rot `e2e/typ-manifest-faellt-aus.spec.ts:40`: der Schalter „geteilte Übungen anderer Väter anzeigen" ändert beim Klick seinen Zustand nicht. Ursache (analysiert, nicht behoben): seit dem Review-Fix zu B-428 hängt der Schalter an der Adresse (`?shared=1`, `VaterExercises.tsx` `setFilter`); Fach-Wechsel und Klick gehen beide über `setSearchParams`, der zweite liest den Fach-Wert aus dem alten Render (veraltete Closure), und React Router rendert die Adress-Änderung verzögert – der kontrollierte Haken springt zurück. Reparaturweg: `setParams(prev => …)` (aktuelle Adresse statt Render-Werte). Das ist Fund 6 in Sprint 28 und überschreitet den Zähler (5); darum gestoppt, nicht repariert. Der Rest der Story ist gebaut und belegt (Tabelle oben); die Abnahme gehört dem Nutzer bzw. dem nächsten Lauf nach der Reparatur.
