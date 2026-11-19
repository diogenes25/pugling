---
tags: [typ/story, status/ausformuliert, bereich/gamification, bereich/punkte, rolle/student, rolle/supervisor]
aliases: [Pflicht-Erinnerung, Erinnerung vor dem Malus]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Marktvergleich Belohnungs-Apps (Sitzung 2026-11-16, Quick-Win 1)
grund: ""
ersetzt_durch: []
---

# B-592 · Erinnerung vor Ablauf der Pflicht-Frist – für Kind und Vater

Verpasste Pflicht kostet Münzen (`PenaltyCoins`). Alle verglichenen Aufgaben-Apps (Joon, S'moresUp, OurHome, Habitica)
erinnern vor der Frist. Die Idee: rechtzeitig vor dem Tages- bzw. Wochenende eine Erinnerung an das Kind („noch
2 Stunden, sonst −20 Münzen") und an den Vater („Friedrich hat heute noch nicht geübt"). Der Kanal war offen.

Die Annahme der Ursprungsfassung, das Kind erfahre vom Abzug erst nach dem Abzug, **stimmt im geöffneten Programm nicht
mehr**: Frist und Malus stehen dort schon auf der Startseite (B-525). Offen ist nur die Erinnerung **außerhalb** der
App, für ein Kind, das die App gar nicht öffnet.

## User Story

Als **Schüler** möchte ich vor dem Ende der Frist erinnert werden, auch wenn ich die App nicht offen habe, damit ich
den Malus nicht aus Vergessen bekomme.
Als **Vater** möchte ich erfahren, dass mein Sohn heute noch nicht geübt hat, solange noch Zeit ist, damit ich
eingreifen kann, bevor es kostet.

## Ist-Stand am Code

**Schon da (im offenen Programm):**

- Die Startseite zeigt je offener Pflicht „⏰ noch N Std." bzw. „bis Sonntag" und den Malus
  (`frontend/src/sohn/SohnHome.tsx:296-308`, Frist-Logik `frontend/src/sohn/pfad.ts:92-96`).
- Der Malus kommt im Vertrag als `PositionStatus.PenaltyCoins`, „shown to the child on purpose (B-525): a price that
  only appears as a booking the next morning cannot steer anything" (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:17-19`).
- Die Frist rechnet nach **Server-Uhr** (UTC-Tagesende), nicht nach Ortszeit (`pfad.ts:88-91`, `frontend/src/lib/datum.ts:107` und `:113`, dort `getUTCDay`).
  Für MEZ liegt das Tagesende damit um 01:00 bzw. 02:00 Uhr nachts.
- Vater-Seite: das Tages-Dashboard zeigt je Kind „✓ geschafft", „dran" oder nichts (`frontend/src/vater/VaterDashboard.tsx:64-69`,
  Daten `ChildrenDashboardService`/`GET supervisor/children/daily-overview`).

**Wie der Abzug entsteht:** `PositionProgressService.SettleClosedPeriodsAsync` bucht **ohne Zeitgeber** – „There is no
scheduler; this method is called at POST seams (login, shop purchase)" (`Services/Shared/PositionProgressService.cs:746-754`;
Aufrufer `Controllers/AuthController.cs:85`, `Controllers/Student/MeController.cs:296`). Ein Kind, das nie einloggt, wird
nie abgezogen.

**Nicht gefunden (belegt durch Suche):**

- Kein `IHostedService`/`BackgroundService`/`AddHostedService` im Backend (`backend/Pugling.Api`), also kein Ort, an dem
  „vor Frist" je geprüft würde.
- Kein Mail-Versand (kein SMTP-/Mail-Paket), kein Web-Push (kein VAPID, keine Push-Abo-Entität in `Models/`), kein
  Service-Worker-Code mit Push-Handler. `vite-plugin-pwa` erzeugt nur Manifest und Offline-Hülle
  (`frontend/vite.config.ts:6-30`).
- Ein Kontaktweg ist nur halb da: `Adult.Email` und `Account.Email` existieren (`Models/AdminEntities.cs:47`, `Models/IdentityEntities.cs:20`), aber nichts sendet an sie; Ruhezeiten und Benachrichtigungs-Einstellungen: nicht gefunden.

**Randbedingung des Betriebs:** Pugling läuft im Heimnetz; Service Worker und damit Web-Push gibt es nur über HTTPS, hier
über Tailscale (`docs/heimnetz.md:74-93`). Ohne diesen Weg ist Push gar nicht möglich; mit ihm steht er offen,
aber das Kind-Gerät (Tablet, Family Link) muss die Berechtigung erteilen.

## Die echte Lücke

Schmaler als die Idee, aber an anderer Stelle: (1) Ein **Auslöser**, der unabhängig von einem Aufruf des Kindes läuft –
heute existiert keiner. (2) Ein **Kanal** nach außen, den es nicht gibt. (3) Die **Regel**, wann „noch Zeit" ist: die
Frist ist der UTC-Tageswechsel, eine Erinnerung „2 Stunden vorher" läge für das Kind mitten in der Nacht.
Der innere Teil – dem Kind Frist und Preis zeigen – ist erledigt.

## Offene Punkte

1. **Welcher Kanal?** Empfehlung: zuerst **Hinweis beim nächsten Öffnen plus eine Zeile für den Vater im Dashboard**
   (kein neuer Auslöser, kein neuer Kanal, kein Hosted Service), Web-Push als eigene Folge-Story hinter der HTTPS-Voraussetzung.
   Mail und Messenger widersprechen dem Heimnetz-Betrieb ohne Cloud-Anbindung.
2. **Wann gilt „kurz vor Frist"?** Empfehlung: nicht UTC-Mitternacht, sondern ein **fester Zeitpunkt am Abend** je
   Betreuer-Einstellung (Vorgabe: 18:00 Ortszeit). Wer das konfiguriert, ist offen: am `Adult`, am `Child`, oder
   als `appsettings`-Wert wie `Scoring:TimeSlots`.
3. **Wer wird erinnert?** Empfehlung: Kind nur bei **offener Pflicht mit Malus > 0** (Kinder ohne Strafe nicht
   mahnen, das wäre Nörgeln); Vater nur bei **offener Pflicht und noch keinem Üben heute**
   (`ChildDay.Practiced`, `Pugling.Contracts/Supervisor/GoalDtos.cs:36-37`), je Tag höchstens einmal.
4. **Wochenpflicht:** Erinnerung am Sonntag oder schon am Freitag? Empfehlung: Freitag und Sonntag, sonst reicht ein
   Sonntagabend nicht, ein Wochenziel nachzuholen.
5. **Ruhezeiten und Schulzeit:** keine Mahnung in der Nacht und nicht in der Schulzeit. Empfehlung: ein fixes Fenster,
   kein pro-Kind-Kalender.
6. **Darf die Mahnung die Strafe abschwächen?** Nein: sie ändert nichts am Buchen. Empfehlung: ausdrücklich festhalten,
   damit sie nicht als Schonfrist gelesen wird. Dass das **Münz-/Malus-Regeln berührt**, ist hier nicht der Fall,
   solange nur angezeigt und nicht verändert wird.
7. **Grundregel für den Vater im Leitbild Privatlehrer** ([Leitbild](../leitbild-privatlehrer.md)): wenn der Vater nur
   noch belohnt (B-681), wäre „Sohn hat nicht geübt" eine Meldung für den Lehrer-Agenten. Empfehlung: den Empfänger
   offen halten; die Rollenfrage entscheidet der Nutzer dort.

## Entscheidungen

Noch keine – Stufe `ausformuliert`.

## Akzeptanzkriterien (Entwurf)

Je Kriterium: welche Änderung macht es rot.

1. Hat ein Kind eine offene Pflicht mit `PenaltyCoins > 0`, liefert der Server nach dem Abend-Zeitpunkt einen
   Erinnerungseintrag mit Positionsnamen und Betrag. Rot, wenn die Regel nur auf Pflicht, nicht auf Malus prüft.
2. Ist die Pflicht erfüllt oder gibt es keinen Malus, entsteht kein Eintrag. Rot, sobald die Bedingung weggelassen wird.
3. Je Kind und Tag höchstens eine Erinnerung, auch bei mehreren Aufrufen. Rot, wenn die Eindeutigkeit fehlt
   (wie der Unique-Index an `PositionGoalPenalty`).
4. Die Erinnerung ändert nichts an `SettleClosedPeriodsAsync`: derselbe Tag, derselbe Abzug mit und ohne Erinnerung.
   Rot, wenn die Erinnerung eine Schonfrist erzeugt (Regressionstest gegen `PositionGoalPenalty`).
5. Der Vater erhält den Hinweis nur für **seine** Kinder (404 bei fremdem Kind). Rot, wenn der Besitzfilter fehlt.
6. Die Erinnerung steht in der Sohn-App ohne Zutun auf der Startseite und verschwindet nach Quittierung. Rot, wenn
   sie dauerhaft bleibt.

## Verlauf

- **2026-11-16** — angelegt aus dem Marktvergleich Belohnungs-Apps (Quick-Win 1).
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Frist und Malus stehen im offenen Programm schon (B-525); es fehlen
  Auslöser, Kanal und Zeitpunktregel, weil das Backend keinen Zeitgeber kennt (`SettleClosedPeriodsAsync` läuft
  nur an Login/Kauf); sieben offene Punkte mit Empfehlung.
