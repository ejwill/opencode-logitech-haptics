# OpenCode Logi Companion

OpenCode Logi Companion is an independent Logitech integration for OpenCode. It connects OpenCode notification events to MX Master 4 / MX 4 haptic feedback and provides a foundation for optional Actions Ring controls. Inside Logi Options+ it appears as **OpenCode Companion**.

OpenCode Logi Companion is not affiliated with or endorsed by the OpenCode or Logitech projects.

The plugin icon is based on the OpenCode square brand asset. OpenCode and its logo are trademarks of their respective owner; this project uses the mark to identify compatibility and is independently maintained.

This repository is a monorepo with two pieces:

- **OpenCode plugin**: listens for OpenCode events and sends normalized events to localhost.
- **Shared notification core**: validates adapter configuration and owns the loopback-only bridge transport.
- **Logitech plugin**: a C# Logi Actions SDK plugin that receives those events and raises MX Master 4 haptic events.

## Architecture

```text
OpenCode plugin
  session.idle / permission / error / question / plan_exit
  ↓ POST http://127.0.0.1:17844/haptic
Logitech C# Actions SDK plugin
  local HTTP listener
  ↓ PluginEvents.RaiseEvent("opencodeComplete")
Logitech haptics mapping
  events/extra/eventMapping.yaml
  ↓
MX Master 4 / MX 4 haptic feedback
```

## Repository layout

```text
opencode-plugin/                    # OpenCode JavaScript plugin
opencode-plugin-v2/                 # Separate OpenCode v2 beta package
packages/haptics-core/               # Runtime-neutral configuration and bridge transport
logitech-plugin/OpenCodeHapticsPlugin/ # C# Logi Actions SDK implementation for OpenCode Companion
tests/PluginApiStubs/               # source-built CI stub for PluginApi.dll
tests/ServerSmokeTest/              # curl-based HTTP bridge smoke test
docs/                               # implementation brief and design notes
```

## Default event mapping

| OpenCode event | Logitech haptic event | Default waveform |
| --- | --- | --- |
| `complete` | `opencodeComplete` | `completed` |
| `permission` | `opencodePermission` | `knock` |
| `error` | `opencodeError` | `angry_alert` |
| `question` | `opencodeQuestion` | `ringing` |
| `plan_exit` | `opencodePlanExit` | `happy_alert` |
| `test` | `opencodeTest` | `sharp_state_change` |

## User setup

See [`docs/USER_GUIDE.md`](docs/USER_GUIDE.md) for the recommended Logi Options+ folder layout, event actions, icons, waveform and intensity configuration, precedence rules, and bridge testing steps.

The initial OpenCode control set is documented there as well: switch agent, switch thinking level, stop the current turn, open the session selector, and open the model selector. These controls are kept separate from the haptic notification adapter; launch/focus remains a planned platform-specific action.

## Local verification

Run the OpenCode plugin tests:

```bash
npm test
```

This runs the legacy adapter, the shared core, and the OpenCode v2 adapter test suites.

Build, smoke test, package, and verify the Logitech plugin:

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet build tests/PluginApiStubs/PluginApiStubs.csproj -c Release
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:SkipLogiDeploy=true \
  /p:PluginApiDir="$PWD/tests/PluginApiStubs/bin/Release/net10.0/"
PLUGIN_DLL_PATH="$PWD/logitech-plugin/OpenCodeHapticsPlugin/bin/Release/bin/OpenCodeCompanionPlugin.dll" \
PLUGIN_API_PATH="$PWD/tests/PluginApiStubs/bin/Release/net10.0/PluginApi.dll" \
  dotnet run --project tests/ServerSmokeTest/ServerSmokeTest.csproj -c Release
node scripts/package-logitech.mjs \
  logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ \
  artifacts/logitech \
  OpenCodeCompanion_0_1_0
node scripts/verify-logitech-package.mjs artifacts/logitech/OpenCodeCompanion_0_1_0_marketplace.lplug4
node scripts/verify-logitech-package.mjs artifacts/logitech/OpenCodeCompanion_0_1_0.lplug4
```

### Configure haptics in Logi Options+

The Logitech plugin includes notification toggle actions under **OpenCode Companion → Haptic Preferences**. Assign the toggles you want to device buttons and press one to enable or disable that notification’s vibration. The choices persist in Logi Plugin Service and are applied to bridge events immediately.

The **Test Haptic Feedback** action remains under **OpenCode Companion → Diagnostics**. Use it to confirm the device and haptic path before tuning notification preferences.

Waveforms can be requested in the OpenCode `waveforms` configuration object. Set `intensity` to `subtle`, `normal`, or `strong` for a curated profile, then use `waveforms` for exact per-event choices. The Logitech package keeps `events/extra/eventMapping.yaml` as the device-safe fallback and exposes the same choices through the **Haptic Preferences** Action Editor when supported by the installed Logi Plugin Service.

Configuration precedence is: a saved Logitech per-event waveform override, a saved Logitech intensity profile, an OpenCode per-event `waveforms` override, an OpenCode `intensity` profile, then the packaged Logitech default. This keeps the OpenCode JSONC file easy to edit or have an assistant update while still allowing device-local preferences in Logi Options+. A separate companion web app is not required for the current configuration model.

`DOTNET_ROLL_FORWARD=Major` is only needed on machines with a newer runtime but no .NET 10 runtime. GitHub Actions installs .NET 10 and does not need it.

For local development against an installed Logi Plugin Service, build without `SkipLogiDeploy` and point at the host's API assembly:

```bash
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:PluginApiDir="/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/"
```

This creates a `.link` file in the Logi Plugin Service `Plugins` directory and asks the service to reload the plugin. It is the preferred local-development path; `SkipLogiDeploy=true` is for CI and package-only validation.

To install a verified package without relying on the `.lplug4` file association, use the Logi Plugin Tool directly:

```bash
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool install /absolute/path/OpenCodeCompanion_0_1_0.lplug4
```

The SDK installs the package into the Logi Plugin Service `Plugins` directory. A double-click install requires a host application such as Logi Options+ or Loupedeck and a registered Logi Plugin Service package installer.

## Manual bridge test

For source-to-device install and test steps, see [`docs/INSTALL_AND_TEST.md`](docs/INSTALL_AND_TEST.md).

On an OpenCode v2 setup, the v2 adapter registers a `/haptic-test` command that sends a diagnostic event through the configured bridge endpoint. After installing/loading the Logitech plugin, you can also send a test event directly:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"opencode","event":"test","message":"Manual test"}'
```

Expected response: `HTTP/1.1 202 Accepted`.

For the full real-device checklist, see [`docs/hardware-validation.md`](docs/hardware-validation.md).

For OpenCode live-event testing, use [`opencode-plugin/examples/live-test-config.json`](opencode-plugin/examples/live-test-config.json).

## Releases

CI creates release artifacts on every run and creates a GitHub Release for tags matching `v*`. Tagged releases also publish the two OpenCode npm packages through the protected `npm-publish` job after package verification succeeds.

To cut a release:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The release workflow attaches:

- both OpenCode npm packages (`*.tgz`)
- the Logitech plugin package (`OpenCodeCompanion_<version>.lplug4`)

Branch/PR CI artifacts include the ref type, sanitized ref, and short SHA, for example `OpenCodeCompanion_branch_main_1234567.lplug4`. Tagged releases keep version-only names, for example `OpenCodeCompanion_0_1_0.lplug4`.

Before tagging, follow [`docs/release-checklist.md`](docs/release-checklist.md).

## Troubleshooting

### `curl` cannot connect to `127.0.0.1:17844`

The Logitech plugin is not listening yet. Confirm Logi Options+ / Logi Plugin Service is running and the plugin loaded successfully.

### `202 Accepted` but no haptic

The HTTP bridge accepted the event, but the Logitech haptic mapping or device setup is not firing. Check that MX Master 4 / MX 4 haptics are enabled and that the packaged YAML mappings installed correctly.

### OpenCode does not trigger haptics, but curl works

Check `OPENCODE_LOGI_COMPANION_CONFIG`, verify OpenCode loaded the plugin, and temporarily set `suppressDuplicatesMs` to `0` in the live-test config.

## Build phases

1. Build a local-file OpenCode plugin. ✅
2. Generate the Logitech plugin scaffold. ✅
3. Add a localhost HTTP listener to the Logitech plugin. ✅
4. Register Logitech haptic events and add YAML waveform mappings. ✅
5. Test with `curl`, then with real OpenCode events. CI curl smoke test ✅; MX 4 hardware validation ✅ through the local `.link` workflow.
6. Package the Logitech plugin as `.lplug4`; optionally package the OpenCode plugin on npm. ✅

See [`docs/AI_IMPLEMENTATION_BRIEF.md`](docs/AI_IMPLEMENTATION_BRIEF.md) for the complete implementation brief.
