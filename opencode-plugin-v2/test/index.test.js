import assert from "node:assert/strict"
import { describe, it } from "node:test"
import plugin, { createV2Plugin, notificationFromV2Event, observedV2EventType, PLUGIN_ID } from "../src/index.js"
import { loadV2Config, resolveV2Options } from "../src/config.js"

describe("OpenCode v2 adapter", () => {
  it("loads the shared JSONC config and keeps inline options as overrides", () => {
    const files = new Map([["/config/opencode-companion.jsonc", '{ "endpoint": "http://localhost:17844/haptic", "advanced": { "eventTypes": { "session.execution.succeeded": "complete" } }, "logEventTypes": true }']])
    const loaded = loadV2Config({ cwd: "/config", home: "/home/user", exists: (path) => files.has(path), readFile: (path) => files.get(path) })
    assert.equal(loaded.path, "/config/opencode-companion.jsonc")
    assert.equal(loaded.logEventTypes, true)
    assert.deepEqual(loaded.eventTypes, { "session.execution.succeeded": "complete" })

    const resolved = resolveV2Options({ advanced: { eventTypes: { "form.created:question": "question" } }, logEventTypes: false }, {
      cwd: "/config",
      home: "/home/user",
      exists: (path) => files.has(path),
      readFile: (path) => files.get(path),
    })
    assert.equal(resolved.endpoint, "http://localhost:17844/haptic")
    assert.deepEqual(resolved.eventTypes, {
      "session.execution.started": "session_started",
      "session.execution.succeeded": "complete",
      "session.execution.interrupted": "error",
      "form.created:question": "question",
    })
    assert.equal(resolved.logEventTypes, false)
  })

  it("exports the documented Plugin.define shape", () => {
    assert.equal(plugin.id, PLUGIN_ID)
    assert.equal(typeof plugin.setup, "function")
  })

  it("maps only explicitly configured event-stream types", () => {
    assert.deepEqual(notificationFromV2Event({ type: "session.idle" }, { "session.idle": "complete" }), { event: "complete", message: "OpenCode v2 event: session.idle" })
    assert.deepEqual(
      notificationFromV2Event(
        { type: "form.created", properties: { form: { metadata: { kind: "question" } } } },
        { "form.created:question": "question" },
      ),
      { event: "question", message: "OpenCode v2 event: form.created" },
    )
    assert.deepEqual(
      notificationFromV2Event(
        { type: "form.created", data: { form: { metadata: { kind: "question" } } } },
        { "form.created:question": "question" },
      ),
      { event: "question", message: "OpenCode v2 event: form.created" },
    )
    assert.equal(
      notificationFromV2Event(
        { type: "form.created", properties: { form: { metadata: { kind: "permission" } } } },
        { "form.created:question": "question" },
      ),
      undefined,
    )
    assert.equal(notificationFromV2Event({ type: "session.idle" }, { "session.idle": "not-supported" }), undefined)
    assert.equal(notificationFromV2Event({ type: "unknown" }, {}), undefined)
  })

  it("discovers only non-empty event type names", () => {
    const observed = []
    observedV2EventType({ type: "session.idle" }, (type) => observed.push(type))
    observedV2EventType({ type: "" }, (type) => observed.push(type))
    observedV2EventType({}, (type) => observed.push(type))
    assert.deepEqual(observed, ["session.idle"])
  })

  it("routes configured events and runtime tools, then awaits cleanup", async () => {
    const sent = []
    let release
    const fakePlugin = { define: (definition) => definition }
    const hooks = []
    let subscriptionOptions
    const ctx = {
      options: { suppressDuplicatesMs: 0, advanced: { eventTypes: { "session.idle": "complete" } }, logEventTypes: true },
      tool: { hook: async (name, callback) => hooks.push({ name, callback }) },
      event: { subscribe: async function* (options) { subscriptionOptions = options; yield { type: "session.idle" }; await new Promise((resolve) => { release = resolve }) } },
    }
    const observed = []
    const cleanup = await createV2Plugin(fakePlugin, {
      fetchImpl: async (_url, request) => {
        sent.push(JSON.parse(request.body))
        return { ok: true, status: 202 }
      },
      logEventType: (type) => observed.push(type),
    }).setup(ctx)
    await new Promise((resolve) => setImmediate(resolve))
    await hooks[0].callback({ tool: "question" })
    release()
    await cleanup()
    assert.equal(hooks[0].name, "execute.before")
    assert.ok(subscriptionOptions.signal.aborted)
    assert.deepEqual(observed, ["session.idle"])
    assert.deepEqual(sent.map((payload) => payload.event), ["complete", "question"])
  })
})
