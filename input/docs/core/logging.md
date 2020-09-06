Title: Logging
---

# Logging

Most will ask the question, why do we need another logging abstraction when we have Microsoft.Extensions.Logging.  This is a good question and an easy answer.  That particular extension brought in everything and the kitchen sink.  I didn't want end users having to dive that far into things for little gain.  Thus, a simple logging mechanism was introduced into shiny to allow you to log your own stuff as well as Shiny internals.

Logging is exceptionally important in Shiny due to debugging in the background sometimes being a bit difficult.  Shiny has been designed to really protect the background from crashing and collecting log information for when things go bad.  Out of the box, Shiny has a bunch of loggers

In the core:
* Debug
* Console
* File - Log.UseFile(); 

With the SQLite integration package, it also adds a logger for SQLite as well :)


```csharp
// TODO
```