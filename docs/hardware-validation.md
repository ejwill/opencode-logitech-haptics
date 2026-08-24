# Hardware Validation Checklist

Use this checklist when you are ready to validate the bridge on a real Logitech setup.

## Prerequisites

- Logi Options+ or Loupedeck installed and running.
- Logi Plugin Service installed, enabled, and running.
- MX Master 4 / MX 4 paired and haptics enabled.
- A verified `.lplug4` package from either:
  - local build: `OpenCodeCompanion_*.lplug4`
  - GitHub Actions artifact: `logitech-plugin-package`
  - GitHub Release asset
- OpenCode available for live event testing.

## 1. Install the Logitech plugin

Build both package forms below. The marketplace artifact is a ZIP, matching current published Logitech packages. The unsuffixed artifact is also emitted as a clean POSIX USTAR tar for hosts that require that format; the current `logiplugintool install` command on this machine still rejects the tar with a misleading metadata error, so package-install acceptance remains a host-version gate:

If you are using a locally built package, build and verify it first:

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet build tests/PluginApiStubs/PluginApiStubs.csproj -c Release
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:SkipLogiDeploy=true \
  /p:PluginApiDir="$PWD/tests/PluginApiStubs/bin/Release/net10.0/"
node scripts/package-logitech.mjs \
  logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ \
  artifacts/logitech \
  OpenCodeCompanion_0_1_0
node scripts/verify-logitech-package.mjs artifacts/logitech/OpenCodeCompanion_0_1_0_marketplace.lplug4
node scripts/verify-logitech-package.mjs artifacts/logitech/OpenCodeCompanion_0_1_0.lplug4
```

For local source validation on macOS, build against the installed Plugin Service API instead:

```bash
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:PluginApiDir="/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/"
```

This creates a development `.link` and reloads the plugin. It avoids the distributable `.lplug4` installer while validating that the current host can load the plugin.

`DOTNET_ROLL_FORWARD=Major` is only needed on machines that have a newer .NET runtime but not the .NET 10 runtime.

## 2. Verify the plugin loaded

Expected signs:

- The plugin appears in Logi Options+ / Logi Plugin Service.
- The plugin starts without errors.
- Local port `17844` accepts HTTP requests after the plugin loads.

Quick port probe:

```bash
curl -i http://127.0.0.1:17844/haptic
```

Expected response:

```text
HTTP/1.1 405 Method Not Allowed
method not allowed
```

A connection failure means the Logitech plugin is not listening yet.

## 3. Run direct haptic curl tests

### Test event

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"manual","event":"test","message":"Manual haptic test"}'
```

Expected:

```text
HTTP/1.1 202 Accepted
accepted
```

Expected haptic event: `test` at the server boundary, mapped to the Logitech `opencodeTest` action.

### Complete event

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"manual","event":"complete","message":"Manual completion test"}'
```

Expected:

```text
HTTP/1.1 202 Accepted
accepted
```

Expected haptic event: `complete` at the server boundary, mapped to the Logitech `opencodeComplete` action.

### Error handling checks

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{}'
```

Expected: `400 Bad Request` with body `missing event`.

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data 'nope'
```

Expected: `400 Bad Request` with body `invalid json`.

## 4. Run OpenCode live event tests

Use the committed live-test config:

```bash
OPENCODE_LOGITECH_HAPTICS_CONFIG="$PWD/opencode-plugin/examples/live-test-config.json" opencode
```

Trigger these OpenCode flows:

| Flow | Expected bridge event | Expected Logitech event |
| --- | --- | --- |
| Session completes / becomes idle | `complete` | `opencodeComplete` |
| Permission prompt appears | `permission` | `opencodePermission` |
| OpenCode session error or controlled failure | `error` | `opencodeError` |
| Tool asks a question | `question` | `opencodeQuestion` |
| Plan exit event | `plan_exit` | `opencodePlanExit` |

If an event is hard to trigger naturally, use the direct curl test for the matching event first, then revisit the OpenCode flow later.

## 5. Expected haptic mapping

| Bridge event | Logitech event | Default waveform |
| --- | --- | --- |
| `complete` | `opencodeComplete` | `completed` |
| `permission` | `opencodePermission` | `knock` |
| `error` | `opencodeError` | `angry_alert` |
| `question` | `opencodeQuestion` | `ringing` |
| `plan_exit` | `opencodePlanExit` | `happy_alert` |
| `test` | `opencodeTest` | `sharp_state_change` |

Mappings live in:

- `logitech-plugin/OpenCodeHapticsPlugin/src/package/events/DefaultEventSource.yaml`
- `logitech-plugin/OpenCodeHapticsPlugin/src/package/events/extra/eventMapping.yaml`

## 6. Record validation results

Validation recorded on 2026-08-21: the `.link` development build loaded through Logi Plugin Service 6.4.1.3246, the live endpoint returned `202` for `test`, `complete`, `error`, `permission`, `question`, and `plan_exit`, and the connected MX 4 produced a physical vibration. The package forms now verify structurally; clean marketplace installation remains a separate host acceptance gate.

After testing, record:

- Date/time:
- OS:
- Logi Options+ version:
- Device model:
- Package file:
- Package SHA256:
- Direct curl `test` haptic result:
- Direct curl `complete` haptic result:
- OpenCode `complete` event result:
- OpenCode `permission` event result:
- OpenCode `error` event result:
- Notes / failures:

## Troubleshooting

### `curl: Failed to connect`

Likely causes:

- Logi Plugin Service is not running.
- The plugin did not load.
- Another process is using port `17844`.
- The plugin crashed during startup.

Checks:

```bash
curl -i http://127.0.0.1:17844/haptic
```

Restart Logi Options+ / Logi Plugin Service, then retry.

### HTTP `202 accepted` but no haptic

Likely causes:

- Plugin is receiving events but haptic mapping is not active.
- Device does not support the selected waveform.
- MX Master 4 / MX 4 haptics are disabled.
- Event mapping YAML did not install correctly.

Check the mapping files listed above and verify the plugin package contains them.

### OpenCode does not trigger haptics, but curl works

Likely causes:

- `OPENCODE_LOGITECH_HAPTICS_CONFIG` points to the wrong file.
- OpenCode did not load the local plugin.
- The specific event is disabled in config.
- Duplicate suppression filtered repeat events.

Try lowering or disabling duplicate suppression in the live-test config:

```json
"suppressDuplicatesMs": 0
```

### Package install fails

Run package verification:

```bash
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool verify OpenCodeCompanion_0_1_0.lplug4
```

Then inspect the package metadata and install it without the GUI association:

```bash
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool metadata OpenCodeCompanion_0_1_0.lplug4
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool install OpenCodeCompanion_0_1_0.lplug4
```

If verification fails, rebuild from a clean tree and retry. If verification and CLI installation succeed but the plugin does not load, the remaining failure is in the host/Plugin Service runtime or plugin startup; record that separately from package-format validation.

If the package still reports `plugin installation cannot start` but the development-link build loads the plugin, treat the package installer and the plugin runtime as separate gates. Keep using the `.link` workflow for local development while the Logi Plugin Tool/LPS package-install compatibility is resolved.

If installation prints:

```text
ERROR: Cannot connect to Logi Plugin Service. Check that it is running.
```

install or repair Logi Options+ or Loupedeck, start the host application, confirm that Logi Plugin Service is enabled, and retry. Starting the `.lplug4` installer without a reachable Plugin Service cannot complete package installation.
