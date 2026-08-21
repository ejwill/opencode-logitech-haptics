# OpenCode v2 Mapping Contract

Pinned and tested OpenCode v2 contract: `@opencode-ai/cli@0.0.0-beta-17498` with `@opencode-ai/plugin@0.0.0-beta-17498`. Live event discovery was also performed against the running `opencode2` beta service on 2026-08-21.

The v2 API documents `Plugin.define`, `ctx.event.subscribe()`, `ctx.tool.hook("execute.before", callback)`, options through `ctx.options`, and asynchronous cleanup. Event-stream event names are not documented as a stable notification taxonomy, so the adapter keeps the beta-specific mapping internal and exposes user-facing notification preferences instead. Runtime-specific overrides are available under `advanced.eventTypes` for diagnostics and future variants.

The public runtime-hook sources used by the adapter are:

| V2 source | Mapping | Status |
| --- | --- | --- |
| `ctx.event.subscribe()` | Adapter-owned beta mapping | Implemented for the observed runtime; advanced override available |
| `ctx.tool.hook("execute.before")` + `question` | `question` | Implemented; verify on a live v2 installation |
| `ctx.tool.hook("execute.before")` + `plan_exit` | `plan_exit` | Implemented; verify on a live v2 installation |

The following mappings are owned by the adapter for the current live beta runtime:

```text
turnStarted  <- session.execution.started
completion   <- session.execution.succeeded
error        <- session.execution.interrupted
question     <- form.created where metadata.kind = question
```

`form.created:question` matches `form.created` only when the event payload contains `form.metadata.kind: "question"`. A generic `form.created` mapping remains possible, but is intentionally not recommended because the same event family may represent other forms, including permissions. `session.execution.failed` remains unverified and should be enabled only after capturing a real failure payload.

## User configuration

The v2 adapter accepts the same `opencode-companion.jsonc` file as the legacy adapter. It searches in this order:

1. `OPENCODE_LOGITECH_HAPTICS_CONFIG`
2. `opencode-companion.jsonc` in the current project directory
3. `~/.config/opencode/opencode-companion.jsonc`

Inline plugin options take precedence over file values. The file controls shared bridge settings (`enabled`, `endpoint`, `notifications`, and duplicate suppression). The v2 adapter owns the current beta event mapping internally. Runtime-specific overrides live under `advanced.eventTypes`, while `logEventTypes` remains a diagnostic option. See [`opencode-plugin-v2/config.example.jsonc`](../opencode-plugin-v2/config.example.jsonc) for a complete starting point.

## Event discovery

Set `logEventTypes: true` temporarily in the plugin options to log only the `type` field of each event received from `ctx.event.subscribe()`. This is an opt-in diagnostic mode: event payloads are not logged. If a future runtime needs a custom mapping, add only confirmed mappings under `advanced.eventTypes`, then disable the diagnostic option.
