#!/bin/bash
# Builds the Declared Age Range Swift bridge into a dynamic xcframework consumed by Shiny.AgeSignals (net10.0-ios and
# net10.0-macos) via NativeReference. DeclaredAgeRange is weak-linked so the bridge loads on OS versions before 26.
# Runs automatically from the csproj on macOS; run manually after changing Swift sources
# if you're not building through MSBuild.
set -euo pipefail

NAME=ShinyAgeRange
MIN_IOS=15.0
MIN_MACOS=12.0
DIR="$(cd "$(dirname "$0")" && pwd)"
SRC="$DIR/Sources"
OUT="$DIR/build"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/$NAME.XXXXXX")"
LOCK="$DIR/.build.lock"
trap 'rm -rf "$WORK"' EXIT

# net10.0-ios and net10.0-macos both run this, in parallel on a multi-TFM build. One builds; the other waits, then finds
# the framework up to date. (mkdir is the lock - macOS has no flock.) A lock older than 10 minutes is a killed build.
while ! mkdir "$LOCK" 2>/dev/null; do
    if [ -n "$(find "$LOCK" -maxdepth 0 -mmin +10 2>/dev/null)" ]; then
        rm -rf "$LOCK"
    else
        sleep 1
    fi
done
trap 'rm -rf "$WORK" "$LOCK"' EXIT

PLIST="$OUT/$NAME.xcframework/Info.plist"
if [ -f "$PLIST" ] && [ -z "$(find "$SRC" "$0" -newer "$PLIST" -type f 2>/dev/null)" ]; then
    echo "$NAME.xcframework is up to date"
    exit 0
fi

build_slice() {
    local sdk=$1 plist_platform=$2 slice=$3 min_key=$4 min_version=$5
    shift 5
    local sdk_path
    sdk_path="$(xcrun --sdk "$sdk" --show-sdk-path)"
    local fw="$WORK/$slice/$NAME.framework"
    mkdir -p "$fw"

    local binaries=()
    for triple in "$@"; do
        echo "Compiling $NAME for $triple"
        xcrun --sdk "$sdk" swiftc \
            -emit-library \
            -module-name "$NAME" \
            -target "$triple" \
            -sdk "$sdk_path" \
            -swift-version 5 \
            -O -whole-module-optimization \
            -Xlinker -install_name -Xlinker "@rpath/$NAME.framework/$NAME" \
            -Xlinker -weak_framework -Xlinker DeclaredAgeRange \
            -o "$WORK/$slice/$NAME-$triple" \
            "$SRC"/*.swift
        binaries+=("$WORK/$slice/$NAME-$triple")
    done

    lipo -create "${binaries[@]}" -output "$fw/$NAME"

    cat > "$fw/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key><string>en</string>
    <key>CFBundleExecutable</key><string>$NAME</string>
    <key>CFBundleIdentifier</key><string>net.shinylib.$NAME</string>
    <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
    <key>CFBundleName</key><string>$NAME</string>
    <key>CFBundlePackageType</key><string>FMWK</string>
    <key>CFBundleShortVersionString</key><string>1.0</string>
    <key>CFBundleVersion</key><string>1</string>
    <key>CFBundleSupportedPlatforms</key><array><string>$plist_platform</string></array>
    <key>$min_key</key><string>$min_version</string>
</dict>
</plist>
EOF

    # macOS frameworks use the versioned bundle layout - codesign rejects a shallow one embedded in an app
    if [ "$plist_platform" = "MacOSX" ]; then
        mkdir -p "$fw/Versions/A/Resources"
        mv "$fw/$NAME" "$fw/Versions/A/$NAME"
        mv "$fw/Info.plist" "$fw/Versions/A/Resources/Info.plist"
        ln -s A "$fw/Versions/Current"
        ln -s "Versions/Current/$NAME" "$fw/$NAME"
        ln -s Versions/Current/Resources "$fw/Resources"
    fi
}

build_slice iphoneos iPhoneOS ios-arm64 MinimumOSVersion $MIN_IOS "arm64-apple-ios$MIN_IOS"
build_slice iphonesimulator iPhoneSimulator ios-arm64_x86_64-simulator MinimumOSVersion $MIN_IOS "arm64-apple-ios$MIN_IOS-simulator" "x86_64-apple-ios$MIN_IOS-simulator"
build_slice macosx MacOSX macos-arm64_x86_64 LSMinimumSystemVersion $MIN_MACOS "arm64-apple-macos$MIN_MACOS" "x86_64-apple-macos$MIN_MACOS"

xcodebuild -create-xcframework \
    -framework "$WORK/ios-arm64/$NAME.framework" \
    -framework "$WORK/ios-arm64_x86_64-simulator/$NAME.framework" \
    -framework "$WORK/macos-arm64_x86_64/$NAME.framework" \
    -output "$WORK/$NAME.xcframework" >/dev/null

# swap in atomically so a failed build never leaves a half-written framework behind
mkdir -p "$OUT"
rm -rf "$OUT/$NAME.xcframework"
mv "$WORK/$NAME.xcframework" "$OUT/$NAME.xcframework"
echo "Built $OUT/$NAME.xcframework"
