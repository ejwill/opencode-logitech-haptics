# OpenCode Logitech Haptics — Project Idea

## Summary

Build a first-party integration that turns OpenCode agent events into Logitech MX Master 4 haptic feedback. The project ships as a small OpenCode JavaScript plugin plus a Logitech Actions SDK plugin that listens locally and raises haptic events inside Logi Plugin Service.

## Problem

Long-running AI coding sessions often finish, block, or request permission while the user is looking elsewhere. Existing notifications are easy to miss, especially when working across terminals, browser tabs, and editors.

A subtle mouse haptic signal gives immediate ambient feedback without adding another noisy notification channel.

## Core Idea

Use OpenCode's plugin/event system to detect useful agent lifecycle events, then forward them to a local Logitech plugin over HTTP:

```text
OpenCode event
  -> OpenCode JS plugin
  -> POST http://127.0.0.1:17844/haptic
  -> Logitech Actions SDK C# plugin
  -> PluginEvents.RaiseEvent(...)
  -> Logitech haptic mapping
  -> MX Master 4 haptic feedback
```

## Goals

- Provide tactile feedback for important OpenCode state changes.
- Keep the OpenCode side tiny, portable, and easy to install.
- Keep the Logitech side native to the Actions SDK so it works with Logi Options+ / Logi Plugin Service.
- Make haptic mappings configurable without recompiling code.
- Avoid third-party notification plugins for core behavior.

## Non-Goals

- Do not require a cloud service.
- Do not send code, prompts, or agent output off-machine.
- Do not replace desktop notifications entirely.
- Do not depend on undocumented Logitech internals if the Actions SDK can handle it.

## Initial Event Mapping

| OpenCode Event | Meaning | Haptic Event | Suggested Feel |
|---|---|---|---|
| `session.idle` | Agent finished work | `opencodeComplete` | `completed` |
| permission requested | User input needed | `opencodePermission` | `knock` |
| error/failure | Agent hit an error | `opencodeError` | `angry_alert` |
| notification/info | Informational update | `opencodeNotify` | `ringing` |
| test/build success | Positive completion | `opencodeSuccess` | `happy_alert` |
| test/build failure | Action failed | `opencodeFailure` | `sharp_state_change` |

## Architecture

### OpenCode Plugin

Location: `opencode-plugin/`

Responsibilities:

- Register OpenCode event hooks.
- Normalize OpenCode events into simple haptic payloads.
- POST payloads to the local Logitech bridge endpoint.
- Fail silently if the Logitech bridge is unavailable.
- Support config/env overrides for endpoint and event mapping.

Example payload:

```json
{
  "event": "opencodeComplete",
  "source": "opencode",
  "reason": "session.idle",
  "timestamp": "2026-07-05T00:00:00.000Z"
}
```

### Logitech Plugin

Location: `logitech-plugin/`

Responsibilities:

- Run inside Logi Plugin Service using the Logitech Actions SDK.
- Start a localhost HTTP listener on `127.0.0.1:17844`.
- Accept `POST /haptic` requests from the OpenCode plugin.
- Validate the requested haptic event name.
- Raise SDK events with `PluginEvents.RaiseEvent(...)`.
- Define haptic mapping in `events/extra/eventMapping.yaml`.

## Install Modes

### Fast Local Mode

For development and quick testing:

1. Copy/link the OpenCode JS plugin into OpenCode's plugin config.
2. Build/install the Logitech C# plugin locally.
3. Run OpenCode and verify localhost haptic posts.

### Full Logitech Plugin Mode

For normal usage:

1. Package the Logitech Actions SDK plugin.
2. Install it through Logi Options+ / Logi Plugin Service.
3. Configure OpenCode to load the JS plugin.
4. Adjust haptic mappings from the Logitech plugin package.

## Configuration

OpenCode plugin config should allow:

```json
{
  "endpoint": "http://127.0.0.1:17844/haptic",
  "enabled": true,
  "events": {
    "session.idle": "opencodeComplete",
    "permission": "opencodePermission",
    "error": "opencodeError"
  }
}
```

Environment override:

```bash
OPENCODE_LOGITECH_HAPTICS_ENDPOINT=http://127.0.0.1:17844/haptic
```

## Security / Privacy

- Bind only to `127.0.0.1`, never `0.0.0.0`.
- Accept only known haptic event names.
- Do not include prompts, code, diffs, file paths, or model output in the payload.
- Treat the bridge as local IPC, not a public API.
- Keep the payload schema intentionally boring.

## MVP

- [ ] OpenCode plugin sends `session.idle` to local endpoint.
- [ ] Logitech plugin receives `POST /haptic`.
- [ ] Logitech plugin raises `opencodeComplete`.
- [ ] `eventMapping.yaml` maps `opencodeComplete` to a haptic waveform.
- [ ] README documents install and test flow.
- [ ] Basic tests cover event mapping and bridge failure handling.

## Stretch Ideas

- Per-project haptic profiles.
- Different haptics for success, failure, permission, and idle.
- Rate limiting / debounce for noisy event bursts.
- A tiny local diagnostics page showing received events.
- Optional desktop notification fallback.
- Support additional Logitech devices if the Actions SDK exposes compatible haptics.

## Open Questions

- Which OpenCode events are stable enough to rely on long-term?
- Does the Logitech Actions SDK require any special packaging for MX Master 4 haptics specifically?
- Can haptic mappings be updated without restarting Logi Plugin Service?
- What is the best user-facing install story for non-developers?

## Success Criteria

The project succeeds when a user can:

1. Install the OpenCode plugin.
2. Install/build the Logitech Actions SDK plugin.
3. Start an OpenCode task.
4. Feel a distinct MX Master 4 haptic when the task completes or needs attention.

No cloud service, no prompt leakage, no third-party notification dependency.
