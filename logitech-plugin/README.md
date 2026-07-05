# Logitech Plugin

C# Logi Actions SDK plugin for MX Master 4 / MX 4 haptic notifications from OpenCode.

## Generate project

```bash
dotnet tool install --global LogiPluginTool
logiplugintool generate OpenCodeHaptics
cd OpenCodeHaptics
dotnet build
```

Then add:

- local HTTP listener bound to `127.0.0.1:17844`
- haptic event registration with `PluginEvents.AddEvent(...)`
- haptic triggers with `PluginEvents.RaiseEvent(...)`
- `HasHapticMapping` in `metadata/LoupedeckPackage.yaml`
- `events/DefaultEventSource.yaml`
- `events/extra/eventMapping.yaml`
- optional `Test Haptic` command

The Logitech layer controls **how notifications feel**.
