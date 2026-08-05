# Pugling.Api.Tests – Wegwerf-Dateien

Lädt nur, wenn unter `backend/Pugling.Api.Tests/` gearbeitet wird.

**Wer eine Wegwerf-Datei anlegt, löscht sie im selben Objekt.** Kein Zähl-Wächter dafür (B-55): ein
frischer CI-Runner hat nie etwas anzusammeln, und lokal stört ihn jeder parallel laufende Testprozess mit
eigenem Bestand – ein Tor würde hier das Falsche messen. Die Regel ist Disziplin, nicht Mechanik, und
gehört darum resident, wo sie beim Schreiben eines neuen Tests gelesen wird.

- **Ein einzelner Test** (kein Klassen-Fixture): `try`/`finally` um den Testkörper, im `finally`
  `SqliteConnection.ClearPool(...)` **vor** `File.Delete(...)` – der Pool hält sonst das Datei-Handle
  offen, auch wenn jede `SqliteConnection` längst disposed ist (Vorbild: `QueryPlanSmokeTests`).
- **Ein Klassen-Fixture** (`IClassFixture<T>`): Aufräumen gehört in `DisposeAsync()`, **nicht** in
  `Dispose(bool)`. xUnit entsorgt eine Fixture über `IAsyncDisposable`, wenn der Typ es anbietet, und die
  Basisklasse `WebApplicationFactory<TEntryPoint>.DisposeAsync()` führt **nicht** durch `Dispose(bool)` –
  ein Aufräum-Schritt dort ist toter Code für jede Klassen-Fixture (Vorbild:
  `PuglingWebAppFactory.DisposeAsync()`).

Beide Muster teilen denselben Fallstrick: Aufräum-Code, der aussieht, als liefe er, ist schlimmer als
keiner – er täuscht eine Zusicherung vor, die es nicht gibt.
