# OpenCode Logitech Haptics v2

This is a separate OpenCode v2 beta adapter. It exports `Plugin.define` with the ID `opencode.logitech-haptics`, validates localhost bridge options using the shared core, and cleans up its public event-stream task on reload/shutdown.

Install it from an npm registry with the OpenCode v2 CLI, then configure options in `opencode.jsonc`:

```jsonc
{
  "plugins": [{
    "package": "opencode-logitech-haptics-v2@0.1.0",
    "options": {
      "endpoint": "http://127.0.0.1:17844/haptic",
      "eventTypes": { "session.idle": "complete" }
    }
  }]
}
```

`eventTypes` deliberately requires explicit event-stream mappings. Use the discovery fixture before adding a mapping, because the v2 API is beta and this adapter never assumes that a legacy event name is a v2 event contract. The documented `execute.before` tool hook forwards `question` and `plan_exit` when those public tool names are emitted.
