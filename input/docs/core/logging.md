Title: Logging
---

# Logging

Most will ask the question, why do we need another logging abstraction when we have Microsoft.Extensions.Logging.  This is a good question and an easy answer.  That particular extension brought in everything and the kitchen sink.  I didn't want end users having to dive that far into things for little gain.  Thus, a simple logging mechanism was introduced into shiny to allow you to log your own stuff as well as Shiny internals.