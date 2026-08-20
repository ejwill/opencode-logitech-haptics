# OpenCode v2 Mapping Contract

Pinned and tested OpenCode v2 contract: `@opencode-ai/cli@0.0.0-beta-17498` with `@opencode-ai/plugin@0.0.0-beta-17498`.

The v2 API documents `Plugin.define`, `ctx.event.subscribe()`, `ctx.tool.hook("execute.before", callback)`, options through `ctx.options`, and asynchronous cleanup. Event-stream event names are not documented as a stable notification taxonomy. Therefore this package has no implicit stream-event mapping: users must provide `options.eventTypes` after observing their pinned v2 installation. The adapter only emits a normalized event when the configured value is one of the shared core's supported events.

The public runtime-hook sources used by the adapter are:

| V2 source | Mapping | Status |
| --- | --- | --- |
| `ctx.event.subscribe()` | Explicit `options.eventTypes` entry | Configured, evidence required per installation |
| `ctx.tool.hook("execute.before")` + `question` | `question` | Implemented; verify on a live v2 installation |
| `ctx.tool.hook("execute.before")` + `plan_exit` | `plan_exit` | Implemented; verify on a live v2 installation |

No default `complete`, `error`, or `permission` mapping is shipped until a v2 event example is captured from a real pinned installation.
