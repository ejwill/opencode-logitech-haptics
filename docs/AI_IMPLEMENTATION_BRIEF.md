# AI Implementation Brief: OpenCode Logitech Haptics

## Goal

Build a two-part integration that makes a Logitech MX Master 4, also referred to as MX 4, provide haptic feedback when OpenCode has important notification events.

The project must not depend on third-party OpenCode notification plugins. It should include its own OpenCode plugin and its own Logitech Actions SDK C# plugin.

## Repository

Use one monorepo:

```text
opencode-logitech-haptics
```

## Architecture

```text
OpenCode plugin
  configurable event filtering
  ↓ HTTP POST
Logitech C# Actions SDK plugin
  normalized event → Logitech haptic event
  ↓ PluginEvents.RaiseEvent(...)
Logitech haptics YAML
  Logitech haptic event → MX Master 4 waveform
  ↓
MX Master 4 / MX 4 haptic feedback
```

## OpenCode plugin requirements

The OpenCode plugin listens for OpenCode events and POSTs normalized events to the local Logitech plugin.

Do not depend on:

- `opencode-notifier`
- `opencode-notificator`
- OS desktop notification systems
- AppleScript / `osascript`, except as a debug example

### Events

Normalize OpenCode events into:

- `complete`
- `permission`
- `error`
- `question`
- `plan_exit`
- `test`

### HTTP payload

```json
{
  "source": "opencode",
  "event": "complete",
  "message": "Session completed",
  "directory": "/path/to/project",
  "worktree": "/path/to/worktree",
  "time": "2026-06-17T00:00:00.000Z"
}
```

Endpoint:

```http
POST http://127.0.0.1:17844/haptic
Content-Type: application/json
```

### Config

OpenCode config controls when to notify:

```jsonc
{
  "enabled": true,
  "endpoint": "http://127.0.0.1:17844/haptic",
  "events": {
    "complete": true,
    "permission": true,
    "error": true,
    "question": true,
    "plan_exit": true,
    "session_started": false,
    "user_message": false,
    "subagent_complete": false
  },
  "minDurationSeconds": 0,
  "suppressDuplicatesMs": 750
}
```

## Logitech C# Actions SDK plugin requirements

Create the project with:

```bash
dotnet tool install --global LogiPluginTool
logiplugintool generate OpenCodeCompanion
cd OpenCodeCompanion
dotnet build
```

The plugin must include:

- `OpenCodeCompanionPlugin : Plugin`
- `OpenCodeHapticsApplication : ClientApplication`
- haptic event registration with `this.PluginEvents.AddEvent(...)`
- haptic event triggering with `this.PluginEvents.RaiseEvent(...)`
- `HasHapticMapping` in `metadata/LoupedeckPackage.yaml`
- `events/DefaultEventSource.yaml`
- `events/extra/eventMapping.yaml`

### Logitech event names

```text
opencodeComplete
opencodePermission
opencodeError
opencodeQuestion
opencodePlanExit
opencodeTest
```

### Haptic capability

`metadata/LoupedeckPackage.yaml` must include:

```yaml
pluginCapabilities:
  - HasHapticMapping
```

### Haptic waveform mapping

```yaml
haptics:
  opencodeComplete:
    DEFAULT: completed
    MX Master 4: completed
  opencodePermission:
    DEFAULT: knock
    MX Master 4: knock
  opencodeError:
    DEFAULT: angry_alert
    MX Master 4: angry_alert
  opencodeQuestion:
    DEFAULT: ringing
    MX Master 4: ringing
  opencodePlanExit:
    DEFAULT: happy_alert
    MX Master 4: happy_alert
  opencodeTest:
    DEFAULT: sharp_state_change
    MX Master 4: sharp_state_change
```

Known waveform names include `sharp_state_change`, `damp_state_change`, `sharp_collision`, `damp_collision`, `subtle_collision`, `happy_alert`, `angry_alert`, `completed`, `square`, `wave`, `firework`, `mad`, `knock`, `jingle`, and `ringing`.

## Local HTTP server requirements

- Bind only to `127.0.0.1`.
- Default port: `17844`.
- Accept only `POST /haptic`.
- Reject malformed JSON with `400`.
- Return `404` for unknown paths.
- Ignore unknown event names safely.
- Never execute shell commands from request input.
- Stop cleanly in `Plugin.Unload()`.
- Avoid blocking the Logi Plugin Service thread.

## Manual tests

```bash
curl -fsS -X POST http://127.0.0.1:17844/haptic   -H "Content-Type: application/json"   --data '{"source":"test","event":"test","message":"manual test"}'
```

Expected: HTTP `200 OK` and haptic feedback on MX Master 4 / MX 4.

## Packaging

Package Logitech plugin as `.lplug4`:

```bash
dotnet build -c Release
logiplugintool pack ./bin/Release/ ./OpenCodeCompanion_0_1.lplug4
logiplugintool verify ./OpenCodeCompanion_0_1.lplug4
```

## Acceptance criteria

- Repo contains both projects in a monorepo.
- OpenCode plugin does not depend on third-party notification plugins.
- Logitech plugin uses official C# Logi Actions SDK haptics.
- MX Master 4 / MX 4 haptics trigger for session complete, permission request, error, question, and plan ready.
- Event filtering is configurable in OpenCode layer.
- Haptic mapping is configurable in Logitech/YAML layer.
- Logitech plugin can be packaged and verified as `.lplug4`.
