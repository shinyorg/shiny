Title: Best Practices
---
# Best Practices

1. Always use the <TheShinyService>.RequestAccess to check if your user gives the right permissions to use the service in question. 
2. Don't do UI centric things from a Shiny delegate, this includes things like calling RequestAccess.  Most Shiny services have an equivalent "foreground" type call on the service that can be used safely within the UI portion of your application.
3. Don't try to setup background processes in a delegate or as your app is going to sleep.  The methods for setting up background processes in Shiny are all asyncc and the OS will not wait for things to complete.  
4. Treat everything as a singleton in Shiny