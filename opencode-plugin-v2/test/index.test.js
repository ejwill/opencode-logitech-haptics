import assert from "node:assert/strict"
import { describe, it } from "node:test"
import plugin, { createV2Plugin, notificationFromV2Event, PLUGIN_ID } from "../src/index.js"

describe("OpenCode v2 adapter", () => {
  it("exports the documented Plugin.define shape", () => {
    assert.equal(plugin.id, PLUGIN_ID)
    assert.equal(typeof plugin.setup, "function")
  })

  it("maps only explicitly configured event-stream types", () => {
    assert.deepEqual(notificationFromV2Event({ type: "session.idle" }, { "session.idle": "complete" }), { event: "complete", message: "OpenCode v2 event: session.idle" })
    assert.equal(notificationFromV2Event({ type: "session.idle" }, { "session.idle": "not-supported" }), undefined)
    assert.equal(notificationFromV2Event({ type: "unknown" }, {}), undefined)
  })

  it("routes configured events and runtime tools, then awaits cleanup", async () => {
    const sent = []
    let release
    const fakePlugin = { define: (definition) => definition }
    const definition = createV2Plugin(fakePlugin, { fetchImpl: async (_url, request) => {
      sent.push(JSON.parse(request.body))
      return { ok: true, status: 202 }
    } })
    const hooks = []
    let subscriptionOptions
    const ctx = {
      options: { suppressDuplicatesMs: 0, eventTypes: { "session.idle": "complete" } },
      tool: { hook: async (name, callback) => hooks.push({ name, callback }) },
      event: { subscribe: async function* (options) { subscriptionOptions = options; yield { type: "session.idle" }; await new Promise((resolve) => { release = resolve }) } },
    }
    const cleanup = await definition.setup(ctx)
    await new Promise((resolve) => setImmediate(resolve))
    await hooks[0].callback({ tool: "question" })
    release()
    await cleanup()
    assert.equal(hooks[0].name, "execute.before")
    assert.ok(subscriptionOptions.signal.aborted)
    assert.deepEqual(sent.map((payload) => payload.event), ["complete", "question"])
  })
})
