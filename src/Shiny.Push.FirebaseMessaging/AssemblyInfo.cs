using Shiny.Attributes;

[assembly: AutoStartupWithDelegate("Shiny.Push.IPushDelegate", "UseFirebaseMessaging", true)]
[assembly: StaticGeneration("Shiny.Push.IPushManager", "ShinyPush")]