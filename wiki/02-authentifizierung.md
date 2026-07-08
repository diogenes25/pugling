---
tags: [typ/konzept, bereich/auth]
---

# 02 · Authentifizierung & Rollen

← [Zurück zum Wiki-Index](../README.md)

Alle geschützten Aufrufe brauchen ein **JWT** im Header `Authorization: Bearer <token>`. Der Token
kommt aus einem PIN-Login und trägt die Rolle plus die Identitäts-Claims.

---

## 1. Login

### Vater

```http
POST /api/v1/auth/father
Content-Type: application/json

{ "fatherId": 1, "pin": "0000" }
```

Antwort (`LoginResponse`):

```json
{ "token": "eyJ…", "role": "Vater", "id": 1, "name": "Papa", "expiresAt": "2026-07-04T22:00:00Z" }
```

### Sohn (Kind)

```http
POST /api/v1/auth/child
{ "childId": 1, "pin": "1111" }
→ { "token": "eyJ…", "role": "Sohn", "id": 1, "name": "Sohn", "expiresAt": … }
```

Den `token` in **allen** weiteren Aufrufen mitgeben. Gültigkeit: 12 h (HS256, `TokenService`).
In Swagger den „Authorize"-Button nutzen.

### Selbstauskunft

```http
GET /api/v1/auth/me        → { role, fatherId, childId, name }
```

---

## 2. Registrierung / Bootstrapping

Ein neuer **Vater** kann sich **anonym** anlegen (einziger nicht authentifizierter Schreibpfad):

```http
POST /api/v1/fathers
{ "name": "Klaus", "email": "klaus@example.com", "pin": "1234" }
```

Danach als dieser Vater einloggen und **Kinder** anlegen:

```http
POST /api/v1/children      (Bearer: Vater)
{ "name": "Peter", "birthYear": 2015, "pin": "1111" }
```

> **PINs liegen im Klartext** (`Father.Pin`/`Child.Pin`) — bewusster offener Punkt bis Prod
> (siehe Backlog). Vor Produktion hashen + Login-Rate-Limit.

---

## 3. Rollen & Berechtigungen

| Bereich | Vater | Sohn |
| --- | --- | --- |
| Katalog (Subjects/Chapters/Exercises), Stores (Vocabulary/Cloze), Kategorien | ✅ CRUD | ⛔ 403 |
| Study-Pläne anlegen/ändern (`POST/PATCH …`) | ✅ | ⛔ 403 |
| Study-Pläne lesen, üben, testen (`GET/practice/tests`) | ✅ (eigene Kinder) | ✅ (nur eigene) |
| Missionen/Auszeichnungen **definieren** (`children/{}/…`) | ✅ | ⛔ |
| Eigene Missionen/Auszeichnungen/Punkte lesen (`me/…`) | ⛔ (nutzt `children/{}`) | ✅ |
| Punkte manuell buchen (`POST children/{}/points`) | ✅ | ⛔ |
| Stundenplan pflegen (`children/{}/timetable`) | ✅ | ⛔ |
| Inhalte bewerten (`study-plans/{}/ratings`) | — | ✅ |
| Einen Tag **nachtragen** (`day` ≠ heute im Practice/Test-Start) | ✅ | ⛔ 403 (Anti-Schummel) |

Technisch: `[Authorize(Roles = Roles.Vater)]` bzw. `[Authorize]` auf den Controllern.

---

## 4. Eigentum (Ownership)

Zusätzlich zur Rolle wird geprüft, dass die Ressource **dem angemeldeten Nutzer gehört**:

- **`AuthAccess`** (`OwnsPlanAsync`/`OwnsChildAsync`/`FatherOwnsChildAsync`): Sohn nur eigene Pläne
  (`plan.ChildId == cid`), Vater nur Pläne seiner Kinder.
- **`PlanOwnershipFilter`** (`[ServiceFilter]`): sichert alle Endpunkte unter `{planId}` zentral
  (Existenz + Eigentum) — nicht inline wiederholen.
- **`ChildOwnershipFilter`**: sichert alle Endpunkte unter `{childId}`.

Ergebnis bei Fremdzugriff: **403 Forbidden** — bzw. **404** dort, wo das Enumerieren fremder IDs
verhindert werden soll (z. B. „Kind nicht gefunden" beim Plan-Anlegen).

---

## 5. Anti-Schummel-Prinzipien

Weil der Sohn unbeaufsichtigt lernt und Punkte etwas „wert" sind, erzwingt der Server:

1. **Server-autoritative Bewertung** — das Frontend liefert nur die Antwort, nie „richtig". Der Server
   prüft gegen die Lösung.
2. **Stufe aus dem Fahrplan** — der Client kann die Teststufe nicht heruntersetzen (sonst würde er eine
   getippte Stufe auf „Selbsteinschätzung" drücken).
3. **`RequireTypedTest`** — zählt ein Test/Review nur, wenn wirklich getippt wurde (Stufe ≥ Buchstabenfelder).
4. **Kein Tag-Nachtragen** durch den Sohn (`day` ≠ heute → 403). Heute muss heute gemacht werden.
5. **Heartbeat-Clamp** — pro Heartbeat höchstens 120 s anrechenbar (kein Zeit-Cheat).
6. **Speed-Bonus-Untergrenze** — Antworten unter 1 s zählen nicht als „schnell" (kein Doppel-Klick-Farming).
7. **Idempotente Belohnungen** — Tages-Punkte, Missionen und Auszeichnungen fließen nie doppelt (Unique-Index).

Details der Bewertung: [05 · Punkte & Bonus](05-punkte-und-bonus.md).
