Title: Pedometer
---

<?! PackageInfo "Shiny.Sensors" "Shiny.Sensors.IPedometer" /?>

### iOS

If you plan to use the pedometer on iOS, you need to add the following to your Info.plist

```xml
<dict>
	<key>NSMotionUsageDescription</key>
	<string>Using some motion</string>
</dict>
```