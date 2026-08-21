# OpenCode Companion Logitech Plugin

This is an independent community integration and is not affiliated with or endorsed by the OpenCode project.

C# Logitech Actions SDK plugin for MX Master 4 / MX 4 haptics.

## Local development

On a real Logitech development machine, this project expects Logi Options+ / Logi Plugin Service and Logitech `PluginApi.dll` to be installed in the default SDK location.

For CI and local non-Logitech machines, build against the source stub:

```bash
dotnet build tests/PluginApiStubs/PluginApiStubs.csproj -c Release
dotnet build logitech-plugin/OpenCodeHapticsPlugin/OpenCodeHapticsPlugin.sln \
  -c Release \
  /p:SkipLogiDeploy=true \
  /p:PluginApiDir="$PWD/tests/PluginApiStubs/bin/Release/net10.0/"
```

`/p:SkipLogiDeploy=true` skips writing the local Logi Plugin Service `.link` file and reloading the plugin, which is required on CI and useful on machines without Logi Options+.

## Test the HTTP bridge

If the plugin is running in Logi Plugin Service:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"opencode","event":"test","message":"Manual test"}'
```

Expected: `HTTP/1.1 202 Accepted`.

For the full real-device checklist, see [`../../docs/hardware-validation.md`](../../docs/hardware-validation.md).

CI also runs a smoke test directly against `OpenCodeCompanionServer`:

```bash
PLUGIN_DLL_PATH="$PWD/logitech-plugin/OpenCodeHapticsPlugin/bin/Release/bin/OpenCodeCompanionPlugin.dll" \
  dotnet run --project tests/ServerSmokeTest/ServerSmokeTest.csproj -c Release
```

Expected smoke output includes:

```text
complete=202:accepted
missing=400:missing event
invalid=400:invalid json
unknown=204:
get=404:not found
raised=opencodeComplete
```

## Package

```bash
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run logiplugintool pack logitech-plugin/OpenCodeHapticsPlugin/bin/Release/ OpenCodeCompanion_0_1.lplug4
dotnet tool run logiplugintool verify OpenCodeCompanion_0_1.lplug4
```

On machines with only newer .NET runtimes installed, prefix the `logiplugintool` commands with `DOTNET_ROLL_FORWARD=Major`.
