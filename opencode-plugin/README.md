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
  "intensity": "normal",
  "notifications": {
    "turnStarted": false,
    "completion": true,
    "permission": true,
    "question": true,
    "error": true,
    "planReady": true,
    "userMessage": false,
    "subagentCompletion": false
  },
  "minDurationSeconds": 0,
  "suppressDuplicatesMs": 750
}
```

Config can be loaded from:

- `opencode-logi-companion.jsonc` in the project directory (default)
- `opencode-logi-companion.json` in the project directory (default)
- the same filenames under `~/.config/opencode/` when the project has no config
- `OPENCODE_LOGI_COMPANION_CONFIG` env var pointing to a JSON file
- `OPENCODE_LOGI_COMPANION_URL` env var overrides the endpoint

A leftover `opencode-companion.jsonc` from earlier releases is reported as a diagnostic; rename it to `opencode-logi-companion.jsonc` — its values are not loaded.

Only `http://127.0.0.1`, `http://localhost`, and `http://[::1]` endpoints are accepted. Invalid configuration falls back safely to defaults; adapter consumers can inspect `loadConfigResult().diagnostics` for the reason.

The OpenCode layer controls **when to notify** and may request a validated waveform through the `waveforms` configuration object. Use `intensity` with `subtle`, `normal`, or `strong` to select a curated waveform profile; explicit entries in `waveforms` override that profile. The Logitech package still owns the safe device-specific fallback mapping.

## Live hardware test

After the Logitech plugin is installed and listening on `127.0.0.1:17844`, run OpenCode with the committed live-test config:

```bash
OPENCODE_LOGI_COMPANION_CONFIG="$PWD/opencode-plugin/examples/live-test-config.json" opencode
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

- `OPENCODE_LOGI_COMPANION_CONFIG` points to the committed JSON file.
- The plugin is loaded by OpenCode.
- The event is enabled in the config.
- `suppressDuplicatesMs` is not filtering repeat events.
