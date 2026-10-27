/**
 * English chrome texts of the son's arcade (B-294) – the same keys as `de.ts`, translated. The texts are drafted
 * by the night run and still have to be read by a person (B-85: "Die englischen Texte liest am Ende ein Mensch gegen").
 */
export const en = {
  sohn: {
    nav: { home: "Home", path: "Path", account: "Account", shop: "Shop", skins: "Skins" },
    hud: {
      coins_one: "{{count}} coin",
      coins_other: "{{count}} coins",
      gems_one: "{{count}} gem",
      gems_other: "{{count}} gems",
      streak_one: "{{count}}-day streak",
      streak_other: "{{count}}-day streak",
      soundOn: "Turn sound on",
      soundOff: "Turn sound off",
      language: "DE – switch to German (language is English)",
      languageShort: "DE",
    },
    home: {
      loading: "Loading…",
      loadingMission: "Loading mission…",
      error: "Error",
      noMissionTitle: "No mission yet",
      noMissionText: "Your dad hasn't set up a study plan for you yet. As soon as it starts, your daily mission shows up here.",
      signOut: "Sign out",
      choosePlan: "Choose plan",
      dailyMission: "Daily mission",
      doneToday: "✓ Done for today!",
      doneText: "Strong. Every extra round only makes your {{skin}} stronger.",
      goalsToday: "🎯 Goals today",
      noExercises: "This plan has no exercises yet. Your dad is adding them right away.",
      cadenceDaily: "daily goal",
      cadenceWeekly: "weekly goal",
      cadenceNone: "free",
      open: "open",
      practice: "▶ PRACTISE",
      practiceDue: "▶ PRACTISE ({{count}} due)",
      playThrough: "▶ PLAY THROUGH",
      test: "TEST",
    },
    missions: {
      title: "🎯 Missions",
      done: "{{done}}/{{total}} done",
      celebrateOne: "MISSION!",
      celebrateMany: "{{count}} MISSIONS!",
      periodDaily: "Today",
      periodWeekly: "This week",
      periodOneOff: "Goal",
    },
    interests: {
      introTitle: "Tell us about you!",
      introText: "Tell us what you like – then the pictures and examples fit you. Takes a minute, no typing.",
      introGo: "Let's go",
    },
  },
} as const;
