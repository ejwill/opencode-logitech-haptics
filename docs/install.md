# Install Guide

For the complete source-to-device runbook, use [INSTALL_AND_TEST.md](INSTALL_AND_TEST.md). It covers building both npm packages, clean-install artifact testing, Logitech package verification, legacy and v2 configuration, and the distinction between bridge and physical-device proof.

This guide is for installing the packaged OpenCode + Logitech haptics bridge.

## Install artifacts

Use either local artifacts or GitHub Release assets:

- OpenCode plugin npm package: `opencode-companion-0.1.0.tgz`
- Logitech direct-install package: `OpenCodeCompanion_0_1_0.lplug4` (tar)
- Logitech marketplace package: `OpenCodeCompanion_0_1_0_marketplace.lplug4` (ZIP)

Local artifacts can be created with:

```bash
mkdir -p artifacts/npm artifacts/logitech
npm pack --workspace opencode-companion --pack-destination artifacts/npm
node scripts/package-logitech.mjs \
  logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ \
  artifacts/logitech \
  OpenCodeCompanion_0_1_0
```

## 1. Install the Logitech plugin

Prerequisites:

- Logi Options+ or Loupedeck installed as the host application.
- Logi Plugin Service installed and enabled by the host application.
- MX Master 4 / MX 4 paired with haptics enabled.

Verify and install the direct-install tar `OpenCodeCompanion_0_1_0.lplug4` with the Logi Plugin Tool. This avoids relying on the operating system's `.lplug4` file association:

```bash
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool verify /absolute/path/OpenCodeCompanion_0_1_0.lplug4
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool install /absolute/path/OpenCodeCompanion_0_1_0.lplug4
node scripts/verify-logitech-install.mjs OpenCodeCompanion
```

The tar artifact is the direct-install form for hosts that support tar `.lplug4` packages. On the current Logi Plugin Service build, `logiplugintool install` still rejects this form with a misleading metadata error; use the development link for local validation and submit the `_marketplace.lplug4` ZIP to Logitech.

The SDK's GUI path is also supported: double-click the package after installing a host application. If it reports that plugin installation cannot start, use the CLI commands above and restart the host application / Logi Plugin Service.

For a source checkout on macOS, use the SDK development-link workflow instead of installing a local package:

```bash
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:PluginApiDir="/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/"
```

The build creates `OpenCodeCompanion.link`, pointing Logi Plugin Service at the build output. Restart Logi Plugin Service or Logi Options+ if the plugin does not appear.

On macOS, the installed plugin directory is:

```text
~/Library/Application Support/Logi/LogiPluginService/Plugins/
```

After install, confirm the HTTP bridge is listening:

```bash
curl -i http://127.0.0.1:17844/haptic
```

Expected response:

```text
HTTP/1.1 405 Method Not Allowed
method not allowed
```

Then trigger a manual haptic:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"manual","event":"test","message":"Manual install test"}'
```

Expected response:

```text
HTTP/1.1 202 Accepted
accepted
```

Expected Logitech event: `opencodeTest`.

## 2. Install the legacy OpenCode plugin package

Install the local package tarball with npm:

```bash
npm install ./artifacts/npm/opencode-companion-0.1.0.tgz
```

If your OpenCode setup loads plugins from local files instead of installed npm packages, point it at the repository/plugin entrypoint according to your OpenCode config conventions:

```text
opencode-plugin/src/index.js
```

## 3. Configure OpenCode for live testing

Use the committed live-test config:

```bash
OPENCODE_LOGITECH_HAPTICS_CONFIG="$PWD/opencode-plugin/examples/live-test-config.json" opencode
```

Default endpoint:

```text
http://127.0.0.1:17844/haptic
```

Override the loopback endpoint if needed:

```bash
LOGITECH_HAPTICS_URL="http://localhost:17844/haptic" opencode
```

## 4. Configure the OpenCode v2 package

OpenCode v2 is a separate beta runtime and uses the independently packaged `opencode-companion-v2` adapter. Install the package from a registry when it is published, or reference the local package directory while developing:

Copy `opencode-plugin-v2/config.example.jsonc` to one of these locations:

```text
OPENCODE_LOGITECH_HAPTICS_CONFIG=/absolute/path/opencode-companion.jsonc
./opencode-companion.jsonc
~/.config/opencode/opencode-companion.jsonc
```

The adapter checks those locations in that order (the environment variable wins), and inline plugin options override file values. The example contains the live mappings verified against the current OpenCode v2 beta:

```jsonc
{
  "notifications": {
    "turnStarted": false,
    "completion": true,
    "question": true,
    "permission": false,
    "error": true,
    "planReady": true
  }
}
```

The file controls haptic enablement, notification filtering, bridge timing, and duplicate suppression. The adapter owns the current v2 event mapping. Keep `logEventTypes` false after discovery; use `advanced.eventTypes` only when adapting to a different runtime event contract.

The adapter also forwards the documented `execute.before` tool hooks when their public tool names are `question` or `plan_exit`. Permission is disabled in the v2 example until a live permission payload is confirmed; a runtime-specific mapping can be added under `advanced.eventTypes` once captured.

## 5. Validate end-to-end

1. Start / reload Logi Plugin Service.
2. Confirm `curl -i http://127.0.0.1:17844/haptic` returns `405 method not allowed`.
3. Confirm direct POST returns `202 accepted` and fires a haptic.
4. Start OpenCode with `OPENCODE_LOGITECH_HAPTICS_CONFIG` set.
5. Trigger an OpenCode session completion.
6. Confirm the MX Master 4 / MX 4 haptic fires.

For the full hardware checklist and troubleshooting, see [`hardware-validation.md`](hardware-validation.md).

## Uninstall / reset

- Remove the Logitech plugin through Logi Options+ / Logi Plugin Service.
- Remove the npm package from the OpenCode environment:

```bash
npm uninstall opencode-companion
```

- Remove the v2 package from its OpenCode v2 configuration or use `opencode2 plugin remove opencode-companion-v2` after registry installation.

- Restart OpenCode and Logi Plugin Service.
