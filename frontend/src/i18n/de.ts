/**
 * Deutsche Chrome-Texte der Sohn-Arcade (B-294). Nur UI-Chrome: Nutzerdaten (Plan-/Übungstitel, Missionen,
 * Shop-Artikel) bleiben, wie der Vater sie eingegeben hat (B-85, Entscheidung 3). Die Schlüssel sind die
 * Quelle – `en.ts` muss jeden davon tragen (`i18n.test.ts` hält die Parität).
 */
export const de = {
  sohn: {
    nav: { home: "Basis", path: "Weg", account: "Konto", shop: "Shop", skins: "Skins" },
    hud: {
      // `_one`/`_other`: i18next wählt die Form nach `count` (B-298) – die Chips werden vorgelesen.
      coins_one: "{{count}} Münze",
      coins_other: "{{count}} Münzen",
      gems_one: "{{count}} Gem",
      gems_other: "{{count}} Gems",
      streak_one: "{{count}} Tag Streak",
      streak_other: "{{count}} Tage Streak",
      soundOn: "Ton einschalten",
      soundOff: "Ton ausschalten",
      language: "EN – auf Englisch umschalten (Sprache ist Deutsch)",
      languageShort: "EN",
    },
    home: {
      loading: "Lade…",
      loadingMission: "Lade Mission…",
      error: "Fehler",
      noMissionTitle: "Noch keine Mission",
      noMissionText: "Dein Vater hat noch keinen Lehrplan für dich erstellt. Sobald es losgeht, erscheint hier deine Tagesmission.",
      signOut: "Abmelden",
      choosePlan: "Plan wählen",
      dailyMission: "Tagesmission",
      doneToday: "✓ Heute geschafft!",
      doneText: "Stark. Jede weitere Runde macht deinen {{skin}} nur stärker.",
      goalsToday: "🎯 Ziele heute",
      noExercises: "Dieser Plan hat noch keine Übungen. Dein Vater fügt sie gleich hinzu.",
      cadenceDaily: "Tagesziel",
      cadenceWeekly: "Wochenziel",
      cadenceNone: "frei",
      open: "offen",
      practice: "▶ ÜBEN",
      practiceDue: "▶ ÜBEN ({{count}} fällig)",
      playThrough: "▶ DURCHSPIELEN",
      test: "TEST",
    },
    missions: {
      title: "🎯 Missionen",
      done: "{{done}}/{{total}} geschafft",
      celebrateOne: "MISSION!",
      celebrateMany: "{{count}} MISSIONEN!",
      periodDaily: "Heute",
      periodWeekly: "Diese Woche",
      periodOneOff: "Ziel",
    },
    interests: {
      introTitle: "Erzähl mal von dir!",
      introText: "Sag uns, was du magst – dann passen die Bilder und Beispiele zu dir. Dauert eine Minute, ohne Tippen.",
      introGo: "Los geht's",
    },
  },
} as const;
