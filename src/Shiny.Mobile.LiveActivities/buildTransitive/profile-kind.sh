#!/bin/sh
# Prints the kind of a provisioning profile: Development, Ad Hoc, App Store or Enterprise.
# Used by Shiny.Mobile.LiveActivities.targets to catch an app and widget extension signed with different kinds.
plist=`security cms -D -i "$1" 2>/dev/null`
if echo "$plist" | plutil -extract ProvisionsAllDevices raw -o - - 2>/dev/null | grep -q true; then
    echo Enterprise
elif echo "$plist" | plutil -extract ProvisionedDevices xml1 -o - - >/dev/null 2>&1; then
    if echo "$plist" | plutil -extract Entitlements.get-task-allow raw -o - - 2>/dev/null | grep -q true; then
        echo Development
    else
        echo "Ad Hoc"
    fi
else
    echo "App Store"
fi
