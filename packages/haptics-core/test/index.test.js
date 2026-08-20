import assert from "node:assert/strict"
import { describe, it } from "node:test"
import { createNotifier, DEFAULT_CONFIG, normalizeConfig, stripJsonComments } from "../src/index.js"

describe("normalizeConfig", () => {
  it("keeps the bridge on loopback and ignores unsupported event names", () => {
    const { config, diagnostics } = normalizeConfig({ endpoint: "https://example.test/haptic", events: { complete: false, surprise: true } })
    assert.equal(config.endpoint, DEFAULT_CONFIG.endpoint)
    assert.equal(config.events.complete, false)
    assert.match(diagnostics.join("\n"), /loopback/)
    assert.match(diagnostics.join("\n"), /unsupported/)
  })

  it("strips JSONC comments without changing URL-like strings", () => {
    assert.equal(stripJsonComments('{ "endpoint": "http://127.0.0.1/haptic" } // note'), '{ "endpoint": "http://127.0.0.1/haptic" } \n')
  })
})

describe("createNotifier", () => {
  it("emits the stable bridge payload and sends only after eligibility", async () => {
    const calls = []
    const notifier = createNotifier({ config: { suppressDuplicatesMs: 10 }, now: () => 1234, fetchImpl: async (url, request) => {
      calls.push({ url, payload: JSON.parse(request.body) })
      return { ok: true, status: 202 }
    } })
    const result = await notifier.notify("complete", { message: "done", directory: "/project" })
    assert.equal(result.sent, true)
    assert.deepEqual(calls[0], { url: DEFAULT_CONFIG.endpoint, payload: { source: "opencode", event: "complete", message: "done", directory: "/project", time: new Date(1234).toISOString() } })
  })

  it("does not consume duplicate suppression on failed delivery", async () => {
    let attempts = 0
    const notifier = createNotifier({ config: { suppressDuplicatesMs: 1000 }, now: () => 1, fetchImpl: async () => {
      attempts += 1
      throw new Error("offline")
    } })
    await notifier.notify("error", { message: "first" })
    await notifier.notify("error", { message: "retry" })
    assert.equal(attempts, 2)
  })
})
