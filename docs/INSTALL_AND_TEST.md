# Install and Test Guide

This guide covers the complete local path: build the two OpenCode packages and the Logitech bridge, install them, then distinguish automated bridge proof from real-device haptic proof.

## What you install

| Artifact | Purpose | Installed by |
| --- | --- | --- |
| `opencode-logitech-haptics-<version>.tgz` | Legacy OpenCode adapter | Your legacy OpenCode plugin environment |
| `opencode-logitech-haptics-v2-<version>.tgz` | OpenCode v2 beta adapter | OpenCode v2 package/configuration |
| `OpenCodeCompanion_<version>.lplug4` | Local Logitech HTTP bridge, haptic mappings, and OpenCode companion actions | Logi Options+ / Logi Plugin Service |

Both adapters POST only to the local loopback bridge at `http://127.0.0.1:17844/haptic` by default. They do not send haptic events to remote endpoints.

## Prerequisites

- Node.js 20 or newer and npm.
- .NET SDK 10 for the Logitech package and the current Logi Plugin Service API.
- Legacy OpenCode (`opencode`) for the legacy adapter and/or OpenCode v2 (`opencode2`) for the v2 adapter.
- For real haptics: Logi Options+, the Logi Plugin Service, and a supported MX Master 4 / MX 4 with haptics enabled.

## 1. Verify the source checkout

From the repository root:

```bash
npm install
npm test
```

`npm test` runs the legacy adapter, shared core, and v2 adapter suites.

Build and smoke-test the Logitech bridge:

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet build tests/PluginApiStubs/PluginApiStubs.csproj -c Release
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:SkipLogiDeploy=true \
  /p:PluginApiDir="$PWD/tests/PluginApiStubs/bin/Release/net10.0/"
PLUGIN_DLL_PATH="$PWD/logitech-plugin/OpenCodeHapticsPlugin/bin/Release/bin/OpenCodeCompanionPlugin.dll" \
  dotnet run --project tests/ServerSmokeTest/ServerSmokeTest.csproj -c Release
```

The smoke test proves the localhost HTTP contract, including valid events, malformed requests, body limits, concurrency, and listener lifecycle. It does not prove that a physical mouse produced haptics.

## 2. Build installable artifacts

Create a clean artifact directory and pack both npm packages:

```bash
mkdir -p artifacts/npm artifacts/logitech
npm pack --workspace opencode-logitech-haptics --pack-destination artifacts/npm
npm pack --workspace opencode-logitech-haptics-v2 --pack-destination artifacts/npm
```

Package and verify the Logitech plugin:

```bash
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool pack \
  logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ \
  artifacts/logitech/OpenCodeCompanion_0_1_0.lplug4
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool verify \
  artifacts/logitech/OpenCodeCompanion_0_1_0.lplug4
```

Use the actual package version in the output filename when it differs from `0.1.0`.

## 3. Test the packed npm artifacts

This step installs the just-packed tarballs in a clean temporary npm prefix and exercises their public imports and a legacy bridge send:

```bash
node scripts/verify-packed-adapters.mjs artifacts/npm
```

Expected output:

```text
packed-adapters=ok
```

## 4. Install the Logitech bridge

Install the verified `.lplug4` with the Logi Plugin Tool:

```bash
DOTNET_ROLL_FORWARD=Major dotnet tool run logiplugintool install \
  /absolute/path/to/OpenCodeCompanion_0_1_0.lplug4
node scripts/verify-logitech-install.mjs OpenCodeCompanion
```

The `.lplug4` double-click flow is a convenience path and depends on a host application such as Logi Options+ or Loupedeck being installed and the Logi Plugin Service package installer being registered. If the GUI reports `plugin installation cannot start`, use the CLI command above.

For local source development, build against the installed host API instead:

```bash
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:PluginApiDir="/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/"
```

This writes a `.link` file and reloads the plugin without using the distributable-package installer.

On macOS, the Plugin Service extracts installed packages into:

```text
~/Library/Application Support/Logi/LogiPluginService/Plugins/
```

After the plugin is loaded, verify the bridge is reachable:

```bash
curl -i http://127.0.0.1:17844/haptic
```

Expected response:

```text
HTTP/1.1 405 Method Not Allowed
method not allowed
```

Then send a direct test event:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"manual","event":"test","message":"Manual installation test"}'
```

Expected response:

```text
HTTP/1.1 202 Accepted
accepted
```

The HTTP result proves the bridge accepted the request. Confirm the physical haptic separately.

## 5. Install and test the legacy OpenCode adapter

Install the legacy package into the Node environment used by your OpenCode plugin setup:

```bash
npm install ./artifacts/npm/opencode-logitech-haptics-0.1.0.tgz
```

For local repository development, configure OpenCode to load the legacy entrypoint according to its plugin configuration conventions:

```text
opencode-plugin/src/index.js
```

Use the supplied live-test configuration:

```bash
OPENCODE_LOGITECH_HAPTICS_CONFIG="$PWD/opencode-plugin/examples/live-test-config.json" opencode
```

Configuration can also be discovered from `opencode-logitech-haptics.jsonc` or `opencode-logitech-haptics.json` in the current directory. `LOGITECH_HAPTICS_URL` can select another loopback host such as `http://localhost:17844/haptic`; non-loopback endpoints are rejected.

Trigger a session completion, permission prompt, error, question, or plan exit. See the legacy event table in [opencode-plugin/README.md](../opencode-plugin/README.md).

## 6. Install and test the OpenCode v2 adapter

The v2 adapter is independent of the legacy package. Install it from a registry when published, or point OpenCode v2 at the local package while developing:

```jsonc
// opencode.jsonc
{
  "plugins": [{
    "package": "./opencode-plugin-v2",
    "options": {
      "endpoint": "http://127.0.0.1:17844/haptic",
      "eventTypes": {
        // Replace with event names captured from your pinned v2 runtime.
        "your.observed.event": "complete"
      }
    }
  }]
}
```

For a registry package, use the package name instead:

```jsonc
"package": "opencode-logitech-haptics-v2@0.1.0"
```

The v2 plugin is pinned to the documented beta API version in its `package.json`. Its event stream is intentionally opt-in through `eventTypes`; do not copy legacy event names unless you captured them from your matching v2 runtime. The documented `question` and `plan_exit` tool-hook names are forwarded when emitted.

## 7. Record real-device results

Use [hardware-validation.md](hardware-validation.md) to record the OS, Logi Options+ version, device, package checksum, bridge results, and each OpenCode flow. Keep these distinctions explicit:

- **Automated proof:** npm tests, clean package-install check, C# smoke test, and `.lplug4` verification.
- **Bridge proof:** `202 Accepted` from the loaded Logitech bridge.
- **Hardware proof:** a supported physical mouse produced the expected haptic.

## Uninstall

1. Remove the Logitech plugin through Logi Options+ / Logi Plugin Service.
2. Remove the legacy package from its Node/OpenCode environment:

   ```bash
   npm uninstall opencode-logitech-haptics
   ```

3. Remove the v2 plugin entry from `opencode.json(c)`, or after registry installation run:

   ```bash
   opencode2 plugin remove opencode-logitech-haptics-v2
   ```

4. Restart OpenCode and Logi Plugin Service.
