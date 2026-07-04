# Pugling 🐶

Lernapp mit Punktesystem: Sohn lernt Vokabeln (Leitner-Prinzip) auf dem Handy (PWA), Vater sieht Lernzeiten und Fortschritt im Dashboard und verwaltet Punkte, Zeitfenster und Belohnungen.

## Stack

- **Backend:** ASP.NET Core 10 (C# 14), EF Core, SQLite, REST-API mit OpenAPI + Swagger UI → `backend/Pugling.Api`
- **Frontend:** React + TypeScript, Vite, PWA → `frontend`

> **Hinweis (API-First):** Pugling ist API-First; die REST-API ist die Quelle der Wahrheit
> (direkt bzw. über die Skills `vater`/`sohn`). Der ursprüngliche Template-Stack (`/api/vocab`,
> `/api/sessions`, `/api/points`, `/api/settings`) wurde entfernt – das mitgelieferte PWA-Frontend
> hängt daran und ist bis zum Neubau gegen die neue API **vorübergehend außer Betrieb**.
> Details & Migrationsplan: [docs/architektur-entscheidung.md](docs/architektur-entscheidung.md).

## Starten

```bash
# Backend (Terminal 1) – läuft auf http://localhost:5200
# Swagger UI: http://localhost:5200/swagger  ·  OpenAPI-JSON: http://localhost:5200/openapi/v1.json
cd backend/Pugling.Api
dotnet run

# Frontend (Terminal 2) – läuft auf http://localhost:5173, proxied /api ans Backend
cd frontend
npm install
npm run dev
```

Beim ersten Start wird `pugling.db` angelegt und mit Beispieldaten gefüllt (Nutzer „Papa"/„Sohn", Zeitfenster, Beispiel-Vokabeln).

## Was schon funktioniert

- Lernkarten nach Leitner (5 Boxen, Intervalle 1/2/4/7/14 Tage)
- Punkte: neue Vokabel 10, Wiederholung weniger (je höher die Box), multipliziert mit dem Zeitfenster (Vormittag 1,5× / Nachmittag 1× / Abend 0,8× – per API änderbar)
- Aktivitätstracking: Heartbeat alle 15 s; ohne Interaktion (Touch/Taste/Karte) innerhalb 25 s zählt die Zeit als **inaktiv** – das Dashboard zeigt aktiv vs. „nur geguckt" getrennt
- Belohnungen: Sohn macht Angebot („30 min Fernsehen" für X Punkte), Vater nimmt an oder lehnt ab; bei Annahme werden Punkte abgezogen
- Vater-Dashboard: Punktestand, aktive/inaktive Lernzeit, letzte Lerneinheiten
- Tagging & Klassenarbeiten (API): Übungen von Vater/Sohn taggen, Klassenarbeiten planen und benoten, gezielt für eine anstehende Arbeit üben bzw. Übungen schlecht benoteter Arbeiten wiederholen – siehe [docs/klassenarbeiten-tagging.md](docs/klassenarbeiten-tagging.md)

## Dokumentation

- [docs/lehrplan-erstellen.md](docs/lehrplan-erstellen.md) – Handbuch für den Vater: einen kompletten Lehrplan mit Fächern/Modulen und Übungen anlegen (Skills `vater`/`sohn`), inkl. vollständigem Beispiel und Tutorial (auch für mehrere Söhne)
- [docs/tutorial.md](docs/tutorial.md) – Pugling-App per API steuern (Vater richtet ein, Sohn lernt)
- [docs/klassenarbeiten-tagging.md](docs/klassenarbeiten-tagging.md) – Übungen taggen & für Klassenarbeiten üben

## Nächste Schritte (bewusst noch offen)

1. **Login per PIN** – Feld existiert im Datenmodell, UI schaltet aktuell frei um
2. **Push-Erinnerungen** – Web Push vom Backend (NuGet-Paket `WebPush`, VAPID-Keys), `ReminderTime` steht schon im Lernplan
3. **Abschlusstest** pro Thema – `TestResult`-Entity existiert bereits
4. **Vokabel-Verwaltung im Dashboard** – API vorhanden (`POST /api/learn/vocabulary` als Store, Verknüpfung über `StudyPlan`-Items), UI fehlt
5. **EF-Migrationen** statt `EnsureCreated()` sobald das Schema stabiler wird
6. **Deployment** – damit die PWA aufs Handy kann, muss das Ganze per HTTPS erreichbar sein (z. B. kleiner VPS, Azure App Service o. ä.); HTTPS ist Pflicht für PWA-Installation und Push

## Hinweis zum Handy

PWA auf Android installieren: Seite in Chrome öffnen → Menü → „Zum Startbildschirm hinzufügen". Erst dann funktionieren Push-Benachrichtigungen.
