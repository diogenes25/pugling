# API-Beispiele – Übersicht

Automatisch erzeugt von `backend/Pugling.Api.Tests/DocsCaptureTests.cs`. Insgesamt **143** Beispiele in **12** Gruppen.

| Gruppe | Beispiele | Fehlerfälle | Datei |
| --- | ---: | ---: | --- |
| auth | 6 | 3 | [`auth.md`](./auth.md) |
| catalog | 22 | 5 | [`catalog.md`](./catalog.md) |
| children | 7 | 2 | [`children.md`](./children.md) |
| class-tests | 3 | 2 | [`class-tests.md`](./class-tests.md) |
| exercise-grants | 7 | 3 | [`exercise-grants.md`](./exercise-grants.md) |
| me | 15 | 5 | [`me.md`](./me.md) |
| remarks | 19 | 7 | [`remarks.md`](./remarks.md) |
| shop | 28 | 8 | [`shop.md`](./shop.md) |
| study-plans | 24 | 9 | [`study-plans.md`](./study-plans.md) |
| tags | 5 | 3 | [`tags.md`](./tags.md) |
| timetable | 3 | 2 | [`timetable.md`](./timetable.md) |
| vocabulary | 4 | 2 | [`vocabulary.md`](./vocabulary.md) |

## Fehler-Code-Abdeckung

Verifiziert: **34 / 60** Codes aus `ApiErrors`.

| Code | Beispiel |
| --- | --- |
| `activation_not_pending` | shop – Aktivierung erneut genehmigen |
| `conflict` | catalog – Doppelte Art anlegen |
| `duplicate_key` | vocabulary – Vokabel mit doppeltem Key |
| `duplicate_tag_name` | tags – Tag mit doppeltem Namen |
| `exercise_empty` | study-plans – Ungefüllte Übung zuweisen |
| `exercise_in_use` | catalog – Verwendete Übung löschen |
| `exercise_not_executable` | exercise-grants – Nicht ausführbare Übung zuweisen |
| `forbidden` | me – Vater greift auf Sohn-Route zu |
| `insufficient_coins` | shop – Shop-Angebot kaufen (kein Guthaben) |
| `insufficient_gems` | me – Skin kaufen ohne Gems |
| `insufficient_inventory` | shop – Aktivierungsanfrage (Inventar erschöpft) |
| `invalid_credentials` | auth – Login mit falscher PIN |
| `invalid_reference` | study-plans – Position mit unbekannter Übung |
| `last_owner` | exercise-grants – Letzten Owner entfernen |
| `no_checkable_content` | study-plans – Test auf Übung ohne prüfbaren Inhalt |
| `no_tag_matches` | study-plans – Tag-Schnappschuss ohne Treffer |
| `not_author` | catalog – Fremd-Autor-Übung bearbeiten |
| `not_found` | children – Fremdes Kind lesen |
| `not_owner` | exercise-grants – Rechte einer fremden Übung auflisten |
| `plan_inactive` | study-plans – Deaktivierten Plan spielen |
| `position_has_data` | study-plans – Bespielte Position löschen |
| `remark_not_found` | remarks – Fremde Anmerkung lesen (Sohn) |
| `remark_scope_forbidden` | remarks – Alle Konten lesen als Sohn |
| `shop_insufficient_stock` | shop – Shop-Angebot kaufen (ausverkauft) |
| `shop_listing_inactive` | shop – Shop-Angebot kaufen (deaktiviert) |
| `skin_already_unlocked` | me – Bereits besessenen Skin kaufen |
| `skin_not_unlocked` | me – Nicht besessenen Skin ausrüsten |
| `test_already_submitted` | study-plans – Test erneut abgeben |
| `test_attempts_exhausted` | study-plans – Dritter Testversuch des Tages (Deckel) |
| `timetable_slot_taken` | timetable – Gleiches Fach am selben Wochentag |
| `unauthorized` | auth – Selbstauskunft ohne Token |
| `unknown_field` | timetable – Unbekanntes Feld im Body |
| `validation_error` | auth – Login mit nicht-numerischer adultId |
| `vocabulary_in_use` | vocabulary – Verwendete Grundform löschen |

## Nicht automatisch erfassbar

- `bad_request` — Generischer 400-Default (`ForStatus`): nur Sicherheitsnetz für Framework-Antworten ohne spezifischen Code – alle regulären 400-Pfade tragen bereits einen fachlichen Code.
- `concurrency_conflict` — Erfordert eine echte Schreib-Kollision (Doppelklick/Retry) über das Concurrency-Token; in-process nicht deterministisch per HTTP auslösbar (siehe SkinPurchaseTests, direkt über DbContext).
- `duplicate_achievement` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `duplicate_email` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `duplicate_key_result` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `duplicate_profile_name` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `duplicate_vocabulary_in_exercise` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `exercise_not_assigned` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `http_error` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `internal_error` — 500-Fallback für unbehandelte Ausnahmen – kein sicherer, gezielter Auslöser über die öffentliche API.
- `item_not_found` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_already_linked` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_link_not_found` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_no_alternative` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_not_an_image` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_not_on_card` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_upload_too_large` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_variant_exists` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `media_variant_not_found` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `purchase_not_open` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `rate_limited` — Login-Rate-Limit ist in der Test-Factory bewusst abgeschaltet (`RateLimiting:LoginEnabled=false`), sonst würden die vielen Test-Logins scheitern.
- `remark_comment_not_found` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `series_without_subject` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `stage_not_testable` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `unknown_exercise_type` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
- `vocabulary_not_assigned` — Von DocsCaptureTests nicht mitgeschnitten – ob ein anderer Test den Code über HTTP auslöst, ist hier nicht geprüft.
