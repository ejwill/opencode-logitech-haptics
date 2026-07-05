# OpenCode Haptics Logitech Plugin

C# Logitech Actions SDK plugin for MX Master 4 / MX 4 haptics.

## Local development

Requires Logi Options+ / Logi Plugin Service and Logitech `PluginApi.dll` installed on the target machine.

```bash
cd logitech-plugin/OpenCodeHapticsPlugin/src
dotnet build
```

## Test the HTTP bridge

```bash
curl -i -X POST http://127.0.0.1:17844/haptic   -H "Content-Type: application/json"   --data '{"source":"opencode","event":"test","message":"Manual test"}'
```

Expected: `HTTP/1.1 202 Accepted`.
