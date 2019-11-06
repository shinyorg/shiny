Title: Pedometer
---
# Platforms Supported
|Platform|Version|
|--------|-------|
|Android|5|
|iOS|9|
|tizen|4|
|watchOS|5|

### iOS

If you plan to use the pedometer on iOS, you need to add the following to your Info.plist

```xml
<dict>
	<key>NSMotionUsageDescription</key>
	<string>Using some motion</string>
</dict>
```