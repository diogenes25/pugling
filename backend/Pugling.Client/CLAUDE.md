# Pugling.Client – die eine HTTP-Schicht

Die *eine* HTTP-Schicht für Nicht-Browser-Konsumenten (die KI-Agenten). Registrierung per
`services.AddPuglingClient(config)`.

- `AuthHandler` – konto-zentrischer Login, proaktive Token-Erneuerung, 401-Retry – über den geteilten
  `PuglingTokenStore` (**eine** Anmeldung für alle Fassaden; der Handler selbst wird je Client neu
  erzeugt – eine geteilte `DelegatingHandler`-Instanz lehnt die `HttpClientFactory` beim zweiten Client ab).
- `PuglingJson` – Web-Defaults **+** `JsonStringEnumConverter`; Enum-Parität ist Pflicht.
- `PuglingResponse` – ProblemDetails → `PuglingApiException` mit stabilem `code`.
- Die dünnen Fassaden `CreatorApi`/`SupervisorApi`/`StudentApi` (Letztere = die Lernstand-Lesesichten,
  die ein Supervisor-Konto mitlesen darf).

Verifiziert von `PuglingClientTests` gegen den echten In-Process-Server.
