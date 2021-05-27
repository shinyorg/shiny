# Shiny Samples



## To Run:

Android
* Copy your own google-services.json and ensure its build action is set to GoogleServicesJson

iOS 
* For firebase testing, copy in your own GoogleService-Info.plist
* Ensure your provisioning profile is assigned push notification permissions

## Secrets.json
Create a secrets.json in the solution root with the following values:

* AzureNotificationHubFullConnectionString - full endpoint that allows sending messages
* AzureNotificationHubListenerConnectionString - a listen only connection string
* AzureNotificationHubName - your test hub name
* GoogleCredentialProjectId - Firebase project ID
* GoogleCredentialPrivateKeyId - for testing firebase (leave blank otherwise)
* GoogleCredentialPrivateKey - for testing firebase (leave blank otherwise)
* GoogleCredentialClientId - for testing firebase (leave blank otherwise)
* GoogleCredentialClientEmail - for testing firebase (leave blank otherwise)
* GoogleCredentialClientCertUrl - for testing firebase (leave blank otherwise)
* AppCenterKey - Your appcenter key for android/ios (leave blank if not using)