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

  it("translates user-facing notification preferences into bridge events", () => {
    const { config, diagnostics } = normalizeConfig({ notifications: { completion: false, planReady: false } })
    assert.equal(config.notifications.completion, false)
    assert.equal(config.notifications.planReady, false)
    assert.equal(config.events.complete, false)
    assert.equal(config.events.plan_exit, false)
    assert.deepEqual(diagnostics, [])
  })

  it("strips JSONC comments without changing URL-like strings", () => {
    assert.equal(stripJsonComments('{ "endpoint": "http://127.0.0.1/haptic" } // note'), '{ "endpoint": "http://127.0.0.1/haptic" } \n')
  })

  it("validates waveform overrides", () => {
    const { config, diagnostics } = normalizeConfig({ waveforms: { complete: "happy_alert", error: "unknown", surprise: "ringing" } })
    assert.equal(config.waveforms.complete, "happy_alert")
    assert.equal(config.waveforms.error, DEFAULT_CONFIG.waveforms.error)
    assert.match(diagnostics.join("\n"), /waveforms\.error must be a supported waveform/)
    assert.match(diagnostics.join("\n"), /waveforms\.surprise is unsupported/)
  })

  it("selects an intensity profile before applying explicit waveform overrides", () => {
    const { config, diagnostics } = normalizeConfig({ intensity: "subtle", waveforms: { error: "angry_alert" } })
    assert.equal(config.intensity, "subtle")
    assert.equal(config.waveforms.complete, "damp_state_change")
    assert.equal(config.waveforms.error, "angry_alert")
    assert.deepEqual(diagnostics, [])
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
    assert.deepEqual(calls[0], { url: DEFAULT_CONFIG.endpoint, payload: { source: "opencode", event: "complete", waveform: "completed", message: "done", directory: "/project", time: new Date(1234).toISOString() } })
  })

  it("sends a configured waveform", async () => {
    let payload
    const notifier = createNotifier({ config: { waveforms: { complete: "wave" }, suppressDuplicatesMs: 0 }, fetchImpl: async (_url, request) => {
      payload = JSON.parse(request.body)
      return { ok: true, status: 202 }
    } })
    await notifier.notify("complete", { message: "done" })
    assert.equal(payload.waveform, "wave")
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

  it("announces the OpenCode server URL without creating a haptic event", async () => {
    const calls = []
    const notifier = createNotifier({ fetchImpl: async (url, request) => {
      calls.push({ url, payload: JSON.parse(request.body) })
      return { ok: true, status: 202 }
    }, now: () => 1234 })
    const result = await notifier.announceServer("http://127.0.0.1:49374", { directory: "/project" })
    assert.equal(result.sent, true)
    assert.deepEqual(calls[0].payload, { source: "opencode", type: "server_info", serverUrl: "http://127.0.0.1:49374", directory: "/project", time: new Date(1234).toISOString() })
  })
})
