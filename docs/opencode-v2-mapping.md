# OpenCode v2 Mapping Contract

Pinned and tested OpenCode v2 contract: `@opencode-ai/cli@0.0.0-beta-17498` with `@opencode-ai/plugin@0.0.0-beta-17498`; the plugin dependency was bumped to `@opencode-ai/plugin@0.0.0-beta-18138` on 2026-08-24 with the adapter test suite green (`Plugin.define`, `ctx.event.subscribe`, and the `execute.before` hook shapes are unchanged between the two betas). Live event discovery was performed against the running `opencode2` beta service on 2026-08-21; re-run `logEventTypes` discovery when upgrading the pinned runtime.

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
permission   <- permission.v2.asked
question     <- form.created where metadata.kind = question
```

`form.created:question` matches `form.created` only when the event payload contains `form.metadata.kind: "question"`. A generic `form.created` mapping remains possible, but is intentionally not recommended because the same event family may represent other forms, including permissions. `session.execution.failed` remains unverified and should be enabled only after capturing a real failure payload.

## User configuration

The v2 adapter accepts the same `opencode-logi-companion.jsonc` file as the legacy adapter. It searches in this order:

1. `OPENCODE_LOGI_COMPANION_CONFIG`
2. `opencode-logi-companion.jsonc` in the current project directory
3. `~/.config/opencode/opencode-logi-companion.jsonc`

Inline plugin options take precedence over file values. The file controls shared bridge settings (`enabled`, `endpoint`, `notifications`, and duplicate suppression). The v2 adapter owns the current beta event mapping internally. Runtime-specific overrides live under `advanced.eventTypes`, while `logEventTypes` remains a diagnostic option. See [`opencode-plugin-v2/config.example.jsonc`](../opencode-plugin-v2/config.example.jsonc) for a complete starting point.

## Event discovery

Set `logEventTypes: true` temporarily in the plugin options to log only the `type` field of each event received from `ctx.event.subscribe()`. This is an opt-in diagnostic mode: event payloads are not logged. If a future runtime needs a custom mapping, add only confirmed mappings under `advanced.eventTypes`, then disable the diagnostic option.

## Controls compatibility with opencode2 (spike, 2026-08-24)

The C# `OpenCodeControlClient` speaks the V1 server contract. The V2 HTTP API (`/v2/docs/api`, 2026-08-24) shows these equivalents:

| Control action | V1 endpoint | V2 candidate | Assessment |
| --- | --- | --- | --- |
| Stop Current Turn | `POST /session/{id}/abort` | `POST /api/session/{id}/interrupt` | Direct equivalent; needs an active-session lookup (`GET /api/session/active`) |
| Switch Agent | `POST /tui/execute-command {agent_cycle}` | `GET /api/agent` + `POST /api/session/{id}/agent` | No cycle primitive; client lists agents and cycles locally, then switches explicitly |
| Switch Thinking Level | `POST /tui/execute-command {variant_cycle}` | `GET /api/model` + `POST /api/session/{id}/model` | Variant cycling must be derived from the model catalog client-side |
| Open Session Selector | `POST /tui/open-sessions` | none | **No TUI routes exist in V2.** Server cannot open client UI |
| Open Model Selector | `POST /tui/open-models` | none | Same gap as above |

Key contract differences: all V2 routes are namespaced under `/api/`; sessions are identified by `Session.ID`; agent and model switches are explicit (list-then-pick) rather than server-side cycles.

Open questions before implementing dual-API support in the C# client:

1. Confirm `POST /api/session/{id}/interrupt` body/response shapes from the OpenAPI document (`/v2/openapi.json`).
2. Determine how model variants are expressed in the `model` switch payload (config docs join variant as `provider/model#variant`; confirm the API accepts the same form).
3. Decide the fate of the two selector controls under V2: hide them when a V2 runtime is detected, or replace them with a device-side list picker fed by `GET /api/session/active` / `GET /api/model`.
4. Probe a live `opencode2` service to verify each route (pending; no service was running during this spike).
