import { describe, it } from "node:test"
import assert from "node:assert/strict"
import { mkdtemp, mkdir, rm, writeFile } from "node:fs/promises"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { createLogitechHapticsPlugin, DEFAULT_CONFIG, loadConfig, loadConfigResult } from "../src/index.js"

function makeFetch({ status = 202, fail = false } = {}) {
  const calls = []
  const fetch = async (url, request) => {
    calls.push({ url, request, payload: JSON.parse(request.body) })
    if (fail) throw new Error("bridge unavailable")
    return { ok: status >= 200 && status < 300, status }
  }
  fetch.calls = calls
  return fetch
}

async function makePlugin(config = {}, options = {}) {
  const fetch = options.fetch ?? makeFetch()
  const plugin = createLogitechHapticsPlugin({ ...DEFAULT_CONFIG, suppressDuplicatesMs: 0, ...config }, { fetchImpl: fetch, now: options.now })
  const hooks = await plugin({ directory: "/tmp/project", worktree: "/tmp/project", serverUrl: options.serverUrl })
  return { fetch, hooks }
}

describe("loadConfig", () => {
  it("returns default config when no config source exists", () => {
    const config = loadConfig({ env: {}, cwd: "/does-not-exist", home: "/does-not-exist" })
    assert.equal(config.enabled, true)
    assert.equal(config.endpoint, "http://127.0.0.1:17844/haptic")
  })

  it("rejects non-loopback endpoints without failing initialization", () => {
    const result = loadConfigResult({ env: { OPENCODE_LOGI_COMPANION_URL: "http://10.0.0.1:19876/haptic" }, cwd: "/does-not-exist", home: "/does-not-exist" })
    assert.equal(result.config.endpoint, "http://127.0.0.1:17844/haptic")
    assert.match(result.diagnostics.join("\n"), /loopback/)
  })

  it("warns without loading when only a legacy config file exists", async () => {
    const dir = await mkdtemp(join(tmpdir(), "opencode-logi-companion-"))
    await writeFile(join(dir, "opencode-companion.jsonc"), "{ \"events\": { \"complete\": false } }")
    try {
      const result = loadConfigResult({ env: {}, cwd: dir, home: "/does-not-exist" })
      assert.equal(result.path, undefined)
      assert.equal(result.config.events.complete, true)
      assert.match(result.diagnostics.join("\n"), /rename it to opencode-logi-companion\.jsonc/)
    } finally { await rm(dir, { recursive: true, force: true }) }
  })

  it("loads JSONC from the documented default path", async () => {
    const dir = await mkdtemp(join(tmpdir(), "opencode-logi-companion-"))
    await writeFile(join(dir, "opencode-logi-companion.jsonc"), "// local bridge\n{ \"events\": { \"complete\": false } }")
    try {
      const config = loadConfig({ env: {}, cwd: dir, home: "/does-not-exist" })
      assert.equal(config.events.complete, false)
      assert.equal(config.events.error, true)
    } finally { await rm(dir, { recursive: true, force: true }) }
  })

  it("falls back to the global config directory when the project has none", async () => {
    const homeDir = await mkdtemp(join(tmpdir(), "opencode-logi-companion-home-"))
    await mkdir(join(homeDir, ".config", "opencode"), { recursive: true })
    await writeFile(join(homeDir, ".config", "opencode", "opencode-logi-companion.jsonc"), "// global bridge\n{ \"events\": { \"error\": false } }")
    try {
      const result = loadConfigResult({ env: {}, cwd: "/does-not-exist", home: homeDir })
      assert.equal(result.path, join(homeDir, ".config", "opencode", "opencode-logi-companion.jsonc"))
      assert.equal(result.config.events.error, false)
      assert.equal(result.config.events.complete, true)
    } finally { await rm(homeDir, { recursive: true, force: true }) }
  })

  it("reports malformed configuration while preserving defaults", () => {
    const result = loadConfigResult({ env: { OPENCODE_LOGI_COMPANION_CONFIG: "/bad.json" }, readFile: () => "{ bad", exists: () => true })
    assert.equal(result.config.endpoint, DEFAULT_CONFIG.endpoint)
    assert.match(result.diagnostics.join("\n"), /Could not read configuration/)
  })
})

describe("createLogitechHapticsPlugin", () => {
  it("reports the OpenCode server URL when the host provides it", async () => {
    const { fetch } = await makePlugin({}, { serverUrl: "http://127.0.0.1:49374" })
    assert.equal(fetch.calls[0].payload.type, "server_info")
    assert.equal(fetch.calls[0].payload.serverUrl, "http://127.0.0.1:49374")
  })

  it("fires complete event on session.idle", async () => {
    const { fetch, hooks } = await makePlugin()
    await hooks.event({ event: { type: "session.idle", properties: { session: { time: { created: new Date(Date.now() - 10_000).toISOString() } } } } })
    assert.equal(fetch.calls.length, 1)
    const payload = fetch.calls[0].payload
    assert.equal(payload.event, "complete")
    assert.equal(payload.source, "opencode")
    assert.equal(payload.message, "OpenCode session completed")
    assert.equal(payload.directory, "/tmp/project")
    assert.ok(payload.durationSeconds >= 0)
  })

  it("routes error, permission, and tool events", async () => {
    const { fetch, hooks } = await makePlugin()
    await hooks.event({ event: { type: "session.error" } })
    await hooks["permission.ask"]({ type: "shell" })
    await hooks["tool.execute.before"]({ tool: "question" })
    await hooks["tool.execute.before"]({ tool: "plan_exit" })
    assert.deepEqual(fetch.calls.map((call) => call.payload.event), ["error", "permission", "question", "plan_exit"])
    assert.equal(fetch.calls[1].payload.permissionType, "shell")
  })

  it("filters disabled events", async () => {
    const { fetch, hooks } = await makePlugin({ events: { ...DEFAULT_CONFIG.events, complete: false } })
    await hooks.event({ event: { type: "session.idle" } })
    assert.equal(fetch.calls.length, 0)
  })

  it("does not suppress a retry after a failed send", async () => {
    const fetch = makeFetch({ fail: true })
    const { hooks } = await makePlugin({ suppressDuplicatesMs: 1_000 }, { fetch, now: () => 1000 })
    await hooks.event({ event: { type: "session.error" } })
    await hooks.event({ event: { type: "session.error" } })
    assert.equal(fetch.calls.length, 2)
  })

  it("suppresses a successful duplicate", async () => {
    const { fetch, hooks } = await makePlugin({ suppressDuplicatesMs: 1_000 }, { now: () => 1000 })
    await hooks.event({ event: { type: "session.error" } })
    await hooks.event({ event: { type: "session.error" } })
    assert.equal(fetch.calls.length, 1)
  })
})
