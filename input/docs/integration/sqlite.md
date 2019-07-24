Title: SQLite
---
# INTEGRATIONS - SQLite

The SQLite integration provides several overrides to the built-in modules within Shiny.  

* Caching
* Storage
* Settings
* Logging

## Registering Them

For the most part, you won't see SQLite after you register it in your Shiny startup.  You are simply swapping "engines" for the "under the hood" stuff. 


## Why Swap with SQLite

There are 3 main points to consider when swapping in SQLite
1. If you are using SQLite Crypto - you will gain data protection across all of the different function points.  
2. Performance is not necessarily going to be better and in some cases will be worse.  
3. Data persistence will last across sessions where in-memory providers (cache) are used.

|Feature|Benefit(s)|Sacrific(s)
|-------|-----|
|Caching|Your cache values will survive across sessions if the values don't expire.  You will sacrifice performance vs the in-memory provider though. |
|Storage|The benefit here is that you can use SQLite encryption to potentially protect values|
|Settings|Same benefits of potential encryption.  However, if you are doing frequent (many changes in a second) writes to settings, you will lose performance|