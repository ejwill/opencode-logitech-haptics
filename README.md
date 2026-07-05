# OpenCode Logitech Haptics

OpenCode Logitech Haptics connects OpenCode notification events to Logitech MX Master 4 / MX 4 haptic feedback.

This repository is a monorepo with two pieces:

- **OpenCode plugin**: listens for OpenCode events and sends normalized events to localhost.
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
logitech-plugin/OpenCodeHapticsPlugin/ # C# Logi Actions SDK plugin
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

## Local verification

Run the OpenCode plugin tests:

```bash
npm test
```

Build, smoke test, package, and verify the Logitech plugin:

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet build tests/PluginApiStubs/PluginApiStubs.csproj -c Release
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:SkipLogiDeploy=true \
  /p:PluginApiDir="$PWD/tests/PluginApiStubs/bin/Release/net8.0/"
PLUGIN_DLL_PATH="$PWD/logitech-plugin/OpenCodeHapticsPlugin/bin/Release/bin/OpenCodeHapticsPlugin.dll" \
  dotnet run --project tests/ServerSmokeTest/ServerSmokeTest.csproj -c Release
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool pack \
  logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ \
  OpenCodeHaptics_0_1.lplug4
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool verify OpenCodeHaptics_0_1.lplug4
```

`DOTNET_ROLL_FORWARD=Major` is only needed on machines with a newer runtime but no .NET 8 runtime. GitHub Actions installs .NET 8 and does not need it.

## Manual bridge test

For install steps, see [`docs/install.md`](docs/install.md).

After installing/loading the Logitech plugin, send a test event:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"opencode","event":"test","message":"Manual test"}'
```

Expected response: `HTTP/1.1 202 Accepted`.

For the full real-device checklist, see [`docs/hardware-validation.md`](docs/hardware-validation.md).

For OpenCode live-event testing, use [`opencode-plugin/examples/live-test-config.json`](opencode-plugin/examples/live-test-config.json).

## Releases

CI creates release artifacts on every run and creates a GitHub Release for tags matching `v*`.

To cut a release:

```bash
git tag v0.1.0
git push origin v0.1.0
```

The release workflow attaches:

- the OpenCode npm package (`*.tgz`)
- the Logitech plugin package (`OpenCodeHaptics_<version>.lplug4`)

Branch/PR CI artifacts include the ref type, sanitized ref, and short SHA, for example `OpenCodeHaptics_branch_main_1234567.lplug4`. Tagged releases keep version-only names, for example `OpenCodeHaptics_0_1_0.lplug4`.

Before tagging, follow [`docs/release-checklist.md`](docs/release-checklist.md).

## Troubleshooting

### `curl` cannot connect to `127.0.0.1:17844`

The Logitech plugin is not listening yet. Confirm Logi Options+ / Logi Plugin Service is running and the plugin loaded successfully.

### `202 Accepted` but no haptic

The HTTP bridge accepted the event, but the Logitech haptic mapping or device setup is not firing. Check that MX Master 4 / MX 4 haptics are enabled and that the packaged YAML mappings installed correctly.

### OpenCode does not trigger haptics, but curl works

Check `OPENCODE_LOGITECH_HAPTICS_CONFIG`, verify OpenCode loaded the plugin, and temporarily set `suppressDuplicatesMs` to `0` in the live-test config.

## Build phases

1. Build a local-file OpenCode plugin. ✅
2. Generate the Logitech plugin scaffold. ✅
3. Add a localhost HTTP listener to the Logitech plugin. ✅
4. Register Logitech haptic events and add YAML waveform mappings. ✅
5. Test with `curl`, then with real OpenCode events. CI curl smoke test ✅; real hardware validation pending.
6. Package the Logitech plugin as `.lplug4`; optionally package the OpenCode plugin on npm. ✅

See [`docs/AI_IMPLEMENTATION_BRIEF.md`](docs/AI_IMPLEMENTATION_BRIEF.md) for the complete implementation brief.
