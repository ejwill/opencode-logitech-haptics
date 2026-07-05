import { describe, it } from "node:test"
import assert from "node:assert/strict"
import { mkdtemp, rm, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { createLogitechHapticsPlugin, DEFAULT_CONFIG, loadConfig } from "../src/index.js"

function makeShell() {
  const calls = []
  const shell = (strings, ...values) => {
    calls.push({ strings: [...strings], values })
    return { quiet: async () => ({ exitCode: 0 }) }
  }
  shell.calls = calls
  shell.payloads = () => calls.map((call) => JSON.parse(call.values[1]))
  return shell
}

async function makePlugin(config = {}) {
  const shell = makeShell()
  const plugin = createLogitechHapticsPlugin({ ...DEFAULT_CONFIG, suppressDuplicatesMs: 0, ...config })
  const hooks = await plugin({ $, directory: "/tmp/project", worktree: "/tmp/project" })
  return { shell, hooks }

  function $(strings, ...values) {
    return shell(strings, ...values)
  }
}

describe("loadConfig", () => {
  it("returns default config when no env vars are set", () => {
    const config = loadConfig({})
    assert.equal(config.enabled, true)
    assert.equal(config.endpoint, "http://127.0.0.1:17844/haptic")
    assert.equal(config.suppressDuplicatesMs, 750)
  })

  it("overrides endpoint from env", () => {
    const config = loadConfig({ LOGITECH_HAPTICS_URL: "http://10.0.0.1:19876/haptic" })
    assert.equal(config.endpoint, "http://10.0.0.1:19876/haptic")
  })

  it("overrides config from env path", async () => {
    const dir = await mkdtemp(join(tmpdir(), "opencode-logitech-haptics-"))
    const path = join(dir, "config.json")
    await writeFile(path, JSON.stringify({ endpoint: "http://custom:9999/haptic", events: { complete: false } }))

    try {
      const config = loadConfig({ OPENCODE_LOGITECH_HAPTICS_CONFIG: path })
      assert.equal(config.endpoint, "http://custom:9999/haptic")
      assert.equal(config.events.complete, false)
      assert.equal(config.events.error, true)
    } finally {
      await rm(dir, { recursive: true, force: true })
    }
  })
})

describe("createLogitechHapticsPlugin", () => {
  it("fires complete event on session.idle", async () => {
    const { shell, hooks } = await makePlugin()
    await hooks.event({ event: { type: "session.idle", properties: { session: { time: { created: new Date(Date.now() - 10_000).toISOString() } } } } })

    assert.equal(shell.calls.length, 1)
    const payload = shell.payloads()[0]
    assert.equal(payload.event, "complete")
    assert.equal(payload.source, "opencode")
    assert.equal(payload.message, "OpenCode session completed")
    assert.equal(payload.directory, "/tmp/project")
    assert.ok(payload.durationSeconds >= 0)
  })

  it("fires error event on session.error", async () => {
    const { shell, hooks } = await makePlugin()
    await hooks.event({ event: { type: "session.error" } })

    assert.equal(shell.calls.length, 1)
    assert.equal(shell.payloads()[0].event, "error")
  })

  it("fires permission event on permission.ask", async () => {
    const { shell, hooks } = await makePlugin()
    await hooks["permission.ask"]({ type: "shell" })

    assert.equal(shell.calls.length, 1)
    const payload = shell.payloads()[0]
    assert.equal(payload.event, "permission")
    assert.equal(payload.permissionType, "shell")
  })

  it("fires tool routing events", async () => {
    const { shell, hooks } = await makePlugin()
    await hooks["tool.execute.before"]({ tool: "question" })
    await hooks["tool.execute.before"]({ tool: "plan_exit" })

    assert.deepEqual(shell.payloads().map((payload) => payload.event), ["question", "plan_exit"])
  })

  it("filters disabled events", async () => {
    const { shell, hooks } = await makePlugin({ events: { ...DEFAULT_CONFIG.events, complete: false } })
    await hooks.event({ event: { type: "session.idle" } })

    assert.equal(shell.calls.length, 0)
  })

  it("respects suppressDuplicatesMs", async () => {
    const { shell, hooks } = await makePlugin({ suppressDuplicatesMs: 1_000 })
    await hooks.event({ event: { type: "session.error" } })
    await hooks.event({ event: { type: "session.error" } })

    assert.equal(shell.calls.length, 1)
  })

  it("handles disabled plugin", async () => {
    const { shell, hooks } = await makePlugin({ enabled: false })
    await hooks.event({ event: { type: "session.error" } })

    assert.equal(shell.calls.length, 0)
  })
})
