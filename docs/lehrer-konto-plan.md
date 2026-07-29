---
tags: [typ/plan, bereich/api, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Lehrer-Konto, Creator-only, Teacher-Account, Rollentrennung]
---

# Das Lehrer-Konto: ein Erwachsener ohne Betreuungsauftrag

Status: **Umgesetzt** (2026-07-29), inklusive Selbstverwaltung. 0 Build-Warnungen, 491 Backend-Tests,
21 Vitest, 25 Playwright grün.

## Warum keine `Teacher`-Entität

Der ältere Entwurf „Lehrer verteilt Hausaufgaben" (Plan in `.claude/plans/`, 2026-07-05 abgesegnet) sieht
eine eigene `Teacher`-Tabelle samt `Roles.Lehrer` und `tid`-Claim vor – als **Etappe 1 von 6**. Diese
Entscheidung war für *Hausaufgaben* richtig: Klassen, Einschreibung und die Ownership-Umkehr brauchen eine
eigene Identität.

Für „ein Konto, das nur Inhalte erstellt" ist sie der falsche Preis. Zwei Befunde aus dem Code:

1. **Das Token-Modell trägt es schon.** `TokenService` setzt die Rollen-Claims **je Profil einzeln**;
   ein Konto mit nur einem Creator-Profil war nie ausgeschlossen. Blockiert hat allein
   `AccountService.EnsureForFatherAsync`, das jedem Vater *beide* Profile anhängte.
2. **Eine `Teacher`-Zeile könnte nichts besitzen.** `Exercise.AuthorFatherId` und
   `ExerciseGrant.CreatorId` zeigen auf `Father`. Ohne die Autor-Verallgemeinerung (im Entwurf als offener
   Punkt notiert) hätte ein Teacher keine Übung anlegen, teilen oder zurückziehen können – das Konto wäre
   funktionslos gewesen, und die Migration hätte zugleich alles berührt, was gerade an Freigabe/Rücknahme
   und Verwendungs-Zählung entstanden ist.

Darum: **ein Lehrer ist ein Erwachsener ohne Betreuungsauftrag.** Die drei Ebenen sind Rollen, entkoppelt
vom Login ([grundprinzip.md](grundprinzip.md)) – hier wird diese Entkopplung zum ersten Mal ausgenutzt,
statt sie mit einer parallelen Identität zu umgehen. Die `Teacher`-Entität bleibt Teil des
Hausaufgaben-Features, wo Klassen und Ownership-Umkehr sie tatsächlich verlangen.

## Was ein Lehrer-Konto ist

| | Vater-Konto | Lehrer-Konto |
|---|---|---|
| Profile | Creator **+** Supervisor | **nur** Creator |
| Token-Claims | `Creator`, `Supervisor` | `Creator` |
| `LoginResponse.role` | `Supervisor` | `Creator` |
| Perspektiven | Betreuen, Zuweisen, Erstellen | **nur** Erstellen |
| Kinder, Pläne, Shop, Klassenarbeiten | ja | nein (403 vom Server) |
| Übungen anlegen, Rechte, Freigabe/Rücknahme | ja | ja – unverändert |

Fachlich hängt er an einer `Father`-Zeile: daran hängen Autorschaft und RWX-Rechte. Der Name der Tabelle
ist an dieser Stelle zu eng (ein „Father" ist hier ein Erwachsener) – eine Umbenennung ist eine eigene,
rein mechanische Aufgabe und wurde bewusst nicht mit hineingezogen.

## Die vier Stellen, die „Erwachsener = Supervisor" annahmen

Das war der ganze Umbau im Kern:

1. `AccountService` hängte jedem Vater beide Profile an → neuer Weg `EnsureForTeacherAsync`, und das
   Anlegen ist **nicht nachrüstend**: ein bestehendes Konto behält seine Rollen. Ohne diese Zusage hätte
   `auth/adult` (das `EnsureForFatherAsync` ruft) dem Lehrer beim ersten Login stillschweigend den
   Betreuungsauftrag verliehen – die Trennung hätte sich selbst aufgehoben.
2. `AuthController.Login` klappte jede Nicht-Student-Rolle auf `Supervisor` zusammen → Rangfolge
   Supervisor → Creator → Student in `PrimaryRoleOf`.
3. `auth/me` trug **dieselbe** Annahme ein zweites Mal (mit Kommentar „auch reiner Creator → Supervisor").
   Gefunden hat sie ein Test, nicht das Lesen.
4. `VaterApp` verlangte `role === "Supervisor"` → kennt jetzt „Erwachsener" als Supervisor *oder* Creator.

## Oberfläche

- **Registrierung** mit Konto-Art (👤 Vater / 🎓 Lehrer). Eine Wahl beim Anlegen, keine Einstellung danach:
  der Unterschied sind die Rollen, und die entstehen dort.
- **Kein Perspektiven-Umschalter** bei einem Lehrer – ein Schalter mit einer Stellung ist Dekoration.
- **Marke sagt die Rolle** („🎓 Pugling · Lehrer"), und die Werkstatt verspricht ihm nichts, was er nicht
  hat: statt „zugewiesen wird später unter *Zuweisen*" (ein Link, der ihn zurückwerfen würde) steht dort
  „Zuweisen tun die Eltern".
- **Perspektiv-Schranke** statt 403-Seiten: ruft ein Lehrer `/vater`, `/vater/plaene` oder `/vater/kind/1`
  auf, führt der Weg in die Werkstatt. Die Rechteprüfung bleibt beim Server; das ist nur Höflichkeit.
  Ausgenommen sind die perspektivlosen Seiten (Konto, Anmerkungen) – ohne diese Ausnahme käme ein Lehrer
  nicht einmal an sein eigenes Profil.

Die Routen bleiben unter `/vater/…`. Ein Umzug nach `/lehrer/…` wäre Kosmetik gegen den Preis aller Links,
Lesezeichen und E2E – dieselbe Abwägung wie beim Perspektiven-Umbau.

## Zwei Funde beim Prüfen

- **`GET supervisor/study-plans` antwortet einem Lehrer `200`, nicht `403`** – und das ist Absicht: die
  *lesende* Liste dient Vater und Sohn und trennt inline (CLAUDE.md). Für einen Lehrer ist sie **leer**,
  weil über `SupervisorLinks` gefiltert wird. Kein Datenleck; meine Test-Erwartung war falsch, nicht der Code.
- **Der Profil-Link führte ins Leere.** `AdultsController` ist Supervisor-gegated; ein Lehrer bekäme dort
  403. Er sieht seinen Namen jetzt ohne Link.

## Selbstverwaltung: `PATCH auth/me`

Nachgereicht, weil ein Konto, das seine PIN nicht ändern kann, kein Konto ist. Der Weg liegt bei `auth/…`
und nicht in einer Ebene, weil er zu keiner gehört – **derselbe Mensch** bedient ihn aus jeder Rolle (die
dokumentierte Ausnahme in CLAUDE.md). Damit gilt er für **beide** Erwachsenen-Arten, nicht nur als
Lückenfüller für den Lehrer.

Drei Entscheidungen, die darin stecken:

- **Zwei Stellen schreiben.** Das `Account` trägt den Login, die `Father`-Zeile den fachlichen Namen (er
  erscheint als Autor an den Übungen). Der PIN-Hash *musste* schon immer gespiegelt werden, sonst läuft
  `auth/login` aus dem Takt; Name und E-Mail werden hier **mit** gespiegelt. `AdultsController` tat das
  bisher nicht – Konto- und Vater-Name konnten auseinanderdriften.
- **Kein Kind.** Ein Kind ändert Name und PIN nicht selbst: die PIN ist der Zugang, den der Vater vergibt.
  Sonst hätte sich das Kind der Aufsicht entzogen, und zwar über einen Endpunkt, der „mein Konto" heißt.
- **E-Mail nur mit Schalter löschbar** (`ClearEmail`), Eindeutigkeit gegen andere Konten → `409 conflict`.

Die Oberfläche: `/vater/profil` bedient jetzt beide Arten – der Lehrer sieht „Lehrer-Id" und seine Rollen
statt „Betreute Kinder"/„Konto seit", und der Profil-Link im Kopf ist für ihn zurück.

## Offen

- **`Father` als Tabellenname** für „Erwachsener" (siehe oben).
- **Hausaufgaben** – dafür bleibt der ältere Entwurf gültig, inklusive `Teacher`-Entität, Klassen,
  Beitrittscode und Ownership-Umkehr.
