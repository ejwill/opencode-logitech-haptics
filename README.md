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
opencode-plugin/       # OpenCode JS/TS plugin
logitech-plugin/       # C# Logi Actions SDK plugin skeleton/notes
docs/                  # implementation brief and design notes
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

## Build phases

1. Build a local-file OpenCode plugin.
2. Generate the Logitech plugin with `logiplugintool generate OpenCodeHaptics`.
3. Add a localhost HTTP listener to the Logitech plugin.
4. Register Logitech haptic events and add YAML waveform mappings.
5. Test with `curl`, then with real OpenCode events.
6. Package the Logitech plugin as `.lplug4`; optionally package the OpenCode plugin on npm.

See [`docs/AI_IMPLEMENTATION_BRIEF.md`](docs/AI_IMPLEMENTATION_BRIEF.md) for the complete implementation brief.
