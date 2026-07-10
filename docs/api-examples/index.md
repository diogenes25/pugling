# API-Beispiele – Übersicht

Automatisch erzeugt von `backend/Pugling.Api.Tests/DocsCaptureTests.cs`. Insgesamt **101** Beispiele in **10** Gruppen.

| Gruppe | Beispiele | Fehlerfälle | Datei |
| --- | ---: | ---: | --- |
| auth | 6 | 3 | [`auth.md`](./auth.md) |
| catalog | 10 | 5 | [`catalog.md`](./catalog.md) |
| children | 7 | 2 | [`children.md`](./children.md) |
| class-tests | 3 | 2 | [`class-tests.md`](./class-tests.md) |
| me | 15 | 5 | [`me.md`](./me.md) |
| shop | 28 | 8 | [`shop.md`](./shop.md) |
| study-plans | 21 | 6 | [`study-plans.md`](./study-plans.md) |
| tags | 5 | 3 | [`tags.md`](./tags.md) |
| timetable | 2 | 1 | [`timetable.md`](./timetable.md) |
| vocabulary | 4 | 2 | [`vocabulary.md`](./vocabulary.md) |

## Fehler-Code-Abdeckung

Verifiziert: **25 / 32** Codes aus `ApiErrors`.

| Code | Beispiel |
| --- | --- |
| `activation_not_pending` | shop – Aktivierung erneut genehmigen |
| `conflict` | catalog – Doppelte Art anlegen |
| `duplicate_key` | vocabulary – Vokabel mit doppeltem Key |
| `duplicate_tag_name` | tags – Tag mit doppeltem Namen |
| `exercise_in_use` | catalog – Verwendete Übung löschen |
| `forbidden` | me – Vater greift auf Sohn-Route zu |
| `insufficient_coins` | shop – Shop-Angebot kaufen (kein Guthaben) |
| `insufficient_gems` | me – Skin kaufen ohne Gems |
| `insufficient_inventory` | shop – Aktivierungsanfrage (Inventar erschöpft) |
| `invalid_credentials` | auth – Login mit falscher PIN |
| `invalid_reference` | study-plans – Position mit unbekannter Übung |
| `no_checkable_content` | study-plans – Test auf Übung ohne prüfbaren Inhalt |
| `not_author` | catalog – Fremd-Autor-Übung bearbeiten |
| `not_found` | children – Fremdes Kind lesen |
| `plan_inactive` | study-plans – Deaktivierten Plan spielen |
| `position_has_data` | study-plans – Bespielte Position löschen |
| `shop_insufficient_stock` | shop – Shop-Angebot kaufen (ausverkauft) |
| `shop_listing_inactive` | shop – Shop-Angebot kaufen (deaktiviert) |
| `skin_already_unlocked` | me – Bereits besessenen Skin kaufen |
| `skin_not_unlocked` | me – Nicht besessenen Skin ausrüsten |
| `test_already_submitted` | study-plans – Test erneut abgeben |
| `timetable_slot_taken` | timetable – Gleiches Fach am selben Wochentag |
| `unauthorized` | auth – Selbstauskunft ohne Token |
| `validation_error` | auth – Login mit nicht-numerischer fatherId |
| `vocabulary_in_use` | vocabulary – Verwendete Grundform löschen |

## Nicht automatisch erfassbar

- `bad_request` — Generischer 400-Default (`ForStatus`): nur Sicherheitsnetz für Framework-Antworten ohne spezifischen Code – alle regulären 400-Pfade tragen bereits einen fachlichen Code.
- `concurrency_conflict` — Erfordert eine echte Schreib-Kollision (Doppelklick/Retry) über das Concurrency-Token; in-process nicht deterministisch per HTTP auslösbar (siehe SkinPurchaseTests, direkt über DbContext).
- `http_error` — Über HTTP im In-Process-Test nicht erreichbar.
- `internal_error` — 500-Fallback für unbehandelte Ausnahmen – kein sicherer, gezielter Auslöser über die öffentliche API.
- `item_not_found` — Über HTTP im In-Process-Test nicht erreichbar.
- `purchase_not_open` — Über HTTP im In-Process-Test nicht erreichbar.
- `rate_limited` — Login-Rate-Limit ist in der Test-Factory bewusst abgeschaltet (`RateLimiting:LoginEnabled=false`), sonst würden die vielen Test-Logins scheitern.

