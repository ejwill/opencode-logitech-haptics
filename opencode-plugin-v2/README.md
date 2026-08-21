# OpenCode Logitech Haptics v2

This is a separate OpenCode v2 beta adapter for OpenCode Companion. It exports `Plugin.define` with the ID `opencode.companion`, validates localhost bridge options using the shared core, and cleans up its public event-stream task on reload/shutdown.

Install it from an npm registry with the OpenCode v2 CLI. You can configure it in a shared JSONC file or with inline OpenCode plugin options.

Copy [`config.example.jsonc`](config.example.jsonc) to `~/.config/opencode/opencode-companion.jsonc` for a global setup, or to the project root for project-specific behavior:

```bash
cp opencode-plugin-v2/config.example.jsonc ~/.config/opencode/opencode-companion.jsonc
```

The adapter searches for configuration in this order: `OPENCODE_LOGITECH_HAPTICS_CONFIG`, the current project directory, then `~/.config/opencode/`. If inline plugin options are present, they override values from the file. The file is not an OpenCode plugin registration file; the plugin still needs to be installed and loaded separately.

Inline options remain supported:

```jsonc
{
  "plugin": [{
    "package": "opencode-companion-v2@0.1.0",
    "options": {
      "endpoint": "http://127.0.0.1:17844/haptic",
      "notifications": {
        "turnStarted": false,
        "completion": true,
        "question": true,
        "error": true,
        "planReady": true
      },
      "logEventTypes": false
    }
  }]
}
```

`notifications` is the public configuration surface: it controls which user-facing haptic notifications are enabled. The adapter internally maps the current beta event stream to those notification roles. For runtime-specific overrides, use the advanced `advanced.eventTypes` object; this is intended for diagnostics and future OpenCode runtime variants, not normal configuration. The documented `execute.before` tool hook forwards `question` and `plan_exit` when those public tool names are emitted.

Set `logEventTypes` to `true` temporarily to print only observed event type names while testing a pinned runtime. The adapter does not log event payloads. If a runtime-specific override is needed, add only confirmed mappings under `advanced.eventTypes`, then set `logEventTypes` back to `false`.
