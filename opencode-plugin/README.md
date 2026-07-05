# OpenCode Plugin

This plugin listens for OpenCode notification-style events and POSTs normalized events to the local Logitech haptics bridge.

Default endpoint:

```text
http://127.0.0.1:17844/haptic
```

## Events

- `session.idle` → `complete`
- `session.error` → `error`
- `permission.asked` / `permission.ask` → `permission`
- `tool.execute.before` with `question` → `question`
- `tool.execute.before` with `plan_exit` → `plan_exit`

## Config

See [`opencode-logitech-haptics.jsonc`](opencode-logitech-haptics.jsonc).

The OpenCode layer controls **when to notify**. It should not contain Logitech waveform details.
