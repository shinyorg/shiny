#!/bin/bash
# Builds the StoreKit 2 Swift bridge into a dynamic xcframework consumed by Shiny.Mobile.InAppPurchases (net10.0-ios)
# via NativeReference. Runs automatically from the csproj on macOS; run manually after changing Swift sources
# if you're not building through MSBuild.
set -euo pipefail

NAME=ShinyStoreKit
MIN_IOS=15.0
DIR="$(cd "$(dirname "$0")" && pwd)"
SRC="$DIR/Sources"
OUT="$DIR/build"
WORK="$(mktemp -d "${TMPDIR:-/tmp}/$NAME.XXXXXX")"
trap 'rm -rf "$WORK"' EXIT

build_slice() {
    local sdk=$1 plist_platform=$2 slice=$3
    shift 3
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
    <key>MinimumOSVersion</key><string>$MIN_IOS</string>
</dict>
</plist>
EOF
}

build_slice iphoneos iPhoneOS ios-arm64 "arm64-apple-ios$MIN_IOS"
build_slice iphonesimulator iPhoneSimulator ios-arm64_x86_64-simulator "arm64-apple-ios$MIN_IOS-simulator" "x86_64-apple-ios$MIN_IOS-simulator"

xcodebuild -create-xcframework \
    -framework "$WORK/ios-arm64/$NAME.framework" \
    -framework "$WORK/ios-arm64_x86_64-simulator/$NAME.framework" \
    -output "$WORK/$NAME.xcframework" >/dev/null

# swap in atomically so a failed build never leaves a half-written framework behind
mkdir -p "$OUT"
rm -rf "$OUT/$NAME.xcframework"
mv "$WORK/$NAME.xcframework" "$OUT/$NAME.xcframework"
echo "Built $OUT/$NAME.xcframework"
