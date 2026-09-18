# Shiny Client for .NET — Working Notes

Guidance for maintaining this repo. This is the **core monorepo** — many modules ship from `src/`
(e.g. Core/Foundation, Jobs, Locations, Notifications, Push, BluetoothLE + BLE Hosting, HTTP
Transfers, Stores, Configuration, DI). Code lives in `src/`, tests in `tests/`, the published Claude
Code skills in `skills/` (one per module, e.g. `shiny-jobs`, `shiny-locations`, `shiny-push`, …),
and the public documentation site in a **separate** repo at `~/Desktop/dev/documentation` (rendered
to https://shinylib.net).

## Documentation site

The public docs live in a **separate repo**: `~/Desktop/dev/documentation` (Astro / Starlight).
Every module in this monorepo lives under **one docs folder**, `src/content/docs/client/`, with one
subfolder per module (URLs are `/client/<module>/...`):

- Feature pages: `src/content/docs/client/<module>/*.mdx` — `core/`, `ble/`, `blehosting/`,
  `beacons/`, `locations/`, `discovery/`, `wifi/`, `screenrecorder/`, `contactstore/`,
  `calendarstore/`, `jobs/`, `notifications/`, `push/`, `liveactivities/`, `httptransfers/`,
  `datasync/`, `configuration/`.
- Release notes: **one shared file**, `src/content/docs/client/release-notes.mdx`. Every package
  ships under the same version, so notes are grouped `## v<major>` → `### <version> - <date>` →
  `#### <Component>` (e.g. `#### BluetoothLE`, `#### Push Notifications`). Add your note under the
  right component heading in the current version, creating either heading if it isn't there yet.
- Menu (sidebar): `src/sidebar-topics.mjs` — the modules stay spread across their topics
  (Foundation, Hardware & Connectivity, Device Data, Background & Delivery, MAUI App, Data &
  Storage), and every module's **Release Notes** item links to the same `client/release-notes`
  page. Add/update the relevant node when you add a feature page.
- Moving or renaming a page needs a redirect in `astro.config.mjs`.

Docs folders outside `client/` (e.g. `foundation/`, `stores/`, `di/`, `mauihost/`, `permissions/`)
belong to other repos.

### Required updates for EVERY fix & feature

A change is not "done" until these are in sync:

1. **readme.md** (repo root) — reflect new/changed behavior.
2. **Skill** (`skills/<shiny-module>/`) — the agent-facing "how to generate correct code" doc for
   that module; update the trigger keyword list when a new public API is introduced.
3. **Docs site** — update the relevant module's feature page and add a **release note** under that
   module's component heading in `client/release-notes.mdx`.

### Release notes

Notes use the `<RN>` component (`import RN from '/src/components/ReleaseNote.astro'`), with
`type="feature|enhancement|fix|chore"`, an optional `breaking` flag, and an optional
`platform="iOS|Android|Windows|…"`. Group under a `## v<major>` heading; newest version section
stays at the top. Use a `### <version> - TBD` heading for unreleased work and promote it to a dated
heading when cutting the release.

**Never write a release note for a sample-app fix.** Release notes describe the *shipped library*.
Changes confined to `samples/` get no note — fix the sample, and only add a note if the same change
also altered library behavior or documented guidance (write the note about *that*, not the sample).

## Linux / D-Bus (Tmds.DBus.Protocol)

The Linux packages that reference `Tmds.DBus.Protocol` (`Shiny.BluetoothLE.Linux`,
`Shiny.BluetoothLE.Hosting.Linux`, `Shiny.Net.Wifi.Linux`, `Shiny.Notifications.Linux`,
`Shiny.ScreenRecorder.Linux`) talk to BlueZ, NetworkManager, the notification service and the
portals through it. Its low-level API fails in ways that
compile cleanly, pass on a dev machine, and only break against a real system bus. Three of these shipped
in `Shiny.BluetoothLE.Linux` 5.7.0 and made every scan fail on any machine where BlueZ already knew a
device. Hold these rules:

1. **`Reader` and `MessageWriter` are `ref struct`s — pass them by `ref`.** A helper declared
   `this Reader reader` advances a *copy*: the caller's position never moves, the next read takes a
   value as a property name, and parsing throws `DBusReadException: Invalid variant signature`.
   Helpers are `this ref Reader reader` (see `Bluez/DbusExtensions.cs`), and writer helpers take
   `ref MessageWriter` (as `Hosting.Linux`'s `GattObjects` already do).
2. **Skip a variant with `ReadVariantValue()` alone** — use the `SkipVariant()` extension. It reads
   the variant's own signature, so `ReadSignature()` before it reads the signature twice. The typed
   readers (`ReadStringVariant`, `ReadInt16Variant`, …) are the opposite: `ReadSignature()` then the
   raw value. Don't mix the two.
3. **In an `AddMatchAsync` / `Watch*Async` handler, check `IsCompletion` before anything else —
   never `n.Exception != null`.** On a value notification, reading `Exception` *throws*
   (`Check IsCompletion before accessing Exception`), and **a handler that throws disconnects the whole
   connection**. For BlueZ that silently ends discovery, notifications and every other call on that
   connection, a few milliseconds after it starts. Any parsing in a handler goes inside `try` and
   reports to the subscriber (`ob.OnError`) rather than throwing.

### Verifying a Linux change

Unit tests and a macOS build prove none of the above — the first hardware run does. Check against a
real bus (a Pi is the usual target):

- **Test the package, not a swapped DLL.** Dropping one rebuilt assembly into a published app fails
  with `BadImageFormatException` whenever the working tree differs from the published package's
  commit. Instead, clone the release commit into a scratch directory, apply only the fix, and pack it
  with `-p:PublicRelease=true` (Nerdbank ignores `-p:PackageVersion`) so it is exactly the released
  version. Then restore the consumer against a scratch feed, using `RestoreConfigFile` and
  `NUGET_PACKAGES` environment variables so no real `NuGet.config` or package cache changes.
- **A small file-based console app** (`#:package`, `#:property PublishAot=false`, `SelfContained`,
  `PublishSingleFile`, `dotnet publish -r linux-arm64`) copied to the device exercises the library
  with nothing else in the way.
- **Ask the connection why it died:** `DBusConnection.DisconnectedAsync()` completes with the reason
  — that is what named rule 3. `dbus-monitor --system` shows the calls and a client's `NameLost`, and
  `btmon` shows BlueZ's own management commands (such as a `Stop Discovery` nobody asked for).
- **Compare against the system tool** (`bluetoothctl scan on`, `nmcli`, `busctl`) to separate "the
  hardware can't" from "the library doesn't".

## Blog posts (only when explicitly requested)

Do **not** write blog posts automatically as part of a fix/feature. Write them **only when the user asks**. When asked to blog a feature, produce **two** posts — first the docs-site version, then adapt it for the personal blog.

### 1. Docs site — `~/Desktop/dev/documentation`

- File: `src/content/docs/blog/YYYY/MM/<slug>.mdx` (current year/month folders; create the month folder if needed).
- Frontmatter:
  ```yaml
  ---
  title: '...'
  description: '...'
  date: YYYY-MM-DD
  authors:
    - allanritchie
  tags:
    - Release        # or Feature, AI, etc.
  ---
  ```
- Body is MDX. Reuse components where relevant, e.g. `import NugetBadge from '/src/components/NugetBadge.astro';` then `<NugetBadge name="Shiny.Jobs" />` (use the module package you're writing about).
- Voice: product/release-note tone — what shipped, breaking changes, code samples, how to use it. **No hero image** on this site.

### 2. Personal blog — `~/Desktop/dev/blog` (adapt the docs post)

- File: `src/content/blog/YYYY/MM/<slug>.mdx` (note: `content/blog`, not `content/docs/blog`).
- Frontmatter (different schema — see `src/content.config.ts`):
  ```yaml
  ---
  title: '...'
  description: '...'
  pubDate: 'Mon DD YYYY'                          # e.g. 'Jun 15 2026'
  heroImage: '../../../../assets/<slug>-hero.svg'
  tags: ['Shiny', '.NET MAUI']
  ---
  ```
- Voice: rework the docs post into a personal, first-person narrative ("Here's something that shouldn't be hard but is…", "So I built…") — story/motivation up front, not a dry changelog.
- **Hero image is required.** Create `src/assets/<slug>-hero.svg`:
  - SVG, `viewBox="0 0 1200 630"`, `width="1200" height="630"`.
  - Match the house style: dark navy/indigo gradient background (`#0f172a` → `#1e1b4b`), cyan/green/violet accent gradients, subtle glow filters, the feature name as the headline. Crib an existing one (e.g. `datasync-hero.svg`, `documentdb-orleans-hero.svg`) as a starting template.
