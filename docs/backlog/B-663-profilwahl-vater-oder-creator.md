---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Rollenwahl beim Login, Creator nur im Web]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-663 · Beim Einloggen zwischen „Vater" und „Creator" wählen

Ein Konto trägt heute Creator **und** Supervisor zugleich, die Oberfläche mischt beides. Der Vater will
zwei getrennte Profile:

- **Vater** (Rolle `Supervisor`): Fortschritt der Kinder und der Shop, also Einlösen der gekauften
  Artikel – das, was [B-662](B-662-vater-sicht-arbeit-tagesziel-kaeufe.md) beschreibt. Muss auf dem Handy
  gehen.
- **Creator**: Übungen erstellen und ansehen. Das ist so komplex, dass es auf dem Handy nicht handhabbar
  ist – **nur Web**. Außerdem erledigen das ohnehin die KI-Agenten (`Pugling.Agent.Creator`); der Mensch
  sieht hier eher nach, als dass er baut.

## User Story

Als **Vater** möchte ich beim Anmelden festlegen, ob ich als Vater (Kinder und Shop) oder als Creator (Übungen) arbeite,
**damit** ich am Handy nur die Betreuung sehe und die Werkstatt nur am Rechner.

## Ist-Stand am Code

**Wie `LoginResponse.role` routet (Frage der Notiz):**

- **1.** Das Token trägt **alle** Rollen des Kontos: eine `Role`-Claim je Profil (Creator/Supervisor/Student) plus `fid`/`cid`
   (`backend/Pugling.Api/Auth/TokenService.cs:21-34`, `IssueForAccount`).
- **2.** `LoginResponse.role` ist der **primäre** Anteil mit der Rangfolge Supervisor vor Creator vor Student
   (`backend/Pugling.Api/Controllers/AuthController.cs:31-37`, `PrimaryRoleOf`; verwendet bei `:64` und `:113`, ebenso in
   `Me()` bei `:121-125` und `:190`). Ein Konto mit beiden Rollen bekommt also `Supervisor` – wie die Notiz sagt.
   `MeResponse` nennt zusätzlich alle Rollen des Kontos (`:190-192`).
- **3.** Gegated wird je Endpunkt auf der Ebenen-Rolle (`[Authorize(Roles = Roles.Creator)]`/`Roles.Supervisor`; `CLAUDE.md`,
   Architektur). Ein Token, das nur **eine** Rolle trüge, ließe die Endpunkte der anderen Ebene mit 403 ab.
- **4.** Die KI-Agenten melden sich konto-zentrisch über `auth/login` an und brauchen die Creator-Claim
   (`backend/Pugling.Client/PuglingTokenStore.cs:14`, `AuthHandler.cs:48`). Eine Verengung des Tokens müsste für sie
   weiter möglich sein, also wählbar bleiben.

**Was das Frontend schon trennt – die Wahl ist zum Teil gebaut:**

- **5.** Seit 2026-07-28 gibt es drei **Perspektiven** (Betreuen, Zuweisen, Erstellen) mit Umschalter
   (`frontend/src/vater/navigation.ts:54-97`, `frontend/src/vater/VaterApp.tsx:120,185-199`, Plan
   `docs/vater-perspektiven-plan.md`). Die aktive Perspektive folgt dem Pfad; nur der Klick auf den Umschalter merkt sie
   (`navigation.ts:179`, `rememberPerspective`).
- **6.** **Nach der Anmeldung** führt die zuletzt bewusst gewählte Perspektive (`VaterApp.tsx:59-65`,
   `rememberedPerspective`, `navigation.ts:187`), ein reines Lehrer-Konto geht immer in die Werkstatt
   (`VaterApp.tsx:61-63`).
- **7.** Ein **Lehrer-Konto** (nur Creator) gibt es als eigene Kontoart; das Anlegeformular unterscheidet „Vater" und „Lehrer"
   (`frontend/src/vater/VaterLogin.tsx:156`), `perspectivesFor("Creator")` zeigt ihm nur die Werkstatt, ohne Umschalter
   (`navigation.ts:160`).
- **8.** Die Perspektiven sind ausdrücklich **keine Rechte** („ein Vater darf alles", `navigation.ts:11-15`); die Rechtewand
   steht im Server.

**Was fehlt:**

- **9.** Eine **Wahl im Anmeldevorgang** (statt „zuletzt gewählt" aus `localStorage`): nicht gefunden. Das Anmeldeformular
   fragt nur nach Zugang (Vater oder Kind, `VaterLogin.tsx:37`) und nach der Art des Kontos beim Anlegen.
- **10.** Ein Token, das nur die gewählte Rolle trägt: nicht gefunden (Punkt 1).
- **11.** „Nur Web": ein Abschalten oder Ausblenden der Erstellen-Seiten bei schmalem Bildschirm: nicht gefunden. Im Frontend
    liest nur `prefersReducedMotion` einen Medien-Zustand (`frontend/src/lib/ui.ts:23`); es gibt keine
    Breitenweiche (`matchMedia` sonst nicht gefunden). Die einzige Telefon-nahe Probe ist 768 px
    (`frontend/e2e/responsive-vater.spec.ts:19`).

## Die echte Lücke

Deutlich schmaler als die Idee („zwei getrennte Profile"): die Trennung der Oberfläche **ist** gebaut, und die
Rechtewand sitzt im Server. Offen sind drei Dinge:

1. Die Wahl liegt beim **Umschalter und `localStorage`**, nicht beim Anmelden. Ein Vater am neuen Gerät oder Handy
   landet auf `/vater` (Betreuen) und sieht den Umschalter mit „Erstellen" (Punkt 6).
2. **Erstellen auf dem Handy** ist weder gesperrt noch ausgeblendet (Punkt 11).
3. Ob das **Token** die Rolle verengen soll (Punkt 10), ist eine Sicherheits-Entscheidung, keine Oberfläche; der Vorteil
   ist klein, weil die Perspektiven ohnehin keine Rechte sind und ein Vater beides darf.

Möglich, dass 1 und 2 sich ohne Token-Eingriff lösen lassen. Das entscheidet die Grill-Runde.

## Offene Punkte

Je mit Empfehlung. Die Rollenaufteilung zwischen Vater und Lehrer gehört zu
[B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md) („Leitbild Privatlehrer") und ist dort zu entscheiden;
hier nur als Randbedingung.

1. **Reine Frontend-Wahl oder Token?**
   *Empfehlung:* reine Frontend-Wahl. Das Token trägt weiter alle Rollen. Ein verengtes Token bräuchte eine zweite
   Anmeldung für die Agenten (Punkt 4) und verändert `LoginResponse`/`MeResponse` (Vertragsbruch möglich) für wenig
   Sicherheitsgewinn: die Perspektiven sind keine Rechte.
2. **Wo fällt die Wahl?** Als Schritt nach der PIN („Als Vater oder als Creator?") oder weiter als Umschalter?
   *Empfehlung:* ein Auswahlschritt **nach** der PIN, nur für Konten mit beiden Rollen (`MeResponse` nennt sie), mit
   Vorbelegung aus `rememberedPerspective`. Konten mit einer Rolle sehen den Schritt nicht.
3. **Was heißt „nur Web"?** Erstellen auf schmalen Bildschirmen ausblenden oder nur nicht dafür optimieren?
   *Empfehlung:* auf schmalen Bildschirmen den Eintrag „Erstellen" und „Zuweisen" aus Umschalter und Wahlschritt
   nehmen und beim direkten Aufruf zur Betreuen-Startseite führen, mit einem Hinweis „Am Rechner öffnen". Nicht
   optimieren: der Vater sagt, es sei nicht handhabbar. Die Schwelle ist eine Zahl für die Grill-Runde (Vorschlag:
   dieselbe wie die Telefonbreite der Probe aus B-662).
4. **Gilt die Sperre nur für Handy oder für jedes schmale Fenster?**
   *Empfehlung:* nach Fensterbreite (`matchMedia`), nicht nach Gerät: ein Browserfenster auf dem Rechner darf wachsen.
5. **Reihenfolge mit B-662:** B-663 legt fest, welche Rolle gerade aktiv ist, B-662 füllt die Betreuen-Startseite.
   *Empfehlung:* B-663 zuerst, weil die Perspektiv-Weiche bei B-662 mit der Telefonbreite zusammenfällt.
6. **Wo „Zuweisen"?** Es ist weder Vater- noch Creator-Rolle allein (`navigation.ts:11-21`).
   *Empfehlung:* auf dem Handy mit Erstellen ausblenden; die Entscheidung gehört zu B-681 (Planer).

## Akzeptanzkriterien (Entwurf)

Je Kriterium die Änderung, die es rot macht. Entwurf; endgültig erst nach der Grill-Runde.

1. Ein Konto mit Creator **und** Supervisor sieht nach der PIN einen Wahlschritt „Vater" oder „Creator" und landet in
   der gewählten Startseite (`/vater` bzw. `/vater/inhalte`). *Rot, wenn* nach dem Anmelden weiter nur `/vater` oder
   die gemerkte Perspektive öffnet (`VaterApp.tsx:59-65`).
2. Ein Konto mit nur einer Rolle sieht den Wahlschritt nicht. *Rot, wenn* der Schritt auch für ein Lehrer-Konto
   erscheint.
3. Bei schmaler Fensterbreite zeigen Umschalter und Wahlschritt kein „Erstellen"; der direkte Aufruf von
   `/vater/inhalte` führt zur Betreuen-Startseite. *Rot, wenn* `perspectivesFor` die Breite nicht kennt
   (`navigation.ts:160`) – die neue Vitest-Probe fällt dort.
4. Dieselbe Seite bei breitem Fenster zeigt wieder alle Perspektiven. *Rot, wenn* die Sperre nach einmaliger
   Verengung bleibt (Zustand statt Abfrage).
5. Das Token und `LoginResponse`/`MeResponse` bleiben unverändert. *Rot, wenn* ein Vertragsfeld oder eine Claim
   hinzukommt (Vertragsbruch-Tor `unknown_field` bzw. Contracts-Diff).
6. Der KI-Creator meldet sich weiter mit Creator-Claim an. *Rot, wenn* `auth/login` die Rolle verengt
   (`AuthHandler.cs:48`).

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- 2026-11-19 · ausformuliert (Vorbereitungslauf): Ist-Stand am Code. Die Oberfläche trennt Betreuen/Zuweisen/Erstellen schon
  seit 2026-07-28 (Umschalter, gemerkte Perspektive, eigene Lehrer-Kontoart); `LoginResponse.role` ist Supervisor vor
  Creator, das Token trägt alle Rollen. Offen: Wahl beim Anmelden, Erstellen auf schmalen Bildschirmen. Hängt an B-662
  und B-681.
