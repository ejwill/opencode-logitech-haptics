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

```json
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

Config can be loaded from:

- `opencode-logitech-haptics.jsonc` (default)
- `opencode-logitech-haptics.json` (default)
- `OPENCODE_LOGITECH_HAPTICS_CONFIG` env var pointing to a JSON file
- `LOGITECH_HAPTICS_URL` env var overrides the endpoint

The OpenCode layer controls **when to notify**. It should not contain Logitech waveform details.

## Live hardware test

After the Logitech plugin is installed and listening on `127.0.0.1:17844`, run OpenCode with the committed live-test config:

```bash
OPENCODE_LOGITECH_HAPTICS_CONFIG="$PWD/opencode-plugin/examples/live-test-config.json" opencode
```

Expected request path:

```text
OpenCode event → this plugin → POST http://127.0.0.1:17844/haptic → Logitech plugin → haptic event
```

Useful events to trigger manually:

| OpenCode flow | Bridge event |
| --- | --- |
| Session becomes idle/completes | `complete` |
| Permission prompt appears | `permission` |
| Session errors | `error` |
| Tool asks a question | `question` |
| Plan exits | `plan_exit` |

If curl tests work but OpenCode does not trigger haptics, check:

- `OPENCODE_LOGITECH_HAPTICS_CONFIG` points to the committed JSON file.
- The plugin is loaded by OpenCode.
- The event is enabled in the config.
- `suppressDuplicatesMs` is not filtering repeat events.
