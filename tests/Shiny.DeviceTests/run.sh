#!/usr/bin/env bash
# Builds Shiny.DeviceTests for a target, installs it, and runs the suites through Shiny.DeviceTests.Runner.
#
#   tests/Shiny.DeviceTests/run.sh ios [runner args...]           booted iOS simulator
#   tests/Shiny.DeviceTests/run.sh android [runner args...]       first adb device / emulator
#   tests/Shiny.DeviceTests/run.sh maccatalyst [runner args...]
#
# e.g. run.sh ios --filter Gps,Geofencing --junit artifacts/device-tests/ios.xml
# The two-device BLE loop (installs are separate - see readme.md):
#   run.sh ios --device <iphone-udid> --agent http://<iphone-ip>:9301 --peer maccatalyst --filter Ble
set -euo pipefail

platform="${1:?usage: run.sh <ios|android|maccatalyst> [runner args...]}"
shift
root="$(cd "$(dirname "$0")/../.." && pwd)"
project="$root/tests/Shiny.DeviceTests/Shiny.DeviceTests.csproj"
runner="$root/tests/Shiny.DeviceTests.Runner/Shiny.DeviceTests.Runner.csproj"
bin="$root/tests/Shiny.DeviceTests/bin/Debug"

# a physical iPhone is chosen with --device <udid> - pick the matching RID
device=""
args=("$@")
for ((i = 0; i < ${#args[@]}; i++)); do
    if [[ "${args[$i]}" == "--device" ]]; then device="${args[$((i + 1))]}"; fi
done

case "$platform" in
    ios)
        if [[ -n "$device" ]] && ! xcrun simctl list devices | grep -q "$device"; then
            dotnet build "$project" -f net10.0-ios -p:RuntimeIdentifier=ios-arm64
            xcrun devicectl device install app --device "$device" "$bin/net10.0-ios/ios-arm64/Shiny.DeviceTests.app"
        else
            dotnet build "$project" -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64
            xcrun simctl install "${device:-booted}" "$bin/net10.0-ios/iossimulator-arm64/Shiny.DeviceTests.app"
        fi
        ;;
    android)
        dotnet build "$project" -f net10.0-android -t:Install
        ;;
    maccatalyst)
        dotnet build "$project" -f net10.0-maccatalyst
        open "$(find "$bin/net10.0-maccatalyst" -maxdepth 2 -name "Shiny.DeviceTests.app" | head -1)"
        ;;
    *)
        echo "unknown platform $platform" >&2
        exit 2
        ;;
esac

dotnet build "$runner" -v q
exec dotnet run --no-build --project "$runner" -- --platform "$platform" --launch "$@"
