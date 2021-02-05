using Shiny.Attributes;

[assembly: AutoStartupWithDelegate("Shiny.Net.Http.IHttpTransferDelegate", "UseHttpTransfers", true)]

[assembly: StaticGeneration("Shiny.Net.Http.IHttpTransferManager", "ShinyHttpTransfers")]

